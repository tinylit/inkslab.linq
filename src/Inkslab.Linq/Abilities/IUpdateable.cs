using Inkslab.Annotations;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Linq.Abilities
{
    /// <summary>
    /// 删除能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    [Ignore]
    public interface IUpdateable<TEntity> : IUpdateableDataSharding<TEntity>
    {
        /// <summary>
        /// 数据分区。
        /// </summary>
        /// <param name="shardingKey">分区键。</param>
        /// <returns>分区。</returns>
        IUpdateableDataSharding<TEntity> DataSharding(string shardingKey);
    }

    /// <summary>
    /// 条件。
    /// </summary>
    /// <typeparam name="TEntity">元素。</typeparam>
    [Ignore]
    public interface IUpdateableDataSharding<TEntity> : IUpdateableByLimit<TEntity>
    {
        /// <summary>
        /// 只更新的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IUpdateableByLimit<TEntity> Set(string[] columns);

        /// <summary>
        /// 只更新的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IUpdateableByLimit<TEntity> Set<TColumn>(Expression<Func<TEntity, TColumn>> columns);

        /// <summary>
        /// 不更新的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IUpdateableByLimit<TEntity> SetExcept(string[] columns);

        /// <summary>
        /// 不更新的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IUpdateableByLimit<TEntity> SetExcept<TColumn>(Expression<Func<TEntity, TColumn>> columns);
    }

    /// <summary>
    /// 更新能力。
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    [Ignore]
    public interface IUpdateableByLimit<TEntity> : IUpdateableTimeout<TEntity>
    {
        /// <summary>
        /// 设置超时时间。
        /// </summary>
        /// <param name="commandTimeout">命令执行超时时间。</param>
        /// <returns>超时删除能力。</returns>
        IUpdateableTimeout<TEntity> Timeout(int commandTimeout);
    }

    /// <summary>
    /// 超时。
    /// </summary>
    /// <typeparam name="TEntity">元素。</typeparam>
    [Ignore]
    public interface IUpdateableTimeout<TEntity> : IUpdateableExecute<TEntity>
    {
        /// <summary>
        /// 跳过幂等验证。
        /// </summary>
        /// <returns></returns>
        IUpdateableExecute<TEntity> SkipIdempotentValid();
    }

    /// <summary>
    /// 更新能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    [Ignore]
    public interface IUpdateableExecute<TEntity>
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
