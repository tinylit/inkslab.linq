using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Transactions;
using IsolationLevel = System.Data.IsolationLevel;
using Transaction = System.Transactions.Transaction;
using OwnerTransaction = Inkslab.Transactions.Transaction;

namespace Inkslab.Linq
{
    /// <summary>
    /// 链接管道。
    /// </summary>
    public class DbConnectionPipeline : IDbConnectionPipeline
    {
        private readonly IConnections _connections;
        private readonly IEnumerable<IDbConnectionBulkCopyFactory> _bulkCopyFactories;

        /// <summary>
        /// 链接管道。
        /// </summary>
        /// <param name="connections">链接调度器。</param>
        /// <param name="bulkCopyFactories">批量复制工厂集合。</param>
        public DbConnectionPipeline(IConnections connections, IEnumerable<IDbConnectionBulkCopyFactory> bulkCopyFactories)
        {
            _connections = connections;
            _bulkCopyFactories = bulkCopyFactories;
        }

        /// <inheritdoc/>
        public DbConnection Get(IConnection databaseStrings)
        {
            if (databaseStrings is null)
            {
                throw new ArgumentException("数据库链接无效!", nameof(databaseStrings));
            }

            var serializable = Serializable.Current;

            var current = Transaction.Current;

            if (current is null) //? 不在事务范围中。
            {
                return OwnerTransactionConnections.Get(OwnerTransaction.Current, serializable, databaseStrings, _connections);
            }

            return TransactionConnections.Get(current, serializable, databaseStrings, _connections);
        }

        #region 事务。

        private static IsolationLevel ToIsolationLevel(Transactions.IsolationLevel isolationLevel) => isolationLevel switch
        {
            Transactions.IsolationLevel.Chaos => IsolationLevel.Chaos,
            Transactions.IsolationLevel.ReadUncommitted => IsolationLevel.ReadUncommitted,
            Transactions.IsolationLevel.ReadCommitted => IsolationLevel.ReadCommitted,
            Transactions.IsolationLevel.RepeatableRead => IsolationLevel.RepeatableRead,
            Transactions.IsolationLevel.Serializable => IsolationLevel.Serializable,
            Transactions.IsolationLevel.Snapshot => IsolationLevel.Snapshot,
            Transactions.IsolationLevel.Unspecified => IsolationLevel.Unspecified,
            _ => IsolationLevel.Unspecified,
        };

