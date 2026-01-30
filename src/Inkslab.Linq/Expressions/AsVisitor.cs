using System.Diagnostics;
using System.Linq.Expressions;
using Inkslab.Linq.Enums;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 别名访问器。
    /// </summary>
    [DebuggerDisplay("As")]
    public class AsVisitor : BaseVisitor
    {
        /// <inheritdoc/>
        public AsVisitor(BaseVisitor visitor) : base(visitor)
        {
        }

        /// <inheritdoc/>
        public override void Startup(Expression node)
        {
            if (RequiresConditionalEscape() && IsCondition(node))
            {
                using (var domain = Writer.Domain())
                {
                    Condition(node);

                    if (domain.IsEmpty)
                    {
                        Writer.True();
                    }
                    else
                    {
                        Writer.Keyword(SqlKeyword.THEN);

                        Writer.True();

                        Writer.Keyword(SqlKeyword.ELSE);

                        Writer.False();

                        Writer.Keyword(SqlKeyword.END);

                        domain.Flyback();

                        Writer.Keyword(SqlKeyword.CASE);
                        Writer.Keyword(SqlKeyword.WHEN);
                    }
                }
            }
            else
            {
                var length = Writer.Length;

                Visit(node);

                if (length == Writer.Length)
                {
                    if (IsCondition(node))
                    {
                        Writer.True();
                    }
                    else
                    {
                        Writer.Keyword(SqlKeyword.NULL);
                    }
                }
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