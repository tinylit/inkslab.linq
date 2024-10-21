using System.Data;
using System.Threading.Tasks;
using System.Threading;
using System.Data.Common;

namespace Inkslab.Linq
{
    /// <summary>
    /// 批量助手。
    /// </summary>
    public interface IBulkAssistant
    {
        /// <summary>
        /// 批量处理。
        /// </summary>
        /// <param name="connection">数据库链接。</param>
        /// <param name="dt">数据表。</param>
        /// <param name="commandTimeout">命令超时时间。</param>
        int WriteToServer(DbConnection connection, DataTable dt, int? commandTimeout = null);

        /// <summary>
        /// 批量处理。
        /// </summary>
        /// <param name="connection">数据库链接。</param>
        /// <param name="dt">数据表。</param>
        /// <param name="transaction">事务。</param>
        /// <param name="commandTimeout">命令超时时间。</param>
        int WriteToServer(DbConnection connection, DbTransaction transaction, DataTable dt, int? commandTimeout = null);

        /// <summary>
        /// 批量处理。
        /// </summary>
        /// <param name="connection">数据库链接。</param>
        /// <param name="dt">数据表。</param>
        /// <param name="commandTimeout">命令超时时间。</param>
        /// <param name="cancellationToken">取消。</param>
        Task<int> WriteToServerAsync(DbConnection connection, DataTable dt, int? commandTimeout = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量处理。
        /// </summary>
        /// <param name="connection">数据库链接。</param>
        /// <param name="transaction">事务。</param>
        /// <param name="dt">数据表。</param>
        /// <param name="commandTimeout">命令超时时间。</param>
        /// <param name="cancellationToken">取消。</param>
        Task<int> WriteToServerAsync(DbConnection connection, DbTransaction transaction, DataTable dt, int? commandTimeout = null, CancellationToken cancellationToken = default);
    }
}