        /// <inheritdoc/>
        public IDatabaseBulkCopy Create(DbConnection connection, DatabaseEngine engine)
        {
            if (connection is null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            var bulkCopyFactory = _bulkCopyFactories.FirstOrDefault(x => x.Engine == engine) ?? throw new InvalidOperationException("未找到合适的批量复制工厂。");

            if (connection is IDatabase database)
            {
                return database.CreateBulkCopy(bulkCopyFactory);
            }

            return bulkCopyFactory.Create(connection);
        }

        /// <inheritdoc/>
        public async Task<IDatabaseBulkCopy> CreateAsync(DbConnection connection, DatabaseEngine engine, CancellationToken cancellationToken)
        {
            if (connection is null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            var bulkCopyFactory = _bulkCopyFactories.FirstOrDefault(x => x.Engine == engine) ?? throw new InvalidOperationException("未找到合适的批量复制工厂。");

            if (connection is IDatabase database)
            {
                return await database.CreateBulkCopyAsync(bulkCopyFactory, cancellationToken);
            }

            return bulkCopyFactory.Create(connection);
        }

        private interface IDatabase
        {
            IDatabaseBulkCopy CreateBulkCopy(IDbConnectionBulkCopyFactory bulkCopyFactory);
            Task<IDatabaseBulkCopy> CreateBulkCopyAsync(IDbConnectionBulkCopyFactory bulkCopyFactory, CancellationToken cancellationToken);
        }

        /// <summary>
        /// 事务连接池。
        /// </summary>
        private static class OwnerTransactionConnections
        {
            private static readonly ConcurrentDictionary<OwnerTransaction, Dictionary<string, TransactionEntry>> _transactionConnections = new ConcurrentDictionary<OwnerTransaction, Dictionary<string, TransactionEntry>>();

            private static TransactionEntry PrivateGet(OwnerTransaction transaction, Serializable serializable, IConnection databaseStrings, IConnections connections)
            {
                Dictionary<string, TransactionEntry> dictionary = _transactionConnections.GetOrAdd(transaction, transaction =>
                {
                    transaction.TransactionCompleted += OnTransactionCompleted;

                    return new Dictionary<string, TransactionEntry>();
                });

                if (dictionary.TryGetValue(databaseStrings.Strings, out TransactionEntry transactionEntry))
                {
                    return transactionEntry;
                }

                lock (dictionary)
                {
                    if (!dictionary.TryGetValue(databaseStrings.Strings, out transactionEntry))
                    {
                        dictionary.Add(databaseStrings.Strings,
                            transactionEntry = new TransactionEntry(transaction,
                                serializable is null
                                ? connections.Get(databaseStrings)
                                : serializable.Get(connections, databaseStrings)
                            )
                        );
                    }
                }

                return transactionEntry;
            }

            public static DbConnection Get(OwnerTransaction transaction, Serializable serializable, IConnection databaseStrings, IConnections connections)
            {
                if (transaction is null)
                {
                    return serializable is null
                        ? connections.Get(databaseStrings)
                        : new MyDatabase(serializable.Get(connections, databaseStrings));
                }

                var transactionEntry = PrivateGet(transaction, serializable, databaseStrings, connections);

                return transactionEntry.GetConnection();
            }

            private static void OnTransactionCompleted(object sender, TransactionEventArgs e)
            {
                if (_transactionConnections.TryRemove(e.Transaction, out Dictionary<string, TransactionEntry> dictionary))
                {
                    foreach (TransactionEntry connection in dictionary.Values)
                    {
                        connection.Dispose();
                    }

                    dictionary.Clear();
                }
            }

            #region 内嵌类。
            private class TransactionEntry : IDisposable
            {
                private readonly OwnerTransaction _transaction;
                private readonly DbConnection _connection;
                private readonly AsynchronousLock _asynchronousLock = new AsynchronousLock();

                public TransactionEntry(OwnerTransaction transaction, DbConnection connection)
                {
                    _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
                    _connection = connection ?? throw new ArgumentNullException(nameof(connection));
                }

                public DbTransaction Transaction { private set; get; }

                public DbConnection GetConnection() => new TransactionLink(this, _connection);

                public async ValueTask<DbTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
                {
                    if (Transaction != null)
                    {
                        return Transaction;
                    }

                    using (await _asynchronousLock.AcquireAsync(cancellationToken))
                    {
                        if (Transaction != null)
                        {
                            return Transaction;
                        }

                        Transaction = await _connection.BeginTransactionAsync(ToIsolationLevel(_transaction.IsolationLevel), cancellationToken);

                        _transaction.EnlistTransaction(new LinqTransaction(Transaction));

                    }

                    return Transaction;
                }

                public DbTransaction BeginTransaction()
                {
                    if (Transaction != null)
                    {
                        return Transaction;
                    }

                    using (_asynchronousLock.Acquire())
                    {
                        if (Transaction != null)
                        {
                            return Transaction;
                        }

                        Transaction = _connection.BeginTransaction(ToIsolationLevel(_transaction.IsolationLevel));

                        _transaction.EnlistTransaction(new LinqTransaction(Transaction));
                    }

                    return Transaction;
                }

                public void Dispose()
                {
                    if (_connection.State == ConnectionState.Open)
                    {
                        _connection.Close();
                    }

                    _connection.Dispose();
                }

                private class LinqTransaction : ITransaction
                {
                    private readonly DbTransaction _transaction;

                    public LinqTransaction(DbTransaction transaction)
                    {
                        _transaction = transaction;
                    }

                    public Task CommitAsync(CancellationToken cancellationToken = default) => _transaction.CommitAsync(cancellationToken);

                    public void Dispose() => _transaction.Dispose();

                    public ValueTask DisposeAsync() => _transaction.DisposeAsync();

                    public void Rollback() => _transaction.Rollback();

                    public Task RollbackAsync(CancellationToken cancellationToken = default) => _transaction.RollbackAsync(cancellationToken);
                }
            }

            private class TransactionLink : DbConnection, IDatabase
            {
                private readonly TransactionEntry _transaction;
                private readonly DbConnection _connection;

                private ConnectionState _connectionState = ConnectionState.Closed;

                public TransactionLink(TransactionEntry transaction, DbConnection connection)
                {
                    _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
                    _connection = connection ?? throw new ArgumentNullException(nameof(connection));
                }

                public override string ConnectionString { get => _connection.ConnectionString; set => _connection.ConnectionString = value; }

                public override string Database => _connection.Database;

                public override string DataSource => _connection.DataSource;

                public override string ServerVersion => _connection.ServerVersion;

                public override int ConnectionTimeout => _connection.ConnectionTimeout;

#if NET6_0_OR_GREATER
                [Obsolete]
#endif
                public override object InitializeLifetimeService() => _connection.InitializeLifetimeService();

                public override DataTable GetSchema() => _connection.GetSchema();

                public override DataTable GetSchema(string collectionName) => _connection.GetSchema(collectionName);

                public override DataTable GetSchema(string collectionName, string[] restrictionValues) => _connection.GetSchema(collectionName, restrictionValues);

                public override ISite Site { get => _connection.Site; set => _connection.Site = value; }

                public override event StateChangeEventHandler StateChange { add { _connection.StateChange += value; } remove { _connection.StateChange -= value; } }

                public override ConnectionState State => _connectionState == ConnectionState.Closed ? _connectionState : _connection.State;

                public override void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);

                public override void EnlistTransaction(Transaction transaction) => _connection.EnlistTransaction(transaction);

                public override void Close() => _connectionState = ConnectionState.Closed;

                public IDatabaseBulkCopy CreateBulkCopy(IDbConnectionBulkCopyFactory bulkCopyFactory)
                {
                    if (_connection.State == ConnectionState.Closed)
                    {
                        _connection.Open();
                    }

                    var transaction = _transaction.BeginTransaction();

                    return bulkCopyFactory.Create(_connection, transaction);
                }

                public async Task<IDatabaseBulkCopy> CreateBulkCopyAsync(IDbConnectionBulkCopyFactory bulkCopyFactory, CancellationToken cancellationToken)
                {
                    if (_connection.State == ConnectionState.Closed)
                    {
                        await _connection.OpenAsync(cancellationToken);
                    }

                    var transaction = await _transaction.BeginTransactionAsync(cancellationToken);

                    return bulkCopyFactory.Create(_connection, transaction);
                }

                public override void Open()
                {
                    switch (_connection.State)
                    {
                        case ConnectionState.Connecting:
                            do
                            {
                                Thread.Sleep(5);

                            } while (_connection.State == ConnectionState.Connecting);

                            goto default;
                        case ConnectionState.Broken:
                            _connection.Close();

                            goto default;
                        default:
                            if (_connection.State == ConnectionState.Closed)
                            {
                                _connection.Open();
                            }
                            break;
                    }

                    _connectionState = _connection.State;
                }

                public override async Task OpenAsync(CancellationToken cancellationToken)
                {
                    switch (_connection.State)
                    {
                        case ConnectionState.Connecting:
                            do
                            {
                                await Task.Delay(5, cancellationToken);

                            } while (State == ConnectionState.Connecting);

                            goto default;
                        case ConnectionState.Broken:
                            await _connection.CloseAsync();

                            goto default;
                        default:
                            if (_connection.State == ConnectionState.Closed)
                            {
                                await _connection.OpenAsync(cancellationToken);
                            }
                            break;
                    }

                    _connectionState = _connection.State;
                }

                public override Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken = default) => _connection.ChangeDatabaseAsync(databaseName, cancellationToken);

                protected override ValueTask<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken) => _connection.BeginTransactionAsync(isolationLevel, cancellationToken);

                public override Task CloseAsync()
                {
                    _connectionState = ConnectionState.Closed;

                    return Task.CompletedTask;
                }

                public override async ValueTask DisposeAsync()
                {
                    if (_disposed)
                    {
                        return;
                    }

                    _disposed = true;

                    await CloseAsync();

                    await base.DisposeAsync();
                }

                private bool _disposed;

                protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => _connection.BeginTransaction(isolationLevel);

                protected override System.Data.Common.DbCommand CreateDbCommand()
                {
                    var command = _connection.CreateCommand();

                    return new DbCommand(command, _transaction);
                }

                protected override void Dispose(bool disposing)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    _disposed = true;

                    Close();

                    base.Dispose(disposing);
                }

