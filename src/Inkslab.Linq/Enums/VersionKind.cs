namespace Inkslab.Linq.Enums
{
    /// <summary>
    /// 幂等版本类型。
    /// </summary>
    public enum VersionKind
    {
        /// <summary>
        /// 无。
        /// </summary>
        None,
        /// <summary>
        /// 递增。
        /// </summary>
        Increment,
        /// <summary>
        /// 时刻。
        /// </summary>
        Ticks,
        /// <summary>
        /// 时间戳。
        /// </summary>
        Timestamp,
        /// <summary>
        /// 此刻。
        /// </summary>
        Now
    }
}