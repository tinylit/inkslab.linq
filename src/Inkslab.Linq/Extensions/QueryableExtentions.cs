using System.Linq.Expressions;
using Inkslab;

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace System.Linq
#pragma warning restore IDE0130 // 命名空间与文件夹结构不匹配
{
    /// <summary>
    /// 仓库扩展。
    /// </summary>
    public static class QueryableExtentions
    {
        /// <summary>
        /// 数据分区。
        /// </summary>
        /// <typeparam name="TSource">源类型。</typeparam>
        /// <param name="source">源数据。</param>
        /// <param name="shardingKey">分区键。</param>
        /// <returns></returns>
        public static IQueryable<TSource> DataSharding<TSource>(this IQueryable<TSource> source, string shardingKey)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (string.IsNullOrEmpty(shardingKey))
            {
                throw new ArgumentException($"'{nameof(shardingKey)}' cannot be null or empty.", nameof(shardingKey));
            }

            return source.Provider.CreateQuery<TSource>(Expression.Call(null, QueryableMethods.DataSharding.MakeGenericMethod(source.ElementType), new Expression[2] {
                source.Expression,
                Expression.Constant(shardingKey)
            }));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="test"></param>
        /// <param name="ifTrue"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IQueryable<TSource> WhereIf<TSource>(this IQueryable<TSource> source, bool test, Expression<Func<TSource, bool>> ifTrue)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (ifTrue is null)
            {
                throw new ArgumentNullException(nameof(ifTrue));
            }

            return test
                ? source.Where(ifTrue)
                : source;
        }

        /// <summary>
        /// 获取或设置在终止尝试执行命令并生成错误之前的等待时间。
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <param name="commandTimeout">超时时间，单位：秒。<see cref="Data.IDbCommand.CommandTimeout"/></param>
        /// <returns></returns>
        public static IQueryable<TSource> Timeout<TSource>(this IQueryable<TSource> source, int commandTimeout)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (commandTimeout < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(commandTimeout));
            }

            return source.Provider.CreateQuery<TSource>(Expression.Call(null, QueryableMethods.Timeout.MakeGenericMethod(source.ElementType), new Expression[2] {
                source.Expression,
                Expression.Constant(commandTimeout)
            }));
        }

        /// <summary>
        /// 未查询到数据的异常消息。仅支持以下方法（其它方法不生效）：
        /// <br/><see cref="Queryable.First{TSource}(IQueryable{TSource})"/>
        /// <br/><seealso cref="Queryable.First{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>
        /// <br/><seealso cref="Queryable.Last{TSource}(IQueryable{TSource})"/>
        /// <br/><seealso cref="Queryable.Last{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>
        /// <br/><seealso cref="Queryable.Single{TSource}(IQueryable{TSource})"/>
        /// <br/><seealso cref="Queryable.Single{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>
        /// <br/><seealso cref="Queryable.ElementAt{TSource}(IQueryable{TSource}, int)"/>
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <param name="errMsg">错误信息。</param>
        /// <returns></returns>
        public static IQueryable<TSource> NoElementError<TSource>(this IQueryable<TSource> source, string errMsg)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (string.IsNullOrEmpty(errMsg))
            {
                throw new ArgumentException($"“{nameof(errMsg)}”不能为 null 或空。", nameof(errMsg));
            }

            return source.Provider.CreateQuery<TSource>(Expression.Call(null, QueryableMethods.NoElementError.MakeGenericMethod(source.ElementType), new Expression[2] {
                source.Expression,
                Expression.Constant(errMsg)
            }));
        }

        /// <summary>
        /// 分页查询。
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <param name="pageIndex">页码（从“1”开始）</param>
        /// <param name="pageSize">每页条目数。</param>
        /// <returns>分页数据。</returns>
        public static PagedList<TSource> ToList<TSource>(this IQueryable<TSource> source, int pageIndex, int pageSize)
        {
            if (pageIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageIndex), "页码不能小于“1”。");
            }

            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "每页条目数不能小于“1”。");
            }

            var sources = pageIndex == 1
                ? source.Take(pageSize).ToList()
                : source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();

            if (sources.Count == 0 || sources.Count == pageSize)
            {
                int total = source.Count();

                return new PagedList<TSource>(sources, pageIndex, pageSize, total);
            }

            return new PagedList<TSource>(sources, pageIndex, pageSize, (pageIndex - 1) * pageSize + sources.Count);
        }

        /// <summary>
        /// 更新。
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <param name="updateSet">更新表达式。</param>
        /// <returns></returns>
        internal static int Update<TSource>(IQueryable<TSource> source, Expression<Func<TSource, TSource>> updateSet)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (updateSet is null)
            {
                throw new ArgumentNullException(nameof(updateSet));
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// 删除。
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <returns></returns>
        internal static int Delete<TSource>(IQueryable<TSource> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// 删除。
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <param name="predicate">删除条件。</param>
        /// <returns></returns>
        internal static int Delete<TSource>(IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// 忽略。
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <returns>忽略仓储。</returns>
        internal static IQueryable<TSource> Ignore<TSource>(IQueryable<TSource> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// 插入。
        /// </summary>
        /// <typeparam name="TSource">资源类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <param name="querable">查询插入数据的表达式。</param>
        /// <returns>影响行。</returns>
        internal static int Insert<TSource>(IQueryable<TSource> source, IQueryable<TSource> querable)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (querable is null)
            {
                throw new ArgumentNullException(nameof(querable));
            }

            throw new NotImplementedException();
        }
    }
}
