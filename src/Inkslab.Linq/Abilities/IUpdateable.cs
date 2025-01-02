using System;
using System.Linq.Expressions;
using Inkslab.Annotations;

namespace Inkslab.Linq.Abilities
{
    /// <summary>
    /// 删除能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    [Ignore]
    public interface IUpdateable<TEntity> : IUpdateableByLimit<TEntity>
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
    public interface IUpdateableTimeout<TEntity> : ICommandExecutor
    {
        /// <summary>
        /// 跳过幂等验证。
        /// </summary>
        /// <returns></returns>
        ICommandExecutor SkipIdempotentValid();
    }
}
