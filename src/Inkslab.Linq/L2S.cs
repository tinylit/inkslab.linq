using System;
using System.Linq.Expressions;

namespace Inkslab.Linq
{
    /// <summary>
    /// Linq 2 SQL 扩展（仅用于表达式）。
    /// </summary>
    public static class L2S
    {
        /// <summary>
        /// 条件表达式。
        /// </summary>
        /// <typeparam name="T">参数类型。</typeparam>
        /// <param name="source">源。</param>
        /// <param name="predicate">条件。</param>
        /// <returns>是否满足。</returns>
        public static bool Condition<T>(T source, Expression<Func<T, bool>> predicate) => throw new NotImplementedException();
    }
}