namespace Inkslab.Transactions
{
    /// <summary>
    /// 隔离级别。
    /// </summary>
    public enum IsolationLevel
    {
        /// <summary>
        /// 正在使用的隔离级别与指定的隔离级别不同，但级别为无法确定。
        /// </summary>
        Unspecified = -1,
        /// <summary>
        /// 来自更高隔离级别事务的待处理更改无法被覆盖。
        /// </summary>
        Chaos = 16,
        /// <summary>
        /// 可能发生脏读，这意味着不会发出共享锁，也不会尊重排他锁。
        /// </summary>
        ReadUncommitted = 256,
        /// <summary>
        /// 在读取数据时会保持共享锁以避免脏读，但数据可以在事务结束之前被更改，从而导致不可重复读或幻读。
        /// </summary>
        ReadCommitted = 4096,
        /// <summary>
        /// 锁定所有在查询中使用的数据，防止其他用户更新数据。防止不可重复读，但幻影行仍然可能发生。
        /// </summary>
        RepeatableRead = 65536,
        /// <summary>
        /// 对 System.Data.DataSet 施加范围锁，防止其他用户更新或插入行，直到事务完成。
        /// </summary>
        Serializable = 1048576,
        /// <summary>
        /// 通过存储一个版本的数据来减少阻塞，一个应用程序可以在另一个应用程序修改相同数据时读取该版本。
        /// 表示从一个事务中无法看到其他事务所做的更改，即使重新查询。
        /// 查看其他事务中所做的更改，即使您进行了查询。
        /// </summary>
        Snapshot = 16777216
    }
}
