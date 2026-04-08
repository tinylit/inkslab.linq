using System;
using System.Diagnostics;
using System.Linq.Expressions;
using Inkslab.Linq.Enums;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 条件访问器。
    /// </summary>
    [DebuggerDisplay("ConditionalVisitor")]
    public class ConditionalVisitor : BaseVisitor
    {
        /// <inheritdoc/>
        public ConditionalVisitor(BaseVisitor visitor) : base(visitor)
        {
        }

        /// <inheritdoc/>
        public override void Startup(Expression node)
        {
            if (node.NodeType != ExpressionType.Conditional)
            {
                throw new InvalidOperationException();
            }

            base.Startup(node);
        }

        /// <inheritdoc/>
        protected override void Conditional(ConditionalExpression node)
        {
            if (IsPlainVariable(node.Test, true))
            {
                var constant = node.Test.GetValueFromExpression<bool>();
                var valueNode = constant ? node.IfTrue : node.IfFalse;

                VisitConditionalEscape(valueNode);

                return;
            }

            using var domain = Writer.Domain();

            if (RequiresConditionalEscape())
            {
                Condition(node.Test);
            }
            else
            {
                Visit(node.Test);
            }

            if (domain.IsEmpty)
            {
                VisitConditionalEscape(node.IfTrue);

                return;
            }

            Writer.Keyword(SqlKeyword.THEN);
            VisitConditionalEscape(node.IfTrue, true);
            Writer.Keyword(SqlKeyword.ELSE);
            VisitConditionalEscape(node.IfFalse, true);
            Writer.Keyword(SqlKeyword.END);
            Writer.CloseBrace();
            domain.Flyback();
            Writer.OpenBrace();
            Writer.Keyword(SqlKeyword.CASE);
            Writer.Keyword(SqlKeyword.WHEN);
        }

        /// <summary>
        /// 访问条件分支节点，若需要转义则生成 <c>CASE WHEN ... THEN TRUE ELSE FALSE END</c>。
        /// </summary>
        private void VisitConditionalEscape(Expression node, bool isSubExpression = false)
        {
            if (!RequiresConditionalEscape() || !IsCondition(node))
            {
                Load(node);

                return;
            }

            using var domainSub = Writer.Domain();

            Condition(node);

            if (domainSub.IsEmpty)
            {
                Writer.Keyword(SqlKeyword.NULL);

                return;
            }

            Writer.Keyword(SqlKeyword.THEN);
            Writer.True();
            Writer.Keyword(SqlKeyword.ELSE);
            Writer.False();
            Writer.Keyword(SqlKeyword.END);

            if (isSubExpression)
            {
                Writer.OpenBrace();
            }

            domainSub.Flyback();

            if (isSubExpression)
            {
                Writer.CloseBrace();
            }

            Writer.Keyword(SqlKeyword.CASE);
            Writer.Keyword(SqlKeyword.WHEN);
        }

        /// <summary>
        /// 访问条件表达式节点。
        /// </summary>
        /// <param name="node">条件表达式节点。</param>
        protected virtual void Load(Expression node)
        {
            var length = Writer.Length;

            Visit(node);

            if (Writer.Length == length)
            {
                Writer.Keyword(SqlKeyword.NULL);
            }
        }
    }
}