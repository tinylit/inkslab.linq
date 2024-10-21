using System.Reflection;
using System.Linq.Expressions;
using System;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 别名访问器。
    /// </summary>
    public class AsVisitor : BaseVisitor
    {
        private readonly bool _showAs;

        /// <inheritdoc/>
        public AsVisitor(BaseVisitor visitor, bool showAs) : base(visitor)
        {
            _showAs = showAs;
        }

        /// <inheritdoc/>
        public void Startup(MemberInfo memberInfo, Expression node)
        {
            if (IsCondition(node))
            {
                Writer.Keyword(Enums.SqlKeyword.CASE);
                Writer.Keyword(Enums.SqlKeyword.WHEN);

                Visit(node);

                Writer.Keyword(Enums.SqlKeyword.THEN);

                Writer.AlwaysTrue();

                Writer.Keyword(Enums.SqlKeyword.ELSE);

                Writer.AlwaysFalse();

                Writer.Keyword(Enums.SqlKeyword.END);
            }
            else
            {
                Visit(node);
            }

            if (_showAs)
            {
                Writer.AsName(memberInfo.Name);
            }
        }

        /// <inheritdoc/>
        protected override void MethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == Types.Enumerable)
            {
                using (var visitor = new AggregateTermVisitor(this))
                {
                    visitor.Startup(node);
                }
            }
            else
            {
                base.MethodCall(node);
            }
        }
    }
}