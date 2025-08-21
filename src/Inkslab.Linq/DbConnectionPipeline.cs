using Inkslab.Transactions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
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

        /// <summary>
        /// 链接管道。
        /// </summary>
        /// <param name="connections">链接调度器。</param>
        public DbConnectionPipeline(IConnections connections)
        {
            _connections = connections;
        }

        /// <inheritdoc/>
        public DbConnection Get(IConnection databaseStrings)
        {
            if (databaseStrings is null)
            {
                throw new ArgumentException("数据库链接无效!", nameof(databaseStrings));
            }

            var current = Transaction.Current;

            if (current is null) //? 不在事务范围中。
            {
                var transcation = OwnerTransaction.Current;

                return transcation is null
                    ? _connections.Get(databaseStrings)
                    : OwnerTransactionConnections.Get(transcation, databaseStrings, _connections);
            }

            return TransactionConnections.Get(current, databaseStrings, _connections);
        }

        #region 事务。

        private static System.Data.IsolationLevel ToIsolationLevel(Transactions.IsolationLevel isolationLevel) => isolationLevel switch
        {
            Transactions.IsolationLevel.Chaos => System.Data.IsolationLevel.Chaos,
            Transactions.IsolationLevel.ReadUncommitted => System.Data.IsolationLevel.ReadUncommitted,
            Transactions.IsolationLevel.ReadCommitted => System.Data.IsolationLevel.ReadCommitted,
            Transactions.IsolationLevel.RepeatableRead => System.Data.IsolationLevel.RepeatableRead,
            Transactions.IsolationLevel.Serializable => System.Data.IsolationLevel.Serializable,
            Transactions.IsolationLevel.Snapshot => System.Data.IsolationLevel.Snapshot,
            Transactions.IsolationLevel.Unspecified => System.Data.IsolationLevel.Unspecified,
            _ => System.Data.IsolationLevel.Unspecified,
        };

        /// <summary>
        /// 事务连接池。
        /// </summary>
        private static class OwnerTransactionConnections
        {
            private static readonly ConcurrentDictionary<OwnerTransaction, Dictionary<string, TransactionEntry>> _transactionConnections = new ConcurrentDictionary<OwnerTransaction, Dictionary<string, TransactionEntry>>();

            public static bool TryGet(OwnerTransaction transaction, string connectionString, out DbConnection connection)
            {
                if (_transactionConnections.TryGetValue(transaction, out var dictionary) && dictionary.TryGetValue(connectionString, out TransactionEntry info))
                {
                    connection = info.GetConnection();

                    return true;
                }

                connection = null;

                return false;
            }

            public static DbConnection Get(OwnerTransaction transaction, IConnection databaseStrings, IConnections connections)
            {
                Dictionary<string, TransactionEntry> dictionary = _transactionConnections.GetOrAdd(transaction, transaction =>
                {
                    transaction.TransactionCompleted += OnTransactionCompleted;

                    return new Dictionary<string, TransactionEntry>();
                });

                if (dictionary.TryGetValue(databaseStrings.Strings, out TransactionEntry info))
                {
                    return info.GetConnection();
                }

                lock (dictionary)
                {
                    if (!dictionary.TryGetValue(databaseStrings.Strings, out info))
                    {
                        dictionary.Add(databaseStrings.Strings, info = new TransactionEntry(transaction, connections.Get(databaseStrings)));
                    }
                }

                return info.GetConnection();
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
                    if (Transaction is null)
                    {
                        using (await _asynchronousLock.AcquireAsync(cancellationToken))
                        {
                            if (Transaction is null)
                            {
                                Transaction = await _connection.BeginTransactionAsync(ToIsolationLevel(_transaction.IsolationLevel), cancellationToken);

                                _transaction.EnlistTransaction(new LinqTransaction(Transaction));
                            }
                        }
                    }

                    return Transaction;
                }

                public DbTransaction BeginTransaction()
                {
                    if (Transaction is null)
                    {
                        using (_asynchronousLock.Acquire())
                        {
                            if (Transaction is null)
                            {
                                Transaction = _connection.BeginTransaction(ToIsolationLevel(_transaction.IsolationLevel));

                                _transaction.EnlistTransaction(new LinqTransaction(Transaction));
                            }
                        }
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
            private class TransactionLink : DbConnection, ITransactionLink
            {
                private readonly TransactionEntry _transaction;
                private readonly DbConnection _connection;

                private ConnectionState connectionState = ConnectionState.Closed;

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

                public DbTransaction Transaction => _transaction.BeginTransaction();

                public DbConnection Connection => _connection;

#if NET6_0_OR_GREATER
                [Obsolete]
#endif
                public override object InitializeLifetimeService() => _connection.InitializeLifetimeService();

                public override DataTable GetSchema() => _connection.GetSchema();

                public override DataTable GetSchema(string collectionName) => _connection.GetSchema(collectionName);

                public override DataTable GetSchema(string collectionName, string[] restrictionValues) => _connection.GetSchema(collectionName, restrictionValues);

                public override ISite Site { get => _connection.Site; set => _connection.Site = value; }

                public override event StateChangeEventHandler StateChange { add { _connection.StateChange += value; } remove { _connection.StateChange -= value; } }

                public override ConnectionState State => connectionState == ConnectionState.Closed ? connectionState : _connection.State;

                public override void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);

                public override void EnlistTransaction(Transaction transaction) => _connection.EnlistTransaction(transaction);

                public override void Close() => connectionState = ConnectionState.Closed;

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

                    connectionState = _connection.State;
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

                    connectionState = _connection.State;
                }

                public override Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken = default) => _connection.ChangeDatabaseAsync(databaseName, cancellationToken);

                protected override ValueTask<DbTransaction> BeginDbTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken) => _connection.BeginTransactionAsync(isolationLevel, cancellationToken);

                public override Task CloseAsync()
                {
                    connectionState = ConnectionState.Closed;

                    return Task.CompletedTask;
                }

                public override async ValueTask DisposeAsync()
                {
                    if (disposed)
                    {
                        return;
                    }

                    disposed = true;

                    await CloseAsync();

                    await base.DisposeAsync();
                }

                private volatile bool disposed;

                protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel) => _connection.BeginTransaction(isolationLevel);

                protected override System.Data.Common.DbCommand CreateDbCommand()
                {
                    var command = _connection.CreateCommand();

                    return new DbCommand(command, _transaction);
                }

                protected override void Dispose(bool disposing)
                {
                    if (disposed)
                    {
                        return;
                    }

                    disposed = true;

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

            public static DbConnection Get(Transaction transaction, IConnection databaseStrings, IConnections connections)
            {
                Dictionary<string, TransactionEntry> dictionary = _transactionConnections.GetOrAdd(transaction, transaction =>
                {
                    transaction.TransactionCompleted += OnTransactionCompleted;

                    return new Dictionary<string, TransactionEntry>();
                });

                if (dictionary.TryGetValue(databaseStrings.Strings, out TransactionEntry info))
                {
                    return info.GetConnection();
                }

                lock (dictionary)
                {
                    if (!dictionary.TryGetValue(databaseStrings.Strings, out info))
                    {
                        dictionary.Add(databaseStrings.Strings, info = new TransactionEntry(connections.Get(databaseStrings)));
                    }
                }

                return info.GetConnection();
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

                public DbConnection GetConnection() => new TransactionLink(_connection);

                public void Dispose()
                {
                    if (_connection.State == ConnectionState.Open)
                    {
                        _connection.Close();
                    }

                    _connection.Dispose();
                }
            }
            private class TransactionLink : DbConnection, ITransactionLink
            {
                private readonly DbConnection _connection;
                private ConnectionState connectionState = ConnectionState.Closed;

                public TransactionLink(DbConnection connection)
                {
                    _connection = connection ?? throw new ArgumentNullException(nameof(connection));
                }

                public override string ConnectionString { get => _connection.ConnectionString; set => _connection.ConnectionString = value; }

                public override string Database => _connection.Database;

                public override string DataSource => _connection.DataSource;

                public override string ServerVersion => _connection.ServerVersion;

                public override int ConnectionTimeout => _connection.ConnectionTimeout;

                public DbTransaction Transaction => null;

                public DbConnection Connection => _connection;
#if NET6_0_OR_GREATER
                [Obsolete]
#endif
                public override object InitializeLifetimeService() => _connection.InitializeLifetimeService();

                public override DataTable GetSchema() => _connection.GetSchema();

                public override DataTable GetSchema(string collectionName) => _connection.GetSchema(collectionName);

                public override DataTable GetSchema(string collectionName, string[] restrictionValues) => _connection.GetSchema(collectionName, restrictionValues);

                public override ISite Site { get => _connection.Site; set => _connection.Site = value; }

                public override event StateChangeEventHandler StateChange { add { _connection.StateChange += value; } remove { _connection.StateChange -= value; } }

                public override ConnectionState State => connectionState == ConnectionState.Closed ? connectionState : _connection.State;

                public override void ChangeDatabase(string databaseName) => _connection.ChangeDatabase(databaseName);

                public override void EnlistTransaction(Transaction transaction) => _connection.EnlistTransaction(transaction);

                public override void Close() => connectionState = ConnectionState.Closed;

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

                    connectionState = _connection.State;
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

                    connectionState = _connection.State;
                }

                public override Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken = default) => _connection.ChangeDatabaseAsync(databaseName, cancellationToken);

                protected override ValueTask<DbTransaction> BeginDbTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken) => _connection.BeginTransactionAsync(isolationLevel, cancellationToken);

                public override Task CloseAsync()
                {
                    connectionState = ConnectionState.Closed;

                    return Task.CompletedTask;
                }

                public override async ValueTask DisposeAsync()
                {
                    if (disposed)
                    {
                        return;
                    }

                    disposed = true;

                    await CloseAsync();

                    await base.DisposeAsync();
                }

                private volatile bool disposed;

                protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel) => _connection.BeginTransaction(isolationLevel);

                protected override System.Data.Common.DbCommand CreateDbCommand() => new DbCommand(_connection.CreateCommand());

                protected override void Dispose(bool disposing)
                {
                    if (disposed)
                    {
                        return;
                    }

                    disposed = true;

                    Close();

                    base.Dispose(disposing);
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
        #endregion
    }
}
