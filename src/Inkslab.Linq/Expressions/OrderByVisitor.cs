using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 排序。
    /// </summary>
    public class OrderByVisitor : CoreVisitor
    {
        private readonly bool _isDescending;
        private readonly bool _isGroupHaving;

        /// <inheritdoc/>
        public OrderByVisitor(CoreVisitor visitor, bool isDescending, bool isGroupHaving = false) : base(visitor, false)
        {
            _isDescending = isDescending;
            _isGroupHaving = isGroupHaving;
        }

        /// <inheritdoc/>
        protected override void Member(MemberExpression node)
        {
            base.Member(node);

            if (_isDescending)
            {
                Writer.Keyword(Enums.SqlKeyword.DESC);
            }
        }

        /// <inheritdoc/>
        protected override void Member(MemberInfo memberInfo, Expression node)
        {
            base.Member(memberInfo, node);

            if (_isDescending)
            {
                Writer.Keyword(Enums.SqlKeyword.DESC);
            }
        }

        /// <inheritdoc/>
        protected override void Member(string schema, string field, string name)
        {
            base.Member(schema, field, name);

            if (_isDescending)
            {
                Writer.Keyword(Enums.SqlKeyword.DESC);
            }
        }

        /// <inheritdoc/>
        protected override void LinqCall(MethodCallExpression node)
        {
            using (var visitor = new SelectVisitor(this))
            {
                visitor.Startup(node);
            }
        }

        /// <inheritdoc/>
        protected override void ByEnumerable(MethodCallExpression node)
        {
            if (_isGroupHaving && node.IsGrouping())
            {
                using (var visitor = new AggregateTermVisitor(this))
                {
                    visitor.Startup(node);
                }
            }
            else
            {
                base.ByEnumerable(node);
            }
        }
    }
}
