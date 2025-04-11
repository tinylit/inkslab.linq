using System;

namespace Inkslab.Linq
{
    /// <summary>
    /// Linq 2 SQL 扩展（仅用于表达式）。
    /// </summary>
    public static class Ranks
    {
        /// <summary>
        /// 排名（避免表达式用于其它运算）。
        /// </summary>
        public readonly struct RankOnly { }

        /// <summary>
        /// 根据条件“<paramref name="rank"/>”计算排名。
        /// </summary>
        /// <typeparam name="TSource">源类型。</typeparam>
        /// <param name="source">源。</param>
        /// <param name="rank">排序。</param>
        /// <returns></returns>
        public static RankOnly By<TSource>(TSource source, Func<IRank<TSource>, IRank> rank) =>
                throw new NotImplementedException("只能在排序表达式中使用！");
    }
}