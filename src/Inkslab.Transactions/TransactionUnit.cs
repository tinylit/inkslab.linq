using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Transactions
{
    /// <summary>
    /// 事务单元。
    /// </summary>
    public class TransactionUnit : IAsyncDisposable, IDisposable
    {
        private readonly Transaction _transaction;
        private readonly Transaction _previousTransaction;
        private readonly TransactionOption _transactionOption;

        /// <summary>
        /// 事务。
        /// </summary>
        public TransactionUnit() : this(TransactionOption.Required, IsolationLevel.Unspecified)
        {
        }

        /// <summary>
        /// 事务。
        /// </summary>
        /// <param name="transactionOption">事务配置。</param>
        public TransactionUnit(TransactionOption transactionOption) : this(transactionOption, IsolationLevel.Unspecified)
        {
        }

        /// <summary>
        /// 事务。
        /// </summary>
        /// <param name="isolationLevel">隔离级别。</param>
        public TransactionUnit(IsolationLevel isolationLevel) : this(TransactionOption.Required, isolationLevel)
        {
        }

        /// <summary>
        /// 事务。
        /// </summary>
        /// <param name="transactionOption">事务配置。</param>
        /// <param name="isolationLevel">隔离级别。</param>
        public TransactionUnit(TransactionOption transactionOption, IsolationLevel isolationLevel)
        {
            if (!Enum.IsDefined(typeof(IsolationLevel), isolationLevel))
            {
                throw new ArgumentOutOfRangeException(nameof(isolationLevel));
            }

            if (!Enum.IsDefined(typeof(TransactionOption), transactionOption))
            {
                throw new ArgumentOutOfRangeException(nameof(transactionOption));
            }

            _transaction = _previousTransaction = Transaction.Current;

            if (transactionOption == TransactionOption.Suppress)
            {
                Transaction.Current = null;
            }
            else if (_transaction is null || transactionOption == TransactionOption.RequiresNew)
            {
                _transaction = Transaction.Current = new Transaction(isolationLevel);
            }

            _transactionOption = transactionOption;
        }

        private bool _complete;

        /// <summary>
        /// 提交事务，若提交失败自动回滚。
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TransactionUnit));
            }

            if (_complete)
            {
                throw new InvalidOperationException();
            }

            _complete = true;

            if (_transactionOption == TransactionOption.Suppress)
            {

            }
            else if (_transactionOption == TransactionOption.RequiresNew || _previousTransaction is null)
            {
                await _transaction.CommitAsync(cancellationToken);
            }
        }
        
        private bool _disposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                Transaction.Current = _previousTransaction;

                if (_transactionOption == TransactionOption.Suppress)
                {

                }
                else if (_transactionOption == TransactionOption.RequiresNew || _previousTransaction is null)
                {
                    if (!_complete)
                    {
                        _transaction.Rollback();
                    }

                    _transaction.Dispose();
                }
            }

            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;

                Transaction.Current = _previousTransaction;

                if (_transactionOption == TransactionOption.Suppress)
                {

                }
                else if (_transactionOption == TransactionOption.RequiresNew || _previousTransaction is null)
                {
                    if (!_complete)
                    {
                        await _transaction.RollbackAsync();
                    }

                    await _transaction.DisposeAsync();
                }
            }

            GC.SuppressFinalize(this);
        }
    }
}
