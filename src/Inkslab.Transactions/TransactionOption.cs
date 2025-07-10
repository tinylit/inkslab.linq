namespace Inkslab.Transactions
{
    /// <summary>
    /// 事务配置。
    /// </summary>
    public enum TransactionOption
    {
        /// <summary>
        /// 如果上下文中存在事务，则不开启新事务。
        /// </summary>
        Required,
        /// <summary>
        /// 始终开启新事务。
        /// </summary>
        RequiresNew,
        /// <summary>
        /// 阻止事务。
        /// </summary>
        Suppress
    }
}
