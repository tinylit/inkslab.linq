using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Transactions
{
    /// <summary>
    /// 事务提交事件。
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void TransactionCompletedEventHandler(object sender, TransactionEventArgs e);

    /// <summary>
    /// 事务。
    /// </summary>
    public sealed class Transaction : IEqualityComparer<Transaction>, ITransaction, IAsyncDisposable, IDisposable
    {
        private static readonly AsyncLocal<TransactionHolder> _transactionCurrent = new AsyncLocal<TransactionHolder>();

        private readonly List<IDelivery> _deliveries = new List<IDelivery>();
        private readonly List<ITransaction> _transactions = new List<ITransaction>(1);
        private readonly object _syncRoot = new object();

        /// <summary>
        /// 事务。
        /// </summary>
        /// <param name="isolationLevel">隔离级别。</param>
        public Transaction(IsolationLevel isolationLevel)
        {
            if (!Enum.IsDefined(typeof(IsolationLevel), isolationLevel))
            {
                throw new ArgumentOutOfRangeException(nameof(isolationLevel));
            }

            TransactionId = Guid.NewGuid();

            IsolationLevel = isolationLevel;
        }

        /// <summary>
        /// 当前事务。
        /// </summary>
        public static Transaction Current
        {
            get
            {
                var transactionHolder = _transactionCurrent.Value;

                if (transactionHolder is null)
                {
                    return null;
                }

                var transaction = transactionHolder.Transaction;

                if (transaction is null || transaction.Status > TransactionStatus.Active)
                {
                    return null;
                }

                return transaction;
            }

            set
            {
                if (value is null)
                {
                    _transactionCurrent.Value = null;
                }
                else
                {
                    _transactionCurrent.Value = new TransactionHolder { Transaction = value };
                }
            }
        }

        /// <summary>
        /// 委托，若当前存在事务，则在事务提交成功时，交付；否则，自动完成。
        /// </summary>
        /// <param name="delivery">交付。</param>
        /// <exception cref="ArgumentNullException">参数“<paramref name="delivery"/>”为<see langword="null"/>！</exception>
        public static void Delegation(IDelivery delivery)
        {
            if (delivery is null)
            {
                throw new ArgumentNullException(nameof(delivery));
            }

            var transaction = Current;

            if (transaction is null)
            {
                delivery.Done();
            }
            else
            {
                transaction.RegisterDelivery(delivery);
            }
        }

        /// <summary>
        /// 注册交付，将在事务提交成功时，触发“<see cref="IDelivery.Done()"/>”方法完成交付。
        /// </summary>
        /// <param name="delivery">交付。</param>
        public void RegisterDelivery(IDelivery delivery)
        {
            if (delivery is null)
            {
                throw new ArgumentNullException(nameof(delivery));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            if (_complete)
            {
                throw new InvalidOperationException("事务已完成，无法注册新的交付。");
            }

            lock (_syncRoot)
            {
                _deliveries.Add(delivery);
            }
        }

        /// <summary>
        /// 事务唯一标识（父子事务时，和父事务一致）。
        /// </summary>
        public Guid TransactionId { get; }

        /// <summary>
        /// 隔离级别。
        /// </summary>
        public IsolationLevel IsolationLevel { get; }

        /// <summary>
        /// 事务状态。
        /// </summary>
        public TransactionStatus Status { private set; get; }

        /// <summary>
        /// 事务提交。
        /// </summary>
        public event TransactionCompletedEventHandler TransactionCompleted;

        /// <summary>
        /// 签署事务。
        /// </summary>
        /// <param name="transaction">事务。</param>
        public void EnlistTransaction(ITransaction transaction)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            if (_complete)
            {
                throw new InvalidOperationException("事务已完成，无法签署新的子事务。");
            }

            lock (_syncRoot)
            {
                _transactions.Add(transaction);
            }
        }

        private volatile bool _complete;

        /// <summary>
        /// 提交事务。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            if (_complete)
            {
                throw new InvalidOperationException();
            }

            _complete = true;

            int i = 0, length = _transactions.Count;

            try
            {
                try
                {
                    for (; i < length; i++)
                    {
                        await _transactions[i].CommitAsync(cancellationToken);
                    }
                }
                catch (Exception)
                {
                    for (int j = i; j < length; j++)
                    {
                        try
                        {
                            await _transactions[j].RollbackAsync(cancellationToken);
                        }
                        catch
                        {
                            // 回滚失败时不应阻止后续事务的回滚。
                        }
                    }

                    throw;
                }

                if (_deliveries.Count > 0)
                {
                    List<Exception> deliveryExceptions = null;

                    foreach (var delivery in _deliveries)
                    {
                        try
                        {
                            delivery.Done();
                        }
                        catch (Exception ex)
                        {
                            deliveryExceptions ??= new List<Exception>();
                            deliveryExceptions.Add(ex);
                        }
                    }

                    if (deliveryExceptions is { Count: > 0 })
                    {
                        throw new AggregateException("一个或多个交付执行失败。", deliveryExceptions);
                    }
                }
            }
            finally
            {
                Status = TransactionStatus.Committed;

                TransactionCompleted?.Invoke(this, new TransactionEventArgs(this));
            }
        }

        /// <summary>
        /// 回滚事务。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            if (_complete)
            {
                throw new InvalidOperationException();
            }

            _complete = true;

            List<Exception> exceptions = null;

            try
            {
                for (int i = 0, length = _transactions.Count; i < length; i++)
                {
                    try
                    {
                        await _transactions[i].RollbackAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        exceptions ??= new List<Exception>();
                        exceptions.Add(ex);
                    }
                }
            }
            finally
            {
                Status = TransactionStatus.Aborted;

                TransactionCompleted?.Invoke(this, new TransactionEventArgs(this));
            }

            if (exceptions is { Count: > 0 })
            {
                throw new AggregateException("回滚过程中发生一个或多个错误。", exceptions);
            }
        }

        /// <summary>
        /// 回滚事务。
        /// </summary>
        public void Rollback()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            if (_complete)
            {
                throw new InvalidOperationException();
            }

            _complete = true;

            List<Exception> exceptions = null;

            try
            {
                for (int i = 0, length = _transactions.Count; i < length; i++)
                {
                    try
                    {
                        _transactions[i].Rollback();
                    }
                    catch (Exception ex)
                    {
                        exceptions ??= new List<Exception>();
                        exceptions.Add(ex);
                    }
                }
            }
            finally
            {
                Status = TransactionStatus.Aborted;

                TransactionCompleted?.Invoke(this, new TransactionEventArgs(this));
            }

            if (exceptions is { Count: > 0 })
            {
                throw new AggregateException("回滚过程中发生一个或多个错误。", exceptions);
            }
        }

        private volatile bool _disposed;

        /// <summary>
        /// 释放事务。
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;

                List<Exception> exceptions = null;

                try
                {
                    for (int i = 0, length = _transactions.Count; i < length; i++)
                    {
                        var transaction = _transactions[i];

                        if (!_complete)
                        {
                            try
                            {
                                await transaction.RollbackAsync();
                            }
                            catch (Exception ex)
                            {
                                exceptions ??= new List<Exception>();
                                exceptions.Add(ex);
                            }
                        }

                        try
                        {
                            await transaction.DisposeAsync();
                        }
                        catch (Exception ex)
                        {
                            exceptions ??= new List<Exception>();
                            exceptions.Add(ex);
                        }
                    }
                }
                finally
                {
                    if (!_complete)
                    {
                        _complete = true;

                        Status = TransactionStatus.Aborted;

                        TransactionCompleted?.Invoke(this, new TransactionEventArgs(this));
                    }
                }

                if (exceptions is { Count: > 0 })
                {
                    throw new AggregateException("事务释放过程中发生一个或多个错误。", exceptions);
                }
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~Transcation()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }
        /// <summary>
        /// 释放内存。
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                List<Exception> exceptions = null;

                try
                {
                    for (int i = 0, length = _transactions.Count; i < length; i++)
                    {
                        var transaction = _transactions[i];

                        if (!_complete)
                        {
                            try
                            {
                                transaction.Rollback();
                            }
                            catch (Exception ex)
                            {
                                exceptions ??= new List<Exception>();
                                exceptions.Add(ex);
                            }
                        }

                        try
                        {
                            transaction.Dispose();
                        }
                        catch (Exception ex)
                        {
                            exceptions ??= new List<Exception>();
                            exceptions.Add(ex);
                        }
                    }
                }
                finally
                {
                    if (!_complete)
                    {
                        _complete = true;

                        Status = TransactionStatus.Aborted;

                        TransactionCompleted?.Invoke(this, new TransactionEventArgs(this));
                    }
                }

                if (exceptions is { Count: > 0 })
                {
                    throw new AggregateException("事务释放过程中发生一个或多个错误。", exceptions);
                }
            }
        }

        /// <inheritdoc/>
        public bool Equals(Transaction x, Transaction y)
        {
            if (x is null)
            {
                return y is null;
            }

            if (y is null)
            {
                return false;
            }

            return x.TransactionId == y.TransactionId;
        }

        /// <inheritdoc/>
        public int GetHashCode(Transaction obj)
        {
            if (obj is null)
            {
                return 0;
            }

            return obj.TransactionId.GetHashCode();
        }

        private sealed class TransactionHolder
        {
            public Transaction Transaction;
        }
    }
}
