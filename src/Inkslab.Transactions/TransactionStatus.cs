namespace Inkslab.Transactions
{
    /// <summary>
    /// 描述分布式事务的当前状态。
    /// </summary>
    public enum TransactionStatus
    {
        /// <summary>
        ///     事务的状态未知，因为某些参与者仍然必须进行轮询。
        ///     be polled.
        /// </summary>
        Active = 0,
        /// <summary>
        ///     事务已提交。
        /// </summary>
        Committed = 1,
        /// <summary>
        ///     事务已回滚。
        /// </summary>
        Aborted = 2,
        /// <summary>
        ///     事务的状态未知，因为某些参与者仍然必须进行轮询。
        /// </summary>
        InDoubt = 3
    }
}