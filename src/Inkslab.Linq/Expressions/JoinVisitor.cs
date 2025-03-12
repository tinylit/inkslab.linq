using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Inkslab.Linq.Exceptions;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 链表访问器。
    /// </summary>
    public class JoinVisitor : SelectVisitor
    {
        private JoinType joinType = JoinType.INNER;

        private Transverter transverter = Transverter.Empty;

        private readonly ScriptVisitor _visitor;
        private readonly Dictionary<(Type, string), SelectVisitor> _joinRelationships;
        private readonly bool _buildSelect;
        private readonly TransverterVisitor _transverterVisitor;

        /// <inheritdoc/>
        public JoinVisitor(
            ScriptVisitor visitor,
            Dictionary<(Type, string), SelectVisitor> joinRelationships,
            bool buildSelect
        )
            : base(visitor, ConditionType.On)
        {
            _visitor = visitor;
            _joinRelationships = joinRelationships;
            _buildSelect = buildSelect;
            _transverterVisitor = new TransverterVisitor(this, _visitor, _joinRelationships);
        }

        ///<inheritdoc/>
        protected override void DataSourceMode()
        {
            switch (joinType)
            {
                case JoinType.INNER:
                    Writer.Keyword(Enums.SqlKeyword.INNER);
                    break;
                case JoinType.LEFT:
                    Writer.Keyword(Enums.SqlKeyword.LEFT);
                    break;
                case JoinType.RIGHT:
                    Writer.Keyword(Enums.SqlKeyword.RIGHT);
                    break;
                default:
                    Writer.Keyword(Enums.SqlKeyword.CROSS);
                    break;
            }

            Writer.Keyword(Enums.SqlKeyword.JOIN);
        }

        ///<inheritdoc/>
        protected override void PreparingParameter(LambdaExpression node)
        {
            //? 解决 CROSS JOIN 的问题。
        }

        ///<inheritdoc/>
        protected override bool TryPreparingParameter(
            ParameterExpression node,
            out ParameterExpression parameter
        )
        {
            if (transverter.TryGetValue(node, out parameter))
            {
                if (transverter.IsBridge)
                {
                    return base.TryPreparingParameter(parameter, out parameter);
                }

                return PreparingParameterRelationship(node.Name, parameter);
            }

            return base.TryPreparingParameter(node, out parameter);
        }

        ///<inheritdoc/>
        protected override bool TryGetSourceParameter(
            Expression node,
            out ParameterExpression parameterExpression
        )
        {
            if (transverter.TryGetValue(node, out parameterExpression))
            {
                if (transverter.IsBridge)
                {
                    return base.TryGetSourceParameter(parameterExpression, out parameterExpression);
                }

                return true;
            }

            return base.TryGetSourceParameter(node, out parameterExpression);
        }

        ///<inheritdoc/>
        protected override bool TryGetSourceTableInfo(ParameterExpression node, out ITableInfo tableInfo)
        {
            return _transverterVisitor.TryGetTableInfo(node, out tableInfo);
        }

        /// <inheritdoc/>
        protected override void StartupCore(MethodCallExpression node)
        {
            if (_buildSelect)
            {
                Writer.Keyword(Enums.SqlKeyword.SELECT);
            }

            if (node.Method.Name == nameof(Queryable.SelectMany))
            {
                //? 分析是否为左链接。
                new DefaultIfEmptyVisitor(this)
                    .Visit(node.Arguments[1]);

                _transverterVisitor.ReadySelect(node.Arguments[2]);

                var instanceArg = node.Arguments[0];

                if (instanceArg.NodeType == ExpressionType.Call)
                {
                    Visit(instanceArg);
                }
                else
                {
                    //? 准备参数。
                    _transverterVisitor.Prepare(node.Arguments[1]);

                    using (var domain = Writer.Domain())
                    {
                        _visitor.Visit(instanceArg);

                        domain.Flyback(); //? 先分析表信息，在构建字段。

                        var length = Writer.Length;

                        _transverterVisitor.Build();

                        //? 验证建立查询语句分析。
                        if (_buildSelect ^ (Writer.Length > length))
                        {
                            throw new DSyntaxErrorException("一个查询器中，有且仅有一次查询结果！");
                        }
                    }

                    Visit(node.Arguments[1]); //? 分析 JOIN 表。
                }
            }
            else
            {
                _transverterVisitor.ReadySelect(node.Arguments[4]);

                Join(node, _buildSelect);
            }
        }

        /// <inheritdoc/>
        protected override void LinqCore(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case nameof(Queryable.GroupJoin):

                    _transverterVisitor.ReadyWith(node.Arguments[3]); //? 分析分支数据源的表别名。

                    Join(node, _buildSelect);

                    break;
                default:

                    _visitor.Visit(node);

                    Visit(node.Arguments[1]); //? 分析 JOIN 表。

                    break;
            }
        }

        private void Join(MethodCallExpression node, bool buildSelect)
        {
            //? 准备参数。
            _transverterVisitor.Prepare(node.Arguments[2]);

            using (var domain = Writer.Domain())
            {
                _visitor.Circuity(node.Arguments[0]);

                using (
                    var visitor = new OnVisitor(
                        this,
                        node.Arguments[1],
                        new OnExpression(node.Arguments[2])
                    )
                )
                {
                    visitor.Startup(node.Arguments[3]);
                }

                domain.Flyback(); //? 先分析表信息，再构建查询字段。

                var length = Writer.Length;

                _transverterVisitor.Build();

                //? 验证建立查询语句分析。
                if (buildSelect ^ (Writer.Length > length))
                {
                    throw new DSyntaxErrorException("一个查询器中，有且仅有一次查询结果！");
                }
            }
        }

        /// <inheritdoc/>
        protected override void WhereDependency(Expression instance) => Circuity(instance);

        #region 内嵌类。
        private enum JoinType
        {
            INNER,
            LEFT,
            RIGHT,
            CROSS
        }

        private class Transverter
        {
            private readonly bool _isEmpty = true;

            private readonly ParameterExpression _parameter;
            private readonly HashSet<(Type, string)> _hashSet;

            private Transverter() => _hashSet = new HashSet<(Type, string)>(1);

            public Transverter(
                Type relationshipType,
                string relationshipName,
                ParameterExpression parameter,
                HashSet<(Type, string)> hashSet
            )
            {
                _isEmpty = false;

                _hashSet = hashSet;

                _parameter = parameter;

                _hashSet.Add((relationshipType, relationshipName));
            }

            /// <summary>
            /// 未使用。
            /// </summary>
            public bool IsEmpty => _isEmpty;

            /// <summary>
            /// 中间桥梁。
            /// </summary>
            public bool IsBridge => _hashSet.Count == 1;

            public bool TryGetValue(ParameterExpression node, out ParameterExpression parameter)
            {
                if (_isEmpty || !_hashSet.Contains((node.Type, node.Name)))
                {
                    parameter = null;

                    return false;
                }

                parameter = _parameter;

                return true;
            }

            public bool TryGetValue(Expression node, out ParameterExpression parameter)
            {
                if (_isEmpty)
                {
                    parameter = null;

                    return false;
                }

                switch (node)
                {
                    case ParameterExpression parameterExpression:

                        return TryGetValue(parameterExpression, out parameter);

                    case MemberExpression memberExpression
                        when _hashSet.Contains(
                            (memberExpression.Type, memberExpression.Member.Name)
                        ):

                        parameter = _parameter;

                        return true;
                    default:

                        parameter = null;

                        return false;
                }
            }

            /// <summary>
            /// 空。
            /// </summary>
            public static Transverter Empty = new Transverter();
        }

        private class DefaultIfEmptyVisitor : ExpressionVisitor
        {
            private readonly JoinVisitor _visitor;

            public DefaultIfEmptyVisitor(JoinVisitor visitor)
            {
                _visitor = visitor;
            }

            protected override Expression VisitConstant(ConstantExpression node)
            {
                _visitor.joinType = JoinType.CROSS;

                return base.VisitConstant(node);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.Name == nameof(Queryable.DefaultIfEmpty))
                {
                    _visitor.joinType = JoinType.LEFT;

                    return base.VisitMethodCall(node);
                }

                if (node.Method.Name == nameof(QueryableExtentions.DataSharding))
                {
                    return base.VisitMethodCall(node);
                }

                throw new DSyntaxErrorException();
            }
        }

        private class TransverterVisitor : BaseVisitor
        {
            private bool readyComplete = false;

            private readonly JoinVisitor _visitor;
            private readonly ScriptVisitor _scriptVisitor;
            private readonly List<Expression> _expressions = new List<Expression>(1);
            private readonly Dictionary<(Type, string), SelectVisitor> _joinRelationships;
            private readonly HashSet<(Type, string)> _hashSet = new HashSet<(Type, string)>(1);

            public TransverterVisitor(
                JoinVisitor visitor,
                ScriptVisitor scriptVisitor,
                Dictionary<(Type, string), SelectVisitor> joinRelationships
            ) : base(scriptVisitor)
            {
                _visitor = visitor;
                _scriptVisitor = scriptVisitor;
                _joinRelationships = joinRelationships;
            }

            public void ReadySelect(Expression node) => _expressions.Add(node);

            public void ReadyWith(Expression node) => _expressions.Add(node);

            public void Prepare(Expression node)
            {
                var visitor = new PrepareVisitor(
                    _visitor,
                    _joinRelationships,
                    _hashSet
                );

                foreach (var expression in _expressions)
                {
                    visitor.Visit(expression);
                }

                Startup(node); //? 分析主表达式表别名。

                readyComplete = true;
            }

            protected override void PreparingParameter(LambdaExpression node)
            {
                if (!readyComplete)
                {
                    base.PreparingParameter(node); //? 准备主查询表别名。
                }
            }

            public bool TryGetTableInfo(ParameterExpression node, out ITableInfo tableInfo)
            {
                return base.TryGetSourceTableInfo(node, out tableInfo);
            }

            protected override void Lambda<T>(Expression<T> node)
            {
                if (readyComplete)
                {
                    _scriptVisitor.Visit(node.Body);//? 生成查询字段。
                }
            }

            public void Build() => Visit(_expressions[0]);

            private class PrepareVisitor : ExpressionVisitor
            {
                private readonly JoinVisitor _visitor;
                private readonly HashSet<(Type, string)> _hashSet;
                private readonly Dictionary<(Type, string), SelectVisitor> _joinRelationships;

                public PrepareVisitor(
                    JoinVisitor visitor,
                    Dictionary<(Type, string), SelectVisitor> joinRelationships,
                    HashSet<(Type, string)> hashSet
                )
                {
                    _hashSet = hashSet;
                    _visitor = visitor;
                    _joinRelationships = joinRelationships;
                }

                protected override Expression VisitLambda<T>(Expression<T> node)
                {
                    var parameter = node.Parameters[^1];

                    if (node.Parameters.Count == 2)
                    {
                        _visitor.ParameterRefresh(parameter);

                        _visitor.transverter = new Transverter(
                            parameter.Type,
                            parameter.Name,
                            parameter,
                            _hashSet
                        );
                    }
                    else
                    {
                        _hashSet.Add((parameter.Type, parameter.Name));
                    }

                    _joinRelationships.TryAdd((parameter.Type, parameter.Name), _visitor);

                    return node;
                }
            }
        }

        private class OnExpression : Expression
        {
            private Type _nodeType;
            private readonly Expression _node;

            public OnExpression(Expression node)
            {
                _node = node;
            }

            public void Ref(Expression node)
            {
                _nodeType = node.Type;
            }

            public override ExpressionType NodeType => _node.NodeType;

            public override bool CanReduce => true;

            public override Expression Reduce() => _node;

            public override Type Type => _nodeType ?? _node.Type;

            protected override Expression Accept(ExpressionVisitor visitor)
            {
                return new AcceptVisitor(visitor)
                    .Visit(_node);
            }

            private class AcceptVisitor : ExpressionVisitor
            {
                private readonly ExpressionVisitor _visitor;

                public AcceptVisitor(ExpressionVisitor visitor)
                {
                    _visitor = visitor;
                }

                protected override Expression VisitLambda<T>(Expression<T> node)
                {
                    _visitor.Visit(node.Body);

                    return node;
                }
            }
        }

        private class OnVisitor : BaseVisitor
        {
            private readonly JoinVisitor _visitor;
            private readonly Expression _instance;
            private readonly OnExpression _left;

            public OnVisitor(JoinVisitor visitor, Expression instance, OnExpression left)
                : base(visitor)
            {
                _visitor = visitor;
                _instance = instance;
                _left = left;
            }

            protected override void PreparingParameter(LambdaExpression node)
            {
                //? 不解析，已提前解析。
            }

            protected override void Lambda<T>(Expression<T> node)
            {
                _left.Ref(node.Body);

                _visitor.Where(_instance, Expression.Equal(_left, node.Body));
            }
        }
        #endregion
    }
}
