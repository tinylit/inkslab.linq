using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Linq
{
    /// <summary>
    /// 批量助手。
    /// </summary>
    public interface IDatabaseBulkCopy : IDisposable
    {
        /// <summary>
        /// 命令超时时间。
        /// </summary>
        int BulkCopyTimeout { get; set; }

        /// <summary>
        /// 批量处理。
        /// </summary>
        /// <param name="dt">数据表。</param>
        int WriteToServer(DataTable dt);

        /// <summary>
        /// 批量处理。
        /// </summary>
        /// <param name="dt">数据表。</param>
        /// <param name="cancellationToken">取消。</param>
        Task<int> WriteToServerAsync(DataTable dt, CancellationToken cancellationToken = default);
    }
}
