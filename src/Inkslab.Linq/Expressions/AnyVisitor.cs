using System.Linq.Expressions;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// <see cref="System.Linq.Queryable.Any{TSource}(System.Linq.IQueryable{TSource})"/> 或
    /// <see cref="System.Linq.Queryable.Any{TSource}(System.Linq.IQueryable{TSource}, Expression{System.Func{TSource, bool}})"/>
    /// </summary>
    public class AnyVisitor : SelectVisitor
    {
        /// <inheritdoc/>
        public AnyVisitor(BaseVisitor visitor) : base(visitor)
        {
        }

        /// <inheritdoc/>
        protected override void StartupCore(MethodCallExpression node)
        {
            if (node.Arguments.Count == 1)
            {
                Visit(node.Arguments[0]);
            }
            else
            {
                Where(node);
            }
        }
    }
}
