namespace Inkslab.Linq
{
    /// <summary>
    /// 格式化。
    /// </summary>
    public interface IFormatter
    {
        /// <summary>
        /// 格式化。
        /// </summary>
        /// <param name="tsql">T-SQL</param>
        /// <returns>格式化的结果。</returns>
        string Format(string tsql);
    }
}