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
    public interface IInsertable<TEntity> : IInsertableIgnore<TEntity>
    {
        /// <summary>
        /// 生成 “INSERT IGNORE INTO”语法。
        /// </summary>
        /// <returns>忽略插入器。</returns>
        IInsertableIgnore<TEntity> Ignore();
    }

    /// <summary>
    /// 生成 “INSERT IGNORE INTO”语法。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    [Ignore]
    public interface IInsertableIgnore<TEntity> : IInsertableByTimeout<TEntity>
    {
        /// <summary>
        /// 只插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IInsertableByTimeout<TEntity> Limit(string[] columns);

        /// <summary>
        /// 只插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IInsertableByTimeout<TEntity> Limit<TColumn>(Expression<Func<TEntity, TColumn>> columns);

        /// <summary>
        /// 不插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IInsertableByTimeout<TEntity> Except(string[] columns);

        /// <summary>
        /// 不插入的字段。
        /// </summary>
        /// <param name="columns">字段。</param>
        /// <returns></returns>
        IInsertableByTimeout<TEntity> Except<TColumn>(Expression<Func<TEntity, TColumn>> columns);
    }

    /// <summary>
    /// 更新能力。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    [Ignore]
    public interface IInsertableByTimeout<TEntity> : ICommandExecutor
    {
        /// <summary>
        /// 设置超时时间。
        /// </summary>
        /// <param name="commandTimeout">命令执行超时时间。</param>
        /// <returns>超时删除能力。</returns>
        ICommandExecutor Timeout(int commandTimeout);
    }
}
