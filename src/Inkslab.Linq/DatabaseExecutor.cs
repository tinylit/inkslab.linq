using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Linq.Exceptions;
using Microsoft.Extensions.Logging;
using static System.Linq.Expressions.Expression;

namespace Inkslab.Linq
{
    /// <summary>
    /// 数据库执行器，T-SQL 会直接执行，不会处理和适配。
    /// </summary>
    public class DatabaseExecutor : IDatabaseExecutor
    {
        private static readonly ConcurrentDictionary<Type, MapAdaper> _adapters = new ConcurrentDictionary<Type, MapAdaper>();
        private readonly IDbConnectionPipeline _connectionPipeline;
        private readonly ILogger<DatabaseExecutor> _logger;

        private const string LinqBinary = "System.Data.Linq.Binary";

        /// <summary>
        /// 数据库链接。
        /// </summary>
        /// <param name="connectionPipeline">链接管道。</param>
        /// <param name="logger">日志。</param>
        public DatabaseExecutor(IDbConnectionPipeline connectionPipeline, ILogger<DatabaseExecutor> logger)
        {
            _connectionPipeline = connectionPipeline;
            _logger = logger;
        }

        /// <inheritdoc/>
        public int Execute(IConnection databaseStrings, CommandSql commandSql)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(commandSql.ToString());
            }

            using var dbConnection = _connectionPipeline.Get(databaseStrings);

