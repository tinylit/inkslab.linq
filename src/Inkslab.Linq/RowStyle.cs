namespace Inkslab.Linq
{
    /// <summary>
    /// 查询行。
    /// </summary>
    public enum RowStyle
    {
        /// <summary>
        /// 取第一条。
        /// </summary>
        First = 0,
        /// <summary>
        /// 取第一条或默认。
        /// </summary>
        FirstOrDefault = 1,
        /// <summary>
        /// 有且仅有一条。
        /// </summary>
        Single = 2,
        /// <summary>
        /// 只有一条，或没有。
        /// </summary>
        SingleOrDefault = 3
    }
}