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
                var holder = _transactionCurrent.Value;

                if (holder is null)
                {

                }
                else
                {
                    holder.Transaction = null;
                }

                if (value is null)
                {

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

            _deliveries.Add(delivery);
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

            if (disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            _transactions.Add(transaction);
        }

        private bool complete;

        /// <summary>
        /// 提交事务。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            if (complete)
            {
                throw new InvalidOperationException();
            }

            complete = true;

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
                    for (; i < length; i++)
                    {
                        await _transactions[i].RollbackAsync(cancellationToken);
                    }

                    throw;
                }

                if (_deliveries.Count > 0)
                {
                    foreach (var delivery in _deliveries)
                    {
                        delivery.Done();
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
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            if (complete)
            {
                throw new InvalidOperationException();
            }

            complete = true;

            try
            {
                for (int i = 0, length = _transactions.Count; i < length; i++)
                {
                    await _transactions[i].RollbackAsync(cancellationToken);
                }
            }
            finally
            {
                Status = TransactionStatus.Aborted;

                TransactionCompleted?.Invoke(this, new TransactionEventArgs(this));
            }
        }

        /// <summary>
        /// 回滚事务。
        /// </summary>
        public void Rollback()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(Transaction));
            }

            if (complete)
            {
                throw new InvalidOperationException();
            }

            complete = true;

            try
            {
                for (int i = 0, length = _transactions.Count; i < length; i++)
                {
                    _transactions[i].Rollback();
                }
            }
            finally
            {
                Status = TransactionStatus.Aborted;

                TransactionCompleted?.Invoke(this, new TransactionEventArgs(this));
            }
        }

        private bool disposed;

        /// <summary>
        /// 释放事务。
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (!disposed)
            {
                disposed = true;

                try
                {
                    for (int i = 0, length = _transactions.Count; i < length; i++)
                    {
                        var transaction = _transactions[i];

                        if (!complete)
                        {
                            await transaction.RollbackAsync();
                        }

                        await transaction.DisposeAsync();
                    }
                }
                finally
                {
                    if (!complete)
                    {
                        complete = true;

                        Status = TransactionStatus.Aborted;

                        TransactionCompleted?.Invoke(this, new TransactionEventArgs(this));
                    }
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
            if (!disposed)
            {
                disposed = true;

                try
                {
                    for (int i = 0, length = _transactions.Count; i < length; i++)
                    {
                        var transaction = _transactions[i];

                        if (!complete)
                        {
                            transaction.Rollback();
                        }

                        transaction.Dispose();
                    }
                }
                finally
                {
                    if (!complete)
                    {
                        complete = true;

                        Status = TransactionStatus.Aborted;

                        TransactionCompleted?.Invoke(this, new TransactionEventArgs(this));
                    }
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
