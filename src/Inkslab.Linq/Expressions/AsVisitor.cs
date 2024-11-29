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
            if (Engine == DatabaseEngine.MySQL || !IsCondition(node))
            {
                Visit(node);
            }
            else
            {
                Writer.Keyword(Enums.SqlKeyword.CASE);
                Writer.Keyword(Enums.SqlKeyword.WHEN);

                Visit(node);

                Writer.Keyword(Enums.SqlKeyword.THEN);

                Writer.True();

                Writer.Keyword(Enums.SqlKeyword.ELSE);

                Writer.False();

                Writer.Keyword(Enums.SqlKeyword.END);
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