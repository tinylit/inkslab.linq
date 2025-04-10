using System;
using System.Linq.Expressions;

namespace Inkslab.Linq
{
    /// <summary>
    /// 排序接口。
    /// </summary>
    /// <typeparam name="TSource">源类型。</typeparam>
    public interface IRank<TSource>
    {
        /// <summary>
        /// 根据“<paramref name="condition"/>”条件判断是否需要使用该排名。
        /// </summary>
        /// <param name="condition">条件。</param>
        /// <returns></returns>
        IOrderBy<TSource> When(bool condition);
    }

    /// <summary>
    /// 排序。
    /// </summary>
    /// <typeparam name="TSource">源类型。</typeparam>
    public interface IOrderBy<TSource>
    {
        /// <summary>
        /// 根据“<paramref name="rank"/>”计算升序排名。
        /// </summary>
        /// <typeparam name="TItem">排序项。</typeparam>
        /// <param name="rank"></param>
        /// <returns></returns>
        IThenBy<TSource> OrderBy<TItem>(Expression<Func<TSource, TItem>> rank);

        /// <summary>
        /// 根据“<paramref name="rank"/>”计算降序排名。
        /// </summary>
        /// <typeparam name="TItem">排序项。</typeparam>
        /// <param name="rank"></param>
        /// <returns></returns>
        IThenBy<TSource> OrderByDescending<TItem>(Expression<Func<TSource, TItem>> rank);
    }

    /// <summary>
    /// 继续排序。
    /// </summary>
    /// <typeparam name="TSource">源类型。</typeparam>
    public interface IThenBy<TSource> : IRank<TSource>
    {
        /// <summary>
        /// 根据“<paramref name="rank"/>”计算升序排名。
        /// </summary>
        /// <typeparam name="TItem">排序项。</typeparam>
        /// <param name="rank"></param>
        /// <returns></returns>
        IThenBy<TSource> ThenBy<TItem>(Expression<Func<TSource, TItem>> rank);

        /// <summary>
        /// 根据“<paramref name="rank"/>”计算降序排名。
        /// </summary>
        /// <typeparam name="TItem">排序项。</typeparam>
        /// <param name="rank"></param>
        /// <returns></returns>
        IThenBy<TSource> ThenByDescending<TItem>(Expression<Func<TSource, TItem>> rank);
    }
}