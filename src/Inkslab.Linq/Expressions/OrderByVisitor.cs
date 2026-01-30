using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Inkslab.Linq.Enums;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 排序。
    /// </summary>
    [DebuggerDisplay("OrderBy")]
    public class OrderByVisitor : CoreVisitor
    {
        private readonly SelectVisitor _visitor;
        private readonly bool _isDescending;
        private readonly Action _declaration;
        private readonly bool _isGroupHaving;

        private volatile bool _haveWritten;

        /// <inheritdoc/>
        public OrderByVisitor(SelectVisitor visitor, Action declaration, bool isDescending, bool isGroupHaving = false) : base(visitor)
        {
            _visitor = visitor;
            _isDescending = isDescending;
            _declaration = declaration;
            _isGroupHaving = isGroupHaving;
        }

        /// <inheritdoc/>
        protected override void Member(MemberExpression node)
        {
            if (_haveWritten)
            {
                base.Member(node);

                return;
            }

            _declaration.Invoke();

            base.Member(node);

            if (_isDescending)
            {
                Writer.Keyword(SqlKeyword.DESC);
            }
        }

        /// <inheritdoc/>
        protected override void Member(MemberInfo memberInfo, Expression node)
        {
            _haveWritten = true;

            _declaration.Invoke();

            base.Member(memberInfo, node);

            if (_isDescending)
            {
                Writer.Keyword(SqlKeyword.DESC);
            }

            _haveWritten = false;
        }

        /// <inheritdoc/>
        protected override void Conditional(ConditionalExpression node)
        {
            _haveWritten = true;
            
            _declaration.Invoke();

            base.Conditional(node);

            if (_isDescending)
            {
                Writer.Keyword(SqlKeyword.DESC);
            }

            _haveWritten = false;
        }

        /// <inheritdoc/>
        protected override void Unary(UnaryExpression node)
        {
            _haveWritten = true;

            _declaration.Invoke();

            using (var visitor = new MemberVisitor(this, _isGroupHaving))
            {
                visitor.Startup(node);
            }

            if (_isDescending)
            {
                Writer.Keyword(SqlKeyword.DESC);
            }

            _haveWritten = false;
        }

        /// <inheritdoc/>
        protected override void Binary(BinaryExpression node)
        {
            _haveWritten = true;

            _declaration.Invoke();

            using (var visitor = new MemberVisitor(this, _isGroupHaving))
            {
                visitor.Startup(node);
            }

            if (_isDescending)
            {
                Writer.Keyword(SqlKeyword.DESC);
            }

            _haveWritten = false;
        }

        /// <inheritdoc/>
        protected override void ByString(MethodCallExpression node)
        {
            _haveWritten = true;

            _declaration.Invoke();

            using (var visitor = new MemberVisitor(this, _isGroupHaving))
            {
                visitor.Startup(node);
            }

            if (_isDescending)
            {
                Writer.Keyword(SqlKeyword.DESC);
            }

            _haveWritten = false;
        }

        /// <inheritdoc/>
        protected override void Member(string schema, string field, string name)
        {
            if(_haveWritten)
            {
                base.Member(schema, field, name);

                return;
            }

            _declaration.Invoke();

            base.Member(schema, field, name);

            if (_isDescending)
            {
                Writer.Keyword(SqlKeyword.DESC);
            }
        }

        /// <inheritdoc/>
        protected override void ByEnumerable(MethodCallExpression node)
        {
            if (_isGroupHaving && node.IsGrouping())
            {
                using (var visitor = new GroupByAggregateTermVisitor(_visitor, _declaration, _isDescending))
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
                                    ? (descendingVisitor ??= new OrderByVisitor(_visitor, _declaration, rankExpression.IsDescending ^ _isDescending, _isGroupHaving))
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
            public abstract List<RankExpression> Ranks { get; }
        }


        private class Rank<TSource> : Rank, IRank<TSource>, IOrderBy<TSource>, IThenBy<TSource>, IDefaultBy<TSource>
        {
            private bool initialDefault = true;

            private readonly List<RankExpression> _ranks;

            public Rank() => _ranks = new List<RankExpression>();

            public override List<RankExpression> Ranks => _ranks;

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
                    initialDefault = false;

                    return this;
                }

                return new EmptyRank<TSource>(this);
            }

            public IDefaultBy<TSource> DefaultBy<TItem>(Expression<Func<TSource, TItem>> rank)
            {
                if (rank is null)
                {
                    throw new ArgumentNullException(nameof(rank));
                }

                if (initialDefault)
                {
                    Ranks.Add(new RankExpression(rank, false));
                }

                return this;
            }

            public IDefaultBy<TSource> DefaultByDescending<TItem>(Expression<Func<TSource, TItem>> rank)
            {
                if (rank is null)
                {
                    throw new ArgumentNullException(nameof(rank));
                }

                if (initialDefault)
                {
                    Ranks.Add(new RankExpression(rank, true));
                }

                return this;
            }
        }

        private class EmptyRank<TSource> : Rank, IOrderBy<TSource>, IThenBy<TSource>
        {
            private readonly Rank<TSource> _rank;

            public EmptyRank(Rank<TSource> rank)
            {
                _rank = rank;
            }

            public override List<RankExpression> Ranks => _rank.Ranks;

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

            public IDefaultBy<TSource> DefaultBy<TItem>(Expression<Func<TSource, TItem>> rank) => _rank.DefaultBy(rank);

            public IDefaultBy<TSource> DefaultByDescending<TItem>(Expression<Func<TSource, TItem>> rank) => _rank.DefaultByDescending(rank);
        }

        /// <summary>
        /// 排名项。
        /// </summary>
        private class RankExpression : Expression
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

        private class GroupByAggregateTermVisitor : AggregateTermVisitor
        {
            private readonly Action _declaration;
            private readonly bool _isDescending;

            public GroupByAggregateTermVisitor(SelectVisitor visitor, Action declaration, bool isDescending) : base(visitor)
            {
                _declaration = declaration;
                _isDescending = isDescending;
            }

            protected override void LinqCall(MethodCallExpression node)
            {
                var name = node.Method.Name;

                switch (name)
                {
                    case nameof(Enumerable.Count):
                    case nameof(Enumerable.LongCount):
                    case nameof(Enumerable.Max):
                    case nameof(Enumerable.Min):
                    case nameof(Enumerable.Sum):
                    case nameof(Enumerable.Average):

                        _declaration.Invoke();

                        base.LinqCall(node);

                        if (_isDescending)
                        {
                            Writer.Keyword(SqlKeyword.DESC);
                        }

                        break;
                    default:
                        base.LinqCall(node);

                        break;
                }
            }
        }
        #endregion
    }
}
