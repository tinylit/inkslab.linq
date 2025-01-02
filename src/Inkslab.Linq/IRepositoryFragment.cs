using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Linq
{
    /// <summary>
    /// 仓库片段。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public interface IRepositoryFragment<TEntity>
        where TEntity : class, new()
    {
        /// <summary>
        ///准备。
        /// </summary>
        /// <param name="shardingKey">分区键。</param>
        void Ready(string shardingKey);

        /// <summary>
        /// 准备。
        /// </summary>
        /// <param name="shardingKey">分区键。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>任务。</returns>
        Task ReadyAsync(string shardingKey, CancellationToken cancellationToken = default);
    }
}
