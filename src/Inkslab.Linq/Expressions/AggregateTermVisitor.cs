using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Inkslab.Linq.Enums;
using Inkslab.Linq.Exceptions;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 聚合项表达式。
    /// </summary>
    [DebuggerDisplay("AggregateTerm")]
    public class AggregateTermVisitor : CoreVisitor
    {
        /// <summary>
        /// 去重。
        /// </summary>
        private bool _isDistinct;

        /// <summary>
        /// 生成 SELECT。
        /// </summary>
        private bool _buildSelect = true;

        /// <inheritdoc/>
        public AggregateTermVisitor(BaseVisitor visitor) : base(visitor)
        {
        }

        /// <inheritdoc/>
        protected override void Member(MemberExpression node) => base.MemberHasDependency(node);

        /// <inheritdoc/>
        protected override void ByEnumerable(MethodCallExpression node) => LinqCall(node);//? 按照 Linq 语法检查方法。

        /// <inheritdoc/>
        protected override void Condition(Expression node)
        {
            using (var visitor = new ConditionVisitor(this, true))
            {
                visitor.Startup(node);
            }
        }

        /// <inheritdoc/>
        protected override void LinqCall(MethodCallExpression node)
        {
            var name = node.Method.Name;

            switch (name)
            {
                case nameof(Enumerable.Count) when node.Arguments.Count == 2:
                case nameof(Enumerable.LongCount) when node.Arguments.Count == 2:

                    Writer.Write("COUNT");

                    Writer.OpenBrace();

                    using (var domain = Writer.Domain())
                    {
                        Visit(node.Arguments[0]);

                        if (domain.HasValue)
                        {
                            Writer.Keyword(SqlKeyword.AND);
                        }

                        domain.Flyback();

                        if (_isDistinct)
                        {
                            Writer.Keyword(SqlKeyword.DISTINCT);
                        }

                        Writer.Keyword(SqlKeyword.CASE);
                        Writer.Keyword(SqlKeyword.WHEN);
                    }

                    Visit(node.Arguments[1]);

                    Writer.Keyword(SqlKeyword.THEN);

                    Writer.Write('1');

                    Writer.Keyword(SqlKeyword.END);

                    Writer.CloseBrace();

                    break;
                case nameof(Enumerable.Count):
                case nameof(Enumerable.LongCount):

                    Writer.Write("COUNT");

                    Writer.OpenBrace();

                    using (var domain = Writer.Domain())
                    {
                        Visit(node.Arguments[0]);

                        if (domain.IsEmpty)
                        {
                            Writer.Write('*');
                        }
                        else if (_buildSelect)
                        {
                            Writer.Keyword(SqlKeyword.THEN);

                            Writer.Write('1');

                            Writer.Keyword(SqlKeyword.END);

                            domain.Flyback();

                            Writer.Keyword(SqlKeyword.CASE);
                            Writer.Keyword(SqlKeyword.WHEN);
                        }
                    }

                    Writer.CloseBrace();

                    break;

                case nameof(Enumerable.Max) when node.Arguments.Count == 2:
                case nameof(Enumerable.Min) when node.Arguments.Count == 2:
                case nameof(Enumerable.Sum) when node.Arguments.Count == 2:
                case nameof(Enumerable.Average) when node.Arguments.Count == 2:

                    Writer.Write(name == nameof(Enumerable.Average) ? "AVG" : name.ToUpper());

                    Writer.OpenBrace();

                    using (var domain = Writer.Domain())
                    {
                        Visit(node.Arguments[0]);

                        if (domain.IsEmpty)
                        {
                            if (_isDistinct)
                            {
                                Writer.Keyword(SqlKeyword.DISTINCT);
                            }

                            Select(node.Arguments[1]);
                        }
                        else
                        {
                            Writer.Keyword(SqlKeyword.THEN);

                            Select(node.Arguments[1]);

                            Writer.Keyword(SqlKeyword.END);

                            domain.Flyback();

                            if (_isDistinct)
                            {
                                Writer.Keyword(SqlKeyword.DISTINCT);
                            }

                            Writer.Keyword(SqlKeyword.CASE);
                            Writer.Keyword(SqlKeyword.WHEN);
                        }
                    }

                    Writer.CloseBrace();

                    break;
                case nameof(Enumerable.Max):
                case nameof(Enumerable.Min):
                case nameof(Enumerable.Sum):
                case nameof(Enumerable.Average):

                    Writer.Write(name == nameof(Enumerable.Average) ? "AVG" : name.ToUpper());

                    Writer.OpenBrace();

                    Visit(node.Arguments[0]);

                    Writer.CloseBrace();

                    break;
                case nameof(Queryable.Select):

                    _buildSelect = false;

                    if (_isDistinct)
                    {
                        Writer.Keyword(SqlKeyword.DISTINCT);
                    }

                    using (var domain = Writer.Domain())
                    {
                        Visit(node.Arguments[0]);

                        if (domain.IsEmpty)
                        {
                            Select(node.Arguments[1]);
                        }
                        else
                        {
                            Writer.Keyword(SqlKeyword.THEN);

                            Select(node.Arguments[1]);

                            Writer.Keyword(SqlKeyword.END);

                            domain.Flyback();

                            Writer.Keyword(SqlKeyword.CASE);
                            Writer.Keyword(SqlKeyword.WHEN);
                        }
                    }

                    break;
                case nameof(Queryable.SkipWhile):
                    using (Writer.ConditionReversal())
                    {
                        goto case nameof(Queryable.Where);
                    }
                case nameof(Queryable.Where):
                case nameof(Queryable.TakeWhile):

                    using (var domain = Writer.Domain())
                    {
                        Visit(node.Arguments[0]);

                        if (domain.HasValue)
                        {
                            Writer.Keyword(SqlKeyword.AND);
                        }
                    }

                    Visit(node.Arguments[1]);

                    break;
                case nameof(Queryable.Distinct):

                    _isDistinct = true;

                    Visit(node.Arguments[0]);

                    break;
                default:
                    throw new DSyntaxErrorException($"在聚合查询中，聚合函数“{name}”不被识别!");
            }
        }

        /// <inheritdoc/>
        protected override void Constant(IQueryable value)
        {
            if (_buildSelect)
            {
                var node = value.Expression;

                if (node.Type.IsCell())
                {
                    Writer.Keyword(SqlKeyword.SELECT);

                    if (_isDistinct)
                    {
                        Writer.Keyword(SqlKeyword.DISTINCT);
                    }

                    Select(node);
                }
                else
                {
                    throw new DSyntaxErrorException("聚合查询不支持多字段！");
                }
            }

            base.Constant(value);
        }

        /// <summary>
        /// 生成 SELECT 语句。
        /// </summary>
        /// <param name="node">The expression node to generate the SELECT statement from.</param>
        protected virtual void Select(Expression node)
        {
            using (var visitor = new SelectListVisitor(this, true))
            {
                visitor.Startup(node);
            }
        }
    }
}