using System;

namespace Inkslab.Linq
{
    /// <summary>
    /// Linq 2 SQL 扩展（仅用于表达式）。
    /// </summary>
    public static class Ranks
    {
        /// <summary>
        /// 根据“<paramref name="rank"/>”计算排名。
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        public static int By<TSource>(TSource source, Func<IRank<TSource>, IRank<TSource>> rank) =>
                throw new NotImplementedException("只能在排序表达式中使用！");
    }
}