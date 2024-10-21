using System;
using System.Linq;
using System.Linq.Expressions;
using Inkslab.Linq.Enums;
using Inkslab.Linq.Exceptions;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 聚合表达式。
    /// </summary>
    public class AggregateVisitor : AggregateSelectVisitor
    {
        /// <summary>
        /// 去重。
        /// </summary>
        private bool isDistinct = false;

        /// <summary>
        /// 分组。
        /// </summary>
        private bool isGrouping = false;

        private readonly BaseSelectVisitor _visitor;

        /// <inheritdoc/>
        public AggregateVisitor(BaseSelectVisitor visitor) : base(visitor, false)
        {
            _visitor = visitor;
        }

        /// <inheritdoc/>
        protected override void StartupCore(MethodCallExpression node)
        {
            var name = node.Method.Name;

            Writer.Keyword(SqlKeyword.SELECT);

            switch (name)
            {
                case nameof(Enumerable.Max) when node.Arguments.Count == 2:
                case nameof(Enumerable.Min) when node.Arguments.Count == 2:
                case nameof(Enumerable.Sum) when node.Arguments.Count == 2:
                case nameof(Enumerable.Average) when node.Arguments.Count == 2:

                    Writer.Write(name == nameof(Enumerable.Average) ? "AVG" : name.ToUpper());

                    Writer.OpenBrace();

                    using (var domain = Writer.Domain())
                    {
                        Visit(node.Arguments[0]);

                        domain.Flyback();

                        if (isDistinct)
                        {
                            Writer.Keyword(SqlKeyword.DISTINCT);
                        }

                        Select(node.Arguments[1]);

                        Writer.CloseBrace();
                    }

                    break;
                case nameof(Enumerable.Max):
                case nameof(Enumerable.Min):
                case nameof(Enumerable.Sum):
                case nameof(Enumerable.Average):

                    Writer.Write(name == nameof(Enumerable.Average) ? "AVG" : name.ToUpper());

                    Writer.OpenBrace();

                    Visit(node.Arguments[0]);

                    break;
                default:
                    throw new NotSupportedException($"聚合函数“{node.Method}”不被支持！");
            }
        }

        /// <inheritdoc/>
        protected override void LinqCore(MethodCallExpression node)
        {
            var name = node.Method.Name;

            switch (name)
            {
                case nameof(Queryable.Select):

                    if (isDistinct)
                    {
                        Writer.Keyword(SqlKeyword.DISTINCT);
                    }

                    using (var domian = Writer.Domain())
                    {
                        Visit(node.Arguments[0]);

                        domian.Flyback();

                        Select(node.Arguments[1]);

                        Writer.CloseBrace();
                    }

                    break;
                case nameof(Queryable.Distinct):

                    isDistinct = true;

                    Visit(node.Arguments[0]);

                    break;
                case nameof(Queryable.Reverse):
                case nameof(Queryable.OrderBy):
                case nameof(Queryable.ThenBy):
                case nameof(Queryable.OrderByDescending):
                case nameof(Queryable.ThenByDescending):
                case nameof(Queryable.DefaultIfEmpty):
                    throw new DSyntaxErrorException($"在聚合查询中，聚合函数“{name}”不被识别!");
                default:

                    if (isGrouping || (isGrouping = node.IsGrouping(true)))
                    {
                        base.LinqCore(node);
                    }
                    else
                    {
                        _visitor.Visit(node);
                    }

                    break;
            }
        }

        /// <inheritdoc/>
        protected internal override void Circuity(MethodCallExpression node) => _visitor.Circuity(node);
    }
}