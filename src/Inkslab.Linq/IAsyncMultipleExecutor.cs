using System.Data;
using System.Threading.Tasks;

namespace Inkslab.Linq
{
    /// <summary>
    /// 多执行异步处理器。
    /// </summary>
    public interface IAsyncMultipleExecutor
    {
        /// <summary>
        /// 执行命令。
        /// </summary>
        /// <param name="commandSql">T-SQL 命令。</param>
        /// <returns>执行影响行。</returns>
        Task<int> ExecuteAsync(CommandSql commandSql);

        /// <summary>
        /// 批量处理。
        /// </summary>
        /// <param name="dt">数据表。</param>
        /// <param name="commandTimeout">超时时间。</param>
        Task<int> WriteToServerAsync(DataTable dt, int? commandTimeout = null);
    }
}
