using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Inkslab.Linq.Enums;
using Inkslab.Linq.Exceptions;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 脚本访问器（独立语句的核心结构，负责分析表名称、别名、条件等工作）。
    /// </summary>
    [DebuggerDisplay("Script")]
    public class ScriptVisitor : CoreVisitor, IBackflowVisitor
    {
        /// <summary>
        /// 允许 SELECT。
        /// </summary>
        private int? _commandTimeout;

        /// <summary>
        /// 无排序。
        /// </summary>
        private bool _unsorted = true;

        /// <summary>
        /// 允许 SELECT。
        /// </summary>
        private bool _allowSelect = true;

        /// <summary>
        /// 构建。
        /// </summary>
        private bool _buildSelect;

        /// <summary>
        /// 去重。
        /// </summary>
        private bool _isDistinct;

        /// <summary>
        /// 分组表达式。
        /// </summary>
        private bool _isGrouping;

        /// <summary>
        /// 聚合统计。
        /// </summary>
        private bool _isAggregateCount;

        /// <summary>
        /// 预热处理。
        /// </summary>
        private bool _treatmentPreheating = true;

        /// <summary>
        /// 参数刷新。
        /// </summary>
        private bool _parameterRef = true;

        /// <summary>
        /// 已预热参数。
        /// </summary>
        private bool _preheatedParameter;

        /// <summary>
        /// 预热参数。
        /// </summary>
        private bool _preheatingParameter = true;

        /// <summary>
        /// 参数刷新值。
        /// </summary>
        private ParameterExpression _parameterRel;

        private volatile bool _backflowWorking;

        /// <summary>
        /// 主干表达式。
        /// </summary>
        private readonly bool _rootVisitor;

        private readonly BaseVisitor _visitor;

        /// <summary>
        /// 条件开关。
        /// </summary>
        private readonly WhereSwitch _whereSwitch;

        private readonly HashSet<string> _uniqueNames = new HashSet<string>();

        /// <summary>
        /// 成员关系。
        /// </summary>
        private readonly Dictionary<MemberInfo, ParameterExpression> _memberRelationships = new Dictionary<MemberInfo, ParameterExpression>(2);

        /// <summary>
        /// 连表关系。
        /// </summary>
        private readonly Dictionary<(Type, string), SelectVisitor> _joinRelationships = new Dictionary<(Type, string), SelectVisitor>(2);

        /// <summary>
        /// 参数关系。
        /// </summary>
        private readonly HashSet<(Type, string)> _parameterRelationships = new HashSet<(Type, string)>(2);

        /// <summary>
        /// 自己的参数。
        /// </summary>
        private readonly HashSet<(Type, string)> _parameterOwners = new HashSet<(Type, string)>(1);

        /// <summary>
        /// 连表查询。
        /// </summary>
        private readonly List<JoinVisitor> _joinVisitors = new List<JoinVisitor>(1);

        /// <inheritdoc/>
        protected ScriptVisitor(DbStrictAdapter adapter) : base(adapter)
        {
            _rootVisitor = true;
            _whereSwitch = new WhereSwitch(Writer, ConditionType.Where);
        }

        /// <inheritdoc/>
        protected ScriptVisitor(BaseVisitor visitor, ConditionType conditionType, bool isNewWriter = false) : base(visitor, isNewWriter)
        {
            _visitor = visitor;
            _whereSwitch = new WhereSwitch(Writer, conditionType);
        }

        /// <summary>
        /// 启动方法。
        /// </summary>
        /// <param name="node">节点。</param>
        protected sealed override void Startup(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case nameof(Queryable.Max):
                case nameof(Queryable.Min):
                case nameof(Queryable.Average):
                case nameof(Queryable.Aggregate):
                    _isGrouping = true;
                    break;
                case nameof(Queryable.Count):
                case nameof(Queryable.LongCount):
                    _isAggregateCount = true;
                    break;
                case nameof(Queryable.Union):
                case nameof(Queryable.Concat):
                case nameof(Queryable.Intersect):
                case nameof(Queryable.Except):
                    _buildSelect = true;
                    break;
                case nameof(QueryableExtentions.Insert):
                case nameof(QueryableExtentions.Update):
                case nameof(QueryableExtentions.Delete):
                case nameof(Queryable.Join) when this is JoinVisitor:
                case nameof(Queryable.SelectMany) when this is JoinVisitor:
                    break;
                default:

                    _buildSelect = true;

                    _isGrouping = this is AggregateSelectVisitor;

                    break;
            }

            base.Startup(node);
        }

        /// <summary>
        /// 启动方法。
        /// </summary>
        /// <param name="node">节点。</param>
        public override void Startup(Expression node)
        {
            if (node.NodeType == ExpressionType.Constant
                && node is ConstantExpression { Value: IQueryable queryable })
            {
                var variable = queryable.Expression;

                base.Startup(variable.NodeType == ExpressionType.Constant ? node : variable);
            }
            else
            {
                base.Startup(node);
            }
        }

        private bool SkipLinqCall(MethodCallExpression node)
        {
            if (_isGrouping) //? 已经在分组分析器中了，继续进行分析。
            {
                return false;
            }

            if (_isGrouping = node.IsGrouping(true))
            {
                if (_isAggregateCount)
                {
                    Circuity(node);

                    return true;
                }

                _buildSelect = false;

                using (ScriptVisitor visitor = _unsorted ? new AggregateCheckSortSelectVisitor(this) : new AggregateSelectVisitor(this))
                {
                    visitor.Startup(node);
                }

                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        protected sealed override void LinqCall(MethodCallExpression node)
        {
            if (SkipLinqCall(node))
            {
                return;
            }

            string name = node.Method.Name;

            if (_unsorted)
            {
                switch (name)
                {
                    case nameof(Queryable.OrderBy):
                    case nameof(Queryable.ThenBy):
                    case nameof(Queryable.OrderByDescending):
                    case nameof(Queryable.ThenByDescending):
                        _unsorted = false;
                        break;
                }
            }

            LinqRef(node, ref _allowSelect);

            if (_treatmentPreheating)
            {
                if (_isGrouping
                        ? name == nameof(Queryable.GroupBy)
                        : node.Arguments.Count == (node.Method.IsStatic
                            ? 2
                            : 1)
                   )
                {
                    if (Preheat(node))
                    {
                        _treatmentPreheating = false;
                    }
                }
            }

            if (_isAggregateCount)
            {
                switch (name)
                {
                    case nameof(Queryable.Reverse):
                    case nameof(Queryable.OrderBy):
                    case nameof(Queryable.ThenBy):
                    case nameof(Queryable.OrderByDescending):
                    case nameof(Queryable.ThenByDescending):

                        Visit(node.Method.IsStatic ? node.Arguments[0] : node.Object);

                        break;
                    default:

                        LinqCore(node);

                        break;
                }
            }
            else
            {
                LinqCore(node);
            }

            //? 查询检查。
            if (_allowSelect)
            {
                switch (name)
                {
                    case nameof(Queryable.Distinct):
                        throw new DSyntaxErrorException("使用去重函数需先指定查询字段，如：*.Select(x=>{column-part}).Distinct()。");
                    case nameof(Enumerable.Max):
                    case nameof(Enumerable.Min):
                    case nameof(Enumerable.Sum):
                    case nameof(Enumerable.Average):
                        throw new DSyntaxErrorException($"使用聚合函数需先指定聚合字段，如：*.{name}(x => x.Field) 或 *.Select(x => x.Field).{name}()。");
                }
            }

            //? 必须排序检查。
            if (_unsorted)
            {
                switch (name)
                {
                    case nameof(Queryable.Reverse):
                    case nameof(Queryable.Take):
                    case nameof(Queryable.Skip):
                    case nameof(Queryable.TakeLast):
                    case nameof(Queryable.SkipLast):
                    case nameof(Queryable.Last):
                    case nameof(Queryable.First):
                    case nameof(Queryable.LastOrDefault):
                    case nameof(Queryable.FirstOrDefault):
                        throw new DSyntaxErrorException($"使用函数“{name}”时，必须使用排序函数（OrderBy/OrderByDescending）！");
                }
            }
        }

        /// <summary>
        /// Linq 方法引用。
        /// </summary>
        protected virtual void LinqRef(MethodCallExpression node, ref bool allowSelect)
        {
            switch (node.Method.Name)
            {
                case nameof(QueryableExtentions.Timeout):
                case nameof(QueryableExtentions.NoElementError):
                case nameof(Queryable.Contains):
                case nameof(Queryable.Take):
                case nameof(Queryable.Skip):
                case nameof(Queryable.TakeLast):
                case nameof(Queryable.SkipLast):
                case nameof(Queryable.ElementAt):
                case nameof(Queryable.ElementAtOrDefault):
                case nameof(Queryable.Last) when node.Arguments.Count == 1:
                case nameof(Queryable.First) when node.Arguments.Count == 1:
                case nameof(Queryable.Single) when node.Arguments.Count == 1:
                case nameof(Queryable.LastOrDefault) when node.Arguments.Count == 1:
                case nameof(Queryable.FirstOrDefault) when node.Arguments.Count == 1:
                case nameof(Queryable.SingleOrDefault) when node.Arguments.Count == 1:
                case nameof(Queryable.Any) when node.Arguments.Count == 1:
                case nameof(Queryable.Max) when node.Arguments.Count == 1:
                case nameof(Queryable.Min) when node.Arguments.Count == 1:
                case nameof(Queryable.Average) when node.Arguments.Count == 1:
                case nameof(Queryable.Aggregate) when node.Arguments.Count == 1:
                case nameof(Queryable.Count) when node.Arguments.Count == 1:
                case nameof(Queryable.LongCount) when node.Arguments.Count == 1:

                    allowSelect = this._allowSelect;
                    break;
                //? 跳过组合函数。
                case nameof(Queryable.Union):
                case nameof(Queryable.Concat):
                case nameof(Queryable.Except):
                case nameof(Queryable.Intersect):

                    allowSelect = this._allowSelect;
                    break;
                case nameof(Queryable.Select):

                    if (this._allowSelect)
                    {
                        goto default;
                    }

                    throw new DSyntaxErrorException(
                        "单个脚步仅支持指定一次查询，请将.Select(x=>{column-part})放在过滤、排序和分组等函数之后，如：*.OrderBy(x=>{column-part}).Select(x=>{column-part}).Skip({skipSize}).Take({TakeSize})！");

                //? 跳过字段限制的函数。
                case nameof(Queryable.Cast):
                case nameof(Queryable.OfType):
                    allowSelect = this._allowSelect;
                    break;
                //? 跳过生成 SELECT 的函数。
                case nameof(Queryable.Join):
                case nameof(Queryable.SelectMany):
                    goto default;
                case nameof(Queryable.Distinct):
                    if (this._allowSelect)
                    {
                        allowSelect = true;

                        break;
                    }

                    throw new DSyntaxErrorException("使用去重函数需先指定查询字段，如：*.Select(x=>{column-part}).Distinct()。");
                case nameof(Queryable.DefaultIfEmpty):
                    if (this._allowSelect)
                    {
                        allowSelect = true;

                        break;
                    }

                    throw new DSyntaxErrorException("使用默认值函数需先指定查询字段，如：*.Select(x=>{column-part}).DefaultIfEmpty({default-value})。");
                default:
                    this._allowSelect = allowSelect = false;
                    break;
            }
        }

        /// <inheritdoc/>
        protected override void Constant(IQueryable value)
        {
            if (_buildSelect)
            {
                Writer.Keyword(SqlKeyword.SELECT);

                if (_isDistinct)
                {
                    Writer.Keyword(SqlKeyword.DISTINCT);
                }

                Select(value.Expression);

                _buildSelect = false;
            }

            DataSourceMode();

            Name();

            TableAs();
        }

        /// <summary>
        /// 参数架构。
        /// </summary>
        protected virtual void ParameterSchema()
        {
            Nickname();

            Writer.Write('.');
        }

        /// <summary>
        /// 数据源模式(FROM/[INNER|LEFT|RIGHT|OUTER|FULL] JOIN)。
        /// </summary>
        protected virtual void DataSourceMode() => Writer.Keyword(SqlKeyword.FROM);

        /// <summary>
        /// 别名。
        /// </summary>
        protected virtual void TableAs()
        {
            //? DB2 和 Sybase 不支持 AS 关键字。
            if (Engine is not (DatabaseEngine.DB2 or DatabaseEngine.Sybase or DatabaseEngine.Oracle))
            {
                Writer.Keyword(SqlKeyword.AS);
            }

            Nickname();
        }

        /// <summary>
        /// 别名。
        /// </summary>
        protected void Nickname()
        {
            _parameterRef = false;

            if (_parameterRel is null)
            {
                Writer.Name("g");
            }
            else if (TryGetSourceParameter(_parameterRel, out ParameterExpression parameter))
            {
                Writer.Name(parameter.Name);
            }
            else
            {
                throw new DSyntaxErrorException();
            }
        }

        /// <summary>
        /// 环路处理。
        /// </summary>
        /// <param name="node">节点</param>
        protected internal virtual void Circuity(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case nameof(QueryableExtentions.DataSharding):
                case nameof(Queryable.SelectMany):
                case nameof(Queryable.GroupJoin):
                case nameof(Queryable.Join):
                    VisitMethodCall(node);
                    break;
                default:
                    DataSourceMode();

                    Writer.OpenBrace();

                    using (var visitor = new SelectVisitor(this, true))
                    {
                        visitor.Startup((Expression)node); //? 分析表信息。
                    }

                    Writer.CloseBrace();

                    TableAs();
                    break;
            }
        }

        /// <summary>
        /// 环路处理。
        /// </summary>
        /// <param name="node">节点</param>
        protected internal virtual void Circuity(Expression node)
        {
            if (node.NodeType == ExpressionType.Call)
            {
                Circuity((MethodCallExpression)node);
            }
            else
            {
                Visit(node);
            }
        }

        /// <summary>
        /// Linq 方法。<see cref="Queryable"/> 或 <seealso cref="QueryableExtentions"/>。
        /// </summary>
        /// <param name="node">节点。</param>
        protected virtual void LinqCore(MethodCallExpression node)
        {
            var name = node.Method.Name;

            switch (name)
            {
                case nameof(Queryable.Select):

                    _buildSelect = false;

                    Writer.Keyword(SqlKeyword.SELECT);

                    if (_isDistinct)
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

                    _isDistinct = true;

                    Visit(node.Arguments[0]);

                    break;

                case nameof(Queryable.Union):
                case nameof(Queryable.Concat):
                case nameof(Queryable.Intersect):
                case nameof(Queryable.Except):

                    if (_buildSelect)
                    {
                        Writer.Keyword(SqlKeyword.SELECT);

                        Writer.Write('*');

                        _buildSelect = false;
                    }

                    DataSourceMode();

                    Writer.OpenBrace();

                    //? 解决子查询分页的问题。
                    Writer.OpenBrace();

                    using (var visitor = new SelectVisitor(this, true))
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
                case nameof(Queryable.Where):
                case nameof(Queryable.TakeWhile):

                    Where(node);

                    break;

                case nameof(QueryableExtentions.WhereIf) when IsPlainVariable(node.Arguments[1]):

                    var conditionIsValid = node.Arguments[1].GetValueFromExpression<bool>();

                    if (conditionIsValid)
                    {
                        Where(node.Arguments[0], node.Arguments[2]);
                    }

                    break;
                case nameof(QueryableExtentions.WhereIf):

                    WhereIf(node.Arguments[0], node.Arguments[1], node.Arguments[2]);

                    break;
                case nameof(Queryable.SkipWhile):

                    SkipWhile(node);

                    break;
                case nameof(Queryable.Join):
                case nameof(Queryable.SelectMany):
                {
                    if (_buildSelect)
                    {
                        Writer.Keyword(SqlKeyword.SELECT);

                        if (_isDistinct)
                        {
                            Writer.Keyword(SqlKeyword.DISTINCT);
                        }
                    }

                    var visitor = new JoinVisitor(this, _joinRelationships, _buildSelect);

                    _joinVisitors.Add(visitor);

                    _buildSelect = false;

                    visitor.Startup((Expression)node); //? 分析表信息。

                    break;
                }
                case nameof(QueryableExtentions.DataSharding):

                    VisitMethodCall(node);

                    break;
                default:

                    Backflow(this, node);

                    break;
            }
        }

        /// <summary>
        /// 查询字段。
        /// </summary>
        /// <param name="node">节点。</param>
        protected virtual void Select(Expression node)
        {
        }

        /// <summary>
        /// 链路回流。
        /// </summary>
        /// <param name="visitor">当前访问器。</param>
        /// <param name="node">节点。</param>
        protected virtual void Backflow(ExpressionVisitor visitor, MethodCallExpression node)
        {
            string name = node.Method.Name;

            switch (name)
            {
                case nameof(QueryableExtentions.Timeout):

                    if (!_rootVisitor)
                    {
                        goto default;
                    }

                    int timeOut = node.Arguments[1].GetValueFromExpression<int>();

                    if (_commandTimeout.HasValue)
                    {
                        _commandTimeout += timeOut;
                    }
                    else
                    {
                        _commandTimeout = timeOut;
                    }

                    visitor.Visit(node.Arguments[0]);

                    break;

                default:
                    if (!_backflowWorking && _visitor is IBackflowVisitor backflowVisitor)
                    {
                        _backflowWorking = true;

                        backflowVisitor.Backflow(this, node);

                        _backflowWorking = false;

                        break;
                    }

                    throw new DSyntaxErrorException($"方法“{name}”不被支持！");
            }
        }

        void IBackflowVisitor.Backflow(ExpressionVisitor visitor, MethodCallExpression node) => Backflow(visitor, node);

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _uniqueNames.Clear();
                _parameterOwners.Clear();
                _memberRelationships.Clear();
                _parameterRelationships.Clear();

                if (_joinVisitors.Count > 0)
                {
                    foreach (var visitor in _joinVisitors)
                    {
                        visitor.Dispose();
                    }

                    _joinVisitors.Clear();
                }
            }

            base.Dispose(disposing);
        }

        #region 条件。

        /// <summary>
        /// 条件。
        /// </summary>
        /// <param name="node">节点。</param>
        protected virtual void Where(MethodCallExpression node)
        {
            var instance = node.Method.IsStatic ? node.Arguments[0] : node.Object; //? 自定义函数。

            Where(instance, node.Arguments[^1]);
        }

        /// <summary>
        /// 条件分析。
        /// </summary>
        /// <param name="instance">对象节点。</param>
        /// <param name="predicate">条件节点。</param>
        protected virtual void Where(Expression instance, Expression predicate)
        {
            if (instance.NodeType != ExpressionType.Constant)
            {
                WhereDependency(instance);
            }

            using (var domain = Writer.Domain())
            {
                Condition(predicate);

                domain.Flyback();

                if (instance.NodeType == ExpressionType.Constant)
                {
                    Visit(instance);
                }

                if (domain.HasValue)
                {
                    _whereSwitch.Execute();
                }
            }
        }

        /// <summary>
        /// 条件。
        /// </summary>
        /// <param name="node">节点。</param>
        protected virtual void SkipWhile(MethodCallExpression node)
        {
            var instance = node.Method.IsStatic ? node.Arguments[0] : node.Object; //? 自定义函数。

            SkipWhile(instance, node.Arguments[^1]);
        }

        /// <summary>
        /// 条件分析。
        /// </summary>
        /// <param name="instance">对象节点。</param>
        /// <param name="predicate">条件节点。</param>
        protected virtual void SkipWhile(Expression instance, Expression predicate)
        {
            if (instance.NodeType != ExpressionType.Constant)
            {
                WhereDependency(instance);
            }

            using (var domain = Writer.Domain())
            {
                using (Writer.ConditionReversal())
                {
                    Condition(predicate);
                }

                domain.Flyback();

                if (instance.NodeType == ExpressionType.Constant)
                {
                    Visit(instance);
                }

                if (domain.HasValue)
                {
                    _whereSwitch.Execute();
                }
            }
        }

        /// <summary>
        /// 条件分析。
        /// </summary>
        /// <param name="instance">对象节点。</param>
        /// <param name="condition">是否满足条件。</param>
        /// <param name="predicate">满足“<paramref name="condition"/>”时的条件节点。</param>
        protected virtual void WhereIf(Expression instance, Expression condition, Expression predicate)
        {
            if (instance.NodeType != ExpressionType.Constant)
            {
                WhereDependency(instance);
            }

            using (var domainMain = Writer.Domain())
            {
                using (var domain = Writer.Domain())
                {
                    Condition(condition);

                    if (domain.HasValue)
                    {
                        Writer.Keyword(SqlKeyword.THEN);

                        using (var domainSub = Writer.Domain())
                        {
                            Visit(predicate);

                            if (domainSub.IsEmpty)
                            {
                                Writer.Keyword(SqlKeyword.NULL);
                            }
                            else if (RequiresConditionalEscape() && IsCondition(predicate))
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

                        Writer.Keyword(SqlKeyword.ELSE);

                        Writer.True(); //? 测试条件不满足是始终为真。

                        Writer.Keyword(SqlKeyword.END);

                        Writer.CloseBrace();

                        if (RequiresConditionalEscape())
                        {
                            Writer.Operator(SqlOperator.IsTrue);
                        }

                        domain.Flyback();

                        Writer.OpenBrace();

                        Writer.Keyword(SqlKeyword.CASE);
                        Writer.Keyword(SqlKeyword.WHEN);
                    }
                }

                domainMain.Flyback();

                if (instance.NodeType == ExpressionType.Constant)
                {
                    Visit(instance);
                }

                if (domainMain.HasValue)
                {
                    _whereSwitch.Execute();
                }
            }
        }

        /// <summary>
        /// 条件依赖对象。
        /// </summary>
        /// <param name="instance">对象节点。</param>
        protected virtual void WhereDependency(Expression instance) => Visit(instance);

        /// <inheritdoc/>
        protected override void Condition(Expression node)
        {
            using (var visitor = new ConditionVisitor(this))
            {
                visitor.Startup(node);
            }
        }

        #endregion

        #region 参数。

        /// <summary>
        /// 参数预热。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <param name="parameterNode">参数。</param>
        /// <returns>预热参数。</returns>
        /// <exception cref="NotSupportedException">表达式不规范。</exception>
        private static bool TryParametricPreheating(Expression node, out ParameterExpression parameterNode)
        {
            switch (node)
            {
                case UnaryExpression unary:
                    return TryParametricPreheating(unary.Operand, out parameterNode);
                case LambdaExpression lambda:
                    parameterNode = lambda.Parameters[0];
                    return true;
                default:
                    parameterNode = null;

                    return false;
            }
        }

        /// <summary>
        /// 参数预热。
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns>是否预热成功。</returns>
        protected virtual bool Preheat(MethodCallExpression node)
        {
            if (_preheatingParameter)
            {
                if (TryParametricPreheating(node.Arguments[1], out ParameterExpression parameterNode))
                {
                    _preheatedParameter = true;
                    _preheatingParameter = false;

                    return ParameterRefresh(parameterNode);
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// 参数刷新。
        /// </summary>
        /// <param name="parameter">参数。</param>
        /// <returns>是否刷新成功。</returns>
        protected bool ParameterRefresh(ParameterExpression parameter)
        {
            _parameterOwners.Add((parameter.Type, parameter.Name));

            if (_parameterRef)
            {
                _parameterRel = parameter;

                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        protected override void PreparingParameter(LambdaExpression node)
        {
            switch (node.Parameters.Count)
            {
                case 1
                    : // All/Any/Average/Count/First/FirstOrDefault/GroupBy/GroupJoin/Join/Last/LastOrDefault/LongCount/Max/Min/OrderBy/OrderByDescending/Select/SelectMany/Single/SingleOrDefault/SkipWhile/Sum/TakeWhile/ThenBy/ThenByDescending/Where
                    if (!TryPreparingParameter(node.Parameters[0], out ParameterExpression parameter))
                    {
                        goto default;
                    }

                    ParameterRefresh(parameter);

                    if (node.Body.NodeType is ExpressionType.New or ExpressionType.MemberInit)
                    {
                        var visitor = new RelationshipVisitor(_memberRelationships);

                        visitor.Visit(node.Body);
                    }

                    break;
                case 2: // GroupBy/GroupJoin/Join/SelectMany/SkipWhile/TakeWhile/Where
                    var parameterTwo = node.Parameters[1];

                    var parameterType = parameterTwo.Type;

                    if (parameterType == typeof(int))
                    {
                        throw new NotSupportedException("不支持索引参数！");
                    }

                    if (parameterType.IsGenericType)
                    {
                        var typeDefinition = parameterType.GetGenericTypeDefinition();

                        if (typeDefinition == Types.IEnumerableGeneric) //? GroupBy/GroupJoin
                        {
                            goto case 1;
                        }
                    }

                    if (PreparingParameterRelationship(parameterTwo.Name, parameterTwo)) //? Join
                    {
                        goto case 1;
                    }

                    goto default;
                default:
                    throw new NotSupportedException("请保持查询参数的名称一致性！");
            }
        }

        /// <summary>
        /// 准备参数。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <param name="parameter">真实参数。</param>
        /// <returns>是否准备成功。</returns>
        protected virtual bool TryPreparingParameter(ParameterExpression node, out ParameterExpression parameter)
        {
            if (PreparingParameterRelationship(node.Name, node))
            {
                parameter = node;

                return true;
            }

            parameter = null;

            return false;
        }

        /// <summary>
        /// 准备参数。
        /// </summary>
        /// <param name="relationshipName">关系名称。</param>
        /// <param name="node">节点。</param>
        /// <returns>是否准备成功。</returns>
        protected bool PreparingParameterRelationship(string relationshipName, ParameterExpression node)
        {
            _preheatedParameter = false;

            Type parameterType = node.Type;

            if (_parameterRelationships.Add((parameterType, relationshipName)))
            {
                return _uniqueNames.Add(relationshipName);
            }

            return _uniqueNames.Contains(relationshipName);
        }

        /// <summary>
        /// 是否支持参数关系补充。
        /// </summary>
        /// <returns>是否支持参数关系补充。</returns>
        protected virtual bool ParameterRelationshipSupplementIsSupport() => true;

        /// <inheritdoc/>
        protected override bool TryGetSourceParameter(Expression node, out ParameterExpression parameterExpression)
        {
            if (_preheatedParameter) //? 参数预热，兼容 Union/Concat/Except/Intersect 别名分析。
            {
                _preheatedParameter = false;

                if (ParameterRelationshipSupplementIsSupport())
                {
                    if (!PreparingParameterRelationship(_parameterRel.Name, _parameterRel))
                    {
                        throw new NotSupportedException("请保持查询参数的名称一致性！");
                    }
                }
            }

            switch (node)
            {
                case ParameterExpression parameter:
                    if (_parameterOwners.Contains((parameter.Type, parameter.Name)))
                    {
                        parameterExpression = _parameterRel;

                        _parameterRef = false;

                        return true;
                    }

                    if (_joinRelationships.TryGetValue((parameter.Type, parameter.Name), out var visitor))
                    {
                        return visitor.TryGetSourceParameter(node, out parameterExpression);
                    }

                    break;
                case MemberExpression memberExpression:

                    if (_memberRelationships.TryGetValue(memberExpression.Member, out parameterExpression))
                    {
                        return TryGetSourceParameter(parameterExpression, out parameterExpression);
                    }

                    if (_parameterOwners.Contains((memberExpression.Type, memberExpression.Member.Name)))
                    {
                        parameterExpression = _parameterRel;

                        _parameterRef = false;

                        return true;
                    }

                    if (_joinRelationships.TryGetValue((memberExpression.Type, memberExpression.Member.Name), out visitor))
                    {
                        return visitor.TryGetSourceParameter(node, out parameterExpression);
                    }

                    break;
            }

            return base.TryGetSourceParameter(node, out parameterExpression);
        }

        /// <inheritdoc/>
        protected override bool TryGetSourceTableInfo(ParameterExpression node, out ITableInfo tableInfo)
        {
            if (_joinRelationships.TryGetValue((node.Type, node.Name), out var visitor))
            {
                tableInfo = visitor.Table(true);

                if (tableInfo is null)
                {
                    return false;
                }

                return tableInfo.TypeIs(node.Type);
            }

            tableInfo = Table(true);

            return tableInfo is null
                ? base.TryGetSourceTableInfo(node, out tableInfo)
                : tableInfo.TypeIs(node.Type) || base.TryGetSourceTableInfo(node, out tableInfo);
        }

        #endregion

        /// <summary>
        /// 转SQL。
        /// </summary>
        /// <returns>SQL命令。</returns>
        public virtual CommandSql ToSQL()
        {
            string sql = Writer.ToString();

            return new CommandSql(sql, Writer.Parameters, _commandTimeout);
        }

        #region 嵌套类。

        /// <summary>
        /// 条件类型。
        /// </summary>
        public enum ConditionType
        {
            /// <summary>
            /// ON
            /// </summary>
            On,

            /// <summary>
            /// WHERE
            /// </summary>
            Where,

            /// <summary>
            /// HAVING
            /// </summary>
            Having,

            /// <summary>
            /// AND
            /// </summary>
            And
        }

        /// <summary>
        /// 智能开关。
        /// </summary>
        private class WhereSwitch
        {
            private bool isFirst = true;

            private readonly SqlWriter _writer;
            private readonly ConditionType _conditionType;

            public WhereSwitch(SqlWriter writer, ConditionType conditionType)
            {
                _writer = writer;
                _conditionType = conditionType;
            }

            public void Execute()
            {
                if (isFirst)
                {
                    isFirst = false;

                    switch (_conditionType)
                    {
                        case ConditionType.On:
                            _writer.Keyword(SqlKeyword.ON);
                            break;
                        case ConditionType.Where:
                            _writer.Keyword(SqlKeyword.WHERE);
                            break;
                        case ConditionType.Having:
                            _writer.Keyword(SqlKeyword.HAVING);
                            break;
                        case ConditionType.And:
                            _writer.Keyword(SqlKeyword.AND);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
                else
                {
                    _writer.Keyword(SqlKeyword.AND);
                }
            }
        }

        private class RelationshipVisitor : ExpressionVisitor
        {
            private readonly Dictionary<MemberInfo, ParameterExpression> _memberRelationships;

            public RelationshipVisitor(Dictionary<MemberInfo, ParameterExpression> memberRelationships)
            {
                _memberRelationships = memberRelationships;
            }

            protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
            {
                if (node.Expression.NodeType == ExpressionType.Parameter)
                {
                    var parameter = (ParameterExpression)node.Expression;

                    _memberRelationships.Add(node.Member, parameter);
                }

                return base.VisitMemberAssignment(node);
            }

            protected override Expression VisitNew(NewExpression node)
            {
                if (node.Members is null)
                {
                    return base.VisitNew(node);
                }

                for (var i = 0; i < node.Arguments.Count; i++)
                {
                    var argument = node.Arguments[i];

                    if (argument.NodeType == ExpressionType.Parameter)
                    {
                        _memberRelationships.Add(node.Members[i], (ParameterExpression)argument);
                    }
                }

                return base.VisitNew(node);
            }
        }

        private class AggregateCheckSortSelectVisitor : AggregateSelectVisitor
        {
            private readonly ScriptVisitor _scriptVisitor;

            public AggregateCheckSortSelectVisitor(ScriptVisitor visitor, bool showAs = true) : base(visitor, showAs)
            {
                _scriptVisitor = visitor;
            }

            protected override void LinqCore(MethodCallExpression node)
            {
                if (_scriptVisitor._unsorted)
                {
                    switch (node.Method.Name)
                    {
                        case nameof(Queryable.OrderBy):
                        case nameof(Queryable.ThenBy):
                        case nameof(Queryable.OrderByDescending):
                        case nameof(Queryable.ThenByDescending):
                            _scriptVisitor._unsorted = false;
                            break;
                    }
                }

                base.LinqCore(node);
            }
        }

        #endregion
    }
}