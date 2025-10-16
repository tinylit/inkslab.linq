using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Inkslab.Linq.Exceptions;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 聚合查询访问器。
    /// </summary>
    [DebuggerDisplay("AggregateSelect")]
    public class AggregateSelectVisitor : SelectVisitor
    {
        private readonly bool _showAs;
        private readonly ScriptVisitor _visitor;
        private readonly Dictionary<MemberInfo, Expression> _keyExpressions = new Dictionary<MemberInfo, Expression>(MemberEqualityComparer.Instance);

        private readonly Dictionary<Type, Expression> _attachmentExpressions = new Dictionary<Type, Expression>();
        private readonly Dictionary<string, Expression> _elementExpressions = new Dictionary<string, Expression>();

        /// <inheritdoc/>
        public AggregateSelectVisitor(ScriptVisitor visitor, bool showAs = true) : base(visitor, ConditionType.Having)
        {
            _showAs = showAs;
            _visitor = visitor;
        }

        /// <inheritdoc/>
        protected override bool Preheat(MethodCallExpression node)
        {
            //? 忽略 Group By 的参数分析。

            return true;
        }

        /// <inheritdoc/>
        protected override void PreparingParameter(LambdaExpression node)
        {
            //? 忽略 Group By 的参数分析。
        }

        /// <inheritdoc/>
        protected override bool ParameterRelationshipSupplementIsSupport() => false;//? 聚合查询不支持参数预热。

        /// <inheritdoc/>
        protected override void MemberHasDependency(MemberExpression node)
        {
            string memberName = node.Member.Name;

            if (memberName == "Key" && node.Expression.Type.IsGrouping())
            {
                bool commaFlag = false;

                foreach (var keyNode in _keyExpressions.Values)
                {
                    if (commaFlag)
                    {
                        Writer.Delimiter();
                    }
                    else
                    {
                        commaFlag = true;
                    }

                    Visit(keyNode);
                }
            }
            else if (node.Expression.NodeType == ExpressionType.Parameter)
            {
                if (_elementExpressions.TryGetValue(memberName, out Expression elementNode))
                {
                    _visitor.Visit(elementNode);
                }
                else if (_attachmentExpressions.TryGetValue(node.Expression.Type, out Expression attachmentNode))
                {
                    _visitor.Visit(new ReplaceExpressionVisitor(node.Expression, attachmentNode)
                        .Visit(node));
                }
                else
                {
                    base.MemberHasDependency(node);
                }
            }
            else if (node.IsGrouping())
            {
                if (_keyExpressions.TryGetValue(node.Member, out Expression expression))
                {
                    Visit(expression);
                }
                else
                {
                    throw new DSyntaxErrorException($"列“{memberName}”无效，因为该列没有包含在聚合函数或“GROUP BY”子句中！");
                }
            }
            else
            {
                base.MemberHasDependency(node);
            }
        }

        /// <inheritdoc/>
        protected override void MemberLeavesIsObject(MemberExpression node)
        {
            if (node.Member.Name == "Key" && node.Expression.Type.IsGrouping())
            {
                bool commaFlag = false;

                foreach (var (memberInfo, memberNode) in _keyExpressions)
                {
                    using (var domain = Writer.Domain())
                    {
                        Member(memberInfo, memberNode);

                        if (commaFlag)
                        {
                            domain.Flyback();

                            Writer.Delimiter();
                        }
                        else
                        {
                            commaFlag = true;
                        }
                    }
                }
            }
            else
            {
                base.MemberLeavesIsObject(node);
            }
        }

        /// <inheritdoc/>
        protected override void LinqCore(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case nameof(Queryable.GroupBy):
                    using (var visitor = new GroupByVisitor(_visitor, _attachmentExpressions, _elementExpressions, _keyExpressions))
                    {
                        visitor.Startup(node);
                    }
                    break;
                default:
                    base.LinqCore(node);

                    break;
            }
        }

        /// <inheritdoc/>
        protected override void ByEnumerable(MethodCallExpression node)
        {
            if (node.IsGrouping())
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
        protected override void Condition(Expression node)
        {
            using (var visitor = new ConditionVisitor(this, true))
            {
                visitor.Startup(node);
            }
        }

        /// <inheritdoc/>
        protected override void Select(Expression node)
        {
            using (var visitor = new GroupSelectListVisitor(this, _keyExpressions, _showAs))
            {
                visitor.Startup(node);
            }
        }

        /// <inheritdoc/>
        protected override void OrderBy(Expression node, Action declaration, bool isDescending)
        {
            using (var visitor = new OrderByVisitor(this, declaration, isDescending, true))
            {
                visitor.Startup(node);
            }
        }

        #region 内嵌类。
        private class ReplaceExpressionVisitor : ExpressionVisitor
        {
            private readonly Expression _oldExpression;
            private readonly Expression _newExpression;

            public ReplaceExpressionVisitor(Expression oldExpression, Expression newExpression)
            {
                _oldExpression = oldExpression;
                _newExpression = newExpression;
            }
            public override Expression Visit(Expression node)
            {
                if (_oldExpression == node)
                {
                    return base.Visit(_newExpression);
                }

                return base.Visit(node);
            }
        }
        private class MemberEqualityComparer : IEqualityComparer<MemberInfo>
        {
            public bool Equals(MemberInfo x, MemberInfo y) => string.Equals(x.Name, y.Name);

            public int GetHashCode(MemberInfo obj) => obj.Name.GetHashCode();

            public static readonly MemberEqualityComparer Instance = new MemberEqualityComparer();
        }

        private class GroupSelectListVisitor : SelectListVisitor
        {
            private readonly Dictionary<MemberInfo, Expression> _keyExpressions;

            public GroupSelectListVisitor(CoreVisitor visitor, Dictionary<MemberInfo, Expression> keyExpressions, bool showAs) : base(visitor, showAs, true)
            {
                _keyExpressions = keyExpressions;
            }

            /// <inheritdoc/>
            protected override void MemberLeavesIsObject(MemberExpression node)
            {
                bool commaFlag = false;

                foreach (var (memberInfo, memberNode) in _keyExpressions)
                {
                    using (var domain = Writer.Domain())
                    {
                        Member(memberInfo, memberNode);

                        if (commaFlag)
                        {
                            domain.Flyback();

                            Writer.Delimiter();
                        }
                        else
                        {
                            commaFlag = true;
                        }
                    }
                }
            }
        }
        #endregion
    }
}