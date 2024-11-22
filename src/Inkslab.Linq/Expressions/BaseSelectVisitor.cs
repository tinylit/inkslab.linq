using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Inkslab.Linq.Enums;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 查询基础访问器（负责组合、排序、分页等工作）。
    /// </summary>
    public class BaseSelectVisitor : ScriptVisitor
    {
        /// <summary>
        /// 生成 SELECT。
        /// </summary>
        private bool buildSelect = false;

        /// <summary>
        /// 去重。
        /// </summary>
        private bool isDistinct = false;

        /// <summary>
        /// 逆序。
        /// </summary>
        private bool reversed = false;

        private readonly bool _showAs;

        /// <summary>
        /// 排序开关。
        /// </summary>
        private readonly OrderBySwitch _orderBySwitch;

        /// <inheritdoc/>
        protected BaseSelectVisitor(IDbAdapter adapter) : base(adapter)
        {
            _showAs = true;
            _orderBySwitch = new OrderBySwitch(Writer);
        }

        /// <inheritdoc/>
        protected BaseSelectVisitor(BaseVisitor visitor, bool showAs = false) : this(visitor, ConditionType.Where, true, showAs)
        {
        }

        /// <inheritdoc/>
        protected BaseSelectVisitor(BaseVisitor visitor, ConditionType conditionType, bool isNewWriter = false, bool showAs = false) : base(visitor, conditionType, isNewWriter)
        {
            _showAs = showAs;
            _orderBySwitch = new OrderBySwitch(Writer);
        }

        /// <inheritdoc/>
        public sealed override void Startup(Expression node)
        {
            if (node.NodeType == ExpressionType.Constant)
            {
                Writer.Keyword(SqlKeyword.SELECT);

                using (var domain = Writer.Domain())
                {
                    base.Startup(node); //? 先渲染条件，再补充查询字段。

                    domain.Flyback();

                    Select(node);
                }
            }
            else
            {
                base.Startup(node);
            }
        }

        /// <inheritdoc/>
        protected override void StartupCore(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case nameof(Queryable.Union):
                case nameof(Queryable.Concat):
                case nameof(Queryable.Intersect):
                case nameof(Queryable.Except):

                    //? 解决子查询分页的问题。
                    Writer.OpenBrace();

                    using (var visitor = new SelectVisitor(this, _showAs))
                    {
                        visitor.Startup(node.Arguments[0]);
                    }

                    Writer.CloseBrace();

                    switch (node.Method.Name)
                    {
                        case nameof(Queryable.Union):
                            Writer.Keyword(SqlKeyword.UNION);
                            break;
                        case nameof(Queryable.Intersect):
                            Writer.Keyword(SqlKeyword.INTERSECT);
                            break;
                        case nameof(Queryable.Except):
                            Writer.Keyword(SqlKeyword.EXCEPT);
                            break;
                        default:
                            Writer.Keyword(SqlKeyword.UNION);
                            Writer.Keyword(SqlKeyword.ALL);
                            break;
                    }

                    //? 解决子查询分页的问题。
                    Writer.OpenBrace();

                    using (var visitor = new SelectVisitor(this))
                    {
                        visitor.Startup(node.Arguments[1]);
                    }

                    Writer.CloseBrace();

                    break;
                case nameof(Queryable.Max):
                case nameof(Queryable.Min):
                case nameof(Queryable.Average):
                case nameof(Queryable.Aggregate):

                    using (var visitor = new AggregateVisitor(this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case nameof(Queryable.Join):
                case nameof(Queryable.SelectMany):

                    //? 跳过 buildSelect 问题。
                    base.StartupCore(node);

                    break;
                default:

                    if (IsGrouping(node))
                    {
                        using (var visitor = new AggregateSelectVisitor(this))
                        {
                            visitor.Startup(node);
                        }
                    }
                    else
                    {
                        switch (node.Method.Name)
                        {
                            case nameof(Queryable.Count) when node.Arguments.Count == 1:
                            case nameof(Queryable.LongCount) when node.Arguments.Count == 1:

                                using (var visitor = new CountVisitor(this))
                                {
                                    visitor.Startup(node);
                                }

                                break;
                            case nameof(Queryable.Count):
                            case nameof(Queryable.LongCount):
                                Writer.Keyword(SqlKeyword.SELECT);

                                Writer.Write("COUNT");

                                Writer.OpenBrace();

                                Writer.Write('*');

                                Writer.CloseBrace();

                                Where(node);

                                break;
                            default:

                                buildSelect = true;

                                base.StartupCore(node);

                                break;
                        }
                    }

                    break;
            }
        }

        private bool IsGrouping(MethodCallExpression node)
        {
            return node.Method.Name switch
            {
                nameof(Queryable.Take) or nameof(Queryable.Skip) or nameof(Queryable.TakeLast) => IsGrouping((MethodCallExpression)node.Arguments[0]),
                _ => node.IsGrouping(true),
            };
        }

        /// <inheritdoc/>
        protected override void LinqRef(MethodCallExpression node, ref bool allowSelect)
        {
            base.LinqRef(node, ref allowSelect);

            buildSelect = allowSelect;
        }

        /// <inheritdoc/>
        protected override void LinqCore(MethodCallExpression node)
        {
            var name = node.Method.Name;

            switch (name)
            {
                case nameof(Queryable.Select):

                    buildSelect &= false;

                    Writer.Keyword(SqlKeyword.SELECT);

                    if (isDistinct)
                    {
                        Writer.Keyword(SqlKeyword.DISTINCT);
                    }

                    using (var domain = Writer.Domain())
                    {
                        Visit(node.Arguments[0]);

                        domain.Flyback();

                        Select(node.Arguments[1]); //? 解决 JOIN/GROUP 场景的表别名问题。
                    }
                    break;

                case nameof(Queryable.Distinct):

                    isDistinct = true;

                    Visit(node.Arguments[0]);

                    break;
                case nameof(Queryable.Union):
                case nameof(Queryable.Concat):
                case nameof(Queryable.Intersect):
                case nameof(Queryable.Except):

                    if (buildSelect)
                    {
                        Writer.Keyword(SqlKeyword.SELECT);

                        Writer.Write('*');

                        buildSelect = false;
                    }

                    DataSourceMode();

                    Writer.OpenBrace();

                    //? 解决子查询分页的问题。
                    Writer.OpenBrace();

                    using (var visitor = new SelectVisitor(this, _showAs))
                    {
                        visitor.Startup(node.Arguments[0]);
                    }

                    Writer.CloseBrace();

                    switch (name)
                    {
                        case nameof(Queryable.Union):
                            Writer.Keyword(SqlKeyword.UNION);
                            break;
                        case nameof(Queryable.Intersect):
                            Writer.Keyword(SqlKeyword.INTERSECT);
                            break;
                        case nameof(Queryable.Except):
                            Writer.Keyword(SqlKeyword.EXCEPT);
                            break;
                        default:
                            Writer.Keyword(SqlKeyword.UNION);
                            Writer.Keyword(SqlKeyword.ALL);
                            break;
                    }

                    //? 解决子查询分页的问题。
                    Writer.OpenBrace();

                    using (var visitor = new SelectVisitor(this))
                    {
                        visitor.Startup(node.Arguments[1]);
                    }

                    Writer.CloseBrace();

                    Writer.CloseBrace();

                    TableAs();

                    break;
                case nameof(Queryable.Contains):

                    Visit(node.Arguments[1]);

                    Writer.Keyword(SqlKeyword.IN);

                    Writer.OpenBrace();

                    using (var visitor = new SelectVisitor(this))
                    {
                        visitor.Startup(node.Arguments[0]);
                    }

                    Writer.CloseBrace();

                    break;
                case nameof(Queryable.All):

                    Writer.Keyword(SqlKeyword.NOT);

                    goto case nameof(Queryable.Any);
                case nameof(Queryable.Any):

                    Writer.Keyword(SqlKeyword.EXISTS);

                    Writer.OpenBrace();

                    using (var visitor = new AnyAllVisitor(this))
                    {
                        visitor.Startup(node);
                    }

                    Writer.CloseBrace();

                    break;
                case nameof(Queryable.Max):
                case nameof(Queryable.Min):
                case nameof(Queryable.Average):
                case nameof(Queryable.Aggregate):

                    using (var visitor = new AggregateVisitor(this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case nameof(Queryable.First):
                case nameof(Queryable.FirstOrDefault):
                case nameof(Queryable.Single):
                case nameof(Queryable.SingleOrDefault):

                    Writer.TakeSize(1);

                    if (node.Arguments.Count > 1)
                    {
                        Where(node);
                    }
                    else
                    {
                        Visit(node.Arguments[0]);
                    }

                    break;
                case nameof(Queryable.Last):
                case nameof(Queryable.LastOrDefault):

                    Writer.TakeSize(1);

                    Reverse(node);

                    break;
                case nameof(Queryable.ElementAt):
                case nameof(Queryable.ElementAtOrDefault):

                    Writer.ElementAt(node.Arguments[1].GetValueFromExpression<int>());

                    Visit(node.Arguments[0]);

                    break;
                case nameof(Queryable.Take):

                    Writer.TakeSize(node.Arguments[1].GetValueFromExpression<int>());

                    Visit(node.Arguments[0]);

                    break;

                case nameof(Queryable.Skip):

                    Writer.SkipSize(node.Arguments[1].GetValueFromExpression<int>());

                    Visit(node.Arguments[0]);

                    break;

                case nameof(Queryable.TakeLast):

                    Writer.TakeSize(node.Arguments[1].GetValueFromExpression<int>());

                    Reverse(node);

                    break;

                case nameof(Queryable.SkipLast):

                    Writer.SkipSize(node.Arguments[1].GetValueFromExpression<int>());

                    Reverse(node);

                    break;

                case nameof(Queryable.Reverse):

                    Reverse(node);

                    break;
                case nameof(Queryable.OrderBy):
                case nameof(Queryable.ThenBy):
                case nameof(Queryable.OrderByDescending):
                case nameof(Queryable.ThenByDescending):

                    bool isDescending = reversed ^ name.EndsWith("Descending");

                    Visit(node.Arguments[0]);

                    using (Writer.OrderByAnalysis())
                    {
                        _orderBySwitch.Execute();

                        OrderBy(node.Arguments[1], isDescending);
                    }

                    break;
                default:

                    base.LinqCore(node);

                    break;
            }
        }

        /// <inheritdoc/>
        protected override void Constant(IQueryable value)
        {
            if (buildSelect)
            {
                Writer.Keyword(SqlKeyword.SELECT);

                if (isDistinct)
                {
                    Writer.Keyword(SqlKeyword.DISTINCT);
                }

                Select(value.Expression);

                buildSelect = false;
            }

            base.Constant(value);
        }


        /// <summary>
        /// 逆序和条件分析。
        /// </summary>
        /// <param name="node">节点。</param>
        protected virtual void Reverse(MethodCallExpression node)
        {
            reversed ^= true;

            switch (node.Method.Name)
            {
                case nameof(Queryable.Last) when node.Arguments.Count > 1:
                case nameof(Queryable.LastOrDefault) when node.Arguments.Count > 1:
                    Where(node);
                    break;
                case nameof(Queryable.Last):
                case nameof(Queryable.LastOrDefault):
                case nameof(Queryable.Reverse):
                case nameof(Queryable.TakeLast):
                case nameof(Queryable.SkipLast):
                    Visit(node.Arguments[0]);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 查询字段。
        /// </summary>
        /// <param name="node">节点。</param>
        protected virtual void Select(Expression node)
        {
            using (var visitor = new SelectListVisitor(this, _showAs))
            {
                visitor.Startup(node);
            }
        }

        /// <summary>
        /// 排序。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <param name="isDescending">是否倒序。</param>
        protected virtual void OrderBy(Expression node, bool isDescending)
        {
            using (var visitor = new OrderByVisitor(this, isDescending))
            {
                visitor.Visit(node);
            }
        }


        /// <inheritdoc/>
        protected override void Member(MemberInfo memberInfo, Expression node)
        {
            using (var visitor = new AsVisitor(this, _showAs))
            {
                visitor.Startup(memberInfo, node);
            }
        }

        /// <inheritdoc/>
        protected override void Member(string schema, string field, string name)
        {
            base.Member(schema, field, name);

            if (_showAs)
            {
                Writer.AsName(name);
            }
        }

        #region 内嵌类。
        private class OrderBySwitch
        {
            private bool isFirst = true;

            private readonly SqlWriter _writer;

            public OrderBySwitch(SqlWriter writer)
            {
                _writer = writer;
            }

            public void Execute()
            {
                if (isFirst)
                {
                    isFirst = false;

                    _writer.Keyword(SqlKeyword.ORDER);
                    _writer.Keyword(SqlKeyword.BY);
                }
                else
                {
                    _writer.Delimiter();
                }
            }
        }

        /// <summary>
        /// <see cref="Queryable.Any{TSource}(IQueryable{TSource})"/> 或
        /// <see cref="Queryable.Any{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/>
        /// </summary>
        private class AnyAllVisitor : BaseSelectVisitor
        {
            /// <inheritdoc/>
            public AnyAllVisitor(BaseVisitor visitor) : base(visitor, ConditionType.Where, true)
            {
            }

            /// <inheritdoc/>
            protected override void StartupCore(MethodCallExpression node)
            {
                if (node.Arguments.Count == 1)
                {
                    Startup(node.Arguments[0]);
                }
                else if (node.Method.Name == nameof(Queryable.All))
                {
                    using (Writer.ConditionReversal())
                    {
                        base.StartupCore(node);
                    }
                }
                else
                {
                    base.StartupCore(node);
                }
            }

            protected override void LinqCore(MethodCallExpression node)
            {
                switch (node.Method.Name)
                {
                    case nameof(Queryable.Any):
                    case nameof(Queryable.All):

                        Where(node);

                        break;
                    default:

                        base.LinqCore(node);

                        break;
                }

            }
        }
        #endregion
    }
}
