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
            if (node.NodeType == ExpressionType.Conditional)
            {
                base.Startup(node);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /// <inheritdoc/>
        protected override void Conditional(ConditionalExpression node)
        {
            using (var domain = Writer.Domain())
            {
                Condition(node.Test);

                if (domain.IsEmpty)
                {
                    if (RequiresConditionalEscape() && IsCondition(node.IfFalse))
                    {
                        using (var domainSub = Writer.Domain())
                        {
                            Condition(node.IfFalse);

                            if (domainSub.IsEmpty)
                            {
                                Writer.Keyword(SqlKeyword.NULL);
                            }
                            else
                            {
                                Writer.Keyword(SqlKeyword.THEN);

                                Writer.True();

                                Writer.Keyword(SqlKeyword.ELSE);

                                Writer.False();

                                Writer.Keyword(SqlKeyword.END);

                                domainSub.Flyback();

                                Writer.Keyword(SqlKeyword.CASE);
                                Writer.Keyword(SqlKeyword.WHEN);

                                Writer.CloseBrace();

                                domain.Flyback();

                                Writer.OpenBrace();
                            }
                        }
                    }
                    else
                    {
                        Visit(node.IfFalse);
                    }
                }
                else
                {
                    Writer.Keyword(SqlKeyword.THEN);

                    if (RequiresConditionalEscape() && IsCondition(node.IfTrue))
                    {
                        using (var domainSub = Writer.Domain())
                        {
                            Condition(node.IfTrue);

                            if (domainSub.IsEmpty)
                            {
                                Writer.Keyword(SqlKeyword.NULL);
                            }
                            else
                            {
                                Writer.Keyword(SqlKeyword.THEN);

                                Writer.True();

                                Writer.Keyword(SqlKeyword.ELSE);

                                Writer.False();

                                Writer.Keyword(SqlKeyword.END);

                                domainSub.Flyback();

                                Writer.Keyword(SqlKeyword.CASE);
                                Writer.Keyword(SqlKeyword.WHEN);
                            }
                        }
                    }
                    else
                    {
                        Visit(node.IfTrue);
                    }

                    Writer.Keyword(SqlKeyword.ELSE);

                    if (RequiresConditionalEscape() && IsCondition(node.IfFalse))
                    {
                        using (var domainSub = Writer.Domain())
                        {
                            Condition(node.IfFalse);

                            if (domainSub.IsEmpty)
                            {
                                Writer.Keyword(SqlKeyword.NULL);
                            }
                            else
                            {
                                Writer.Keyword(SqlKeyword.THEN);

                                Writer.True();

                                Writer.Keyword(SqlKeyword.ELSE);

                                Writer.False();

                                Writer.Keyword(SqlKeyword.END);

                                domainSub.Flyback();

                                Writer.Keyword(SqlKeyword.CASE);
                                Writer.Keyword(SqlKeyword.WHEN);
                            }
                        }
                    }
                    else
                    {
                        Visit(node.IfFalse);
                    }

                    Writer.Keyword(SqlKeyword.END);

                    Writer.CloseBrace();

                    domain.Flyback();

                    Writer.OpenBrace();

                    Writer.Keyword(SqlKeyword.CASE);
                    Writer.Keyword(SqlKeyword.WHEN);
                }
            }
        }
    }
}