                private class DbCommand : System.Data.Common.DbCommand, IDbCommand
                {
                    private readonly TransactionEntry _transaction;
                    private readonly System.Data.Common.DbCommand _command;

                    public DbCommand(System.Data.Common.DbCommand command, TransactionEntry transaction)
                    {
                        _command = command;
                        _transaction = transaction;
                    }

                    public override string CommandText { get => _command.CommandText; set => _command.CommandText = value; }
                    public override int CommandTimeout { get => _command.CommandTimeout; set => _command.CommandTimeout = value; }
                    public override CommandType CommandType { get => _command.CommandType; set => _command.CommandType = value; }
                    public override bool DesignTimeVisible { get => _command.DesignTimeVisible; set => _command.DesignTimeVisible = value; }
                    public override UpdateRowSource UpdatedRowSource { get => _command.UpdatedRowSource; set => _command.UpdatedRowSource = value; }
                    protected override DbConnection DbConnection { get => _command.Connection; set => _command.Connection = value; }
                    protected override DbParameterCollection DbParameterCollection => _command.Parameters;
                    protected override DbTransaction DbTransaction { get => _command.Transaction; set => _command.Transaction = value; }
                    public override void Cancel() => _command.Cancel();
                    public override int ExecuteNonQuery()
                    {
                        _command.Transaction ??= _transaction.BeginTransaction();

                        return _command.ExecuteNonQuery();
                    }
                    public override object ExecuteScalar()
                    {
                        _command.Transaction ??= _transaction.BeginTransaction();

                        return _command.ExecuteScalar();
                    }
                    public override void Prepare() => _command.Prepare();

#if NET6_0_OR_GREATER
                    [Obsolete]
#endif
                    public override object InitializeLifetimeService() => _command.InitializeLifetimeService();
                    protected override DbParameter CreateDbParameter() => _command.CreateParameter();
                    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
                    {
                        _command.Transaction ??= _transaction.BeginTransaction();

                        return _command.ExecuteReader(behavior & ~CommandBehavior.CloseConnection);
                    }
                    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
                    {
                        _command.Transaction ??= await _transaction.BeginTransactionAsync(cancellationToken);

                        return await _command.ExecuteReaderAsync(behavior & ~CommandBehavior.CloseConnection, cancellationToken);
                    }

                    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
                    {
                        _command.Transaction ??= await _transaction.BeginTransactionAsync(cancellationToken);

                        return await _command.ExecuteNonQueryAsync(cancellationToken);
                    }

