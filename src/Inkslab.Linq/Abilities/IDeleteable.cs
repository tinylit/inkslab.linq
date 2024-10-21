using Inkslab.Annotations;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Linq.Abilities
{
    /// <summary>
    /// 删除能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public interface IDeleteable<TEntity> : IDeleteableDataSharding<TEntity>
    {
        /// <summary>
        /// 数据分区。
        /// </summary>
        /// <param name="shardingKey">分区键。</param>
        /// <returns>分区。</returns>
        IDeleteableDataSharding<TEntity> DataSharding(string shardingKey);
    }

    /// <summary>
    /// 条件。
    /// </summary>
    /// <typeparam name="TEntity">元素。</typeparam>
    [Ignore]
    public interface IDeleteableDataSharding<TEntity> : IDeleteableTimeout<TEntity>
    {
        /// <summary>
        /// 设置超时时间。
        /// </summary>
        /// <param name="commandTimeout">命令执行超时时间。</param>
        /// <returns>超时删除能力。</returns>
        IDeleteableTimeout<TEntity> Timeout(int commandTimeout);
    }

    /// <summary>
    /// 超时。
    /// </summary>
    /// <typeparam name="TEntity">元素。</typeparam>
    [Ignore]
    public interface IDeleteableTimeout<TEntity> : IDeleteableExecute<TEntity>
    {
        /// <summary>
        /// 跳过幂等验证。
        /// </summary>
        /// <returns></returns>
        IDeleteableExecute<TEntity> SkipIdempotentValid();
    }

    /// <summary>
    /// 删除能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    [Ignore]
    public interface IDeleteableExecute<TEntity>
    {
        /// <summary>
        /// 执行指令。
        /// </summary>
        /// <returns>影响行。</returns>
        int Execute();

        /// <summary>
        /// 执行指令。
        /// </summary>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        Task<int> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
