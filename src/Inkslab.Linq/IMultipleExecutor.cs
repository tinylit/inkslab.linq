using System.Data;

namespace Inkslab.Linq
{
    /// <summary>
    /// 多执行处理器。
    /// </summary>
    public interface IMultipleExecutor
    {
        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="commandSql">T-SQL 命令。</param>
        /// <returns>执行影响行。</returns>
        int Execute(CommandSql commandSql);

        /// <summary>
        /// 批量处理。
        /// </summary>
        /// <param name="dt">数据表。</param>
        /// <param name="commandTimeout">超时时间。</param>
        int WriteToServer(DataTable dt, int? commandTimeout = null);
    }
}
