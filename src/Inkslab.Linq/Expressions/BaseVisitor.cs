using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Inkslab.Collections;
using Inkslab.Linq.Enums;
using Inkslab.Linq.Exceptions;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 基础访问器（负责条件处理和基础访问器结构管理）。
    /// </summary>
    [DebuggerDisplay("Base")]
    public abstract class BaseVisitor : ExpressionVisitor, IDisposable
    {
        private bool disposedValue;

        private volatile bool hasBaseStartup = false;

        private readonly BaseVisitor _visitor;
        private readonly bool _isNewWriter;
        private readonly IDbAdapter _adapter;
        private readonly int _cursorPosition = -1;

        /// <summary>
        /// 数据分片。
        /// </summary>
        private bool dataSharding = false;

        /// <summary>
        /// 数据分片。
        /// </summary>
        private bool dataShardingInvalid = false;

        /// <summary>
        /// 分区键。
        /// </summary>
        private string shardingKey = string.Empty;

        /// <summary>
        /// 表信息。
        /// </summary>
        private ITableInfo tableInformation;

        /// <summary>
        /// 数据库引擎。
        /// </summary>
        protected DatabaseEngine Engine => _adapter.Engine;

        /// <summary>
        /// 写入器。
        /// </summary>
        protected SqlWriter Writer { get; }

        /// <summary>
        /// 访问器。
        /// </summary>
        /// <param name="adapter">数据库适配器。</param>
        /// <exception cref="ArgumentNullException">参数 <paramref name="adapter"/> 不能为空。</exception>
        protected BaseVisitor(IDbAdapter adapter)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));

            Writer = CreateWriter(adapter.Settings);
        }

        /// <inheritdoc />
        protected BaseVisitor(BaseVisitor visitor)
            : this(visitor, false) { }

        /// <summary>
        /// 子表达式。
        /// </summary>
        /// <param name="visitor">父表达式访问器。</param>
        /// <param name="isNewWriter">是否创建新的写入器。</param>
        /// <exception cref="ArgumentNullException">参数<see cref="_visitor"/>为 Null。</exception>
        protected BaseVisitor(BaseVisitor visitor, bool isNewWriter)
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
                if (hasBaseStartup)
                {
                    StartupCore((MethodCallExpression)node);
                }
                else
                {
                    hasBaseStartup = true;

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
            var instanceArg = node.Arguments[0];

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
                        instanceArg.NodeType == ExpressionType.Constant
                        && instanceArg is ConstantExpression constant
                        && constant.Value is IQueryable queryable
                    )
                    {
                        tableInformation ??= TableAnalyzer.Table(queryable.ElementType);
                    }
                    break;
            }

            if (hasBaseStartup)
            {
                StartupCore(node);
            }
            else
            {
                hasBaseStartup = true;

                Startup((Expression)node);
            }
        }

        /// <summary>
        /// 启动核心方法。
        /// </summary>
        /// <param name="node">节点。</param>
        protected virtual void StartupCore(MethodCallExpression node) => MethodCall(node);

        /// <summary>
        /// 是普通变量。
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>是否是常规变量。</returns>
        protected virtual bool IsPlainVariable(Expression node) => IsPlainVariable(node, false);

        private static readonly Lfu<Expression, bool> _lfu = new Lfu<Expression, bool>(10000, ExpressionEqualityComparer.Instance, IsPlainVariableNS);

        private static bool IsPlainVariableNS(Expression node)
        {
            if (node.NodeType == ExpressionType.Parameter)
            {
                return false;
            }

            switch (node)
            {
                case ConstantExpression constant:
                    return constant.Value is not IQueryable;
                case MemberExpression member:
                    if (member.Expression is null)
                    {
                        return true;
                    }

                    switch (member.Member)
                    {
                        case FieldInfo fieldInfo:
                            if (fieldInfo.IsStatic)
                            {
                                return true;
                            }

                            break;

                        default:
                            var declaringType = member.Member.DeclaringType;

                            if (declaringType.IsSealed && declaringType.IsAbstract) //? 静态类。
                            {
                                return true;
                            }

                            break;
                    }

                    return IsPlainVariableNS(member.Expression);
                case MethodCallExpression method
                                    when method.Object is null || IsPlainVariableNS(method.Object):
                    return method.Arguments.Count == 0
                        || method.Arguments.All(arg =>
                            IsPlainVariableNS(arg)
                        );
                case BinaryExpression binary:
                    return IsPlainVariableNS(binary.Left)
                        && IsPlainVariableNS(binary.Right);
                case LambdaExpression lambda when lambda.Parameters.Count == 0:
                    return IsPlainVariableNS(lambda.Body);
                case NewExpression newExpression when newExpression.Members.Count == 0:
                    return newExpression.Arguments.Count == 0
                        || newExpression.Arguments.All(arg =>
                            IsPlainVariableNS(arg)
                        );
                case MemberInitExpression memberInit
                                    when IsPlainVariableNS(memberInit.NewExpression):
                    foreach (var binding in memberInit.Bindings)
                    {
                        if (
                            binding is MemberAssignment assignment
                            && IsPlainVariableNS(assignment.Expression)
                        )
                        {
                            continue;
                        }

                        return false;
                    }
                    return true;
                case ConditionalExpression conditional
                                    when IsPlainVariableNS(conditional.Test):
                    return IsPlainVariableNS(conditional.IfTrue)
                        && IsPlainVariableNS(conditional.IfFalse);
                case UnaryExpression unary when unary.NodeType is ExpressionType.Quote
                                        or ExpressionType.Convert
                                        or ExpressionType.ConvertChecked
                                        or ExpressionType.OnesComplement
                                        or ExpressionType.IsTrue
                                        or ExpressionType.IsFalse
                                        or ExpressionType.Not:
                    return IsPlainVariableNS(unary.Operand);
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
        protected virtual bool IsPlainVariable(Expression node, bool depthVerification)
        {
            if (node.NodeType == ExpressionType.Parameter)
            {
                return false;
            }

            switch (node)
            {
                case ConstantExpression constant:
                    return constant.Value is not IQueryable;
                case MemberExpression member:
                    if (member.Expression is null)
                    {
                        return true;
                    }

                    switch (member.Member)
                    {
                        case FieldInfo fieldInfo:
                            if (fieldInfo.IsStatic)
                            {
                                return true;
                            }

                            break;

                        default:
                            var declaringType = member.Member.DeclaringType;

                            if (declaringType.IsSealed && declaringType.IsAbstract) //? 静态类。
                            {
                                return true;
                            }

                            break;
                    }

                    return IsPlainVariable(member.Expression, depthVerification);
                default:
                    return depthVerification && _lfu.Get(node);
            }
        }

        /// <summary>
        /// 是否需要条件转义。
        /// </summary>
        /// <returns></returns>
        protected bool RequiresConditionalEscape() => Engine != DatabaseEngine.MySQL;

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
        protected override sealed Expression VisitInvocation(InvocationExpression node)
        {
            Invocation(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitMethodCall(MethodCallExpression node)
        {
            var instanceArg = node.Method.IsStatic
                ? node.Arguments[0]
                : node.Object;

            switch (node.Method.Name)
            {
                case nameof(Queryable.Join):

                    MethodCall(node);

                    break;
                case nameof(QueryableExtentions.DataSharding):

                    if (dataSharding)
                    {
                        throw new DSyntaxErrorException("每个数据源的数据分区只能指定一次！");
                    }

                    dataSharding = dataShardingInvalid = true;
                    shardingKey = node.Arguments[1].GetValueFromExpression<string>();

                    Visit(node.Arguments[0]);

                    if (dataShardingInvalid)
                    {
                        throw new DSyntaxErrorException("仅根数据源支持数据分区！");
                    }

                    break;
                case nameof(Queryable.GroupJoin):

                    //? 分析 JOIN 表。
                    instanceArg = node.Arguments[1];

                    goto default;
                default:
                    if (
                        instanceArg.NodeType == ExpressionType.Constant
                        && instanceArg is ConstantExpression constant
                        && constant.Value is IQueryable queryable
                    )
                    {
                        tableInformation ??= TableAnalyzer.Table(queryable.ElementType);
                    }

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
        protected override sealed Expression VisitBinary(BinaryExpression node)
        {
            Binary(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitConditional(ConditionalExpression node)
        {
            Conditional(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitBlock(BlockExpression node)
        {
            Block(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed CatchBlock VisitCatchBlock(CatchBlock node)
        {
            CatchBlock(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is IQueryable queryable)
            {
                tableInformation ??= TableAnalyzer.Table(queryable.ElementType); //? 兼容 LEFT JOIN 导致的函数分析问题。
            }

            dataShardingInvalid &= false;

            Constant(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitDebugInfo(DebugInfoExpression node)
        {
            DebugInfo(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitDefault(DefaultExpression node)
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
        protected override sealed Expression VisitDynamic(DynamicExpression node)
        {
            Dynamic(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed ElementInit VisitElementInit(ElementInit node)
        {
            ElementInit(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitExtension(Expression node)
        {
            Extension(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitGoto(GotoExpression node)
        {
            Goto(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitIndex(IndexExpression node)
        {
            Index(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitLabel(LabelExpression node)
        {
            Label(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed LabelTarget VisitLabelTarget(LabelTarget node)
        {
            LabelTarget(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitListInit(ListInitExpression node)
        {
            ListInit(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitLoop(LoopExpression node)
        {
            Loop(node);

            return node;
        }

        /// <summary>
        /// MySQL
        /// </summary>
        private void MySql(string name, Expression node)
        {
            switch (name)
            {
                case nameof(DateTime.Date):
                    Writer.Write("DATE");
                    break;
                case nameof(DateTime.Second):
                    Writer.Write("SECOND");
                    break;
                case nameof(DateTime.Minute):
                    Writer.Write("MINUTE");
                    break;
                case nameof(DateTime.Hour):
                    Writer.Write("HOUR");
                    break;
                case nameof(DateTime.DayOfWeek):
                    Writer.Write("DAYOFWEEK");
                    break;
                case nameof(DateTime.DayOfYear):
                    Writer.Write("DAYOFYEAR");
                    break;
                default:
                    throw new NotSupportedException($"不支持“{name}”日期片段计算!");
            }

            Writer.OpenBrace();
            Visit(node);
            Writer.CloseBrace();
        }

        /// <summary>
        /// SqlServer
        /// </summary>
        private void SqlServer(string name, Expression node)
        {
            if (name == nameof(DateTime.Date))
            {

                Writer.Write("CAST");
                Writer.OpenBrace();

                Visit(node);

                Writer.Write("AS DATE");
                Writer.CloseBrace();

                return;
            }

            Writer.Write("DATEPART");
            Writer.OpenBrace();

            switch (name)
            {
                case nameof(DateTime.Millisecond):
                    Writer.Write("ms");
                    break;
                case nameof(DateTime.Second):
                    Writer.Write("ss");
                    break;
                case nameof(DateTime.Minute):
                    Writer.Write("mi");
                    break;
                case nameof(DateTime.Hour):
                    Writer.Write("hh");
                    break;
                case nameof(DateTime.DayOfWeek):
                    Writer.Write("dw");
                    break;
                case nameof(DateTime.DayOfYear):
                    Writer.Write("dy");
                    break;
                default:
                    throw new NotSupportedException($"不支持“{name}”日期片段计算!");
            }

            Writer.Delimiter();

            Visit(node);

            Writer.CloseBrace();
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is null)
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
            else if (node.IsLength())
            {
                switch (Engine)
                {
                    case DatabaseEngine.Access:
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
            else if (node.Expression.IsNullable())
            {
                if (node.Member.Name == "HasValue")
                {
                    MemberHasValue(node.Expression);
                }
                else
                {
                    Visit(node.Expression);
                }
            }
            else if (node.Expression.Type == Types.DateTime)
            {
                DateTimeMember(node);
            }
            else if (IsPlainVariable(node, true))
            {
                var constant = node.GetValueFromExpression();

                if (constant is IQueryable queryable)
                {
                    Visit(queryable.Expression);
                }
                else
                {
                    Variable(node.Member.Name.ToCamelCase(), constant);
                }
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
            switch (node.Member.Name)
            {
                case nameof(DateTime.Day) when Engine == DatabaseEngine.PostgreSQL:
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("DAY FROM ");
                    Visit(node.Expression);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Day):
                    Writer.Write("DAY");
                    Writer.OpenBrace();
                    Visit(node.Expression);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Month) when Engine == DatabaseEngine.PostgreSQL:
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("MONTH FROM ");
                    Visit(node.Expression);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Month):
                    Writer.Write("MOUTH");
                    Writer.OpenBrace();
                    Visit(node.Expression);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Year) when Engine == DatabaseEngine.PostgreSQL:
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("YEAR FROM ");
                    Visit(node.Expression);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Year):
                    Writer.Write("YEAR");
                    Writer.OpenBrace();
                    Visit(node.Expression);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Date) when Engine == DatabaseEngine.MySQL:
                case nameof(DateTime.Second) when Engine == DatabaseEngine.MySQL:
                case nameof(DateTime.Minute) when Engine == DatabaseEngine.MySQL:
                case nameof(DateTime.Hour) when Engine == DatabaseEngine.MySQL:
                case nameof(DateTime.DayOfWeek) when Engine == DatabaseEngine.MySQL:
                case nameof(DateTime.DayOfYear) when Engine == DatabaseEngine.MySQL:
                    MySql(node.Member.Name, node.Expression);
                    break;
                case nameof(DateTime.Date) when Engine == DatabaseEngine.SqlServer:
                case nameof(DateTime.Millisecond) when Engine == DatabaseEngine.SqlServer:
                case nameof(DateTime.Second) when Engine == DatabaseEngine.SqlServer:
                case nameof(DateTime.Minute) when Engine == DatabaseEngine.SqlServer:
                case nameof(DateTime.Hour) when Engine == DatabaseEngine.SqlServer:
                case nameof(DateTime.DayOfWeek) when Engine == DatabaseEngine.SqlServer:
                case nameof(DateTime.DayOfYear) when Engine == DatabaseEngine.SqlServer:
                    SqlServer(node.Member.Name, node.Expression);
                    break;
                default:
                    throw new NotSupportedException($"不支持“{node.Member.Name}”日期片段计算!");
            }
        }

        /// <inheritdoc/>
        protected override sealed MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            MemberAssignment(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed MemberBinding VisitMemberBinding(MemberBinding node)
        {
            MemberBinding(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitMemberInit(MemberInitExpression node)
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

        /// <inheritdoc/>
        protected override sealed MemberListBinding VisitMemberListBinding(MemberListBinding node)
        {
            MemberListBinding(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed MemberMemberBinding VisitMemberMemberBinding(
            MemberMemberBinding node
        )
        {
            MemberMemberBinding(node);

            return node;
        }

        private void Version(
            int i,
            System.Collections.ObjectModel.ReadOnlyCollection<Expression> arguments
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
        protected override sealed Expression VisitNew(NewExpression node)
        {
            if (node.Arguments.Count == 0)
            {
                Constant(node.GetValueFromExpression());
            }
            else if (node.Arguments.Count == 1)
            {
                Member(node.Members[0], node.Arguments[0]);
            }
            else if (node.Type == Types.Version)
            {
                Version(node);
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
            else
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
        protected override sealed Expression VisitNewArray(NewArrayExpression node)
        {
            NewArray(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitParameter(ParameterExpression node)
        {
            Parameter(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        {
            RuntimeVariables(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitSwitch(SwitchExpression node)
        {
            Switch(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed SwitchCase VisitSwitchCase(SwitchCase node)
        {
            SwitchCase(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitTry(TryExpression node)
        {
            Try(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            TypeBinary(node);

            return node;
        }

        /// <inheritdoc/>
        protected override sealed Expression VisitUnary(UnaryExpression node)
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
            else
            {
                _visitor.Member(node);
            }
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

        /// <inheritdoc/>
        protected virtual void Constant(ConstantExpression node) => Constant(node.Value);

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
            bool commaFlag = false;

            var tableInfo = Table();

            string schema = TryGetSourceParameter(node, out ParameterExpression parameterExpression)
                ? parameterExpression.Name
                : node.Name;

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
        /// Visits the children of the <see cref="Expression"/> and process it as a coalesce.
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
            if (tableInformation is null)
            {
                throw new DSyntaxErrorException("未能分析到表名称！");
            }

            string name = tableInformation.Name;
            string schema = tableInformation.Schema;

            if (schema.IsEmpty())
            {
                if (Engine == DatabaseEngine.SqlServer)
                {
                    schema = "dbo";
                }
            }

            Writer.Schema(schema);

            if (tableInformation.DataSharding ^ dataSharding)
            {
                if (tableInformation.DataSharding)
                {
                    throw new InvalidOperationException($"分区表“{tableInformation.Name}”的操作，必须指定分区键！");
                }
                else
                {
                    throw new InvalidOperationException($"普通表“{tableInformation.Name}”不支持分区操作！");
                }
            }

            if (dataSharding)
            {
                name = tableInformation.Fragment(shardingKey);
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
                return tableInformation;
            }

            return tableInformation
                ?? _visitor?.Table(onlyMyself)
                ?? throw new DSyntaxErrorException("未能分析到表名称！");
            ;
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
            if (!disposedValue)
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
                disposedValue = true;
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
