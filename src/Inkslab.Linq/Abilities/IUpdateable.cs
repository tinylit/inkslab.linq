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
    public interface IUpdateable<TEntity> : IUpdateableOfSet<TEntity>
    {
        /// <summary>
        /// 只更新的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IUpdateableOfSet<TEntity> Set(string[] columns);

        /// <summary>
        /// 只更新的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IUpdateableOfSet<TEntity> Set<TColumn>(Expression<Func<TEntity, TColumn>> columns);

        /// <summary>
        /// 不更新的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IUpdateableOfSet<TEntity> SetExcept(string[] columns);

        /// <summary>
        /// 不更新的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IUpdateableOfSet<TEntity> SetExcept<TColumn>(Expression<Func<TEntity, TColumn>> columns);
    }

    /// <summary>
    /// 更新能力。
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    [Ignore]
    public interface IUpdateableOfSet<TEntity> : ICommandExecutor
    {
        /// <summary>
        /// 跳过幂等验证。
        /// </summary>
        /// <returns></returns>
        ICommandExecutor SkipIdempotentValid();
    }
}
