using System;
using System.Linq.Expressions;
using Inkslab.Linq.Enums;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// <see cref="System.Linq.Queryable.All{TSource}(System.Linq.IQueryable{TSource}, Expression{Func{TSource, bool}})"/>
    /// </summary>
    public class AllVisitor : SelectVisitor
    {
        /// <inheritdoc/>
        public AllVisitor(BaseVisitor visitor)
            : base(visitor) { }

        /// <summary>
        /// 启动类。
        /// </summary>
        protected override void StartupCore(MethodCallExpression node)
        {
            Writer.Keyword(SqlKeyword.EXISTS);

            Writer.OpenBrace();

            Where(node);

            Writer.CloseBrace();
        }
    }
}
