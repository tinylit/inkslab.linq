using System.Collections.Generic;
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
        /// 执行影响行数。
        /// </summary>
        int RowsExecuted { get; }

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

        /// <summary>        
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="commandSql">T-SQL 命令。</param>
        /// <returns>查询结果。</returns>
        Task<T> ReadAsync<T>(CommandSql<T> commandSql);

        /// <summary>
        /// 读取数据。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="commandSql">T-SQL 命令。</param>
        /// <returns>异步查询结果迭代器。</returns>
        IAsyncEnumerable<T> QueryAsync<T>(CommandSql commandSql);

        /// <summary>
        /// 查询多个结果。
        /// </summary>
        /// <param name="commandSql">T-SQL 命令。</param>
        /// <returns>异步查询器。</returns>
        Task<IAsyncDbGridReader> QueryMultipleAsync(CommandSql commandSql);
    }
}
