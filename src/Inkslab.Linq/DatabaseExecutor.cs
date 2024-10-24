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
using Inkslab.Map;
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
        private readonly IBulkAssistant _assistant;
        private readonly ILogger<DatabaseExecutor> _logger;

        /// <summary>
        /// 数据库链接。
        /// </summary>
        /// <param name="connectionPipeline">链接管道。</param>
        /// <param name="assistant">批处理助手。</param>
        /// <param name="logger">日志。</param>
        public DatabaseExecutor(IDbConnectionPipeline connectionPipeline, IBulkAssistant assistant, ILogger<DatabaseExecutor> logger)
        {
            _connectionPipeline = connectionPipeline;
            _assistant = assistant;
            _logger = logger;
        }

        /// <inheritdoc/>
        public int Execute(string connectionStrings, CommandSql commandSql)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(commandSql.ToString());
            }

            using var connection = _connectionPipeline.Get(connectionStrings);

            bool isClosedConnection = connection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                connection.Open();
            }

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = commandSql.Text;

                    if (commandSql.Timeout.HasValue)
                    {
                        command.CommandTimeout = commandSql.Timeout.Value;
                    }

                    foreach (var (name, value) in commandSql.Parameters)
                    {
                        LookupDb.AddParameterAuto(command, name, value);
                    }

                    return command.ExecuteNonQuery();
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
        public async Task<int> ExecuteAsync(
            string connectionStrings,
            CommandSql commandSql,
            CancellationToken cancellationToken = default
        )
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(commandSql.ToString());
            }

            await using var connection = _connectionPipeline.Get(connectionStrings);

            bool isClosedConnection = connection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = commandSql.Text;

                    if (commandSql.Timeout.HasValue)
                    {
                        command.CommandTimeout = commandSql.Timeout.Value;
                    }

                    foreach (var (name, value) in commandSql.Parameters)
                    {
                        LookupDb.AddParameterAuto(command, name, value);
                    }

                    return await command.ExecuteNonQueryAsync(cancellationToken);
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

        /// <inheritdoc/>
        public List<T> Query<T>(string connectionStrings, CommandSql commandSql)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(commandSql.ToString());
            }

            CommandBehavior behavior =
                CommandBehavior.SequentialAccess | CommandBehavior.SingleResult;

            using var connection = _connectionPipeline.Get(connectionStrings);

            if (connection.State == ConnectionState.Closed)
            {
                behavior |= CommandBehavior.CloseConnection;

                connection.Open();
            }

            var results = new List<T>();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandSql.Text;

                if (commandSql.Timeout.HasValue)
                {
                    command.CommandTimeout = commandSql.Timeout.Value;
                }

                foreach (var (name, value) in commandSql.Parameters)
                {
                    LookupDb.AddParameterAuto(command, name, value);
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
            }

            return results;
        }

        /// <inheritdoc/>
        public IDbGridReader QueryMultiple(string connectionStrings, CommandSql commandSql)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(commandSql.ToString());
            }

            CommandBehavior behavior = CommandBehavior.SequentialAccess;

            var connection = _connectionPipeline.Get(connectionStrings);

            bool isClosedConnection = connection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                behavior |= CommandBehavior.CloseConnection;

                connection.Open();
            }

            DbCommand command = null;
            DbDataReader reader = null;

            try
            {
                command = connection.CreateCommand();

                command.CommandText = commandSql.Text;

                if (commandSql.Timeout.HasValue)
                {
                    command.CommandTimeout = commandSql.Timeout.Value;
                }

                foreach (var (name, value) in commandSql.Parameters)
                {
                    LookupDb.AddParameterAuto(command, name, value);
                }

                reader = command.ExecuteReader(behavior);

                var adaper = _adapters.GetOrAdd(
                        reader.GetType(),
                        type => new MapAdaper(type)
                    );

                return new DbGridReader(connection, command, reader, adaper);
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
                    connection.Close();
                }

                connection.Dispose();

                throw;
            }
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<T> QueryAsync<T>(string connectionStrings, CommandSql commandSql)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(commandSql.ToString());
            }

            return new AsyncEnumerable<T>(connectionStrings, _connectionPipeline, commandSql);
        }

        /// <inheritdoc/>
        public T Read<T>(string connectionStrings, CommandSql<T> commandSql)
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

            using var connection = _connectionPipeline.Get(connectionStrings);

            if (connection.State == ConnectionState.Closed)
            {
                behavior |= CommandBehavior.CloseConnection;

                connection.Open();
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = commandSql.Text;

                if (commandSql.Timeout.HasValue)
                {
                    command.CommandTimeout = commandSql.Timeout.Value;
                }

                foreach (var (name, value) in commandSql.Parameters)
                {
                    LookupDb.AddParameterAuto(command, name, value);
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
        }

        /// <inheritdoc/>
        public async Task<T> ReadAsync<T>(
            string connectionStrings,
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

            await using var connection = _connectionPipeline.Get(connectionStrings);

            if (connection.State == ConnectionState.Closed)
            {
                behavior |= CommandBehavior.CloseConnection;

                await connection.OpenAsync(cancellationToken);
            }

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = commandSql.Text;

                if (commandSql.Timeout.HasValue)
                {
                    command.CommandTimeout = commandSql.Timeout.Value;
                }

                foreach (var (name, value) in commandSql.Parameters)
                {
                    LookupDb.AddParameterAuto(command, name, value);
                }

                await using (
                    var reader = await command.ExecuteReaderAsync(behavior, cancellationToken)
                )
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
        }

        /// <inheritdoc/>
        public async Task<IAsyncDbGridReader> QueryMultipleAsync(string connectionStrings, CommandSql commandSql)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(commandSql.ToString());
            }

            CommandBehavior behavior = CommandBehavior.SequentialAccess;

            var connection = _connectionPipeline.Get(connectionStrings);

            bool isClosedConnection = connection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                behavior |= CommandBehavior.CloseConnection;

                await connection.OpenAsync();
            }

            DbCommand command = null;
            DbDataReader reader = null;

            try
            {
                command = connection.CreateCommand();

                command.CommandText = commandSql.Text;

                if (commandSql.Timeout.HasValue)
                {
                    command.CommandTimeout = commandSql.Timeout.Value;
                }

                foreach (var (name, value) in commandSql.Parameters)
                {
                    LookupDb.AddParameterAuto(command, name, value);
                }

                reader = await command.ExecuteReaderAsync(behavior);

                var adaper = _adapters.GetOrAdd(
                        reader.GetType(),
                        type => new MapAdaper(type)
                    );

                return new AsyncDbGridReader(connection, command, reader, adaper);
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
                    await connection.CloseAsync();
                }

                await connection.DisposeAsync();

                throw;
            }
        }

        /// <inheritdoc/>
        public int ExecuteMultiple(string connectionStrings, Action<IMultipleExecutor> multipleAction, int? commandTimeout)
        {
            if (multipleAction is null)
            {
                throw new ArgumentNullException(nameof(multipleAction));
            }

            using var connection = _connectionPipeline.Get(connectionStrings);

            bool isClosedConnection = connection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                connection.Open();
            }

            var multiple = new MultipleExecute(connection, _assistant, _logger);

            try
            {
                multipleAction.Invoke(multiple);

                return multiple.InfluenceRows;
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
        public async Task<int> ExecuteMultipleAsync(string connectionStrings, Func<IAsyncMultipleExecutor, Task> multipleAction, int? commandTimeout, CancellationToken cancellationToken = default)
        {
            if (multipleAction is null)
            {
                throw new ArgumentNullException(nameof(multipleAction));
            }

            using var connection = _connectionPipeline.Get(connectionStrings);

            bool isClosedConnection = connection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            var multiple = new MultipleExecuteAsync(connection, _assistant, _logger, cancellationToken);

            try
            {
                await multipleAction.Invoke(multiple);

                return multiple.InfluenceRows;
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
        public int WriteToServer(string connectionStrings, DataTable dt, int? commandTimeout = null)
        {
            if (dt is null)
            {
                throw new ArgumentNullException(nameof(dt));
            }

            if (string.IsNullOrEmpty(dt.TableName))
            {
                throw new ArgumentException("请通过“DataTable.TableName”指定目标表名称！");
            }

            using var connection = _connectionPipeline.Get(connectionStrings);

            bool isClosedConnection = connection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                connection.Open();
            }

            try
            {
                return WriteToServer(_assistant, connection, dt, commandTimeout);
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
        public async Task<int> WriteToServerAsync(string connectionStrings, DataTable dt, int? commandTimeout, CancellationToken cancellationToken = default)
        {
            if (dt is null)
            {
                throw new ArgumentNullException(nameof(dt));
            }

            if (string.IsNullOrEmpty(dt.TableName))
            {
                throw new ArgumentException("请通过“DataTable.TableName”指定目标表名称！");
            }

            using var connection = _connectionPipeline.Get(connectionStrings);

            bool isClosedConnection = connection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                return await WriteToServerAsync(_assistant, connection, dt, commandTimeout, cancellationToken);
            }
            finally
            {
                if (isClosedConnection)
                {
                    connection.Close();
                }
            }
        }

        private static int WriteToServer(IBulkAssistant assistant, DbConnection connection, DataTable dt, int? commandTimeout = null)
        {
            if (connection is ITransactionLink transactionLink)
            {
                var transaction = transactionLink.Transaction;

                if (transaction is null)
                {
                    return assistant.WriteToServer(transactionLink.Connection, dt, commandTimeout);
                }

                return assistant.WriteToServer(transactionLink.Connection, transaction, dt, commandTimeout);
            }

            return assistant.WriteToServer(connection, dt, commandTimeout);
        }

        private static Task<int> WriteToServerAsync(IBulkAssistant assistant, DbConnection connection, DataTable dt, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            if (connection is ITransactionLink transactionLink)
            {
                var transaction = transactionLink.Transaction;

                if (transaction is null)
                {
                    return assistant.WriteToServerAsync(transactionLink.Connection, dt, commandTimeout, cancellationToken);
                }

                return assistant.WriteToServerAsync(transactionLink.Connection, transaction, dt, commandTimeout, cancellationToken);
            }

            return assistant.WriteToServerAsync(connection, dt, commandTimeout, cancellationToken);
        }

        private class AsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly string _connectionStrings;
            private readonly IDbConnectionPipeline _connectionPipeline;
            private readonly CommandSql _commandSql;

            public AsyncEnumerable(string connectionStrings, IDbConnectionPipeline connectionPipeline, CommandSql commandSql)
            {
                _connectionStrings = connectionStrings;
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

                    if (_commandSql.Timeout.HasValue)
                    {
                        command.CommandTimeout = _commandSql.Timeout.Value;
                    }

                    foreach (var (name, value) in _commandSql.Parameters)
                    {
                        LookupDb.AddParameterAuto(command, name, value);
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
                }
            }
        }

        #region 执行器
        private class MultipleExecute : IMultipleExecutor
        {
            private readonly DbConnection _connection;
            private readonly IBulkAssistant _assistant;
            private readonly ILogger<DatabaseExecutor> _logger;

            public MultipleExecute(DbConnection connection, IBulkAssistant assistant, ILogger<DatabaseExecutor> logger)
            {
                _connection = connection;
                _assistant = assistant;
                _logger = logger;
            }

            public int InfluenceRows { private set; get; }

            public int Execute(CommandSql commandSql)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(commandSql.ToString());
                }

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = commandSql.Text;

                    if (commandSql.Timeout.HasValue)
                    {
                        command.CommandTimeout = commandSql.Timeout.Value;
                    }

                    foreach (var (name, value) in commandSql.Parameters)
                    {
                        LookupDb.AddParameterAuto(command, name, value);
                    }

                    int influenceRows = command.ExecuteNonQuery();

                    InfluenceRows += influenceRows;

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

                int influenceRows = DatabaseExecutor.WriteToServer(_assistant, _connection, dt, commandTimeout);

                InfluenceRows += influenceRows;

                return influenceRows;
            }
        }

        private class MultipleExecuteTimeout : IMultipleExecutor
        {
            private readonly DbConnection _connection;
            private readonly IBulkAssistant _assistant;
            private readonly int _commandTimeout;
            private readonly ILogger<DatabaseExecutor> _logger;
            private readonly Stopwatch _stopwatch;

            public MultipleExecuteTimeout(DbConnection connection, IBulkAssistant assistant, int commandTimeout, ILogger<DatabaseExecutor> logger)
            {
                _connection = connection;
                _assistant = assistant;
                _commandTimeout = commandTimeout;

                _logger = logger;

                _stopwatch = new Stopwatch();
            }

            public int InfluenceRows { private set; get; }

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

                    if (commandSql.Timeout.HasValue)
                    {
                        command.CommandTimeout = commandSql.Timeout.Value;
                    }

                    foreach (var (name, value) in commandSql.Parameters)
                    {
                        LookupDb.AddParameterAuto(command, name, value);
                    }

                    _stopwatch.Start();

                    int influenceRows = command.ExecuteNonQuery();

                    _stopwatch.Stop();

                    InfluenceRows += influenceRows;

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

                int influenceRows = DatabaseExecutor.WriteToServer(_assistant, _connection, dt, commandTimeout);

                _stopwatch.Stop();

                InfluenceRows += influenceRows;

                return influenceRows;
            }
        }

        private class MultipleExecuteAsync : IAsyncMultipleExecutor
        {
            private readonly DbConnection _connection;
            private readonly IBulkAssistant _assistant;
            private readonly ILogger<DatabaseExecutor> _logger;
            private readonly CancellationToken _cancellationToken;

            public MultipleExecuteAsync(DbConnection connection, IBulkAssistant assistant, ILogger<DatabaseExecutor> logger, CancellationToken cancellationToken)
            {
                _connection = connection;
                _assistant = assistant;
                _logger = logger;
                _cancellationToken = cancellationToken;
            }

            public int InfluenceRows { private set; get; }

            public async Task<int> ExecuteAsync(CommandSql commandSql)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(commandSql.ToString());
                }

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = commandSql.Text;

                    if (commandSql.Timeout.HasValue)
                    {
                        command.CommandTimeout = commandSql.Timeout.Value;
                    }

                    foreach (var (name, value) in commandSql.Parameters)
                    {
                        LookupDb.AddParameterAuto(command, name, value);
                    }

                    int influenceRows = await command.ExecuteNonQueryAsync(_cancellationToken);

                    InfluenceRows += influenceRows;

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

                int influenceRows = await DatabaseExecutor.WriteToServerAsync(_assistant, _connection, dt, commandTimeout, _cancellationToken);

                InfluenceRows += influenceRows;

                return influenceRows;
            }
        }

        private class MultipleExecuteTimeoutAsync : IAsyncMultipleExecutor
        {
            private readonly DbConnection _connection;
            private readonly IBulkAssistant _assistant;
            private readonly ILogger<DatabaseExecutor> _logger;
            private readonly int _commandTimeout;
            private readonly CancellationToken _cancellationToken;
            private readonly Stopwatch _stopwatch;

            public MultipleExecuteTimeoutAsync(DbConnection connection, IBulkAssistant assistant, ILogger<DatabaseExecutor> logger, int commandTimeout, CancellationToken cancellationToken)
            {
                _connection = connection;
                _assistant = assistant;
                _logger = logger;
                _commandTimeout = commandTimeout;
                _cancellationToken = cancellationToken;

                _stopwatch = new Stopwatch();
            }

            public int InfluenceRows { private set; get; }

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

                    if (commandSql.Timeout.HasValue)
                    {
                        command.CommandTimeout = commandSql.Timeout.Value;
                    }

                    foreach (var (name, value) in commandSql.Parameters)
                    {
                        LookupDb.AddParameterAuto(command, name, value);
                    }

                    int influenceRows = await command.ExecuteNonQueryAsync(_cancellationToken);

                    InfluenceRows += influenceRows;

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

                commandTimeout = commandTimeout.HasValue
                    ? Math.Min(_commandTimeout - (int)(_stopwatch.ElapsedMilliseconds / 1000), commandTimeout.Value)
                    : _commandTimeout - (int)(_stopwatch.ElapsedMilliseconds / 1000);

                int influenceRows = await DatabaseExecutor.WriteToServerAsync(_assistant, _connection, dt, commandTimeout, _cancellationToken);

                InfluenceRows += influenceRows;

                return influenceRows;
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
            private readonly MapAdaper _adaper;

            public AsyncDbGridReader(DbConnection connection, DbCommand command, DbDataReader reader, MapAdaper adaper)
            {
                _connection = connection;
                _command = command;
                _reader = reader;
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

                return new AsyncEnumerable<T>(this, gridIndex);
            }

            private async IAsyncEnumerator<T> ReadDeferredAsync<T>(int index, CancellationToken cancellationToken)
            {
                try
                {
                    var map = _adaper.CreateMap<T>();

                    while (index == gridIndex && await _reader.ReadAsync(cancellationToken))
                    {
                        if (!await map.IsInvalidAsync(_reader, cancellationToken))
                        {
                            yield return map.Map(_reader);
                        }
                    }
                }
                finally // finally so that First etc progresses things even when multiple rows
                {
                    if (index == gridIndex)
                    {
                        await NextResultAsync(cancellationToken);
                    }
                }
            }

            private int gridIndex; //, readCount;
            private volatile bool disposed;

            private async Task NextResultAsync(CancellationToken cancellationToken)
            {
                if (await _reader.NextResultAsync(cancellationToken))
                {
                    Interlocked.Increment(ref gridIndex);

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
                if (disposed)
                {
                    return;
                }

                disposed = true;

                if (!_reader.IsClosed)
                {
                    _command.Cancel();
                }

                await _reader.DisposeAsync();

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
            private readonly MapAdaper _adaper;

            public DbGridReader(DbConnection connection, DbCommand command, DbDataReader reader, MapAdaper adaper)
            {
                _connection = connection;
                _command = command;
                _reader = reader;
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

                return ReadDeferred<T>(gridIndex);
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

                    while (index == gridIndex && _reader.Read())
                    {
                        if (!map.IsInvalid(_reader))
                        {
                            yield return map.Map(_reader);
                        }
                    }
                }
                finally // finally so that First etc progresses things even when multiple rows
                {
                    if (index == gridIndex)
                    {
                        NextResult();
                    }
                }
            }

            private int gridIndex; //, readCount;

            private void NextResult()
            {
                if (_reader.NextResult())
                {
                    // readCount++;
                    gridIndex++;

                    IsConsumed = false;
                }
                else
                {
                    Dispose();
                }
            }

            private bool disposed;

            /// <summary>
            /// Dispose the grid, closing and disposing both the underlying reader and command.
            /// </summary>
            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;

                if (!_reader.IsClosed)
                {
                    _command.Cancel();
                }

                _reader.Dispose();

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
            private static readonly ConstructorInfo _errorCtor;
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
                [typeof(bool)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(char)] = TypeCode.Char,
                    [typeof(string)] = TypeCode.String,
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(long)] = TypeCode.Int64
                },
                [typeof(int)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(long)] = TypeCode.Int64 // 兼容 MySQL COUNT([*]) 返回的是长整型。
                },
                [typeof(long)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32
                },
                [typeof(float)] = new Dictionary<Type, TypeCode>
                {
                    [typeof(byte)] = TypeCode.Byte,
                    [typeof(short)] = TypeCode.Int16,
                    [typeof(ushort)] = TypeCode.UInt16,
                    [typeof(int)] = TypeCode.Int32,
                    [typeof(uint)] = TypeCode.UInt32,
                    [typeof(long)] = TypeCode.Int64,
                    [typeof(ulong)] = TypeCode.UInt64
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
                    [typeof(float)] = TypeCode.Single
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
                    [typeof(double)] = TypeCode.Decimal
                }
            };

            static MapAdaper()
            {
                _errorCtor = typeof(NotSupportedException).GetConstructor(new Type[] { Types.String });

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

                var type = typeof(Type);

                _typeCode = type.GetMethod(nameof(Type.GetTypeCode), BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly, null, new Type[] { type }, null);
            }

            private int refCount = 0;

            private volatile bool recovering = false;

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
                var throwUnary = Throw(New(_errorCtor, Call(_concat, Constant("数据库字段“"), GetName(dbVar, iVar), Constant("”的类型和实体属性的类型映射不被支持，请检查映射实体的属性类型！"))));

                if (!_typeMap.TryGetValue(propertyType, out MethodInfo originalFn))
                {
                    return throwUnary;
                }

                if (propertyType == Types.Object)
                {
                    return Call(dbVar, originalFn, iVar);
                }

                var typeVar = Variable(typeof(Type));
                var valueVar = Variable(propertyType);

                var variables = new List<ParameterExpression>
                {
                    typeVar,
                    valueVar
                };

                var expressions = new List<Expression>
                {
                    Assign(typeVar, Call(dbVar, _getFieldType, iVar))
                };

                if (!propertyType.IsValueType || !_typeTransforms.TryGetValue(propertyType, out var transforms))
                {
                    expressions.Add(IfThenElse(Equal(typeVar, Constant(propertyType)), Assign(valueVar, Call(dbVar, originalFn, iVar)), throwUnary));

                    expressions.Add(valueVar);

                    return Block(propertyType, variables, expressions);
                }

                var switchCases = new List<SwitchCase>(transforms.Count);

                if (propertyType == Types.Boolean)
                {
                    foreach (var (key, typeCode) in transforms)
                    {
                        if (_typeMap.TryGetValue(key, out var transformFn))
                        {
                            object comparisonValue = typeCode switch
                            {
                                TypeCode.Boolean => true,
                                TypeCode.Char => '1',
                                TypeCode.Int32 => 1,
                                TypeCode.UInt32 => 1U,
                                TypeCode.Int64 => 1L,
                                TypeCode.UInt64 => 1UL,
                                TypeCode.Single => 1F,
                                TypeCode.Double => 1D,
                                TypeCode.Decimal => 1M,
                                TypeCode.String => "1",
                                _ => System.Convert.ChangeType(1, key),
                            };

                            switchCases.Add(SwitchCase(Assign(valueVar, Equal(Call(dbVar, transformFn, iVar), Constant(comparisonValue, key))), Constant(typeCode)));
                        }
                    }

                    expressions.Add(IfThenElse(Equal(typeVar, Constant(propertyType)),
                        Assign(valueVar, Call(dbVar, originalFn, iVar)),
                        Switch(
                            typeof(void),
                            Call(_typeCode, typeVar),
                            throwUnary,
                            null,
                            switchCases.ToArray()
                        )));

                    expressions.Add(valueVar);

                    return Block(propertyType, variables, expressions);
                }

                foreach (var (key, typeCode) in transforms)
                {
                    if (_typeMap.TryGetValue(key, out var transformFn))
                    {
                        switchCases.Add(SwitchCase(Expression.Convert(Call(dbVar, transformFn, iVar), propertyType), Constant(typeCode)));
                    }
                }

                expressions.Add(IfThenElse(Equal(typeVar, Constant(propertyType)),
                    Assign(valueVar, Call(dbVar, originalFn, iVar)),
                    Switch(
                         typeof(void),
                        Call(_typeCode, typeVar),
                        throwUnary,
                        null,
                        switchCases.ToArray()
                    )));

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
                            if (Interlocked.Increment(ref refCount) >= COLLECT_PER_ITEMS)
                            {
                                if (recovering) { }
                                else
                                {
                                    recovering = true;

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
                                                recovering = false;

                                                Interlocked.Exchange(ref refCount, _mappers.Count);
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

                if (type.IsSimple())
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
