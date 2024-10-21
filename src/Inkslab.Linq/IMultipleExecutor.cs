using System.Collections.Generic;
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
        /// <param name="sql">T-SQL 命令。</param>
        /// <param name="parameters">参数。</param>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns>执行影响行。</returns>
        int Execute(string sql, IDictionary<string, object> parameters = null, int? commandTimeout = null);

        /// <summary>
        /// 批量处理。
        /// </summary>
        /// <param name="dt">数据表。</param>
        /// <param name="commandTimeout">超时时间。</param>
        int WriteToServer(DataTable dt, int? commandTimeout = null);
    }
}
