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
    public partial class DatabaseExecutor : IDatabaseExecutor
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

        #region 通用辅助方法

        /// <summary>
        /// 记录命令调试日志
        /// </summary>
        private void LogCommandIfDebug(CommandSql commandSql)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(commandSql.ToString());
            }
        }

        /// <summary>
        /// 配置数据库命令
        /// </summary>
        private void ConfigureCommand(DbCommand command, IConnection databaseStrings, CommandSql commandSql)
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
        }

        /// <summary>
        /// 执行带连接管理的操作
        /// </summary>
        private TResult ExecuteWithConnection<TResult>(IConnection databaseStrings, Func<DbConnection, bool, TResult> action)
        {
            using var dbConnection = _connectionPipeline.Get(databaseStrings);
            bool isClosedConnection = dbConnection.State == ConnectionState.Closed;

            if (isClosedConnection)
            {
                dbConnection.Open();
            }

            try
            {
                return action(dbConnection, isClosedConnection);
            }
            finally
            {
                if (isClosedConnection)
                {
                    dbConnection.Close();
                }
            }
        }

        /// <summary>
        /// 从 Reader 中读取并映射数据列表
        /// </summary>
        private List<T> MapReaderToList<T>(DbDataReader reader)
        {
            var results = new List<T>();

            if (reader.HasRows)
            {
                var adaper = _adapters.GetOrAdd(reader.GetType(), type => new MapAdaper(type));
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

            return results;
        }

        /// <summary>
        /// 从 Reader 中读取单个结果
        /// </summary>
        private T ReadSingleFromReader<T>(DbDataReader reader, CommandSql<T> commandSql)
        {
            if (reader.HasRows)
            {
                var adaper = _adapters.GetOrAdd(reader.GetType(), type => new MapAdaper(type));
                var map = adaper.CreateMap<T>();

                if (reader.Read())
                {
                    var result = map.Map(reader);

                    if (commandSql.RowStyle >= RowStyle.Single && reader.Read())
                    {
                        ThrowMultipleRows(commandSql.RowStyle);
                    }

                    return result;
                }
            }

            if (commandSql.HasDefaultValue || (commandSql.RowStyle & RowStyle.FirstOrDefault) == RowStyle.FirstOrDefault)
            {
                return commandSql.DefaultValue;
            }

            if (commandSql.CustomError)
            {
                throw new NoElementException(commandSql.NoElementError);
            }

            throw new InvalidOperationException("The input sequence contains more than one element.");
        }

        /// <summary>
        /// 执行 QueryMultiple 的资源清理
        /// </summary>
        private void CleanupQueryMultipleResources(DbDataReader reader, DbCommand command, DbConnection connection, bool isClosedConnection)
        {
            if (reader != null)
            {
                if (!reader.IsClosed)
                {
                    try { command?.Cancel(); } catch { }
                }
                reader.Dispose();
            }

            command?.Dispose();

            if (isClosedConnection)
            {
                connection.Close();
            }

            connection.Dispose();
        }

        #endregion

        /// <inheritdoc/>
        public int Execute(IConnection databaseStrings, CommandSql commandSql)
        {
            LogCommandIfDebug(commandSql);

            return ExecuteWithConnection(databaseStrings, (dbConnection, _) =>
            {
                using var command = dbConnection.CreateCommand();
                ConfigureCommand(command, databaseStrings, commandSql);

                var result = command.ExecuteNonQuery();
                commandSql.Callback(command);

                return result;
            });
        }

        /// <inheritdoc/>
        public List<T> Query<T>(IConnection databaseStrings, CommandSql commandSql)
        {
            LogCommandIfDebug(commandSql);

            CommandBehavior behavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult;

            using var dbConnection = _connectionPipeline.Get(databaseStrings);

            if (dbConnection.State == ConnectionState.Closed)
            {
                behavior |= CommandBehavior.CloseConnection;
                dbConnection.Open();
            }

            using var command = dbConnection.CreateCommand();
            ConfigureCommand(command, databaseStrings, commandSql);

            using var reader = command.ExecuteReader(behavior);
            var results = MapReaderToList<T>(reader);

            commandSql.Callback(command);

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
                CleanupQueryMultipleResources(reader, command, dbConnection, isClosedConnection);
                throw;
            }
        }

        /// <inheritdoc/>
        public T Read<T>(IConnection databaseStrings, CommandSql<T> commandSql)
        {
            LogCommandIfDebug(commandSql);

            CommandBehavior behavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult;

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

            using var command = dbConnection.CreateCommand();
            ConfigureCommand(command, databaseStrings, commandSql);

            try
            {
                using var reader = command.ExecuteReader(behavior);
                return ReadSingleFromReader(reader, commandSql);
            }
            finally
            {
                commandSql.Callback(command);
            }
        }

        /// <inheritdoc/>
        public int ExecuteMultiple(IConnection databaseStrings, Action<IMultipleExecutor> multipleAction, int? commandTimeout)
        {
            if (multipleAction is null)
            {
                throw new ArgumentNullException(nameof(multipleAction));
            }

            return ExecuteWithConnection(databaseStrings, (connection, _) =>
            {
                IMultipleExecutor multiple = new MultipleExecutor(
                    _connectionPipeline,
                    connection,
                    databaseStrings.Engine,
                    _logger,
                    commandTimeout);

                multipleAction.Invoke(multiple);

                return multiple.RowsExecuted;
            });
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

        #region 执行器
        private class MultipleExecutor : IMultipleExecutor
        {
            private readonly IDbConnectionPipeline _pipeline;
            private readonly DbConnection _connection;
            private readonly DatabaseEngine _engine;
            private readonly ILogger<DatabaseExecutor> _logger;
            private readonly int? _globalTimeout;
            private readonly Stopwatch _stopwatch;

            public MultipleExecutor(IDbConnectionPipeline pipeline, DbConnection connection, DatabaseEngine engine, ILogger<DatabaseExecutor> logger, int? globalTimeout)
            {
                _pipeline = pipeline;
                _connection = connection;
                _engine = engine;
                _logger = logger;
                _globalTimeout = globalTimeout;
                _stopwatch = globalTimeout.HasValue ? Stopwatch.StartNew() : null;
            }

            public int RowsExecuted { get; private set; }

            /// <summary>
            /// 应用全局超时策略到命令
            /// </summary>
            private void ApplyTimeoutPolicy(CommandSql commandSql)
            {
                if (_globalTimeout.HasValue)
                {
                    int remainingTimeout = _globalTimeout.Value - (int)(_stopwatch.ElapsedMilliseconds / 1000);
                    commandSql.Timeout = commandSql.Timeout.HasValue
                        ? Math.Min(remainingTimeout, commandSql.Timeout.Value)
                        : remainingTimeout;
                }
            }

            /// <summary>
            /// 配置数据库命令
            /// </summary>
            private void ConfigureCommand(DbCommand command, CommandSql commandSql)
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
            }

            public int Execute(CommandSql commandSql)
            {
                ApplyTimeoutPolicy(commandSql);

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(commandSql.ToString());
                }

                using var command = _connection.CreateCommand();
                ConfigureCommand(command, commandSql);

                int influenceRows = command.ExecuteNonQuery();
                commandSql.Callback(command);

                RowsExecuted += influenceRows;
                return influenceRows;
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
    }
}
