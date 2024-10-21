using System;

namespace Inkslab.Transcations
{
    /// <summary>
    /// 事务事件参数。
    /// </summary>
    public class TransactionEventArgs : EventArgs
    {
        /// <summary>
        /// 事务事件。
        /// </summary>
        /// <param name="transaction">事务。</param>
        public TransactionEventArgs(Transaction transaction)
        {
            Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        }

        /// <summary>
        /// 事务。
        /// </summary>
        public Transaction Transaction { get; }
    }
}
