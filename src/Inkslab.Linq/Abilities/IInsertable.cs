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
    public interface IInsertable<TEntity> : ICommandExecutor
    {
        /// <summary>
        /// 只插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        ICommandExecutor Limit(string[] columns);

        /// <summary>
        /// 只插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        ICommandExecutor Limit<TColumn>(Expression<Func<TEntity, TColumn>> columns);

        /// <summary>
        /// 不插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        ICommandExecutor Except(string[] columns);

        /// <summary>
        /// 不插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        ICommandExecutor Except<TColumn>(Expression<Func<TEntity, TColumn>> columns);
    }
}
