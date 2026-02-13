using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Inkslab.Linq.Enums;
using Inkslab.Linq.Exceptions;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 基础访问器（负责条件处理和基础访问器结构管理）。
    /// </summary>
    [DebuggerDisplay("Base")]
    public abstract partial class BaseVisitor : ExpressionVisitor, IDisposable
    {
        private bool _disposedValue;

        private volatile bool _hasBaseStartup;

        private readonly BaseVisitor _visitor;
        private readonly bool _isNewWriter;
        private readonly DbStrictAdapter _adapter;
        private readonly int _cursorPosition = -1;

        /// <summary>
        /// 数据分片。
        /// </summary>
        private bool _dataSharding;

        /// <summary>
        /// 数据分片。
        /// </summary>
        private bool _dataShardingInvalid;

        /// <summary>
        /// 分区键。
        /// </summary>
        private string _shardingKey = string.Empty;

        /// <summary>
        /// 表信息。
        /// </summary>
        private ITableInfo _tableInformation;

        /// <summary>
        /// 数据库引擎。
        /// </summary>
        protected DatabaseEngine Engine => _adapter.Engine;

        /// <summary>
        /// 写入器。
        /// </summary>
        protected SqlWriter Writer { get; }

        /// <summary>
        /// 条件取反。
        /// </summary>
        public bool IsConditionReversal => Writer.IsConditionReversal;

        /// <summary>
        /// 访问器。
        /// </summary>
        /// <param name="adapter">数据库适配器。</param>
        /// <exception cref="ArgumentNullException">参数 <paramref name="adapter"/> 不能为空。</exception>
        protected BaseVisitor(DbStrictAdapter adapter)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));

            Writer = CreateWriter(adapter.Settings);
        }

        /// <summary>
        /// 子表达式。
        /// </summary>
        /// <param name="visitor">父表达式访问器。</param>
        /// <param name="isNewWriter">是否创建新的写入器。</param>
        /// <exception cref="ArgumentNullException">参数<see cref="_visitor"/>为 Null。</exception>
        protected BaseVisitor(BaseVisitor visitor, bool isNewWriter = false)
        {
            _visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));

            _isNewWriter = isNewWriter;

            _adapter = visitor._adapter;

            var writer = visitor.Writer;

            if (isNewWriter)
            {
                _cursorPosition = writer.CursorPosition;

                Writer = visitor.CreateWriter(writer);
            }
            else
            {
                Writer = writer;
            }
        }

        /// <summary>
        /// 创建写入流。
        /// </summary>
        /// <param name="settings">修正配置。</param>
        /// <returns></returns>
        protected virtual SqlWriter CreateWriter(IDbCorrectSettings settings) =>
            new SqlWriter(settings);

        /// <summary>
        /// 创建写入流。
        /// </summary>
        /// <param name="writer">父级写入器。</param>
        /// <returns></returns>
        protected virtual SqlWriter CreateWriter(SqlWriter writer) => new SqlWriter(writer);

        /// <summary>
        /// 启动方法。
        /// </summary>
        /// <param name="node">节点。</param>
        public virtual void Startup(Expression node)
        {
            if (node.NodeType == ExpressionType.Call)
            {
                if (_hasBaseStartup)
                {
                    StartupCore((MethodCallExpression)node);
                }
                else
                {
                    _hasBaseStartup = true;

                    Startup((MethodCallExpression)node);
                }
            }
            else
            {
                Visit(node);
            }
        }

        /// <summary>
        /// 启动方法。
        /// </summary>
        /// <param name="node">节点。</param>
        protected virtual void Startup(MethodCallExpression node)
        {
            PrepareTableInformation(node);

            if (_hasBaseStartup)
            {
                StartupCore(node);
            }
            else
            {
                _hasBaseStartup = true;

                Startup((Expression)node);
            }
        }

        /// <summary>
        /// 准备表信息。
        /// </summary>
        private void PrepareTableInformation(MethodCallExpression node)
        {
            var instanceArg = node.Method.IsStatic
                ? node.Arguments[0]
                : node.Object;

            switch (node.Method.Name)
            {
                case nameof(Queryable.Join) when this is JoinVisitor:
                case nameof(Queryable.GroupJoin) when this is JoinVisitor:
                case nameof(Queryable.SelectMany) when this is JoinVisitor: //? CROSS JONT

                    //? 分析 JOIN 表。
                    instanceArg = node.Arguments[1];

                    goto default;
                default:
                    if (
                        instanceArg?.NodeType == ExpressionType.Constant
                        && instanceArg is ConstantExpression { Value: IQueryable queryable })
                    {
                        _tableInformation ??= TableAnalyzer.Table(queryable.ElementType);
                    }

                    break;
            }
        }

        /// <summary>
        /// 启动核心方法。
        /// </summary>
        /// <param name="node">节点。</param>
        protected virtual void StartupCore(MethodCallExpression node) => MethodCall(node);

        private static bool IsPlainVariableNs(Expression node)
        {
            if (node is null)
            {
                return true;
            }

            // 性能优化：使用 NodeType（枚举整数比较）替代模式匹配（类型检查）
            // JIT 编译器可将枚举 switch 优化为跳转表，性能提升 15-25%
            switch (node.NodeType)
            {
                case ExpressionType.Parameter:
                    return false;

                case ExpressionType.Constant:
                    return ((ConstantExpression)node).Value is not IQueryable;

                case ExpressionType.MemberAccess:
                    var member = (MemberExpression)node;
                    return member.Expression is null || IsPlainVariableNs(member.Expression);

                case ExpressionType.Call:
                    var method = (MethodCallExpression)node;
                    
                    // 提前验证 Object，避免重复递归
                    if (method.Object is not null && !IsPlainVariableNs(method.Object))
                    {
                        return false;
                    }

                    // 使用 for 循环替代 LINQ All，避免委托调用和迭代器分配
                    var methodArgs = method.Arguments;
                    int methodArgsCount = methodArgs.Count;
                    for (int i = 0; i < methodArgsCount; i++)
                    {
                        if (!IsPlainVariableNs(methodArgs[i]))
                        {
                            return false;
                        }
                    }
                    return true;

                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.Power:
                case ExpressionType.And:
                case ExpressionType.Or:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.LeftShift:
                case ExpressionType.RightShift:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                    var binary = (BinaryExpression)node;
                    return IsPlainVariableNs(binary.Left) && IsPlainVariableNs(binary.Right);

                case ExpressionType.Lambda:
                    var lambda = (LambdaExpression)node;
                    return lambda.Parameters.Count == 0 && IsPlainVariableNs(lambda.Body);

                case ExpressionType.New:
                    var newExpression = (NewExpression)node;
                    
                    // 提前检查 Members 避免无用遍历
                    if (newExpression.Members is { Count: > 0 })
                    {
                        return false;
                    }

                    var newArgs = newExpression.Arguments;
                    int newArgsCount = newArgs.Count;
                    for (int i = 0; i < newArgsCount; i++)
                    {
                        if (!IsPlainVariableNs(newArgs[i]))
                        {
                            return false;
                        }
                    }
                    return true;

                case ExpressionType.MemberInit:
                    var memberInit = (MemberInitExpression)node;
                    
                    // 提前验证 NewExpression 避免重复递归
                    if (!IsPlainVariableNs(memberInit.NewExpression))
                    {
                        return false;
                    }

                    var bindings = memberInit.Bindings;
                    int bindingsCount = bindings.Count;
                    for (int i = 0; i < bindingsCount; i++)
                    {
                        if (bindings[i] is not MemberAssignment assignment
                            || !IsPlainVariableNs(assignment.Expression))
                        {
                            return false;
                        }
                    }
                    return true;

                case ExpressionType.Conditional:
                    var conditional = (ConditionalExpression)node;
                    return IsPlainVariableNs(conditional.Test)
                           && IsPlainVariableNs(conditional.IfTrue)
                           && IsPlainVariableNs(conditional.IfFalse);

                case ExpressionType.Quote:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.OnesComplement:
                case ExpressionType.IsTrue:
                case ExpressionType.IsFalse:
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.UnaryPlus:
                case ExpressionType.TypeAs:
                case ExpressionType.ArrayLength:
                    return IsPlainVariableNs(((UnaryExpression)node).Operand);

                default:
                    return false;
            }
        }

        /// <summary>
        /// 是普通变量。
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <param name="depthVerification">深度验证。</param>
        /// <returns>是否是常规变量。</returns>
        protected virtual bool IsPlainVariable(Expression node, bool depthVerification = true)
        {
            if (node is null)
            {
                return true;
            }

            if (node.NodeType == ExpressionType.Parameter)
            {
                return false;
            }

            switch (node)
            {
                case ConstantExpression:
                    return true;
                case MemberExpression member:
                    if (member.Expression is null)
                    {
                        return true;
                    }

                    return IsPlainVariable(member.Expression, depthVerification);
                default:
                    return depthVerification && IsPlainVariableNs(node);
            }
        }

        /// <summary>
        /// 是否需要条件转义。
        /// </summary>
        /// <returns></returns>
        protected bool RequiresConditionalEscape() => Engine is not (DatabaseEngine.MySQL or DatabaseEngine.PostgreSQL);

        /// <summary>
        /// 是否为条件。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <returns>是否为条件。</returns>
        protected virtual bool IsCondition(Expression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                case ExpressionType.IsTrue:
                case ExpressionType.IsFalse:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Call when node.Type.IsBoolean():
                    return true;
                case ExpressionType.Quote:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.OnesComplement:
                case ExpressionType.Not:
                    return IsCondition(((UnaryExpression)node).Operand);
                case ExpressionType.Lambda:
                    return IsCondition(((LambdaExpression)node).Body);
                default:
                    return false;
            }
        }

        #region Sealed

        /// <inheritdoc />
        protected sealed override Expression VisitLambda<T>(Expression<T> node)
        {
            PreparingParameter(node);

            if (node.Body.NodeType == ExpressionType.Parameter)
            {
                BodyIsParameter((ParameterExpression)node.Body);
            }
            else if (
                node.Body.NodeType == ExpressionType.MemberAccess
                && !node.Body.Type.IsCell()
                && TryGetSourceParameter(node.Body, out ParameterExpression parameterExpression)
            )
            {
                BodyIsParameter(parameterExpression);
            }
            else
            {
                Lambda(node);
            }

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitMethodCall(MethodCallExpression node)
        {
            PrepareTableInformation(node);

            switch (node.Method.Name)
            {
                case nameof(ToString) when node.Arguments.Count == 0:

                    if (node.Object is null)
                    {
                        throw new NotSupportedException("静态的 ToString 方法不支持转换！");
                    }

                    if (node.Object.Type == Types.String || node.Object.Type == Types.JsonbPayload || node.Object.Type == Types.JsonPayload)
                    {
                        Visit(node.Object); //? 支持直接调用 ToString 方法。

                        break;
                    }

                    switch (Engine)
                    {
                        case DatabaseEngine.SQLite:
                        case DatabaseEngine.PostgreSQL:
                        case DatabaseEngine.Oracle:
                        case DatabaseEngine.DB2:
                            // 使用字符串拼接（||）与空字符串进行隐式转换，避免显式转换导致的编码/排序规则问题。

                            Writer.OpenBrace();

                            Visit(node.Object);

                            Writer.Write(" || ");
                            Writer.Write("''");

                            Writer.CloseBrace();

                            break;
                        case DatabaseEngine.MySQL:
                            // 使用 CONCAT 与空字符串拼接来隐式转换，避免显式 CAST 导致的字符集与排序规则冲突。
                            Writer.Write("CONCAT");
                            Writer.OpenBrace();

                            Visit(node.Object);

                            Writer.Delimiter();
                            Writer.Write("''");

                            Writer.CloseBrace();

                            break;
                        case DatabaseEngine.SqlServer:
                        case DatabaseEngine.Sybase:
                            // 使用 + 操作符与空字符串拼接进行隐式转换，避免显式转换导致的类型强制问题。
                            Writer.OpenBrace();
                            Visit(node.Object);

                            Writer.Write(" + ");
                            Writer.Write("''");
                            Writer.CloseBrace();

                            break;
                        default:
                            throw new NotSupportedException(
                                $"不支持数据库引擎“{Engine}”的 ToString() 方法转换！"
                            );
                    }

                    break;
                case nameof(Queryable.Join):

                    MethodCall(node);

                    break;
                case nameof(QueryableExtentions.DataSharding):

                    if (_dataSharding)
                    {
                        throw new DSyntaxErrorException("每个数据源的数据分区只能指定一次！");
                    }

                    _dataSharding = _dataShardingInvalid = true;
                    _shardingKey = node.Arguments[1].GetValueFromExpression<string>();

                    Visit(node.Arguments[0]);

                    if (_dataShardingInvalid)
                    {
                        throw new DSyntaxErrorException("仅根数据源支持数据分区！");
                    }

                    break;
                default:
                    if (
                        _adapter.Visitors.TryGetValue(node.Method, out IMethodVisitor methodVisitor)
                    )
                    {
                        methodVisitor.Visit(this, Writer, node);
                    }
                    else
                    {
                        MethodCall(node);
                    }

                    break;
            }

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is IQueryable queryable)
            {
                _tableInformation ??= TableAnalyzer.Table(queryable.ElementType); //? 兼容 LEFT JOIN 导致的函数分析问题。
            }

            _dataShardingInvalid = false;

            Constant(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitDefault(DefaultExpression node)
        {
            if (node.Type.IsValueType)
            {
                Writer.Constant(Activator.CreateInstance(node.Type));
            }
            else
            {
                Writer.Keyword(SqlKeyword.NULL);
            }

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitMember(MemberExpression node)
        {
            string memberName = node.Member.Name;

            if (memberName == nameof(string.Length) && node.Member.DeclaringType == Types.String)
            {
                switch (Engine)
                {
                    case DatabaseEngine.Sybase:
                    case DatabaseEngine.SqlServer:
                        Writer.Write("LEN");
                        break;
                    case DatabaseEngine.DB2:
                    case DatabaseEngine.MySQL:
                    case DatabaseEngine.SQLite:
                    case DatabaseEngine.Oracle:
                    case DatabaseEngine.PostgreSQL:
                    default:
                        Writer.Write("LENGTH");
                        break;
                }

                Writer.OpenBrace();

                Visit(node.Expression);

                Writer.CloseBrace();
            }
            else if (memberName == "HasValue" && node.Expression.IsNullable())
            {
                MemberHasValue(node.Expression);
            }
            else if (memberName == "Value" && node.Expression.IsNullable())
            {
                Visit(node.Expression);
            }
            else if (IsPlainVariable(node.Expression, false))
            {
                var value = node.GetValueFromExpression();

                if (value is IQueryable queryable)
                {
                    Visit(queryable.Expression);
                }
                else
                {
                    Constant(value);
                }
            }
            else if (node.Expression?.Type == Types.DateTime)
            {
                DateTimeMember(node);
            }
            else if (node.Type.IsCell())
            {
                Member(node);
            }
            else if (TryGetSourceParameter(node, out ParameterExpression parameterExpression))
            {
                VisitParameter(parameterExpression);
            }
            else
            {
                Member(node);
            }

            return node;
        }

        /// <summary>
        /// 是否有值成员。
        /// </summary>
        /// <param name="node">节点。</param>
        protected virtual void MemberHasValue(Expression node)
        {
            Writer.OpenBrace();

            Visit(node);

            Writer.Keyword(SqlKeyword.IS);
            Writer.Keyword(SqlKeyword.NOT);
            Writer.Keyword(SqlKeyword.NULL);

            Writer.CloseBrace();
        }

        /// <summary>
        /// 日期成员。
        /// </summary>
        /// <param name="node">节点。</param>
        protected virtual void DateTimeMember(MemberExpression node)
        {
            switch (Engine)
            {
                case DatabaseEngine.SQLite:
                    SQLite(node.Member.Name, node.Expression);
                    break;
                case DatabaseEngine.Oracle:
                    Oracle(node.Member.Name, node.Expression);
                    break;
                case DatabaseEngine.DB2:
                    DB2(node.Member.Name, node.Expression);
                    break;
                case DatabaseEngine.Sybase:
                    Sybase(node.Member.Name, node.Expression);
                    break;
                case DatabaseEngine.SqlServer:
                    SqlServer(node.Member.Name, node.Expression);
                    break;
                case DatabaseEngine.PostgreSQL:
                    PostgreSql(node.Member.Name, node.Expression);
                    break;
                case DatabaseEngine.MySQL:
                    MySql(node.Member.Name, node.Expression);
                    break;
                default:
                    throw new NotSupportedException(
                        $"不支持数据库引擎“{Engine}”的日期片段计算!"
                    );
            }
        }

        #region DatabaseEngine DateTime Members

        /// <summary>
        /// MySQL
        /// </summary>
        private void MySql(string name, Expression node)
        {
            switch (name)
            {
                case nameof(DateTime.Date):
                    Writer.Write("DATE");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Year):
                    Writer.Write("YEAR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Month):
                    Writer.Write("MONTH");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Day):
                    Writer.Write("DAY");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Hour):
                    Writer.Write("HOUR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Minute):
                    Writer.Write("MINUTE");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Second):
                    Writer.Write("SECOND");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Millisecond):
                    Writer.Write("FLOOR");
                    Writer.OpenBrace();
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("MICROSECOND");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Divide);
                    Writer.Constant(1000);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.DayOfWeek):
                    Writer.Write("DAYOFWEEK");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.DayOfYear):
                    Writer.Write("DAYOFYEAR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Ticks):
                    Writer.Write("TIMESTAMPDIFF");
                    Writer.OpenBrace();
                    Writer.Write("MICROSECOND");
                    Writer.Delimiter();
                    Writer.Write("'0001-01-01'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10);
                    break;
                case nameof(DateTime.TimeOfDay):
                    Writer.Write("TIME");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                default:
                    throw new NotSupportedException($"不支持“{name}”日期片段计算!");
            }
        }

        /// <summary>
        /// PostgreSQL
        /// </summary>
        private void PostgreSql(string name, Expression node)
        {
            switch (name)
            {
                case nameof(DateTime.Date):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("DATE_TRUNC");
                    Writer.OpenBrace();
                    Writer.Write("'day'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS DATE");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Year):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("YEAR");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Month):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("MONTH");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Day):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("DAY");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Hour):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("HOUR");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Minute):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("MINUTE");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Second):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("SECOND");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Millisecond):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("MILLISECONDS");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write("::INTEGER % 1000");
                    break;
                case nameof(DateTime.DayOfWeek):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("DOW");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.DayOfYear):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("DOY");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Ticks):
                    // PostgreSQL: ticks = 621355968000000000 + (EXTRACT(EPOCH FROM (node AT TIME ZONE 'UTC')) * 10000000) + (EXTRACT(MICROSECONDS FROM node) * 10)
                    Writer.OpenBrace();
                    Writer.Constant(621355968000000000L);
                    Writer.Operator(SqlOperator.Add);

                    Writer.OpenBrace();
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("EPOCH");
                    Writer.Keyword(SqlKeyword.FROM);
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.Write(" AT TIME ZONE 'UTC'");
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10000000L);
                    Writer.CloseBrace();

                    Writer.Operator(SqlOperator.Add);

                    Writer.OpenBrace();
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("MICROSECONDS");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10L);
                    Writer.CloseBrace();

                    Writer.CloseBrace();
                    Writer.Write("::BIGINT");
                    break;
                case nameof(DateTime.TimeOfDay):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.Write(" AS TIME");
                    Writer.CloseBrace();
                    break;
                default:
                    throw new NotSupportedException($"不支持“{name}”日期片段计算!");
            }
        }

        /// <summary>
        /// SqlServer
        /// </summary>
        private void SqlServer(string name, Expression node)
        {
            switch (name)
            {
                case nameof(DateTime.Date):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.Write(" AS DATE");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Year):
                    Writer.Write("YEAR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Month):
                    Writer.Write("MONTH");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Day):
                    Writer.Write("DAY");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Hour):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("hour");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Minute):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("minute");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Second):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("second");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Millisecond):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("millisecond");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.DayOfWeek):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("weekday");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.DayOfYear):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("dayofyear");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Ticks):
                    Writer.Write("DATEDIFF_BIG");
                    Writer.OpenBrace();
                    Writer.Write("MICROSECOND");
                    Writer.Delimiter();
                    Writer.Constant("'0001-01-01'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10);
                    break;
                case nameof(DateTime.TimeOfDay):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.Write(" AS time");
                    Writer.CloseBrace();
                    break;
                default:
                    throw new NotSupportedException($"不支持“{name}”日期片段计算!");
            }
        }

        /// <summary>
        /// SQLite
        /// </summary>
        private void SQLite(string name, Expression node)
        {
            switch (name)
            {
                case nameof(DateTime.Date):
                    Writer.Write("DATE");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Year):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%Y'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS INTEGER");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Month):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%m'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS INTEGER");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Day):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%d'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS INTEGER");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Hour):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%H'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS INTEGER");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Minute):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%M'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS INTEGER");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Second):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%S'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS INTEGER");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Millisecond):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%f'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(1000);
                    Writer.CloseBrace();
                    Writer.Write(" % 1000 AS INTEGER");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.DayOfWeek):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%w'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS INTEGER");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.DayOfYear):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%j'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS INTEGER");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Ticks):
                    // SQLite: 使用 unixepoch 和毫秒组合计算，避免 JULIANDAY 浮点精度损失
                    // Ticks = (Unix秒 + 62135596800) * 10000000 + 毫秒部分 * 10000
                    Writer.OpenBrace();
                    Writer.OpenBrace();
                    Writer.OpenBrace();
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%s'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS INTEGER");
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Add);
                    Writer.Constant(62135596800L);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10000000L);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Add);
                    Writer.OpenBrace();
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%f'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(1000);
                    Writer.CloseBrace();
                    Writer.Write(" % 1000 AS INTEGER");
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10000L);
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.TimeOfDay):
                    Writer.Write("TIME");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                default:
                    throw new NotSupportedException($"不支持“{name}”日期片段计算!");
            }
        }

        /// <summary>
        /// Oracle
        /// </summary>
        private void Oracle(string name, Expression node)
        {
            switch (name)
            {
                case nameof(DateTime.Date):
                    Writer.Write("TRUNC");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Year):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("YEAR");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Month):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("MONTH");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Day):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("DAY");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Hour):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("HOUR");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Minute):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("MINUTE");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Second):
                    Writer.Write("FLOOR");
                    Writer.OpenBrace();
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("SECOND");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Millisecond):
                    Writer.Write("FLOOR");
                    Writer.OpenBrace();
                    Writer.Write("MOD");
                    Writer.OpenBrace();
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("SECOND");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(1000);
                    Writer.Delimiter();
                    Writer.Constant(1000);
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.DayOfWeek):
                    Writer.Write("TO_NUMBER");
                    Writer.OpenBrace();
                    Writer.Write("TO_CHAR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.Delimiter();
                    Writer.Write("'D'");
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Subtract);
                    Writer.Constant(1);
                    break;
                case nameof(DateTime.DayOfYear):
                    Writer.Write("TO_NUMBER");
                    Writer.OpenBrace();
                    Writer.Write("TO_CHAR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.Delimiter();
                    Writer.Write("'DDD'");
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Ticks):
                    // Oracle: 计算完整天数 * 每天 ticks + 当天秒数 * 每秒 ticks + 小数秒 * ticks/秒
                    Writer.OpenBrace();
                    Writer.OpenBrace();
                    Writer.Write("TRUNC");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Subtract);
                    Writer.Write("TO_DATE");
                    Writer.OpenBrace();
                    Writer.Write("'0001-01-01'");
                    Writer.Delimiter();
                    Writer.Write("'YYYY-MM-DD'");
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(864000000000L);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Add);
                    Writer.OpenBrace();
                    Writer.Write("FLOOR");
                    Writer.OpenBrace();
                    Writer.Write("TO_NUMBER");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.Operator(SqlOperator.Subtract);
                    Writer.Write("TRUNC");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(86400);
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10000000);
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.TimeOfDay):
                    Writer.Write("TO_CHAR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.Delimiter();
                    Writer.Write("'HH24:MI:SS'");
                    Writer.CloseBrace();
                    break;
                default:
                    throw new NotSupportedException($"不支持“{name}”日期片段计算!");
            }
        }

        /// <summary>
        /// DB2
        /// </summary>
        private void DB2(string name, Expression node)
        {
            switch (name)
            {
                case nameof(DateTime.Date):
                    Writer.Write("DATE");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Year):
                    Writer.Write("YEAR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Month):
                    Writer.Write("MONTH");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Day):
                    Writer.Write("DAY");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Hour):
                    Writer.Write("HOUR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Minute):
                    Writer.Write("MINUTE");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Second):
                    Writer.Write("SECOND");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Millisecond):
                    Writer.Write("MICROSECOND");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Divide);
                    Writer.Constant(1000);
                    break;
                case nameof(DateTime.DayOfWeek):
                    Writer.Write("DAYOFWEEK");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Subtract);
                    Writer.Constant(1);
                    break;
                case nameof(DateTime.DayOfYear):
                    Writer.Write("DAYOFYEAR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Ticks):
                    // DB2: 天数 * 每天ticks + 秒数 * 每秒ticks + 微秒 * 10
                    Writer.OpenBrace();
                    Writer.OpenBrace();
                    Writer.OpenBrace();
                    Writer.Write("DAYS");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Subtract);
                    Writer.Write("DAYS");
                    Writer.OpenBrace();
                    Writer.Write("'0001-01-01'");
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(86400);
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10000000);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Add);
                    Writer.OpenBrace();
                    Writer.Write("MIDNIGHT_SECONDS");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10000000);
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Add);
                    Writer.OpenBrace();
                    Writer.Write("MICROSECOND");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.TimeOfDay):
                    Writer.Write("TIME");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                default:
                    throw new NotSupportedException($"不支持“{name}”日期片段计算!");
            }
        }

        /// <summary>
        /// Sybase
        /// </summary>
        private void Sybase(string name, Expression node)
        {
            switch (name)
            {
                case nameof(DateTime.Date):
                    Writer.Write("CONVERT");
                    Writer.OpenBrace();
                    Writer.Write("DATE");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Year):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("year");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Month):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("month");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Day):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("day");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Hour):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("hour");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Minute):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("minute");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Second):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("second");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Millisecond):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("millisecond");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.DayOfWeek):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("weekday");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Subtract);
                    Writer.Constant(1);
                    break;
                case nameof(DateTime.DayOfYear):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("dayofyear");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Ticks):
                    Writer.Write("DATEDIFF");
                    Writer.OpenBrace();
                    Writer.Write("us");
                    Writer.Delimiter();
                    Writer.Write("'0001-01-01'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10);
                    break;
                case nameof(DateTime.TimeOfDay):
                    Writer.Write("CONVERT");
                    Writer.OpenBrace();
                    Writer.Write("VARCHAR(8)");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.Delimiter();
                    Writer.Constant(108);
                    Writer.CloseBrace();
                    break;
                default:
                    throw new NotSupportedException($"不支持“{name}”日期片段计算!");
            }
        }

        #endregion

        /// <inheritdoc/>
        protected sealed override Expression VisitMemberInit(MemberInitExpression node)
        {
            if (node.Bindings.Count > 0)
            {
                if (node.Type.IsCell())
                {
                    throw new NotSupportedException();
                }
            }
            else
            {
                throw new NotSupportedException();
            }

            bool flag = false;

            foreach (var binding in node.Bindings)
            {
                using (var domain = Writer.Domain())
                {
                    MemberBinding(binding);

                    if (domain.IsEmpty)
                    {
                        continue;
                    }

                    if (flag)
                    {
                        domain.Flyback();

                        Writer.Delimiter();
                    }
                    else
                    {
                        flag = true;
                    }
                }
            }

            return node;
        }

        private void Version(
            int i,
            ReadOnlyCollection<Expression> arguments
        )
        {
            if (i < 3)
            {
                Writer.Write("CONCAT");
                Writer.OpenBrace();

                if (i < arguments.Count)
                {
                    Visit(arguments[i]);
                }

                Writer.Delimiter();
                Writer.Write('.');
                Writer.Delimiter();

                Version(i + 1, arguments);

                Writer.CloseBrace();
            }
            else if (i < arguments.Count)
            {
                Visit(arguments[i]);
            }
            else
            {
                Writer.Write('0');
            }
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitNew(NewExpression node)
        {
            if (node.Arguments.Count == 0)
            {
                Constant(node.GetValueFromExpression());
            }
            else if (node.Arguments.Count == 1 && node.Members?.Count == 1)
            {
                Member(node.Members[0], node.Arguments[0]);
            }
            else if (node.Type == Types.Version)
            {
                Version(node);
            }
            else if (node.Type == Types.JsonbPayload)
            {
                Visit(node.Arguments[0]);

                if (Engine == DatabaseEngine.PostgreSQL)
                {
                    Writer.Write("::jsonb");
                }
            }
            else if (node.Type == Types.JsonPayload)
            {
                Visit(node.Arguments[0]);

                if (Engine == DatabaseEngine.PostgreSQL)
                {
                    Writer.Write("::json");
                }
            }
            else if (node.Type.IsCell())
            {
                throw new NotSupportedException(
                    "不支持基础类型的多参数实例化，如：x => new SimpleType(x.Id,x.Name,3) 或 x => new SimpleType(1, 2, 3) 语法。"
                );
            }
            else if (node.Arguments.All(x => x.NodeType == ExpressionType.Parameter))
            {
                //? 忽略所有属性都是参数的类型。
            }
            else if (node.Arguments.Any(x => x.NodeType == ExpressionType.Parameter))
            {
                throw new NotSupportedException(
                    "不支持基础类型的聚合属性，如：(x,y) => new { x.Id, y } 或 x => new { x } 语法。"
                );
            }
            else if (node.Members?.Count > 1)
            {
                bool flag = false;

                for (int i = 0; i < node.Members.Count; i++)
                {
                    using (var domain = Writer.Domain())
                    {
                        Member(node.Members[i], node.Arguments[i]);

                        if (domain.IsEmpty)
                        {
                            continue;
                        }

                        if (flag)
                        {
                            domain.Flyback();

                            Writer.Delimiter();
                        }
                        else
                        {
                            flag = true;
                        }
                    }
                }
            }

            return node;
        }

        /// <summary>
        /// 版本号。
        /// </summary>
        /// <param name="node">节点。</param>
        protected virtual void Version(NewExpression node) => Version(0, node.Arguments);

        /// <inheritdoc/>
        protected sealed override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Quote)
            {
                return base.VisitUnary(node);
            }

            Unary(node);

            return node;
        }

        #endregion

        #region base virtual

        /// <summary>
        /// Visits the children of the <see cref="TypeBinaryExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void TypeBinary(TypeBinaryExpression node) => base.VisitTypeBinary(node);

        /// <summary>
        /// Visits the children of the <see cref="TryExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Try(TryExpression node) => base.VisitTry(node);

        /// <summary>
        /// Visits the children of the <see cref="System.Linq.Expressions.MemberMemberBinding"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void SwitchCase(SwitchCase node) => base.VisitSwitchCase(node);

        /// <summary>
        /// Visits the children of the <see cref="SwitchExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Switch(SwitchExpression node) => base.VisitSwitch(node);

        /// <summary>
        /// Visits the children of the <see cref="RuntimeVariablesExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void RuntimeVariables(RuntimeVariablesExpression node) =>
            base.VisitRuntimeVariables(node);

        /// <summary>
        /// Visits the children of the <see cref="ParameterExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Parameter(ParameterExpression node) => base.VisitParameter(node);

        /// <summary>
        /// Visits the children of the <see cref="NewArrayExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void NewArray(NewArrayExpression node) => base.VisitNewArray(node);

        /// <summary>
        /// Visits the children of the <see cref="System.Linq.Expressions.MemberMemberBinding"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void MemberMemberBinding(MemberMemberBinding node) =>
            base.VisitMemberMemberBinding(node);

        /// <summary>
        /// Visits the children of the <see cref="System.Linq.Expressions.MemberListBinding"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void MemberListBinding(MemberListBinding node) =>
            base.VisitMemberListBinding(node);

        /// <summary>
        /// 成员。
        /// </summary>
        /// <param name="memberInfo">成员。</param>
        /// <param name="node">赋值表达式。</param>
        protected virtual void Member(MemberInfo memberInfo, Expression node)
        {
            using (var visitor = new MemberVisitor(this))
            {
                visitor.Startup(node);
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="System.Linq.Expressions.MemberBinding"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void MemberBinding(MemberBinding node) => base.VisitMemberBinding(node);

        /// <summary>
        /// Visits the children of the <see cref="MemberAssignment"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void MemberAssignment(MemberAssignment node) =>
            Member(node.Member, node.Expression);

        /// <summary>
        /// Visits the children of the <see cref="MemberExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Member(MemberExpression node)
        {
            if (!node.Type.IsCell())
            {
                MemberLeavesIsObject(node);

                return;
            }

            if (!TryGetSourceParameter(node.Expression, out ParameterExpression parameter))
            {
                MemberHasDependency(node);

                return;
            }

            if (!TryGetSourceTableInfo(parameter, out var tableInfo))
            {
                Writer.Schema(parameter.Name);

                Writer.Name(node.Member.Name);

                return;
            }

            if (!tableInfo.Fields.TryGetValue(node.Member.Name, out string field))
            {
                throw new MissingFieldException(
                    $"属性“{parameter.Type.Name}.{node.Member.Name}”不是有效的数据库字段！"
                );
            }

            Writer.Schema(parameter.Name);

            Writer.Name(field);
        }

        /// <summary>
        /// Visits the children of the <see cref="MemberExpression"/>, the member branch is not a parameter.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void MemberHasDependency(MemberExpression node)
        {
            if (_visitor is null)
            {
                throw new NotSupportedException($"不支持依赖成员“{node.Member.Name}”的解析！");
            }

            _visitor.Member(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="MemberExpression"/>, when the member type is a complex object.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void MemberLeavesIsObject(MemberExpression node)
        {
            if (_visitor is null)
            {
                throw new NotSupportedException("不支持导航属性！");
            }

            _visitor.MemberLeavesIsObject(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="LoopExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Loop(LoopExpression node) => base.VisitLoop(node);

        /// <summary>
        /// Visits the children of the <see cref="ListInitExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void ListInit(ListInitExpression node) => base.VisitListInit(node);

        /// <summary>
        /// Visits the children of the <see cref="System.Linq.Expressions.LabelTarget"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void LabelTarget(LabelTarget node) => base.VisitLabelTarget(node);

        /// <summary>
        /// Visits the children of the <see cref="LabelExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Label(LabelExpression node) => base.VisitLabel(node);

        /// <summary>
        /// Visits the children of the <see cref="IndexExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Index(IndexExpression node) => base.VisitIndex(node);

        /// <summary>
        /// Visits the children of the <see cref="GotoExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Goto(GotoExpression node) => base.VisitGoto(node);

        /// <summary>
        /// Visits the children of the <see cref="Expression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Extension(Expression node) => base.VisitExtension(node);

        /// <summary>
        /// Visits the children of the <see cref="System.Linq.Expressions.ElementInit"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void ElementInit(ElementInit node) => base.VisitElementInit(node);

        /// <summary>
        /// Visits the children of the <see cref="DynamicExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Dynamic(DynamicExpression node) => base.VisitDynamic(node);

        /// <summary>
        /// Visits the children of the <see cref="DebugInfoExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void DebugInfo(DebugInfoExpression node) => base.VisitDebugInfo(node);

        /// <summary>
        /// Visits the children of the <see cref="BlockExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Block(BlockExpression node) => base.VisitBlock(node);

        /// <summary>
        /// Visits the children of the <see cref="ConstantExpression"/>.
        /// </summary>
        protected virtual void Constant(ConstantExpression node) => Constant(node.Value);

        /// <summary>
        /// 常量。
        /// </summary>
        /// <param name="value">对象值</param>
        protected virtual void Constant(object value)
        {
            switch (value)
            {
                case Expression node:

                    Visit(node);

                    break;
                case IQueryable queryable:

                    Constant(queryable);

                    break;
                default:

                    Writer.Constant(value);

                    break;
            }
        }

        /// <summary>
        /// 查询器常量。
        /// </summary>
        /// <param name="value">查询器常量</param>
        /// <exception cref="NotSupportedException">不支持查询器常量！</exception>
        protected virtual void Constant(IQueryable value) =>
            throw new NotSupportedException("不支持查询器常量！");

        /// <summary>
        /// Visits the children of the <see cref="System.Linq.Expressions.CatchBlock"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void CatchBlock(CatchBlock node) => base.VisitCatchBlock(node);

        /// <summary>
        /// Visits the children of the <see cref="ConditionalExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Conditional(ConditionalExpression node)
        {
            using (var visitor = new ConditionalVisitor(this))
            {
                visitor.Startup(node);
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="MethodCallExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void MethodCall(MethodCallExpression node)
        {
            if (_visitor is null)
            {
                throw new NotImplementedException();
            }

            _visitor.MethodCall(node);
        }

        /// <summary>
        /// Visits the children of the <see cref="InvocationExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Invocation(InvocationExpression node) =>
            throw new NotSupportedException();

        /// <summary>
        /// Visits the children of the <see cref="Expression{T}"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Lambda<T>(Expression<T> node) => base.VisitLambda(node);

        /// <summary>
        /// Visits the children of the <see cref="Expression{T}"/>, and <seealso cref="LambdaExpression.Body"/> is <seealso cref="ParameterExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void BodyIsParameter(ParameterExpression node)
        {
            if (!TryGetSourceParameter(node, out ParameterExpression parameter))
            {
                throw new DSyntaxErrorException("未能分析到表参数！");
            }

            if (!TryGetSourceTableInfo(parameter, out var tableInfo))
            {
                throw new DSyntaxErrorException("未能分析到表名称！");
            }

            bool commaFlag = false;

            string schema = parameter.Name;

            foreach (var (name, field) in tableInfo.Fields)
            {
                if (commaFlag)
                {
                    Writer.Delimiter();
                }
                else
                {
                    commaFlag = true;
                }

                Member(schema, field, name);
            }
        }

        /// <summary>
        /// 成员，
        /// </summary>
        /// <param name="schema">架构。</param>
        /// <param name="field">字段。</param>
        /// <param name="name">别名。</param>
        protected virtual void Member(string schema, string field, string name)
        {
            Writer.Schema(schema);

            Writer.Name(field);
        }

        /// <summary>
        /// 变量。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="value">值。</param>
        protected virtual void Variable(string name, object value)
        {
            switch (value)
            {
                case IQueryable queryable:

                    Visit(queryable.Expression);

                    break;
                case Expression node:

                    Visit(node);

                    break;
                default:

                    Writer.Variable(name, value);

                    break;
            }
        }

        /// <summary>
        /// 准备参数信息。
        /// </summary>
        /// <param name="node"></param>
        protected virtual void PreparingParameter(LambdaExpression node)
        {
            if (_visitor is null)
            {
                throw new NotImplementedException();
            }

            _visitor.PreparingParameter(node);
        }

        /// <summary>
        /// 获取参数别名。
        /// </summary>
        /// <param name="node">节点。</param>
        /// <param name="parameterExpression">节点来源参数。</param>
        /// <returns>别名参数。</returns>
        protected virtual bool TryGetSourceParameter(
            Expression node,
            out ParameterExpression parameterExpression
        )
        {
            if (_visitor is null)
            {
                parameterExpression = null;

                return false;
            }

            return _visitor.TryGetSourceParameter(node, out parameterExpression);
        }

        /// <summary>
        /// 尝试获取表名称。
        /// </summary>
        /// <param name="node">参数节点。</param>
        /// <param name="tableInfo">表信息。</param>
        protected virtual bool TryGetSourceTableInfo(
            ParameterExpression node,
            out ITableInfo tableInfo
        )
        {
            if (_visitor is null)
            {
                tableInfo = null;

                return false;
            }

            return _visitor.TryGetSourceTableInfo(node, out tableInfo);
        }

        /// <summary>
        /// Visits the children of the <see cref="UnaryExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Unary(UnaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.UnaryPlus:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:

                    Visit(node.Operand);

                    break;
                case ExpressionType.Decrement:

                    Visit(node.Operand);

                    Writer.Operator(SqlOperator.Decrement);

                    break;
                case ExpressionType.Increment:

                    Visit(node.Operand);

                    Writer.Operator(SqlOperator.Increment);

                    break;
                case ExpressionType.IsFalse:
                    goto case ExpressionType.Not;
                case ExpressionType.IsTrue:

                    Visit(node.Operand);

                    break;
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:

                    Writer.Operator(SqlOperator.Negate);

                    Visit(node.Operand);

                    break;
                case ExpressionType.Not:
                case ExpressionType.OnesComplement when node.Type.IsBoolean():
                    switch (node.Operand.NodeType)
                    {
                        case ExpressionType.Call when node.Type.IsBoolean():
                            goto default;
                        case ExpressionType.Constant:
                        case ExpressionType.MemberAccess:
                        case ExpressionType.Parameter:
                        case ExpressionType.MemberInit:
                        case ExpressionType.NewArrayInit:
                        case ExpressionType.RightShift:
                        case ExpressionType.LeftShift:
                        case ExpressionType.Add:
                        case ExpressionType.AddChecked:
                        case ExpressionType.Subtract:
                        case ExpressionType.SubtractChecked:
                        case ExpressionType.Multiply:
                        case ExpressionType.MultiplyChecked:
                        case ExpressionType.Divide:
                        case ExpressionType.Modulo:
                        case ExpressionType.Coalesce:
                        case ExpressionType.ArrayIndex:
                        case ExpressionType.Negate:
                        case ExpressionType.UnaryPlus:
                        case ExpressionType.Convert:
                        case ExpressionType.ConvertChecked:
                        case ExpressionType.Unbox:
                        case ExpressionType.ArrayLength:
                        case ExpressionType.AndAssign:
                        case ExpressionType.OrAssign:
                        case ExpressionType.ExclusiveOrAssign:
                        case ExpressionType.Assign:
                            Writer.Keyword(SqlKeyword.NOT);

                            Writer.OpenBrace();

                            Visit(node.Operand);

                            Writer.CloseBrace();

                            break;
                        case ExpressionType.AndAlso:
                        case ExpressionType.OrElse:
                        case ExpressionType.Equal:
                        case ExpressionType.GreaterThan:
                        case ExpressionType.GreaterThanOrEqual:
                        case ExpressionType.LessThan:
                        case ExpressionType.LessThanOrEqual:
                        case ExpressionType.Not:
                        case ExpressionType.NotEqual:
                        case ExpressionType.Conditional:
                        case ExpressionType.Quote:
                        default:

                            using (Writer.ConditionReversal())
                            {
                                Visit(node.Operand);
                            }

                            break;
                    }

                    break;
                case ExpressionType.OnesComplement:

                    Writer.Operator(SqlOperator.OnesComplement);

                    if (node.Operand is BinaryExpression or UnaryExpression)
                    {
                        Writer.OpenBrace();

                        Visit(node.Operand);

                        Writer.CloseBrace();
                    }
                    else
                    {
                        Visit(node.Operand);
                    }

                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="BinaryExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Binary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Coalesce)
            {
                Coalesce(node);
            }
            else
            {
                Condition(node);
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="Expression"/> and process it as a condition.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Condition(Expression node)
        {
            using (var visitor = new ConditionVisitor(this))
            {
                visitor.Startup(node);
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="Expression"/> and process it as a coalesced.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Coalesce(Expression node)
        {
            using (var visitor = new CoalesceVisitor(this))
            {
                visitor.Startup(node);
            }
        }

        /// <summary>
        /// 表名称。
        /// </summary>
        protected virtual void Name()
        {
            if (_tableInformation is null)
            {
                throw new DSyntaxErrorException("未能分析到表名称！");
            }

            string name = _tableInformation.Name;
            string schema = _tableInformation.Schema ?? string.Empty;

            if (schema.Length == 0)
            {
                if (Engine == DatabaseEngine.SqlServer)
                {
                    schema = "dbo";
                }
                else if (Engine == DatabaseEngine.PostgreSQL)
                {
                    schema = "public";
                }
            }

            Writer.Schema(schema);

            if (_tableInformation.DataSharding ^ _dataSharding)
            {
                if (_tableInformation.DataSharding)
                {
                    throw new InvalidOperationException($"分区表“{_tableInformation.Name}”的操作，必须指定分区键！");
                }

                throw new InvalidOperationException($"普通表“{_tableInformation.Name}”不支持分区操作！");
            }

            if (_dataSharding)
            {
                name = _tableInformation.Fragment(_shardingKey);
            }

            Writer.Name(name);
        }

        /// <summary>
        /// 表信息。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        protected ITableInfo Table(bool onlyMyself = false)
        {
            if (onlyMyself)
            {
                return _tableInformation;
            }

            return _tableInformation
                   ?? _visitor?.Table()
                   ?? throw new DSyntaxErrorException("未能分析到表名称！");
        }

        #endregion

        /// <inheritdoc/>
        public override string ToString() => Writer.ToString();

        /// <summary>
        /// 释放对象。
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_isNewWriter)
                    {
                        _visitor.Writer.Insert(_cursorPosition, ToString());

                        Writer.Dispose();
                    }
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                _disposedValue = true;
            }
        }

        /// <inheritdoc/>
        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~BaseVisitor()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }
        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}