            bool isClosedConnection = dbConnection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                dbConnection.Open();
            }

            try
            {
                using (var command = dbConnection.CreateCommand())
                {
                    command.CommandText = commandSql.Text;
                    command.CommandType = commandSql.CommandType;

                    if (commandSql.Timeout.HasValue)
                    {
                        command.CommandTimeout = commandSql.Timeout.Value;
                    }

                    foreach (var (name, value) in commandSql.Parameters)
                    {
                        LookupDb.AddParameterAuto(command, databaseStrings.Engine, name, value);
                    }

                    var result = command.ExecuteNonQuery();

                    commandSql.Callback(command);

                    return result;
                }
            }
            finally
            {
                if (isClosedConnection)
                {
                    dbConnection.Close();
                }
            }
        }

        /// <inheritdoc/>
        public async Task<int> ExecuteAsync(
            IConnection databaseStrings,
            CommandSql commandSql,
            CancellationToken cancellationToken = default
        )
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(commandSql.ToString());
            }

            await using var dbConnection = _connectionPipeline.Get(databaseStrings);

            bool isClosedConnection = dbConnection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                await dbConnection.OpenAsync(cancellationToken);
            }

            try
            {
                await using (var command = dbConnection.CreateCommand())
                {
                    command.CommandText = commandSql.Text;
                    command.CommandType = commandSql.CommandType;

                    if (commandSql.Timeout.HasValue)
                    {
                        command.CommandTimeout = commandSql.Timeout.Value;
                    }

                    foreach (var (name, value) in commandSql.Parameters)
                    {
                        LookupDb.AddParameterAuto(command, databaseStrings.Engine, name, value);
                    }

                    var result = await command.ExecuteNonQueryAsync(cancellationToken);

                    commandSql.Callback(command);

                    return result;
                }
            }
            finally
            {
                if (isClosedConnection)
                {
                    await dbConnection.CloseAsync();
                }
            }
        }

        /// <inheritdoc/>
        public List<T> Query<T>(IConnection databaseStrings, CommandSql commandSql)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(commandSql.ToString());
            }

            CommandBehavior behavior =
                CommandBehavior.SequentialAccess | CommandBehavior.SingleResult;

            using var dbConnection = _connectionPipeline.Get(databaseStrings);

            if (dbConnection.State == ConnectionState.Closed)
            {
                behavior |= CommandBehavior.CloseConnection;

                dbConnection.Open();
            }

            var results = new List<T>();

            using (var command = dbConnection.CreateCommand())
            {
                command.CommandText = commandSql.Text;
                command.CommandType = commandSql.CommandType;

                if (commandSql.Timeout.HasValue)
                {
                    command.CommandTimeout = commandSql.Timeout.Value;
                }

                foreach (var (name, value) in commandSql.Parameters)
                {
                    LookupDb.AddParameterAuto(command, databaseStrings.Engine, name, value);
                }

                using (var reader = command.ExecuteReader(behavior))
                {
                    if (reader.HasRows)
                    {
                        var adaper = _adapters.GetOrAdd(
                            reader.GetType(),
                            type => new MapAdaper(type)
                        );

                        var map = adaper.CreateMap<T>();

                        while (reader.Read())
                        {
                            if (map.IsInvalid(reader))
                            {
                                continue;
                            }

                            results.Add(map.Map(reader));
                        }
                    }
                }

                commandSql.Callback(command);
            }

            return results;
        }

        /// <inheritdoc/>
        public IDbGridReader QueryMultiple(IConnection databaseStrings, CommandSql commandSql)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(commandSql.ToString());
            }

            CommandBehavior behavior = CommandBehavior.SequentialAccess;

            var dbConnection = _connectionPipeline.Get(databaseStrings);

            bool isClosedConnection = dbConnection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                behavior |= CommandBehavior.CloseConnection;

                dbConnection.Open();
            }

            DbCommand command = null;
            DbDataReader reader = null;

            try
            {
                command = dbConnection.CreateCommand();

                command.CommandText = commandSql.Text;
                command.CommandType = commandSql.CommandType;

                if (commandSql.Timeout.HasValue)
                {
                    command.CommandTimeout = commandSql.Timeout.Value;
                }

                foreach (var (name, value) in commandSql.Parameters)
                {
                    LookupDb.AddParameterAuto(command, databaseStrings.Engine, name, value);
                }

                reader = command.ExecuteReader(behavior);

                var adaper = _adapters.GetOrAdd(
                        reader.GetType(),
                        type => new MapAdaper(type)
                    );

                return new DbGridReader(dbConnection, command, reader, commandSql, adaper);
            }
            catch
            {
                if (reader != null)
                {
                    if (!reader.IsClosed)
                    {
                        try
                        {
                            command?.Cancel();
                        }
                        catch { }
                    }

                    reader.Dispose();
                }

                command?.Dispose();

                if (isClosedConnection)
                {
                    dbConnection.Close();
                }

                dbConnection.Dispose();

                throw;
            }
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<T> QueryAsync<T>(IConnection databaseStrings, CommandSql commandSql)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(commandSql.ToString());
            }

            return new AsyncEnumerable<T>(databaseStrings, _connectionPipeline, commandSql);
        }

        /// <inheritdoc/>
        public T Read<T>(IConnection databaseStrings, CommandSql<T> commandSql)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(commandSql.ToString());
            }

            CommandBehavior behavior =
                CommandBehavior.SequentialAccess | CommandBehavior.SingleResult;

            if ((commandSql.RowStyle & RowStyle.FirstOrDefault) == RowStyle.FirstOrDefault)
            {
                behavior |= CommandBehavior.SingleRow;
            }

            using var dbConnection = _connectionPipeline.Get(databaseStrings);

            if (dbConnection.State == ConnectionState.Closed)
            {
                behavior |= CommandBehavior.CloseConnection;

                dbConnection.Open();
            }

            using (var command = dbConnection.CreateCommand())
            {
                command.CommandText = commandSql.Text;
                command.CommandType = commandSql.CommandType;

                if (commandSql.Timeout.HasValue)
                {
                    command.CommandTimeout = commandSql.Timeout.Value;
                }

                foreach (var (name, value) in commandSql.Parameters)
                {
                    LookupDb.AddParameterAuto(command, databaseStrings.Engine, name, value);
                }

                try
                {
                    using (var reader = command.ExecuteReader(behavior))
                    {
                        if (reader.HasRows)
                        {
                            var adaper = _adapters.GetOrAdd(
                                reader.GetType(),
                                type => new MapAdaper(type)
                            );

                            var map = adaper.CreateMap<T>();

                            if (reader.Read())
                            {
                                var result = map.Map(reader);

                                if (commandSql.RowStyle >= RowStyle.Single
                                    && reader.Read())
                                {
                                    ThrowMultipleRows(commandSql.RowStyle);
                                }

                                return result;
                            }
                        }

                        if (
                            commandSql.HasDefaultValue
                            || (commandSql.RowStyle & RowStyle.FirstOrDefault)
                                == RowStyle.FirstOrDefault
                        )
                        {
                            return commandSql.DefaultValue;
                        }

                        if (commandSql.CustomError)
                        {
                            throw new NoElementException(commandSql.NoElementError);
                        }

                        throw new InvalidOperationException(
                            "The input sequence contains more than one element."
                        );
                    }
                }
                finally
                {
                    commandSql.Callback(command);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<T> ReadAsync<T>(
            IConnection databaseStrings,
            CommandSql<T> commandSql,
            CancellationToken cancellationToken = default
        )
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(commandSql.ToString());
            }

            CommandBehavior behavior =
                CommandBehavior.SequentialAccess | CommandBehavior.SingleResult;

            if ((commandSql.RowStyle & RowStyle.FirstOrDefault) == RowStyle.FirstOrDefault)
            {
                behavior |= CommandBehavior.SingleRow;
            }

            await using var dbConnection = _connectionPipeline.Get(databaseStrings);

            if (dbConnection.State == ConnectionState.Closed)
            {
                behavior |= CommandBehavior.CloseConnection;

                await dbConnection.OpenAsync(cancellationToken);
            }

            await using (var command = dbConnection.CreateCommand())
            {
                command.CommandText = commandSql.Text;
                command.CommandType = commandSql.CommandType;

                if (commandSql.Timeout.HasValue)
                {
                    command.CommandTimeout = commandSql.Timeout.Value;
                }

                foreach (var (name, value) in commandSql.Parameters)
                {
                    LookupDb.AddParameterAuto(command, databaseStrings.Engine, name, value);
                }

                try
                {
                    await using (var reader = await command.ExecuteReaderAsync(behavior, cancellationToken))
                    {
                        if (reader.HasRows)
                        {
                            var adaper = _adapters.GetOrAdd(
                                reader.GetType(),
                                type => new MapAdaper(type)
                            );

                            var map = adaper.CreateMap<T>();

                            if (await reader.ReadAsync(cancellationToken))
                            {
                                var result = map.Map(reader);

                                if (commandSql.RowStyle >= RowStyle.Single
                                    && await reader.ReadAsync(cancellationToken))
                                {
                                    ThrowMultipleRows(commandSql.RowStyle);
                                }

                                return result;
                            }
                        }

                        if (
                            commandSql.HasDefaultValue
                            || (commandSql.RowStyle & RowStyle.FirstOrDefault)
                                == RowStyle.FirstOrDefault
                        )
                        {
                            return commandSql.DefaultValue;
                        }

                        if (commandSql.CustomError)
                        {
                            throw new NoElementException(commandSql.NoElementError);
                        }

                        throw new InvalidOperationException(
                            "The input sequence contains more than one element."
                        );
                    }

                }
                finally
                {
                    commandSql.Callback(command);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<IAsyncDbGridReader> QueryMultipleAsync(IConnection databaseStrings, CommandSql commandSql)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(commandSql.ToString());
            }

            CommandBehavior behavior = CommandBehavior.SequentialAccess;

            var dbConnection = _connectionPipeline.Get(databaseStrings);

            bool isClosedConnection = dbConnection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                behavior |= CommandBehavior.CloseConnection;

                await dbConnection.OpenAsync();
            }

            DbCommand command = null;
            DbDataReader reader = null;

            try
            {
                command = dbConnection.CreateCommand();

                command.CommandText = commandSql.Text;
                command.CommandType = commandSql.CommandType;

                if (commandSql.Timeout.HasValue)
                {
                    command.CommandTimeout = commandSql.Timeout.Value;
                }

                foreach (var (name, value) in commandSql.Parameters)
                {
                    LookupDb.AddParameterAuto(command, databaseStrings.Engine, name, value);
                }

                reader = await command.ExecuteReaderAsync(behavior);

                var adaper = _adapters.GetOrAdd(
                        reader.GetType(),
                        type => new MapAdaper(type)
                    );

                return new AsyncDbGridReader(dbConnection, command, reader, commandSql, adaper);
            }
            catch
            {
                if (reader != null)
                {
                    if (!reader.IsClosed)
                    {
                        try
                        {
                            command?.Cancel();
                        }
                        catch { }
                    }

                    await reader.DisposeAsync();
                }

                if (command != null)
                {
                    await command.DisposeAsync();
                }

                if (isClosedConnection)
                {
                    await dbConnection.CloseAsync();
                }

                await dbConnection.DisposeAsync();

                throw;
            }
        }

        /// <inheritdoc/>
        public int ExecuteMultiple(IConnection databaseStrings, Action<IMultipleExecutor> multipleAction, int? commandTimeout)
        {
            if (multipleAction is null)
            {
                throw new ArgumentNullException(nameof(multipleAction));
            }

            using var connection = _connectionPipeline.Get(databaseStrings);

            IMultipleExecutor multiple = commandTimeout.HasValue
            ? new MultipleExecuteTimeout(_connectionPipeline, connection, databaseStrings.Engine, commandTimeout.Value, _logger)
            : new MultipleExecute(_connectionPipeline, connection, databaseStrings.Engine, _logger);

            bool isClosedConnection = connection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                connection.Open();
            }

            try
            {
                multipleAction.Invoke(multiple);

                return multiple.RowsExecuted;
            }
            finally
            {
                if (isClosedConnection)
                {
                    connection.Close();
                }
            }
        }

        /// <inheritdoc/>
        public async Task<int> ExecuteMultipleAsync(IConnection databaseStrings, Func<IAsyncMultipleExecutor, Task> multipleAction, int? commandTimeout, CancellationToken cancellationToken = default)
        {
            if (multipleAction is null)
            {
                throw new ArgumentNullException(nameof(multipleAction));
            }

            using var connection = _connectionPipeline.Get(databaseStrings);

            IAsyncMultipleExecutor multipleExecutor = commandTimeout.HasValue
                ? new MultipleExecuteTimeoutAsync(_connectionPipeline, connection, databaseStrings.Engine, commandTimeout.Value, _logger, cancellationToken)
                : new MultipleExecuteAsync(_connectionPipeline, connection, databaseStrings.Engine, _logger, cancellationToken);

            bool isClosedConnection = connection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await multipleAction.Invoke(multipleExecutor);

                return multipleExecutor.RowsExecuted;
            }
            finally
            {
                if (isClosedConnection)
                {
                    await connection.CloseAsync();
                }
            }
        }

        /// <inheritdoc/>
        public int WriteToServer(IConnection databaseStrings, DataTable dataTable, int? commandTimeout = null)
        {
            if (dataTable is null)
            {
                throw new ArgumentNullException(nameof(dataTable));
            }

            if (string.IsNullOrEmpty(dataTable.TableName))
            {
                throw new ArgumentException("请通过“DataTable.TableName”指定目标表名称！");
            }

            using var connection = _connectionPipeline.Get(databaseStrings);

            bool isClosedConnection = connection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                connection.Open();
            }

            try
            {
                using (var bulkCopy = _connectionPipeline.Create(connection, databaseStrings.Engine))
                {
                    if (commandTimeout.HasValue)
                    {
                        bulkCopy.BulkCopyTimeout = commandTimeout.Value;
                    }

                    return bulkCopy.WriteToServer(dataTable);
                }
            }
            finally
            {
                if (isClosedConnection)
                {
                    connection.Close();
                }
            }
        }

        /// <inheritdoc/>
        public async Task<int> WriteToServerAsync(IConnection databaseStrings, DataTable dt, int? commandTimeout, CancellationToken cancellationToken = default)
        {
            if (dt is null)
            {
                throw new ArgumentNullException(nameof(dt));
            }

            if (string.IsNullOrEmpty(dt.TableName))
            {
                throw new ArgumentException("请通过“DataTable.TableName”指定目标表名称！");
            }

            using var connection = _connectionPipeline.Get(databaseStrings);

            bool isClosedConnection = connection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                using (var bulkCopy = await _connectionPipeline.CreateAsync(connection, databaseStrings.Engine, cancellationToken))
                {
                    if (commandTimeout.HasValue)
                    {
                        bulkCopy.BulkCopyTimeout = commandTimeout.Value;
                    }

                    return await bulkCopy.WriteToServerAsync(dt, cancellationToken);
                }
            }
            finally
            {
                if (isClosedConnection)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private class AsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly IConnection _connectionStrings;
            private readonly IDbConnectionPipeline _connectionPipeline;
            private readonly CommandSql _commandSql;

            public AsyncEnumerable(IConnection databaseStrings, IDbConnectionPipeline connectionPipeline, CommandSql commandSql)
            {
                _connectionStrings = databaseStrings;
                _connectionPipeline = connectionPipeline;
                _commandSql = commandSql;
            }

            public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                await using var connection = _connectionPipeline.Get(_connectionStrings);

                CommandBehavior behavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult;

                if (connection.State == ConnectionState.Closed)
                {
                    behavior |= CommandBehavior.CloseConnection;

                    await connection.OpenAsync(cancellationToken);
                }

                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = _commandSql.Text;
                    command.CommandType = _commandSql.CommandType;

                    if (_commandSql.Timeout.HasValue)
                    {
                        command.CommandTimeout = _commandSql.Timeout.Value;
                    }

                    foreach (var (name, value) in _commandSql.Parameters)
                    {
                        LookupDb.AddParameterAuto(command, _connectionStrings.Engine, name, value);
                    }

                    await using (var reader = await command.ExecuteReaderAsync(behavior, cancellationToken))
                    {
                        if (reader.HasRows)
                        {
                            var adaper = _adapters.GetOrAdd(
                                reader.GetType(),
                                type => new MapAdaper(type)
                            );

                            var map = adaper.CreateMap<T>();

                            while (await reader.ReadAsync(cancellationToken))
                            {
                                if (await map.IsInvalidAsync(reader, cancellationToken))
                                {
                                    continue;
                                }

                                yield return map.Map(reader);
                            }
                        }
                    }

                    _commandSql.Callback(command);
                }
            }
        }

        #region 执行器
        private class MultipleExecute : IMultipleExecutor
        {
            private readonly IDbConnectionPipeline _pipeline;
            private readonly DbConnection _connection;
            private readonly DatabaseEngine _engine;
            private readonly ILogger<DatabaseExecutor> _logger;

            public MultipleExecute(IDbConnectionPipeline pipeline, DbConnection connection, DatabaseEngine engine, ILogger<DatabaseExecutor> logger)
            {
                _pipeline = pipeline;
                _connection = connection;
                _engine = engine;
                _logger = logger;
            }

            public int RowsExecuted { private set; get; }

            public int Execute(CommandSql commandSql)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(commandSql.ToString());
                }

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = commandSql.Text;
                    command.CommandType = commandSql.CommandType;

                    if (commandSql.Timeout.HasValue)
                    {
                        command.CommandTimeout = commandSql.Timeout.Value;
                    }

                    foreach (var (name, value) in commandSql.Parameters)
                    {
                        LookupDb.AddParameterAuto(command, _engine, name, value);
                    }

                    int influenceRows = command.ExecuteNonQuery();

                    commandSql.Callback(command);

                    RowsExecuted += influenceRows;

                    return influenceRows;
                }
            }

            public int WriteToServer(DataTable dt, int? commandTimeout = null)
            {
                if (dt is null)
                {
                    throw new ArgumentNullException(nameof(dt));
                }

                if (string.IsNullOrEmpty(dt.TableName))
                {
                    throw new ArgumentException("请通过“DataTable.TableName”指定目标表名称！");
                }

                int influenceRows = 0;

                using (var bulkCopy = _pipeline.Create(_connection, _engine))
                {
                    if (commandTimeout.HasValue)
                    {
                        bulkCopy.BulkCopyTimeout = commandTimeout.Value;
                    }

                    influenceRows = bulkCopy.WriteToServer(dt);
                }

                RowsExecuted += influenceRows;

                return influenceRows;
            }
        }

        private class MultipleExecuteTimeout : IMultipleExecutor
        {
            private readonly IDbConnectionPipeline _pipeline;
            private readonly DbConnection _connection;
            private readonly DatabaseEngine _engine;
            private readonly int _commandTimeout;
            private readonly ILogger<DatabaseExecutor> _logger;
            private readonly Stopwatch _stopwatch;

            public MultipleExecuteTimeout(IDbConnectionPipeline pipeline, DbConnection connection, DatabaseEngine engine, int commandTimeout, ILogger<DatabaseExecutor> logger)
            {
                _pipeline = pipeline;
                _connection = connection;
                _engine = engine;
                _commandTimeout = commandTimeout;

                _logger = logger;

                _stopwatch = new Stopwatch();
            }

            public int RowsExecuted { private set; get; }

            public int Execute(CommandSql commandSql)
            {
                commandSql.Timeout = commandSql.Timeout.HasValue
                    ? Math.Min(_commandTimeout - (int)(_stopwatch.ElapsedMilliseconds / 1000), commandSql.Timeout.Value)
                    : _commandTimeout - (int)(_stopwatch.ElapsedMilliseconds / 1000);

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(commandSql.ToString());
                }

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = commandSql.Text;
                    command.CommandType = commandSql.CommandType;

                    if (commandSql.Timeout.HasValue)
                    {
                        command.CommandTimeout = commandSql.Timeout.Value;
                    }

                    foreach (var (name, value) in commandSql.Parameters)
                    {
                        LookupDb.AddParameterAuto(command, _engine, name, value);
                    }

                    _stopwatch.Start();

                    int influenceRows = command.ExecuteNonQuery();

                    _stopwatch.Stop();

                    commandSql.Callback(command);

                    RowsExecuted += influenceRows;

                    return influenceRows;
                }
            }

            public int WriteToServer(DataTable dt, int? commandTimeout = null)
            {
                if (dt is null)
                {
                    throw new ArgumentNullException(nameof(dt));
                }

                if (string.IsNullOrEmpty(dt.TableName))
                {
                    throw new ArgumentException("请通过“DataTable.TableName”指定目标表名称！");
                }

                commandTimeout = commandTimeout.HasValue
                    ? Math.Min(_commandTimeout - (int)(_stopwatch.ElapsedMilliseconds / 1000), commandTimeout.Value)
                    : _commandTimeout - (int)(_stopwatch.ElapsedMilliseconds / 1000);

                _stopwatch.Start();

                int influenceRows = 0;

                using (var bulkCopy = _pipeline.Create(_connection, _engine))
                {
                    if (commandTimeout.HasValue)
                    {
                        bulkCopy.BulkCopyTimeout = commandTimeout.Value;
                    }

                    influenceRows = bulkCopy.WriteToServer(dt);
                }

                _stopwatch.Stop();

                RowsExecuted += influenceRows;

                return influenceRows;
            }
        }

        private class MultipleExecuteAsync : IAsyncMultipleExecutor
        {
            private readonly IDbConnectionPipeline _pipeline;
            private readonly DbConnection _connection;
            private readonly DatabaseEngine _engine;
            private readonly ILogger<DatabaseExecutor> _logger;
            private readonly CancellationToken _cancellationToken;

            public MultipleExecuteAsync(IDbConnectionPipeline pipeline, DbConnection connection, DatabaseEngine engine, ILogger<DatabaseExecutor> logger, CancellationToken cancellationToken)
            {
                _pipeline = pipeline;
                _connection = connection;
                _engine = engine;
                _logger = logger;
                _cancellationToken = cancellationToken;
            }

            public int RowsExecuted { private set; get; }

            public async Task<int> ExecuteAsync(CommandSql commandSql)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(commandSql.ToString());
                }

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = commandSql.Text;
                    command.CommandType = commandSql.CommandType;

                    if (commandSql.Timeout.HasValue)
                    {
                        command.CommandTimeout = commandSql.Timeout.Value;
                    }

                    foreach (var (name, value) in commandSql.Parameters)
                    {
                        LookupDb.AddParameterAuto(command, _engine, name, value);
                    }

                    int influenceRows = await command.ExecuteNonQueryAsync(_cancellationToken);

                    commandSql.Callback(command);

                    RowsExecuted += influenceRows;

                    return influenceRows;
                }
            }

            public async Task<int> WriteToServerAsync(DataTable dt, int? commandTimeout = null)
            {
                if (dt is null)
                {
                    throw new ArgumentNullException(nameof(dt));
                }

                if (string.IsNullOrEmpty(dt.TableName))
                {
                    throw new ArgumentException("请通过“DataTable.TableName”指定目标表名称！");
                }

                using (var bulkCopy = await _pipeline.CreateAsync(_connection, _engine, _cancellationToken))
                {
                    if (commandTimeout.HasValue)
                    {
                        bulkCopy.BulkCopyTimeout = commandTimeout.Value;
                    }

                    int influenceRows = await bulkCopy.WriteToServerAsync(dt, _cancellationToken);

                    RowsExecuted += influenceRows;

                    return influenceRows;
                }
            }
        }

        private class MultipleExecuteTimeoutAsync : IAsyncMultipleExecutor
        {
            private readonly IDbConnectionPipeline _pipeline;
            private readonly DbConnection _connection;
            private readonly DatabaseEngine _engine;
            private readonly int _commandTimeout;
            private readonly ILogger<DatabaseExecutor> _logger;
            private readonly CancellationToken _cancellationToken;
            private readonly Stopwatch _stopwatch;

            public MultipleExecuteTimeoutAsync(IDbConnectionPipeline pipeline, DbConnection connection, DatabaseEngine engine, int commandTimeout, ILogger<DatabaseExecutor> logger, CancellationToken cancellationToken)
            {
                _pipeline = pipeline;
                _connection = connection;
                _engine = engine;
                _commandTimeout = commandTimeout;
                _logger = logger;
                _cancellationToken = cancellationToken;

                _stopwatch = new Stopwatch();
            }

            public int RowsExecuted { private set; get; }

            public async Task<int> ExecuteAsync(CommandSql commandSql)
            {
                commandSql.Timeout = commandSql.Timeout.HasValue
                    ? Math.Min(_commandTimeout - (int)(_stopwatch.ElapsedMilliseconds / 1000), commandSql.Timeout.Value)
                    : _commandTimeout - (int)(_stopwatch.ElapsedMilliseconds / 1000);

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(commandSql.ToString());
                }

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = commandSql.Text;
                    command.CommandType = commandSql.CommandType;

                    if (commandSql.Timeout.HasValue)
                    {
                        command.CommandTimeout = commandSql.Timeout.Value;
                    }

                    foreach (var (name, value) in commandSql.Parameters)
                    {
                        LookupDb.AddParameterAuto(command, _engine, name, value);
                    }

                    int influenceRows = await command.ExecuteNonQueryAsync(_cancellationToken);

                    commandSql.Callback(command);

                    RowsExecuted += influenceRows;

                    return influenceRows;
                }
            }

            public async Task<int> WriteToServerAsync(DataTable dt, int? commandTimeout = null)
            {
                if (dt is null)
                {
                    throw new ArgumentNullException(nameof(dt));
                }

                if (string.IsNullOrEmpty(dt.TableName))
                {
                    throw new ArgumentException("请通过“DataTable.TableName”指定目标表名称！");
                }

                using (var bulkCopy = await _pipeline.CreateAsync(_connection, _engine, _cancellationToken))
                {
                    bulkCopy.BulkCopyTimeout = commandTimeout.HasValue
                        ? Math.Min(_commandTimeout - (int)(_stopwatch.ElapsedMilliseconds / 1000), commandTimeout.Value)
                        : _commandTimeout - (int)(_stopwatch.ElapsedMilliseconds / 1000);

                    int influenceRows = await bulkCopy.WriteToServerAsync(dt, _cancellationToken);

                    RowsExecuted += influenceRows;

                    return influenceRows;
                }
            }
        }
        #endregion

        #region 读取器
        private static readonly int[] _errTwoRows = new int[2], _errZeroRows = Array.Empty<int>();

        private static void ThrowMultipleRows(RowStyle rowStyle)
        {
            switch (rowStyle)
            {
                case RowStyle.FirstOrDefault:
                    break;
                case RowStyle.First:
                    _errZeroRows.First();
                    break;
                case RowStyle.Single:
                    _errTwoRows.Single();
                    break;
                case RowStyle.SingleOrDefault:
                    _errTwoRows.SingleOrDefault();
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private class AsyncDbGridReader : IAsyncDbGridReader
        {
            private readonly DbConnection _connection;
            private readonly DbCommand _command;
            private readonly DbDataReader _reader;
            private readonly CommandSql _commandSql;
            private readonly MapAdaper _adaper;

            public AsyncDbGridReader(DbConnection connection, DbCommand command, DbDataReader reader, CommandSql commandSql, MapAdaper adaper)
            {
                _connection = connection;
                _command = command;
                _reader = reader;
                _commandSql = commandSql;
                _adaper = adaper;
            }

            public bool IsConsumed { get; private set; }

            public async Task<T> ReadAsync<T>(RowStyle rowStyle, CancellationToken cancellationToken = default)
            {
                if (IsConsumed)
                {
                    throw new InvalidOperationException("Query results must be consumed in the correct order, and each result can only be consumed once");
                }

                IsConsumed = true;

                T result = default;

                if (await _reader.ReadAsync(cancellationToken) && _reader.FieldCount > 0)
                {
                    var map = _adaper.CreateMap<T>();

                    result = map.Map(_reader);

                    if ((rowStyle & RowStyle.Single) == RowStyle.Single)
                    {
                        if (await _reader.ReadAsync(cancellationToken))
                        {
                            ThrowMultipleRows(rowStyle);
                        }
                    }

                    while (await _reader.ReadAsync(cancellationToken)) { /* ignore subsequent rows */ }
                }
                else if ((rowStyle & RowStyle.FirstOrDefault) == 0) // demanding a row, and don't have one
                {
                    ThrowMultipleRows(rowStyle);
                }

                await NextResultAsync(cancellationToken);

                return result;
            }

            public IAsyncEnumerable<T> QueryAsync<T>()
            {
                if (IsConsumed)
                {
                    throw new InvalidOperationException("Query results must be consumed in the correct order, and each result can only be consumed once");
                }

                IsConsumed = true;

                return new AsyncEnumerable<T>(this, _gridIndex);
            }

            private async IAsyncEnumerator<T> ReadDeferredAsync<T>(int index, CancellationToken cancellationToken)
            {
                try
                {
                    var map = _adaper.CreateMap<T>();

                    while (index == _gridIndex && await _reader.ReadAsync(cancellationToken))
                    {
                        if (!await map.IsInvalidAsync(_reader, cancellationToken))
                        {
                            yield return map.Map(_reader);
                        }
                    }
                }
                finally // finally so that First etc progresses things even when multiple rows
                {
                    if (index == _gridIndex)
                    {
                        await NextResultAsync(cancellationToken);
                    }
                }
            }

            private int _gridIndex; //, readCount;
            private volatile bool _disposed;

            private async Task NextResultAsync(CancellationToken cancellationToken)
            {
                if (await _reader.NextResultAsync(cancellationToken))
                {
                    Interlocked.Increment(ref _gridIndex);

                    IsConsumed = false;
                }
                else
                {
                    await DisposeAsync();
                }
            }

            /// <summary>
            /// Dispose the grid, closing and disposing both the underlying reader and command.
            /// </summary>
            public async ValueTask DisposeAsync()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                if (!_reader.IsClosed)
                {
                    _command.Cancel();
                }

                await _reader.DisposeAsync();

                _commandSql.Callback(_command);

                await _command.DisposeAsync();

                await _connection.DisposeAsync();

                GC.SuppressFinalize(this);
            }

            public async Task<List<T>> ReadAsync<T>(CancellationToken cancellationToken)
            {
                var asyncEnumerable = QueryAsync<T>();

                var results = new List<T>();

                await using (var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken))
                {
                    while (await enumerator.MoveNextAsync()
                            .ConfigureAwait(false))
                    {
                        results.Add(enumerator.Current);
                    }
                }

                return results;
            }

            private class AsyncEnumerable<T> : IAsyncEnumerable<T>
            {
                private readonly AsyncDbGridReader _gridReader;
                private readonly int _gridIndex;

                public AsyncEnumerable(AsyncDbGridReader gridReader, int gridIndex)
                {
                    _gridReader = gridReader;
                    _gridIndex = gridIndex;
                }

                public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => _gridReader.ReadDeferredAsync<T>(_gridIndex, cancellationToken);
            }
        }

        private class DbGridReader : IDbGridReader
        {
            private readonly DbConnection _connection;
            private readonly DbCommand _command;
            private readonly DbDataReader _reader;
            private readonly CommandSql _commandSql;
            private readonly MapAdaper _adaper;

            public DbGridReader(DbConnection connection, DbCommand command, DbDataReader reader, CommandSql commandSql, MapAdaper adaper)
            {
                _connection = connection;
                _command = command;
                _reader = reader;
                _commandSql = commandSql;
                _adaper = adaper;
            }

            public bool IsConsumed { get; private set; }

            public T Read<T>(RowStyle rowStyle)
            {
                if (IsConsumed)
                {
                    throw new InvalidOperationException("Query results must be consumed in the correct order, and each result can only be consumed once");
                }

                IsConsumed = true;

                T result = default;

                if (_reader.Read() && _reader.FieldCount > 0)
                {
                    var map = _adaper.CreateMap<T>();

                    result = map.Map(_reader);

                    if ((rowStyle & RowStyle.Single) == RowStyle.Single)
                    {
                        if (_reader.Read())
                        {
                            ThrowMultipleRows(rowStyle);
                        }
                    }

                    while (_reader.Read()) { /* ignore subsequent rows */ }
                }
                else if ((rowStyle & RowStyle.FirstOrDefault) == 0) // demanding a row, and don't have one
                {
                    ThrowMultipleRows(rowStyle);
                }

                NextResult();

                return result;
            }
            public IEnumerable<T> Query<T>()
            {
                if (IsConsumed)
                {
                    throw new InvalidOperationException("Query results must be consumed in the correct order, and each result can only be consumed once");
                }

                IsConsumed = true;

                return ReadDeferred<T>(_gridIndex);
            }

            public List<T> Read<T>()
            {
                var results = Query<T>();

                return results.ToList();
            }

            private IEnumerable<T> ReadDeferred<T>(int index)
            {
                try
                {
                    var map = _adaper.CreateMap<T>();

                    while (index == _gridIndex && _reader.Read())
                    {
                        if (!map.IsInvalid(_reader))
                        {
                            yield return map.Map(_reader);
                        }
                    }
                }
                finally // finally so that First etc progresses things even when multiple rows
                {
                    if (index == _gridIndex)
                    {
                        NextResult();
                    }
                }
            }

            private int _gridIndex; //, readCount;

            private void NextResult()
            {
                if (_reader.NextResult())
                {
                    // readCount++;
                    _gridIndex++;

                    IsConsumed = false;
                }
                else
                {
                    Dispose();
                }
            }

            private bool _disposed;

            /// <summary>
            /// Dispose the grid, closing and disposing both the underlying reader and command.
            /// </summary>
            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                if (!_reader.IsClosed)
                {
                    _command.Cancel();
                }

                _reader.Dispose();

                _commandSql.Callback(_command);

                _command.Dispose();

                _connection.Dispose();

                GC.SuppressFinalize(this);
            }
        }
        #endregion

        #region ORM适配器
        private class MapAdaper
        {
            private const int COLLECT_PER_ITEMS = 1000;

            private static readonly MethodInfo _equals;
            private static readonly MethodInfo _concat;
            private static readonly MethodInfo _typeCode;
            private static readonly MethodInfo _charToString;
            private static readonly MethodInfo _stringToChar;
            private static readonly ConstructorInfo _errorCtor;
            private static readonly ConstructorInfo _errorOutOfRangeCtor;
            private static readonly HashSet<string> _nameHooks = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "GetBoolean",
                "GetByte",
                "GetByteArray",
                "GetChar",
                "GetDateOnly",
                "GetDateTime",
                "GetDateTimeOffset",
                "GetDecimal",
                "GetDouble",
                "GetDoubleArray",
                "GetFloat",
                "GetFloatArray",
                "GetGuid",
                "GetInt16",
                "GetInt16Array",
                "GetInt32",
                "GetInt64",
                "GetSByte",
                "GetStream",
                "GetString",
                "GetTextReader",
                "GetTimeOnly",
                "GetTimeSpan",
                "GetUInt16",
                "GetUInt32",
                "GetUInt64",
                // ----------- MySQL ------------
                "GetMySqlDateTime",
                "GetMySqlDecimal",
                "GetMySqlGeometry",
                // ----------- SqlServer ------------
                "GetSqlBinary",
                "GetSqlBoolean",
                "GetSqlByte",
                "GetSqlBytes",
                "GetSqlChars",
                "GetSqlDateTime",
                "GetSqlDecimal",
                "GetSqlDouble",
                "GetSqlGuid",
                "GetSqlInt16",
                "GetSqlInt32",
                "GetSqlInt64",
                "GetSqlMoney",
                "GetSqlSingle",
                "GetSqlString",
                // ----------- Oracle ------------
                "GetOracleBFile",
                "GetOracleBinary",
                "GetOracleBlob",
                "GetOracleBoolean",
                "GetOracleClob",
                "GetOracleDate",
                "GetOracleDecimal",
                "GetOracleIntervalDS",
                "GetOracleIntervalYM",
                "GetOracleRef",
                "GetOracleRefCursor",
                "GetOracleString",
                "GetOracleTimeStamp",
                "GetOracleTimeStampLTZ",
                "GetOracleTimeStampTZ"
                // ----------- Npgsql ------------
            };
            private static readonly Dictionary<Type, Dictionary<Type, TypeCode>> _typeTransforms = new Dictionary<Type, Dictionary<Type, TypeCode>>
            {
                [typeof(sbyte)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(long)] = TypeCode.Int64,
                    [typeof(ulong)] = TypeCode.UInt64
                },
                [typeof(byte)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(sbyte)] = TypeCode.SByte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(long)] = TypeCode.Int64,
                    [typeof(ulong)] = TypeCode.UInt64
                },
                [typeof(short)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(sbyte)] = TypeCode.SByte,
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(long)] = TypeCode.Int64,
                    [typeof(ulong)] = TypeCode.UInt64
                },
                [typeof(ushort)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(sbyte)] = TypeCode.SByte,
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(long)] = TypeCode.Int64,
                    [typeof(ulong)] = TypeCode.UInt64
                },
                [typeof(int)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(sbyte)] = TypeCode.SByte,
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(long)] = TypeCode.Int64,
                    [typeof(ulong)] = TypeCode.UInt64
                },
                [typeof(uint)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(sbyte)] = TypeCode.SByte,
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(long)] = TypeCode.Int64,
                    [typeof(ulong)] = TypeCode.UInt64
                },
                [typeof(long)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(sbyte)] = TypeCode.SByte,
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(ulong)] = TypeCode.UInt64
                },
                [typeof(ulong)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(sbyte)] = TypeCode.SByte,
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(long)] = TypeCode.Int64
                },
                [typeof(float)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(long)] = TypeCode.Int64,
                    [typeof(ulong)] = TypeCode.UInt64,
                    [typeof(double)] = TypeCode.Double,
                    [typeof(decimal)] = TypeCode.Decimal
                },
                [typeof(double)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(long)] = TypeCode.Int64,
                    [typeof(ulong)] = TypeCode.UInt64,
                    [typeof(float)] = TypeCode.Single,
                    [typeof(decimal)] = TypeCode.Decimal
                },
                [typeof(decimal)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(long)] = TypeCode.Int64,
                    [typeof(ulong)] = TypeCode.UInt64,
                    [typeof(float)] = TypeCode.Single,
                    [typeof(double)] = TypeCode.Double
                }
            };

            static MapAdaper()
            {
                _errorCtor = typeof(NotSupportedException).GetConstructor(new Type[] { Types.String });

                _errorOutOfRangeCtor = typeof(IndexOutOfRangeException).GetConstructor(new Type[] { Types.String });

                _equals = typeof(MapAdaper).GetMethod(
                    nameof(Equals),
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly
                );

                _concat = Types.String.GetMethod(
                    nameof(string.Concat),
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly,
                    null,
                    new Type[] { Types.String, Types.String, Types.String },
                    null);

                _charToString = Types.Char.GetMethod(nameof(char.ToString), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);

                _stringToChar = Types.String.GetMethod("get_Chars", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly, null, new Type[] { Types.Int32 }, null);

                var type = typeof(Type);

                _typeCode = type.GetMethod(nameof(Type.GetTypeCode), BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly, null, new Type[] { type }, null);
            }

            private int _refCount = 0;

            private volatile bool _recovering = false;

            private readonly ConcurrentDictionary<Type, IDbMapper> _mappers =
                new ConcurrentDictionary<Type, IDbMapper>(100, 2 * COLLECT_PER_ITEMS);

            private readonly Type _type;
            private readonly MethodInfo _isDbNull;
            private readonly MethodInfo _getName;
            private readonly MethodInfo _getValue;
            private readonly MethodInfo _getFieldType;
            private readonly Dictionary<Type, MethodInfo> _typeMap = new Dictionary<Type, MethodInfo>(16);

            private static bool Equals(string a, string b)
            {
                return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
            }

            public MapAdaper(Type type)
            {
                var types = new Type[] { Types.Int32 };

                _type = type;

                _getName = type.GetMethod("GetName", types);

                _getValue = type.GetMethod("GetValue", types);

                _isDbNull = type.GetMethod("IsDBNull", types);

                _getFieldType = type.GetMethod("GetFieldType", types);

                foreach (var methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (!_nameHooks.Contains(methodInfo.Name))
                    {
                        continue;
                    }

                    var parameterInfos = methodInfo.GetParameters();

                    if (parameterInfos.Length != 1)
                    {
                        continue;
                    }

                    var parameterInfo = parameterInfos[0];

                    if (parameterInfo.ParameterType == Types.Int32)
                    {
                        _typeMap.TryAdd(methodInfo.ReturnType, methodInfo);
                    }
                }
            }

            public static MethodInfo IgnoreCaseEquals => _equals;

            public ParameterExpression DbVariable() => Parameter(_type);

            public UnaryExpression Convert(ParameterExpression parameterExp) =>
                Expression.Convert(parameterExp, _type);

            public Expression ToSolve(Type propertyType, ParameterExpression dbVar, Expression iVar)
            {
                if (propertyType.FullName == LinqBinary)
                {
                    return New(propertyType.GetConstructor(new Type[] { Types.Object }), Call(dbVar, _getValue, iVar));
                }

                if (propertyType.FullName is "Newtonsoft.Json.Linq.JObject" or "Newtonsoft.Json.Linq.JArray")
                {
                    var jsonVar = Variable(Types.String);

                    return Block(propertyType, new ParameterExpression[] { jsonVar },
                        Assign(jsonVar, Call(dbVar, _typeMap[Types.String], iVar)),
                        Condition(Equal(jsonVar, Constant(null, Types.String)),
                            Constant(null, propertyType),
                            Call(propertyType.GetMethod("Parse", BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly, null, new Type[] { Types.String }, null), jsonVar)
                        )
                    );
                }

                if (propertyType == Types.JsonPayload || propertyType == Types.JsonbPayload)
                {
                    var jsonVar = Variable(Types.String);

                    return Block(propertyType, new ParameterExpression[] { jsonVar },
                        Assign(jsonVar, Call(dbVar, _typeMap[Types.String], iVar)),
                        Condition(Equal(jsonVar, Constant(null, Types.String)),
                            Constant(null, propertyType),
                            New(propertyType.GetConstructor(new Type[] { Types.String }), jsonVar)
                        )
                    );
                }

                if (!_typeMap.TryGetValue(propertyType, out MethodInfo originalFn))
                {
                    var typeArg = Variable(typeof(Type));

                    return Block(new ParameterExpression[] { typeArg },
                        Assign(typeArg, Call(dbVar, _getFieldType, iVar)),
                        ToSolveByTransform(propertyType, dbVar, iVar, typeArg)
                    );
                }
                else if (propertyType == Types.Object)
                {
                    return Call(dbVar, originalFn, iVar);
                }
                else if (propertyType == Types.Char || propertyType == Types.String)
                {
                    var typeArg = Variable(typeof(Type));

                    return Block(new ParameterExpression[] { typeArg },
                        Assign(typeArg, Call(dbVar, _getFieldType, iVar)),
                        Condition(Equal(typeArg, Constant(propertyType)),
                            Call(dbVar, originalFn, iVar),
                            ToSolveByTransform(propertyType, dbVar, iVar, typeArg)
                        )
                    );
                }
                else
                {
                    return Call(dbVar, originalFn, iVar);
                }
            }

            private Expression ToSolveByTransform(Type propertyType, ParameterExpression dbVar, Expression iVar, ParameterExpression typeArg)
            {
                var valueVar = Variable(propertyType);

                var variables = new List<ParameterExpression>(1)
                {
                    valueVar
                };

                var throwUnary = Throw(New(_errorCtor, Call(_concat, Constant("数据库字段“"), GetName(dbVar, iVar), Constant("”的类型和实体属性的类型映射不被支持，请检查映射实体的属性类型！"))));

                if (propertyType == Types.Object)
                {
                    return Block(propertyType, variables, throwUnary, valueVar);
                }

                var expressions = new List<Expression>(2);

                if (!propertyType.IsValueType || !_typeTransforms.TryGetValue(propertyType, out var transforms))
                {
                    switch (Type.GetTypeCode(propertyType))
                    {
                        case TypeCode.String when _typeMap.TryGetValue(Types.Char, out var transformFn):

                            expressions.Add(IfThenElse(Equal(typeArg, Constant(Types.Char)), Assign(valueVar, Call(Call(dbVar, transformFn, iVar), _charToString)), throwUnary));

                            break;
                        case TypeCode.Char when _typeMap.TryGetValue(Types.String, out var transformFn):

                            var stringVar = Variable(Types.String);

                            expressions.Add(IfThenElse(Equal(typeArg, Constant(Types.String)),
                                Block(new ParameterExpression[1] { stringVar },
                                    Assign(stringVar, Call(dbVar, transformFn, iVar)),
                                    IfThenElse(Equal(Property(stringVar, "Length"), Constant(1)),
                                        Assign(valueVar, Call(stringVar, _stringToChar, Constant(0))),
                                        Throw(New(_errorOutOfRangeCtor, Call(_concat, Constant("数据库字段“"), GetName(dbVar, iVar), Constant("”的值超出了实体属性的类型容值范围，请检查映射实体的属性类型！"))))
                                    )
                                ),
                                throwUnary)
                            );

                            break;
                        default:

                            expressions.Add(throwUnary);

                            break;
                    }

                    expressions.Add(valueVar);

                    return Block(propertyType, variables, expressions);
                }

                var switchCases = new List<SwitchCase>(transforms.Count);

                var code = Type.GetTypeCode(propertyType);

                bool unsignedCode = code is TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64;

                var throwOutUnary = Throw(New(_errorOutOfRangeCtor, Call(_concat, Constant("数据库字段“"), GetName(dbVar, iVar), Constant("”的值超出了实体属性的类型容值范围，请检查映射实体的属性类型！"))));

                foreach (var (key, typeCode) in transforms)
                {
                    if (!_typeMap.TryGetValue(key, out var transformFn))
                    {
                        continue;
                    }

                    bool isGreaterThan = code >= typeCode;

                    bool unsignedTypeCode = typeCode is TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64;

                    if (isGreaterThan && (!unsignedCode || unsignedTypeCode))
                    {
                        switchCases.Add(SwitchCase(Assign(valueVar, Expression.Convert(Call(dbVar, transformFn, iVar), propertyType)), Constant(typeCode)));

                        continue;
                    }

                    BinaryExpression test;

                    var transformVar = Variable(key);

                    variables.Add(transformVar);

                    if (!unsignedCode || unsignedTypeCode) //? 原类型有符号，数据类型无符号，或者都是无符号的。 且数据类型一定大于源类型。
                    {
                        if (unsignedTypeCode) //? 源类型有符号，数据类型无符号。
                        {
                            test = LessThanOrEqual(transformVar, Expression.Convert(Field(null, propertyType, "MaxValue"), key));
                        }
                        else if (unsignedCode) //? 原类型无符号，但数据类型有符号，要判断数据值是否大于等于“0”且小于等于“最大值”。
                        {
                            test = AndAlso(GreaterThanOrEqual(transformVar, Constant(System.Convert.ChangeType(0, key))), LessThanOrEqual(transformVar, Expression.Convert(Field(null, propertyType, "MaxValue"), key)));
                        }
                        else  //? 原类型有符号，但数据类型有符号，要判断数据值是否大于等于“最小值”且小于等于“最大值”。。
                        {
                            test = AndAlso(GreaterThanOrEqual(transformVar, Expression.Convert(Field(null, propertyType, "MinValue"), key)), LessThanOrEqual(transformVar, Expression.Convert(Field(null, propertyType, "MaxValue"), key)));
                        }
                    }
                    else //? 源类型无符号，数据类型有符号。
                    {
                        test = GreaterThanOrEqual(transformVar, Constant(System.Convert.ChangeType(0, key)));

                        if (!isGreaterThan)
                        {
                            test = AndAlso(test, LessThanOrEqual(transformVar, Expression.Convert(Field(null, propertyType, "MaxValue"), key)));
                        }
                    }

                    switchCases.Add(SwitchCase(Block(transformVar,
                            Assign(transformVar, Call(dbVar, transformFn, iVar)),
                            IfThenElse(test,
                                Assign(valueVar, Expression.Convert(transformVar, propertyType)),
                                throwOutUnary)
                            ),
                            Constant(typeCode)
                        )
                    );
                }

                expressions.Add(Switch(
                    typeof(void),
                    Call(_typeCode, typeArg),
                    throwUnary,
                    null,
                    switchCases.ToArray()
                ));

                expressions.Add(valueVar);

                return Block(propertyType, variables, expressions);
            }

            public Expression IsDbNull(ParameterExpression dbVar, Expression iVar) =>
                Call(dbVar, _isDbNull, iVar);

            public Expression GetName(ParameterExpression dbVar, Expression iVar) =>
                Call(dbVar, _getName, iVar);

            public DbMapper<T> CreateMap<T>() =>
                (DbMapper<T>)
                    _mappers.GetOrAdd(
                        typeof(T),
                        type =>
                        {
                            if (Interlocked.Increment(ref _refCount) >= COLLECT_PER_ITEMS)
                            {
                                if (_recovering) { }
                                else
                                {
                                    _recovering = true;

                                    new Timer(
                                        render =>
                                        {
                                            var r = new Random();

                                            var keys = (ICollection<Type>)render;

                                            int offset = keys.Count / 2;

                                            var count = r.Next(offset, keys.Count); //? 随机移除一半以上的数据。

                                            var skipSize = r.Next(0, offset); //? 随机开始移除的位置。

                                            try
                                            {
                                                foreach (
                                                    var key in keys.Skip(skipSize).Take(offset)
                                                )
                                                {
                                                    _mappers.TryRemove(key, out _);
                                                }
                                            }
                                            catch { }
                                            finally
                                            {
                                                _recovering = false;

                                                Interlocked.Exchange(ref _refCount, _mappers.Count);
                                            }
                                        },
                                        _mappers.Keys,
                                        100,
                                        Timeout.Infinite
                                    );
                                }
                            }

                            return new DbMapperGen<T>(this).CreateMap();
                        }
                    );
        }

        private class DbMapperGen<T>
        {
            private readonly MapAdaper _adaper;

            public DbMapperGen(MapAdaper adaper)
            {
                _adaper = adaper;
            }

            private Func<DbDataReader, T> MakeSimple(Type type)
            {
                var paramterExp = Parameter(typeof(DbDataReader));

                var iVar = Constant(0);

                var dbVar = _adaper.DbVariable();

                var bodyExp = UncheckedValue(type, dbVar, iVar);

                var lambdaExp = Lambda<Func<DbDataReader, T>>(
                    Block(
                        new ParameterExpression[] { dbVar },
                        Assign(dbVar, _adaper.Convert(paramterExp)),
                        Condition(_adaper.IsDbNull(dbVar, iVar), Default(type), bodyExp)
                    ),
                    paramterExp
                );

                return lambdaExp.Compile();
            }

            private Func<DbDataReader, T> MakeSimpleNull(Type type, Type nullableType)
            {
                var paramterExp = Parameter(typeof(DbDataReader));

                var iVar = Constant(0);

                var dbVar = _adaper.DbVariable();

                var nullableCtor = nullableType.GetConstructor(new Type[] { type });

                var bodyExp = UncheckedValue(type, dbVar, iVar);

                var lambdaExp = Lambda<Func<DbDataReader, T>>(
                    Block(
                        new ParameterExpression[] { dbVar },
                        Assign(dbVar, _adaper.Convert(paramterExp)),
                        Condition(
                            _adaper.IsDbNull(dbVar, iVar),
                            Default(nullableType),
                            New(nullableCtor, bodyExp)
                        )
                    ),
                    paramterExp
                );

                return lambdaExp.Compile();
            }

            private Func<DbDataReader, T> MakeNull(Type type, Type nullableType)
            {
                var nullCtor = nullableType.GetConstructor(new Type[] { type });

                //? 无参构造函数。
                var nonCtor = type.GetConstructor(
                    BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.Instance
                        | BindingFlags.DeclaredOnly,
                    null,
                    Type.EmptyTypes,
                    null
                );

                if (nonCtor is null)
                {
                    var constructorInfos = type.GetConstructors(
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
                    );

                    foreach (var constructorInfo in constructorInfos)
                    {
                        return MakeFor(constructorInfo, newExp => New(nullCtor, newExp));
                    }
                }

                return MakeFor(type, nonCtor, instanceExp => New(nullCtor, instanceExp));
            }

            private Func<DbDataReader, T> MakeFor(
                Type type,
                ConstructorInfo constructorInfo,
                Func<ParameterExpression, Expression> convert
            )
            {
                var instanceExp = Variable(type);

                var paramterExp = Parameter(typeof(DbDataReader));

                var dbVar = _adaper.DbVariable();

                var iVar = Parameter(typeof(int));

                var lenVar = Property(dbVar, "FieldCount");

                var list = new List<Expression>
                {
                    Assign(iVar, Constant(0)),
                    Assign(dbVar, _adaper.Convert(paramterExp)),
                    Assign(instanceExp, New(constructorInfo))
                };

                var listCases = new List<SwitchCase>();

                foreach (
                    var propertyInfo in type.GetProperties(
                        BindingFlags.Instance | BindingFlags.Public
                    )
                )
                {
                    if (!propertyInfo.CanWrite)
                    {
                        continue;
                    }

                    if (propertyInfo.IsIgnore())
                    {
                        continue;
                    }

                    listCases.Add(SwitchCaseAssign(instanceExp, propertyInfo, dbVar, iVar));
                }

                LabelTarget break_label = Label(typeof(void));
                LabelTarget continue_label = Label(typeof(void));

                var body = Switch(
                    _adaper.GetName(dbVar, iVar),
                    null,
                    MapAdaper.IgnoreCaseEquals,
                    listCases
                );

                list.Add(
                    Loop(
                        IfThenElse(
                            LessThan(iVar, lenVar),
                            Block(
                                body,
                                AddAssign(iVar, Constant(1)),
                                Continue(continue_label, typeof(void))
                            ),
                            Break(break_label, typeof(void))
                        ),
                        break_label,
                        continue_label
                    )
                );

                list.Add(convert.Invoke(instanceExp));

                var lambdaExp = Lambda<Func<DbDataReader, T>>(
                    Block(new ParameterExpression[] { iVar, dbVar, instanceExp }, list),
                    paramterExp
                );

                return lambdaExp.Compile();
            }

            private Func<DbDataReader, T> MakeNoArgumentsCtor(
                Type type,
                ConstructorInfo constructorInfo
            ) => MakeFor(type, constructorInfo, instanceExp => instanceExp);

            private Func<DbDataReader, T> MakeFor(
                ConstructorInfo constructorInfo,
                Func<NewExpression, Expression> convert
            )
            {
                var paramterExp = Parameter(typeof(DbDataReader));

                var dbVar = _adaper.DbVariable();

                var parameterInfos = constructorInfo.GetParameters();

                var arguments = new List<Expression>(parameterInfos.Length);

                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    var parameterInfo = parameterInfos[i];

                    var iVar = Constant(i);

                    var uncheckedValue = UncheckedValue(parameterInfo.ParameterType, dbVar, iVar);

                    arguments.Add(
                        Condition(
                            _adaper.IsDbNull(dbVar, iVar),
                            parameterInfo.ParameterType.IsValueType
                                ? Default(parameterInfo.ParameterType)
                                : Constant(null, parameterInfo.ParameterType),
                            uncheckedValue
                        )
                    );
                }

                var lambdaExp = Lambda<Func<DbDataReader, T>>(
                    Block(
                        new ParameterExpression[] { dbVar },
                        Assign(dbVar, _adaper.Convert(paramterExp)),
                        convert.Invoke(New(constructorInfo, arguments))
                    ),
                    paramterExp
                );

                return lambdaExp.Compile();
            }

            private Func<DbDataReader, T> MakeCtor(ConstructorInfo constructorInfo) =>
                MakeFor(constructorInfo, newExp => newExp);

            private Expression UncheckedValue(Type type, ParameterExpression dbVar, Expression iVar)
            {
                bool isEnum = false;
                bool isNullable = false;

                Type propertyType = type;
                Type nonullableType = type;

                if (propertyType.IsValueType)
                {
                    if (propertyType.IsNullable())
                    {
                        isNullable = true;

                        propertyType = nonullableType = Nullable.GetUnderlyingType(propertyType);
                    }

                    if (propertyType.IsEnum)
                    {
                        isEnum = true;

                        propertyType = Enum.GetUnderlyingType(propertyType);
                    }
                }

                Expression body = _adaper.ToSolve(propertyType, dbVar, iVar);

                if (isEnum)
                {
                    body = Convert(body, nonullableType);
                }

                if (isNullable)
                {
                    body = New(type.GetConstructor(new Type[] { nonullableType }), body);
                }

                return body;
            }

            private SwitchCase SwitchCaseAssign(
                Expression instanceExp,
                PropertyInfo propertyItem,
                ParameterExpression dbVar,
                ParameterExpression iVar
            )
            {
                Expression body = UncheckedValue(propertyItem.PropertyType, dbVar, iVar);

                var testValues = new List<Expression>(1) { Constant(propertyItem.Name) };

                return SwitchCase(
                    IfThen(
                        Not(_adaper.IsDbNull(dbVar, iVar)),
                        Assign(Property(instanceExp, propertyItem), body)
                    ),
                    testValues
                );
            }

            public DbMapper<T> CreateMap()
            {
                var type = typeof(T);

                if (type.IsSimple() || type.FullName == LinqBinary)
                {
                    return new DbMapper<T>(MakeSimple(type), true);
                }

                if (type.IsNullable())
                {
                    var conversionType = Nullable.GetUnderlyingType(type);

                    if (conversionType.IsSimple())
                    {
                        return new DbMapper<T>(MakeSimpleNull(conversionType, type), false);
                    }

                    return new DbMapper<T>(MakeNull(conversionType, type), false);
                }

                //? 无参构造函数。
                var nonCtor = type.GetConstructor(
                    BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.Instance
                        | BindingFlags.DeclaredOnly,
                    null,
                    Type.EmptyTypes,
                    null
                );

                if (nonCtor is null)
                {
                    var constructorInfos = type.GetConstructors(
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
                    );

                    foreach (var constructorInfo in constructorInfos)
                    {
                        return new DbMapper<T>(MakeCtor(constructorInfo), false);
                    }
                }

                return new DbMapper<T>(MakeNoArgumentsCtor(type, nonCtor), false);
            }
        }

        private interface IDbMapper
        {
            object Map(DbDataReader reader);

            bool IsInvalid(DbDataReader reader);

            Task<bool> IsInvalidAsync(DbDataReader reader, CancellationToken cancellationToken);
        }

        private class DbMapper<T> : IDbMapper
        {
            private readonly Func<DbDataReader, T> _read;
            private readonly bool _useDefault;

            public DbMapper(Func<DbDataReader, T> read, bool useDefault)
            {
                _read = read;
                _useDefault = useDefault;
            }

            public T Map(DbDataReader reader) => _read.Invoke(reader);

            public bool IsInvalid(DbDataReader reader) => _useDefault && reader.IsDBNull(0);

            public Task<bool> IsInvalidAsync(DbDataReader reader, CancellationToken cancellationToken)
            {
                return _useDefault ? reader.IsDBNullAsync(0, cancellationToken) : Task.FromResult(false);
            }

            object IDbMapper.Map(DbDataReader reader) => _read.Invoke(reader);
        }
        #endregion
    }
}
