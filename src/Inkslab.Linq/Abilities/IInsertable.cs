using Inkslab.Annotations;
using Inkslab.Linq.Enums;
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
    public interface IInsertable<TEntity> : IInsertableIgnore<TEntity>
    {
        /// <summary>
        /// 插入主键或唯一键冲突时，忽略。
        /// </summary>
        /// <param name="level">支持级别。</param>
        /// <returns>忽略插入器。</returns>
        IInsertableIgnore<TEntity> Ignore(SupportLevel level = SupportLevel.Auto);
    }

    /// <summary>
    /// 插入主键或唯一键冲突时，忽略。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public interface IInsertableIgnore<TEntity> : IInsertableDataSharding<TEntity>
    {
        /// <summary>
        /// 数据分区。
        /// </summary>
        /// <param name="shardingKey">分区键。</param>
        /// <returns>分区。</returns>
        IInsertableDataSharding<TEntity> DataSharding(string shardingKey);
    }

    /// <summary>
    /// 条件。
    /// </summary>
    /// <typeparam name="TEntity">元素。</typeparam>
    [Ignore]
    public interface IInsertableDataSharding<TEntity> : IInsertableByLimit<TEntity>
    {
        /// <summary>
        /// 只插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IInsertableByLimit<TEntity> Limit(string[] columns);

        /// <summary>
        /// 只插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IInsertableByLimit<TEntity> Limit<TColumn>(Expression<Func<TEntity, TColumn>> columns);

        /// <summary>
        /// 不插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IInsertableByLimit<TEntity> Except(string[] columns);

        /// <summary>
        /// 不插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IInsertableByLimit<TEntity> Except<TColumn>(Expression<Func<TEntity, TColumn>> columns);
    }

    /// <summary>
    /// 更新能力。
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IInsertableByLimit<TEntity> : IInsertableExecute<TEntity>
    {
        /// <summary>
        /// 设置超时时间。
        /// </summary>
        /// <param name="commandTimeout">命令执行超时时间。</param>
        /// <returns>超时删除能力。</returns>
        IInsertableExecute<TEntity> Timeout(int commandTimeout);
    }

    /// <summary>
    /// 超时。
    /// </summary>
    /// <typeparam name="TEntity">元素。</typeparam>
    [Ignore]
    public interface IInsertableExecute<TEntity>
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
