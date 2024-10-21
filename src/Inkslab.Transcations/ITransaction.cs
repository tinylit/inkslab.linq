using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Transcations
{
    /// <summary>
    /// 事务。
    /// </summary>
    public interface ITransaction : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// 提交事务。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>提交事务。</returns>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 回滚事务。
        /// </summary>
        void Rollback();

        /// <summary>
        /// 回滚事务。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>回滚事务。</returns>
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