                    public override async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
                    {
                        _command.Transaction ??= await _transaction.BeginTransactionAsync(cancellationToken);

                        return await _command.ExecuteScalarAsync(cancellationToken);
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// 事务连接池。
        /// </summary>
        private static class TransactionConnections
        {
            private static readonly ConcurrentDictionary<Transaction, Dictionary<string, TransactionEntry>> _transactionConnections = new ConcurrentDictionary<Transaction, Dictionary<string, TransactionEntry>>();

            private static DbConnection PrivateGet(Serializable serializable, IConnection databaseStrings, IConnections connections)
            {
                return serializable is null ? connections.Get(databaseStrings) : serializable.Get(connections, databaseStrings);
            }

            private static TransactionEntry PrivateGet(Transaction transaction, Serializable serializable, IConnection databaseStrings, IConnections connections)
            {
                Dictionary<string, TransactionEntry> dictionary = _transactionConnections.GetOrAdd(transaction, transaction =>
                {
                    transaction.TransactionCompleted += OnTransactionCompleted;

                    return new Dictionary<string, TransactionEntry>();
                });

                if (dictionary.TryGetValue(databaseStrings.Strings, out TransactionEntry transactionEntry))
                {
                    return transactionEntry;
                }

                lock (dictionary)
                {
                    if (!dictionary.TryGetValue(databaseStrings.Strings, out transactionEntry))
                    {
                        dictionary.Add(databaseStrings.Strings, transactionEntry = new TransactionEntry(PrivateGet(serializable, databaseStrings, connections)));
                    }
                }

                return transactionEntry;
            }

            public static DbConnection Get(Transaction transaction, Serializable serializable, IConnection databaseStrings, IConnections connections)
            {
                var transactionEntry = PrivateGet(transaction, serializable, databaseStrings, connections);

                return transactionEntry.GetConnection();
            }

            private static void OnTransactionCompleted(object sender, System.Transactions.TransactionEventArgs e)
            {
                if (_transactionConnections.TryRemove(e.Transaction, out Dictionary<string, TransactionEntry> dictionary))
                {
                    foreach (TransactionEntry connection in dictionary.Values)
                    {
                        connection.Dispose();
                    }

                    dictionary.Clear();
                }
            }

            #region 内嵌类。
            private class TransactionEntry : IDisposable
            {
                private readonly DbConnection _connection;

                public TransactionEntry(DbConnection connection)
                {
                    _connection = connection ?? throw new ArgumentNullException(nameof(connection));
                }

                public DbConnection GetConnection() => new MyDatabase(_connection);

                public void Dispose()
                {
                    if (_connection.State == ConnectionState.Open)
                    {
                        _connection.Close();
                    }

                    _connection.Dispose();
                }
            }
            #endregion
        }

        private class MyDatabase : DbConnection, IDatabase
        {
            private readonly DbConnection _connection;
            private ConnectionState _connectionState = ConnectionState.Closed;

            public MyDatabase(DbConnection connection)
            {
                _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            }

            public override string ConnectionString { get => _connection.ConnectionString; set => _connection.ConnectionString = value; }

            public override string Database => _connection.Database;

            public override string DataSource => _connection.DataSource;

            public override string ServerVersion => _connection.ServerVersion;

            public override int ConnectionTimeout => _connection.ConnectionTimeout;
#if NET6_0_OR_GREATER
            [Obsolete]
#endif
            public override object InitializeLifetimeService() => _connection.InitializeLifetimeService();

            public override DataTable GetSchema() => _connection.GetSchema();

            public override DataTable GetSchema(string collectionName) => _connection.GetSchema(collectionName);

            public override DataTable GetSchema(string collectionName, string[] restrictionValues) => _connection.GetSchema(collectionName, restrictionValues);

            public override ISite Site { get => _connection.Site; set => _connection.Site = value; }

            public override event StateChangeEventHandler StateChange { add { _connection.StateChange += value; } remove { _connection.StateChange -= value; } }

            public override ConnectionState State => _connectionState == ConnectionState.Closed ? _connectionState : _connection.State;

            public override void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);

            public override void EnlistTransaction(Transaction transaction) => _connection.EnlistTransaction(transaction);

            public override void Close() => _connectionState = ConnectionState.Closed;

            public override void Open()
            {
                switch (_connection.State)
                {
                    case ConnectionState.Connecting:
                        do
                        {
                            Thread.Sleep(5);

                        } while (_connection.State == ConnectionState.Connecting);

                        goto default;
                    case ConnectionState.Broken:
                        _connection.Close();

                        goto default;
                    default:
                        if (_connection.State == ConnectionState.Closed)
                        {
                            _connection.Open();
                        }
                        break;
                }

                _connectionState = _connection.State;
            }

            public override async Task OpenAsync(CancellationToken cancellationToken)
            {
                switch (_connection.State)
                {
                    case ConnectionState.Connecting:
                        do
                        {
                            await Task.Delay(5, cancellationToken);

                        } while (State == ConnectionState.Connecting);

                        goto default;
                    case ConnectionState.Broken:
                        await _connection.CloseAsync();

                        goto default;
                    default:
                        if (_connection.State == ConnectionState.Closed)
                        {
                            await _connection.OpenAsync(cancellationToken);
                        }
                        break;
                }

                _connectionState = _connection.State;
            }

            public override Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken = default) => _connection.ChangeDatabaseAsync(databaseName, cancellationToken);

