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
            using var domain = Writer.Domain();

            if (IsPlainVariable(node.Test, false))
            {
                var constant = node.Test.GetValueFromExpression<bool>();
                var valueNode = constant ? node.IfTrue : node.IfFalse;

                if (RequiresConditionalEscape() && IsCondition(valueNode))
                {
                    VisitConditionalEscapeInBrace(valueNode, domain);
                }
                else
                {
                    Visit(valueNode);
                }

                return;
            }

            Condition(node.Test);

            if (domain.IsEmpty)
            {
                if (RequiresConditionalEscape() && IsCondition(node.IfTrue))
                {
                    VisitConditionalEscapeInBrace(node.IfTrue, domain);
                }
                else
                {
                    Visit(node.IfTrue);
                }

                return;
            }

            Writer.Keyword(SqlKeyword.THEN);
            VisitConditionalEscape(node.IfTrue);
            Writer.Keyword(SqlKeyword.ELSE);
            VisitConditionalEscape(node.IfFalse);
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
        private void VisitConditionalEscape(Expression node)
        {
            if (!RequiresConditionalEscape() || !IsCondition(node))
            {
                Visit(node);

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
            domainSub.Flyback();
            Writer.Keyword(SqlKeyword.CASE);
            Writer.Keyword(SqlKeyword.WHEN);
        }

        /// <summary>
        /// 访问条件分支节点，生成 <c>CASE WHEN ... THEN TRUE ELSE FALSE END</c> 并用括号包裹整体表达式。
        /// </summary>
        private void VisitConditionalEscapeInBrace(Expression node, ISqlDomain outerDomain)
        {
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
            domainSub.Flyback();
            Writer.Keyword(SqlKeyword.CASE);
            Writer.Keyword(SqlKeyword.WHEN);
            Writer.CloseBrace();
            outerDomain.Flyback();
            Writer.OpenBrace();
        }
    }
}