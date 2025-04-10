using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 排序。
    /// </summary>
    [DebuggerDisplay("OrderBy")]
    public class OrderByVisitor : CoreVisitor
    {
        private readonly CoreVisitor _visitor;
        private readonly bool _isDescending;
        private readonly bool _isGroupHaving;

        /// <inheritdoc/>
        public OrderByVisitor(CoreVisitor visitor, bool isDescending, bool isGroupHaving = false) : base(visitor, false)
        {
            _visitor = visitor;
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

        /// <inheritdoc/>
        protected override void LinqCustomCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == Types.Ranks)
            {
                RankBy(node);

                return;
            }
            else
            {
                base.LinqCustomCall(node);
            }
        }

        private void RankBy(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case nameof(Ranks.By):
                    {
                        Rank rank;

                        try
                        {
                            rank = node.Arguments[1].GetValueFromExpressionWithArgs<Rank>(Activator.CreateInstance(typeof(Rank<>).MakeGenericType(node.Arguments[0].Type)));
                        }
                        catch (Exception ex)
                        {
                            throw new NotSupportedException("不支持的排名表达式，表达式的判断条件仅允许使用常量表达式！", ex);
                        }

                        if (rank.Ranks.Count == 0)
                        {
                            break;
                        }

                        // 准备参数。
                        var expressions = new List<Expression>();

                        var argVisitor = new ArgExpressionVisitor(expressions);

                        argVisitor.Visit(node.Arguments[0]);

                        OrderByVisitor descendingVisitor = null;

                        foreach (var rankExpression in rank.Ranks)
                        {
                            var rankVisitor = new OrderByExpressionVisitor(rankExpression.IsDescending
                                    ? (descendingVisitor ??= new OrderByVisitor(_visitor, rankExpression.IsDescending ^ _isDescending, _isGroupHaving))
                                    : this,
                                expressions);

                            rankVisitor.Visit(rankExpression.Body);
                        }

                        break;
                    }
                default:
                    throw new NotSupportedException();
            }
        }

        #region nested


        private class ArgExpressionVisitor : ExpressionVisitor
        {
            private readonly List<Expression> _expressions;

            public ArgExpressionVisitor(List<Expression> expressions)
            {
                _expressions = expressions;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                _expressions.Add(node);

                return node;
            }

            protected override Expression VisitNew(NewExpression node)
            {
                _expressions.AddRange(node.Arguments);

                return node;
            }

            protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
            {
                _expressions.Add(node.Expression);

                return node;
            }
        }

        private class OrderByExpressionVisitor : ExpressionVisitor
        {
            private readonly OrderByVisitor _visitor;
            private readonly List<Expression> _expressions;

            public OrderByExpressionVisitor(OrderByVisitor visitor, List<Expression> expressions)

            {
                _visitor = visitor;
                _expressions = expressions;
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                var replaceVisitor = new ReplaceManyExpressionVisitor(node.Parameters, _expressions);

                _visitor.Visit(replaceVisitor.Visit(node.Body));

                return node;
            }
        }

        private abstract class Rank
        {
            public List<RankExpression> Ranks { get; } = new List<RankExpression>();
        }


        private class Rank<TSource> : Rank, IRank<TSource>, IOrderBy<TSource>, IThenBy<TSource>
        {
            public IThenBy<TSource> OrderBy<TItem>(Expression<Func<TSource, TItem>> rank)
            {
                if (rank is null)
                {
                    throw new ArgumentNullException(nameof(rank));
                }

                Ranks.Add(new RankExpression(rank, false));

                return this;
            }

            public IThenBy<TSource> OrderByDescending<TItem>(Expression<Func<TSource, TItem>> rank)
            {
                if (rank is null)
                {
                    throw new ArgumentNullException(nameof(rank));
                }

                Ranks.Add(new RankExpression(rank, true));

                return this;
            }

            public IThenBy<TSource> ThenBy<TItem>(Expression<Func<TSource, TItem>> rank)
            {
                if (rank is null)
                {
                    throw new ArgumentNullException(nameof(rank));
                }

                Ranks.Add(new RankExpression(rank, false));

                return this;
            }

            public IThenBy<TSource> ThenByDescending<TItem>(Expression<Func<TSource, TItem>> rank)
            {
                if (rank is null)
                {
                    throw new ArgumentNullException(nameof(rank));
                }

                Ranks.Add(new RankExpression(rank, true));

                return this;
            }

            public IOrderBy<TSource> When(bool condition)
            {
                if (condition)
                {
                    return this;
                }

                return new EmptyRank<TSource>(this);
            }
        }

        private class EmptyRank<TSource> : Rank, IOrderBy<TSource>, IThenBy<TSource>
        {
            private readonly IRank<TSource> _rank;

            public EmptyRank(IRank<TSource> rank)
            {
                _rank = rank;
            }

            public IThenBy<TSource> OrderBy<TItem>(Expression<Func<TSource, TItem>> rank)
            {
                if (rank is null)
                {
                    throw new ArgumentNullException(nameof(rank));
                }

                return this;
            }

            public IThenBy<TSource> OrderByDescending<TItem>(Expression<Func<TSource, TItem>> rank)
            {
                if (rank is null)
                {
                    throw new ArgumentNullException(nameof(rank));
                }

                return this;
            }

            public IThenBy<TSource> ThenBy<TItem>(Expression<Func<TSource, TItem>> rank)
            {
                if (rank is null)
                {
                    throw new ArgumentNullException(nameof(rank));
                }

                return this;
            }

            public IThenBy<TSource> ThenByDescending<TItem>(Expression<Func<TSource, TItem>> rank)
            {
                if (rank is null)
                {
                    throw new ArgumentNullException(nameof(rank));
                }

                return this;
            }

            public IOrderBy<TSource> When(bool condition) => _rank.When(condition);
        }

        /// <summary>
        /// 排名项。
        /// </summary>
        public class RankExpression : Expression
        {
            internal RankExpression(Expression body, bool isDescending)
            {
                Body = body;
                IsDescending = isDescending;
            }

            /// <inheritdoc/>
            public override Expression Reduce() => Body;

            /// <summary>
            /// 是否可以减少。
            /// </summary>
            public override bool CanReduce => true;

            /// <summary>
            /// 获取类型。
            /// </summary>
            public override Type Type => Body.Type;

            /// <summary>
            /// 获取表达式类型。
            /// </summary>
            public override ExpressionType NodeType => ExpressionType.Extension;

            /// <summary>
            /// 排名表达式。
            /// </summary>
            public Expression Body { get; }

            /// <summary>
            /// 是否降序。
            /// </summary>
            public bool IsDescending { get; }
        }

        #endregion
    }
}
