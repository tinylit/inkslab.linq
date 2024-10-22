using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Annotations;
using Inkslab.Linq.Abilities;
using Inkslab.Linq.Enums;

namespace Inkslab.Linq
{
    /// <summary>
    /// 仓库。
    /// </summary>
    /// <typeparam name="TEntity">元素。</typeparam>
    public interface IRepository<TEntity> : IRepositoryDataSharding<TEntity> where TEntity : class, new()
    {
        /// <summary>
        ///     The <see cref="MethodInfo" /> for
        ///     <see
        ///         cref="QueryableExtentions.DataSharding{TSource}(IQueryable{TSource}, string)" />
        /// </summary>
        IRepositoryDataSharding<TEntity> DataSharding(string shardingKey);

        /// <summary>
        /// 插入路由执行器。
        /// </summary>
        /// <param name="entry">项目。</param>
        IInsertable<TEntity> AsInsertable(TEntity entry);

        /// <summary>
        /// 插入路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        IInsertable<TEntity> AsInsertable(List<TEntity> entries);

        /// <summary>
        /// 插入路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        IInsertable<TEntity> AsInsertable(params TEntity[] entries);

        /// <summary>
        /// 更新路由执行器。
        /// </summary>
        /// <param name="entry">项目。</param>
        /// <exception cref="NotSupportedException">实体未标记主键！</exception>
        IUpdateable<TEntity> AsUpdateable(TEntity entry);

        /// <summary>
        /// 更新路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <exception cref="NotSupportedException">实体未标记主键！</exception>
        IUpdateable<TEntity> AsUpdateable(List<TEntity> entries);

        /// <summary>
        /// 更新路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <exception cref="NotSupportedException">实体未标记主键！</exception>
        IUpdateable<TEntity> AsUpdateable(params TEntity[] entries);

        /// <summary>
        /// 删除路由执行器。
        /// </summary>
        /// <param name="entry">项目。</param>
        /// <exception cref="NotSupportedException">实体未标记主键！</exception>
        IDeleteable<TEntity> AsDeleteable(TEntity entry);

        /// <summary>
        /// 删除路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <exception cref="NotSupportedException">实体未标记主键！</exception>
        IDeleteable<TEntity> AsDeleteable(List<TEntity> entries);

        /// <summary>
        /// 删除路由执行器。
        /// </summary>
        /// <param name="entries">项目集合。</param>
        /// <exception cref="NotSupportedException">实体未标记主键！</exception>
        IDeleteable<TEntity> AsDeleteable(params TEntity[] entries);
    }

    /// <summary>
    /// 条件。
    /// </summary>
    /// <typeparam name="TEntity">元素。</typeparam>
    [Ignore]
    public interface IRepositoryDataSharding<TEntity> : IRepositoryTimeout<TEntity> where TEntity : class, new()
    {
        /// <summary>
        ///     The <see cref="MethodInfo" /> for
        ///     <see
        ///         cref="QueryableExtentions.Timeout{TSource}(IQueryable{TSource}, int)" />
        /// </summary>
        IRepositoryTimeout<TEntity> Timeout(int commandTimeout);
    }

    /// <summary>
    /// 超时。
    /// </summary>
    /// <typeparam name="TEntity">元素。</typeparam>
    [Ignore]
    public interface IRepositoryTimeout<TEntity> : IRepositoryCondition<TEntity>, IRepositoryIgnore<TEntity> where TEntity : class, new()
    {
        /// <summary>
        /// 忽略。
        /// </summary>
        /// <returns>忽略仓储。</returns>
        IRepositoryIgnore<TEntity> Ignore();
    }

    /// <summary>
    /// 忽略。
    /// </summary>
    /// <typeparam name="TEntity">元素。</typeparam>
    [Ignore]
    public interface IRepositoryIgnore<TEntity> where TEntity : class, new()
    {
        /// <summary>
        /// 插入数据。
        /// </summary>
        /// <param name="querable">需要插入的数据。</param>
        /// <returns>影响行。</returns>
        int Insert(IQueryable<TEntity> querable);

        /// <summary>
        /// 插入数据。
        /// </summary>
        /// <param name="querable">需要插入的数据。</param>
        /// <param name="cancellationToken">令牌。</param>
        /// <returns>影响行。</returns>
        Task<int> InsertAsync(IQueryable<TEntity> querable, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 条件。
    /// </summary>
    /// <typeparam name="TEntity">元素。</typeparam>
    [Ignore]
    public interface IRepositoryCondition<TEntity> where TEntity : class, new()
    {
        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Where{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        IRepositoryCondition<TEntity> Where(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.TakeWhile{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        IRepositoryCondition<TEntity> TakeWhile(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.SkipWhile{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        IRepositoryCondition<TEntity> SkipWhile(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// 删除数据。
        /// </summary>
        /// <returns></returns>
        int Delete();

        /// <summary>
        /// 删除数据。
        /// </summary>
        /// <returns></returns>
        int Delete(Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// 删除数据。
        /// </summary>
        /// <param name="cancellationToken">令牌。</param>
        /// <returns></returns>
        Task<int> DeleteAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除数据。
        /// </summary>
        /// <param name="predicate">删除条件。</param>
        /// <param name="cancellationToken">令牌。</param>
        /// <returns></returns>
        Task<int> DeleteAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新数据。
        /// </summary>
        /// <param name="updateSetter">更新的字段和值。</param>
        /// <returns></returns>
        int Update(Expression<Func<TEntity, TEntity>> updateSetter);

        /// <summary>
        /// 更新数据。
        /// </summary>
        /// <param name="updateSetter">更新的字段和值。</param>
        /// <param name="cancellationToken">令牌。</param>
        /// <returns></returns>
        Task<int> UpdateAsync(Expression<Func<TEntity, TEntity>> updateSetter, CancellationToken cancellationToken = default);
    }
}