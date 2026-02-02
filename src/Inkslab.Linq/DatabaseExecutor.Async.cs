using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Linq.Exceptions;
using Microsoft.Extensions.Logging;

namespace Inkslab.Linq
{
    /// <summary>
    /// 数据库执行器 - 异步部分
    /// </summary>
    public partial class DatabaseExecutor
    {
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
        public IAsyncEnumerable<T> QueryAsync<T>(IConnection databaseStrings, CommandSql commandSql)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(commandSql.ToString());
            }

            return new AsyncEnumerable<T>(databaseStrings, _connectionPipeline, commandSql);
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
                await CleanupQueryMultipleResourcesAsync(reader, command, dbConnection, isClosedConnection);
                throw;
            }
        }

        /// <summary>
        /// 执行 QueryMultiple 的异步资源清理
        /// </summary>
        private async Task CleanupQueryMultipleResourcesAsync(DbDataReader reader, DbCommand command, DbConnection connection, bool isClosedConnection)
        {
            if (reader != null)
            {
                if (!reader.IsClosed)
                {
                    try { command?.Cancel(); } catch { }
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
        public async Task<int> WriteToServerAsync(IConnection databaseStrings, DataTable dt, int? commandTimeout, CancellationToken cancellationToken = default)
        {
            if (dt is null)
            {
                throw new ArgumentNullException(nameof(dt));
            }

            if (string.IsNullOrEmpty(dt.TableName))
            {
                throw new ArgumentException("请通过\"DataTable.TableName\"指定目标表名称！");
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

        #region 异步枚举器
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
        #endregion

        #region 异步执行器
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
                    throw new ArgumentException("请通过\"DataTable.TableName\"指定目标表名称！");
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

                    _stopwatch.Start();

                    int influenceRows = await command.ExecuteNonQueryAsync(_cancellationToken);

                    _stopwatch.Stop();

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
                    throw new ArgumentException("请通过\"DataTable.TableName\"指定目标表名称！");
                }

                commandTimeout = commandTimeout.HasValue
                    ? Math.Min(_commandTimeout - (int)(_stopwatch.ElapsedMilliseconds / 1000), commandTimeout.Value)
                    : _commandTimeout - (int)(_stopwatch.ElapsedMilliseconds / 1000);

                _stopwatch.Start();

                int influenceRows = 0;

                using (var bulkCopy = await _pipeline.CreateAsync(_connection, _engine, _cancellationToken))
                {
                    if (commandTimeout.HasValue)
                    {
                        bulkCopy.BulkCopyTimeout = commandTimeout.Value;
                    }

                    influenceRows = await bulkCopy.WriteToServerAsync(dt, _cancellationToken);
                }

                _stopwatch.Stop();

                RowsExecuted += influenceRows;

                return influenceRows;
            }
        }
        #endregion

        #region 异步读取器
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

            private int _gridIndex;
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
        #endregion
    }
}