            protected override ValueTask<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken) => _connection.BeginTransactionAsync(isolationLevel, cancellationToken);

            public override Task CloseAsync()
            {
                _connectionState = ConnectionState.Closed;

                return Task.CompletedTask;
            }

            public override async ValueTask DisposeAsync()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                await CloseAsync();

                await base.DisposeAsync();
            }

            private volatile bool _disposed;

            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => _connection.BeginTransaction(isolationLevel);

            protected override System.Data.Common.DbCommand CreateDbCommand() => new DbCommand(_connection.CreateCommand());

            protected override void Dispose(bool disposing)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                Close();

                base.Dispose(disposing);
            }

            public IDatabaseBulkCopy CreateBulkCopy(IDbConnectionBulkCopyFactory bulkCopyFactory)
            {
                if (_connection.State == ConnectionState.Closed)
                {
                    _connection.Open();
                }

                return bulkCopyFactory.Create(_connection);
            }

            public async Task<IDatabaseBulkCopy> CreateBulkCopyAsync(IDbConnectionBulkCopyFactory bulkCopyFactory, CancellationToken cancellationToken)
            {
                if (_connection.State == ConnectionState.Closed)
                {
                    await _connection.OpenAsync(cancellationToken);
                }

                return bulkCopyFactory.Create(_connection);
            }

            private class DbCommand : System.Data.Common.DbCommand, IDbCommand
            {
                private readonly System.Data.Common.DbCommand _command;

                public DbCommand(System.Data.Common.DbCommand command)
                {
                    _command = command;
                }

                public override string CommandText { get => _command.CommandText; set => _command.CommandText = value; }
                public override int CommandTimeout { get => _command.CommandTimeout; set => _command.CommandTimeout = value; }
                public override CommandType CommandType { get => _command.CommandType; set => _command.CommandType = value; }
                public override bool DesignTimeVisible { get => _command.DesignTimeVisible; set => _command.DesignTimeVisible = value; }
                public override UpdateRowSource UpdatedRowSource { get => _command.UpdatedRowSource; set => _command.UpdatedRowSource = value; }
                protected override DbConnection DbConnection { get => _command.Connection; set => _command.Connection = value; }
                protected override DbParameterCollection DbParameterCollection => _command.Parameters;
                protected override DbTransaction DbTransaction { get => _command.Transaction; set => _command.Transaction = value; }
                public override void Cancel() => _command.Cancel();
                public override int ExecuteNonQuery() => _command.ExecuteNonQuery();
                public override object ExecuteScalar() => _command.ExecuteScalar();
                public override void Prepare() => _command.Prepare();
#if NET6_0_OR_GREATER
                [Obsolete]
#endif
                public override object InitializeLifetimeService() => _command.InitializeLifetimeService();
                protected override DbParameter CreateDbParameter() => _command.CreateParameter();
                protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => _command.ExecuteReader(behavior & ~CommandBehavior.CloseConnection);
                protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) => _command.ExecuteReaderAsync(behavior & ~CommandBehavior.CloseConnection, cancellationToken);
                public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => _command.ExecuteNonQueryAsync(cancellationToken);
                public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken) => _command.ExecuteScalarAsync(cancellationToken);
            }
        }
        #endregion
    }
}
