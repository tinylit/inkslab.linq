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
    /// 查询基础访问器（负责组合、排序、分页等工作）。
    /// </summary>
    [DebuggerDisplay("Select")]
    public class SelectVisitor : ScriptVisitor
    {
        /// <summary>
        /// 逆序。
        /// </summary>
        private bool reversed = false;

        private readonly bool _showAs;

        /// <summary>
        /// 排序开关。
        /// </summary>
        private readonly OrderBySwitch _orderBySwitch;

        /// <summary>
        /// 父级是否条件反转。
        /// </summary>
        private readonly bool _parentIsConditionReversal = false;

        /// <inheritdoc/>
        protected SelectVisitor(DbStrictAdapter adapter) : base(adapter)
        {
            _showAs = true;
            _orderBySwitch = new OrderBySwitch(Writer);
        }

        /// <inheritdoc/>
        public SelectVisitor(BaseVisitor visitor, bool showAs = false) : this(visitor, ConditionType.Where, true, showAs)
        {
        }

        /// <inheritdoc/>
        protected SelectVisitor(BaseVisitor visitor, ConditionType conditionType, bool isNewWriter = false, bool showAs = false) : base(visitor, conditionType, isNewWriter)
        {
            _showAs = showAs;
            _orderBySwitch = new OrderBySwitch(Writer);
            _parentIsConditionReversal = visitor.IsConditionReversal;
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
                case nameof(Queryable.Count) when node.IsGrouping(true):
                case nameof(Queryable.LongCount) when node.IsGrouping(true):
                    goto default;
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

                    base.StartupCore(node);

                    break;
            }
        }

        /// <inheritdoc/>
        protected override void LinqCore(MethodCallExpression node)
        {
            var name = node.Method.Name;

            switch (name)
            {
                case nameof(Queryable.Contains):

                    Visit(node.Arguments[1]);

                    if (_parentIsConditionReversal)
                    {
                        Writer.Keyword(SqlKeyword.NOT);
                    }

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

                    if (_parentIsConditionReversal)
                    {
                        Writer.Keyword(SqlKeyword.NOT);
                    }

                    Writer.Keyword(SqlKeyword.EXISTS);

                    Writer.OpenBrace();

                    using (var visitor = new AnyAllVisitor(this))
                    {
                        visitor.Startup(node);
                    }

                    Writer.CloseBrace();

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

                    bool isDescending = reversed ^ name.EndsWith("Descending"); //? 是否降序，前置分析是否倒序，否则实际顺序会乱序。

                    Visit(node.Arguments[0]);

                    using (Writer.OrderByAnalysis())
                    {
                        OrderBy(node.Arguments[1], _orderBySwitch.Execute, isDescending);
                    }

                    break;
                default:

                    base.LinqCore(node);

                    break;
            }
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
        protected override void Select(Expression node)
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
        /// <param name="declaration">声明。</param>
        /// <param name="isDescending">是否倒序。</param>
        protected virtual void OrderBy(Expression node, Action declaration, bool isDescending)
        {
            using (var visitor = new OrderByVisitor(this, declaration, isDescending))
            {
                visitor.Startup(node);
            }
        }

        /// <inheritdoc/>
        protected override void Member(MemberInfo memberInfo, Expression node)
        {
            using (var visitor = new AsVisitor(this))
            {
                visitor.Startup(node);
            }

            if (_showAs)
            {
                Writer.AsName(memberInfo.Name);
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
        [DebuggerDisplay("Any/All")]
        private class AnyAllVisitor : SelectVisitor
        {
            /// <inheritdoc/>
            public AnyAllVisitor(SelectVisitor visitor) : base(visitor, ConditionType.Where, true)
            {
            }

            /// <inheritdoc/>
            protected override void StartupCore(MethodCallExpression node)
            {
                if (node.Arguments.Count == 1)
                {
                    Visit(node.Arguments[0]);
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
