using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// <see cref="System.Linq.Queryable.Select{TSource, TResult}(System.Linq.IQueryable{TSource}, Expression{System.Func{TSource, TResult}})"/>。
    /// </summary>
    public class SelectListVisitor : BaseVisitor
    {
        private readonly bool _showAs;

        /// <inheritdoc/>
        public SelectListVisitor(CoreVisitor visitor, bool showAs = false) : base(visitor) => _showAs = showAs;

        /// <inheritdoc/>
        protected override void PreparingParameter(LambdaExpression node)
        {
            //? 查询字段不准备参数。
        }

        /// <inheritdoc/>
        protected override void Member(MemberInfo memberInfo, Expression node)
        {
            using (var visitor = new AsVisitor(this, _showAs))
            {
                visitor.Startup(memberInfo, node);
            }
        }

        /// <inheritdoc/>
        protected override void Member(string schema, string field, string name)
        {
            base.Member(schema, field, name);

            if (_showAs)
            {
                Writer.AsName(name);
            }
        }
    }
}
