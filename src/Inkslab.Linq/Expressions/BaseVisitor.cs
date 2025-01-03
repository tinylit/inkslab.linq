using System;
using System.Collections.Generic;
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
        /// 忽略可空类型。
        /// </summary>
        private bool ignoreNullable = false;

        private class MyNewExpressionVisitor : ExpressionVisitor
        {
            private readonly BaseVisitor _visitor;
            private readonly Dictionary<MemberInfo, Expression> _keyValues;

            public MyNewExpressionVisitor(
                BaseVisitor visitor,
                Dictionary<MemberInfo, Expression> keyValues
            )
            {
                _visitor = visitor;
                _keyValues = keyValues;
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                _visitor.PreparingParameter(node);

                return base.VisitLambda(node);
            }

            protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
            {
                _keyValues[node.Member] = node.Expression;

                return node;
            }

            protected override Expression VisitMemberInit(MemberInitExpression node)
            {
                foreach (var binding in node.Bindings)
                {
                    VisitMemberBinding(binding);
                }

                return node;
            }

            /// <inheritdoc />
            protected override Expression VisitNew(NewExpression node)
            {
                for (int i = 0; i < node.Members.Count; i++)
                {
                    _keyValues[node.Members[i]] = node.Arguments[i];
                }

                return node;
            }
        }

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
            if (node.NodeType == ExpressionType.Constant)
            {
                bool commaFlag = false;

                var tableInfo = Table();

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

                    Member(string.Empty, field, name);
                }
            }
            else if (hasBaseStartup || node.NodeType != ExpressionType.Call)
            {
                Visit(node);
            }
            else
            {
                hasBaseStartup = true;

                Startup((MethodCallExpression)node);
            }
        }

        /// <summary>
        /// 启动方法。
        /// </summary>
        /// <param name="node">节点。</param>
        public virtual void Startup(MethodCallExpression node)
        {
            if (hasBaseStartup)
            {
                VisitMethodCall(node);
            }
            else
            {
                hasBaseStartup = true;

                Startup((Expression)node);
            }
        }

        /// <summary>
        /// 是普通变量。
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>是否是常规变量。</returns>
        protected virtual bool IsPlainVariable(Expression node) => IsPlainVariable(node, false);

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
                {
                    if (depthVerification)
                    {
                        switch (node)
                        {
                            case MethodCallExpression method
                                when method.Object is null
                                    || IsPlainVariable(method.Object, depthVerification):
                                return method.Arguments.Count == 0
                                    || method.Arguments.All(arg =>
                                        IsPlainVariable(arg, depthVerification)
                                    );
                            case BinaryExpression binary:
                                return IsPlainVariable(binary.Left, depthVerification)
                                    && IsPlainVariable(binary.Right, depthVerification);
                            case LambdaExpression lambda when lambda.Parameters.Count == 0:
                                return IsPlainVariable(lambda.Body, depthVerification);
                            case NewExpression newExpression when newExpression.Members.Count == 0:
                                return newExpression.Arguments.Count == 0
                                    || newExpression.Arguments.All(arg =>
                                        IsPlainVariable(arg, depthVerification)
                                    );
                            case MemberInitExpression memberInit
                                when IsPlainVariable(memberInit.NewExpression, depthVerification):
                                foreach (var binding in memberInit.Bindings)
                                {
                                    if (
                                        binding is MemberAssignment assignment
                                        && IsPlainVariable(assignment.Expression, depthVerification)
                                    )
                                    {
                                        continue;
                                    }

                                    return false;
                                }
                                return true;
                            case ConditionalExpression conditional
                                when IsPlainVariable(conditional.Test, depthVerification):
                                return IsPlainVariable(conditional.IfTrue, depthVerification)
                                    && IsPlainVariable(conditional.IfFalse, depthVerification);
                            default:
                                return false;
                        }
                    }

                    return false;
                }
            }
        }

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
                case ExpressionType.IsTrue:
                case ExpressionType.IsFalse:
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
            ignoreNullable |= false; //? 方法始终不忽略内容。

            var instanceArg = node.Arguments[0];

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
                switch (_adapter.Engine)
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
                Visit(node.Expression);

                if (node.Member.Name == "HasValue")
                {
                    Writer.Keyword(SqlKeyword.IS);
                    Writer.Keyword(SqlKeyword.NOT);
                    Writer.Keyword(SqlKeyword.NULL);
                }
            }
            else if (node.Expression.Type == Types.DateTime)
            {
                switch (node.Member.Name)
                {
                    case nameof(DateTime.Day):
                        Writer.Write("DAY");
                        Writer.OpenBrace();
                        Visit(node.Expression);
                        Writer.CloseBrace();
                        break;
                    case nameof(DateTime.Month):
                        Writer.Write("MOUTH");
                        Writer.OpenBrace();
                        Visit(node.Expression);
                        Writer.CloseBrace();
                        break;
                    case nameof(DateTime.Year):
                        Writer.Write("YEAR");
                        Writer.OpenBrace();
                        Visit(node.Expression);
                        Writer.CloseBrace();
                        break;
                    case nameof(DateTime.Second) when Engine == DatabaseEngine.MySQL:
                    case nameof(DateTime.Minute) when Engine == DatabaseEngine.MySQL:
                    case nameof(DateTime.Hour) when Engine == DatabaseEngine.MySQL:
                    case nameof(DateTime.DayOfWeek) when Engine == DatabaseEngine.MySQL:
                    case nameof(DateTime.DayOfYear) when Engine == DatabaseEngine.MySQL:
                        MySql(node.Member.Name, node.Expression);
                        break;
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
                Writer.Constant(node.GetValueFromExpression());
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
        protected virtual void Member(MemberInfo memberInfo, Expression node) => Visit(node);

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

                    if (ignoreNullable)
                    {
                        if (value is null)
                        {
                            break;
                        }
                    }

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
            using (var domain = Writer.Domain())
            {
                Condition(node.Test);

                if (domain.IsEmpty)
                {
                    if (IsCondition(node.IfFalse))
                    {
                        using (var domainSub = Writer.Domain())
                        {
                            Visit(node.IfFalse);

                            if (domainSub.IsEmpty)
                            {
                                Writer.Keyword(SqlKeyword.NULL);
                            }
                            else
                            {
                                Writer.Keyword(SqlKeyword.THEN);

                                Writer.True();

                                Writer.Keyword(SqlKeyword.ELSE);

                                Writer.False();

                                Writer.Keyword(SqlKeyword.END);

                                domainSub.Flyback();

                                Writer.Keyword(SqlKeyword.CASE);
                                Writer.Keyword(SqlKeyword.WHEN);

                                Writer.CloseBrace();

                                domain.Flyback();

                                Writer.OpenBrace();
                            }
                        }
                    }
                    else
                    {
                        Visit(node.IfFalse);
                    }
                }
                else
                {
                    Writer.Keyword(SqlKeyword.THEN);

                    if (IsCondition(node.IfTrue))
                    {
                        using (var domainSub = Writer.Domain())
                        {
                            Visit(node.IfTrue);

                            if (domainSub.IsEmpty)
                            {
                                Writer.Keyword(SqlKeyword.NULL);
                            }
                            else
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
                    }
                    else
                    {
                        Visit(node.IfTrue);
                    }

                    Writer.Keyword(SqlKeyword.ELSE);

                    if (IsCondition(node.IfFalse))
                    {
                        using (var domainSub = Writer.Domain())
                        {
                            Visit(node.IfFalse);

                            if (domainSub.IsEmpty)
                            {
                                Writer.Keyword(SqlKeyword.NULL);
                            }
                            else
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
                    }
                    else
                    {
                        Visit(node.IfFalse);
                    }

                    Writer.Keyword(SqlKeyword.END);

                    Writer.CloseBrace();

                    domain.Flyback();

                    Writer.OpenBrace();

                    Writer.Keyword(SqlKeyword.CASE);
                    Writer.Keyword(SqlKeyword.WHEN);
                }
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

                    if (ignoreNullable)
                    {
                        if (value is null)
                        {
                            break;
                        }
                    }

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
            if (tableInformation is null)
            {
                if (_visitor is null)
                {
                    tableInfo = null;

                    return false;
                }

                return _visitor.TryGetSourceTableInfo(node, out tableInfo);
            }

            tableInfo = tableInformation;

            return true;
        }

        /// <summary>
        /// Visits the children of the <see cref="UnaryExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Unary(UnaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Quote:
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

                            using (Writer.ConditionReversal())
                            {
                                Visit(node.Operand);
                            }

                            break;
                        default:
                            Writer.Keyword(SqlKeyword.NOT);

                            Writer.OpenBrace();

                            Visit(node.Operand);

                            Writer.CloseBrace();

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
        /// <see cref="ExpressionType.Coalesce"/>.
        /// </summary>
        protected virtual void Coalesce(Expression left, Expression right)
        {
            switch (_adapter.Engine)
            {
                case DatabaseEngine.Oracle:
                case DatabaseEngine.DB2:
                case DatabaseEngine.Sybase:
                case DatabaseEngine.MySQL:
                case DatabaseEngine.SQLite:
                case DatabaseEngine.SqlServer:
                case DatabaseEngine.PostgreSQL:

                    using (var domain = Writer.Domain())
                    {
                        ignoreNullable = true;

                        Visit(left);

                        ignoreNullable = false;

                        if (domain.IsEmpty)
                        {
                            Visit(right);
                        }
                        else
                        {
                            Writer.Delimiter();

                            Visit(right);

                            Writer.CloseBrace();

                            domain.Flyback();

                            switch (_adapter.Engine)
                            {
                                case DatabaseEngine.Oracle:
                                    Writer.Write("NVL");
                                    break;
                                case DatabaseEngine.MySQL:
                                case DatabaseEngine.SQLite:
                                    Writer.Write("IFNULL");
                                    break;
                                case DatabaseEngine.DB2:
                                case DatabaseEngine.Sybase:
                                case DatabaseEngine.SqlServer:
                                    Writer.Write("ISNULL");
                                    break;
                                case DatabaseEngine.PostgreSQL:
                                    Writer.Write("COALESCE");
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }

                            Writer.OpenBrace();
                        }
                    }

                    break;
                case DatabaseEngine.Access:
                default:

                    using (var domain = Writer.Domain())
                    {
                        ignoreNullable = true;

                        Visit(left);

                        ignoreNullable = false;

                        if (domain.IsEmpty)
                        {
                            Visit(right);
                        }
                        else
                        {
                            Writer.Keyword(SqlKeyword.THEN);

                            Visit(left);

                            Writer.Keyword(SqlKeyword.ELSE);

                            Visit(right);

                            Writer.Keyword(SqlKeyword.END);

                            Writer.CloseBrace();

                            domain.Flyback();

                            Writer.OpenBrace();

                            Writer.Keyword(SqlKeyword.CASE);
                            Writer.Keyword(SqlKeyword.WHEN);
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="BinaryExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Binary(BinaryExpression node) =>
            BinaryCore(node.Left, node.NodeType, node.Right);

        private static bool IsNewEquals(Expression node)
        {
            return node.NodeType switch
            {
                ExpressionType.Lambda when node is LambdaExpression lambda
                    => IsNewEquals(lambda.Body),
                ExpressionType.Quote when node is UnaryExpression unary
                    => IsNewEquals(unary.Operand),
                ExpressionType.New or ExpressionType.MemberInit => true,
                _ => false,
            };
        }

        /// <summary>
        /// Visits the children of the <see cref="BinaryExpression"/>.(NewExpression support).
        /// </summary>
        /// <param name="left">The expression to visit.</param>
        /// <param name="expressionType">The expression to visit.</param>
        /// <param name="right">The expression to visit.</param>
        protected virtual void Binary(
            Expression left,
            ExpressionType expressionType,
            Expression right
        )
        {
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            if (IsNewEquals(left) && IsNewEquals(right))
            {
                var compareExpressions = new Dictionary<MemberInfo, Expression>();

                var visitor = new MyNewExpressionVisitor(this, compareExpressions);

                visitor.Visit(left);

                if (compareExpressions.Count == 0)
                {
                    BinaryCore(left, expressionType, right);
                }

                var memberExpressions = new Dictionary<MemberInfo, Expression>(compareExpressions);

                visitor.Visit(right);

                bool partitionFlag = false;

                foreach (var (key, node) in memberExpressions)
                {
                    using (var domain = Writer.Domain())
                    {
                        BinaryCore(node, expressionType, compareExpressions[key]);

                        if (domain.IsEmpty)
                        {
                            continue;
                        }

                        if (partitionFlag)
                        {
                            Writer.Keyword(SqlKeyword.AND);
                        }
                        else
                        {
                            partitionFlag = true;
                        }
                    }
                }
            }
            else
            {
                BinaryCore(left, expressionType, right);
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="BinaryExpression"/>.
        /// </summary>
        /// <param name="left">The expression to visit.</param>
        /// <param name="expressionType">The expression to visit.</param>
        /// <param name="right">The expression to visit.</param>
        protected virtual void BinaryCore(
            Expression left,
            ExpressionType expressionType,
            Expression right
        )
        {
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            switch (expressionType)
            {
                case ExpressionType.Coalesce:

                    Coalesce(left, right);

                    break;
                case ExpressionType.Equal
                    when right.NodeType == ExpressionType.Constant
                        && left.NodeType == ExpressionType.MemberAccess
                        && !left.Type.IsCell():
                case ExpressionType.NotEqual
                    when right.NodeType == ExpressionType.Constant
                        && left.NodeType == ExpressionType.MemberAccess
                        && !left.Type.IsCell():
                {
                    var constant = (ConstantExpression)right;

                    if (constant.Value is null)
                    {
                        if (
                            JoinBranchElementIsNull(left, expressionType == ExpressionType.NotEqual)
                        )
                        {
                            break;
                        }

                        throw new DSyntaxErrorException($"不支持参数类型“{left.Type}”与“null”的比较！");
                    }

                    throw new DSyntaxErrorException($"不支持参数类型“{left.Type}”与非“null”常量值的比较！");
                }
                case ExpressionType.Equal
                    when left.NodeType == ExpressionType.Constant
                        && right.NodeType == ExpressionType.MemberAccess
                        && !right.Type.IsCell():
                case ExpressionType.NotEqual
                    when left.NodeType == ExpressionType.Constant
                        && right.NodeType == ExpressionType.MemberAccess
                        && !right.Type.IsCell():
                {
                    var constant = (ConstantExpression)left;

                    if (constant.Value is null)
                    {
                        if (
                            JoinBranchElementIsNull(
                                right,
                                expressionType == ExpressionType.NotEqual
                            )
                        )
                        {
                            break;
                        }

                        throw new DSyntaxErrorException($"不支持参数类型“{right.Type}”与“null”的比较！");
                    }

                    throw new DSyntaxErrorException($"不支持参数类型“{right.Type}”与非“null”常量值的比较！");
                }
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThanOrEqual:

                    var nullableLeft = left.IsNullable();
                    var nullableRight = right.IsNullable();

                    if (nullableLeft == nullableRight)
                    {
                        Visit(left);

                        Writer.Operator(expressionType.GetOperator());

                        Visit(right);

                        break;
                    }

                    //? 忽略数据库非可空类型的字段与 NULL 的比较表达式。
                    using (var domain = Writer.Domain())
                    {
                        ignoreNullable = true;

                        Visit(nullableLeft ? left : right);

                        ignoreNullable = false;

                        if (domain.IsEmpty)
                        {
                            break;
                        }
                    }

                    Writer.Operator(expressionType.GetOperator());

                    Visit(nullableLeft ? left : right);

                    break;
                case ExpressionType.And:
                case ExpressionType.Or:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Modulo:
                case ExpressionType.Divide:
                case ExpressionType.LeftShift:
                case ExpressionType.RightShift:

                    bool isStringConcat =
                        expressionType == ExpressionType.Add
                        && (left.Type == Types.String || right.Type == Types.String);

                    using (var domain = Writer.Domain())
                    {
                        Visit(left);

                        if (domain.IsEmpty)
                        {
                            break;
                        }

                        using (var domainSub = Writer.Domain())
                        {
                            Visit(right);

                            if (domainSub.IsEmpty)
                            {
                                domain.Discard();

                                break;
                            }

                            Writer.CloseBrace();

                            domainSub.Flyback();

                            if (isStringConcat)
                            {
                                Writer.Delimiter();
                            }
                            else
                            {
                                Writer.Operator(expressionType.GetOperator());
                            }
                        }

                        domain.Flyback();

                        if (isStringConcat)
                        {
                            Writer.Write("CONCAT");
                        }

                        Writer.OpenBrace();
                    }

                    break;
                case ExpressionType.Power:

                    using (var domain = Writer.Domain())
                    {
                        Visit(left);

                        if (domain.IsEmpty)
                        {
                            break;
                        }

                        using (var domainSub = Writer.Domain())
                        {
                            Visit(right);

                            if (domainSub.IsEmpty)
                            {
                                domain.Discard();

                                break;
                            }

                            Writer.CloseBrace();

                            domainSub.Flyback();

                            Writer.Delimiter();
                        }

                        domain.Flyback();

                        Writer.Write("POWER");

                        Writer.OpenBrace();
                    }

                    break;
                case ExpressionType.OrElse:
                case ExpressionType.AndAlso:

                    using (var domain = Writer.Domain())
                    {
                        Condition(left);

                        if (domain.IsEmpty)
                        {
                            Condition(right);
                        }
                        else
                        {
                            using (var domainSub = Writer.Domain())
                            {
                                Condition(right);

                                if (domainSub.IsEmpty)
                                {
                                    break;
                                }

                                Writer.CloseBrace();

                                domainSub.Flyback();

                                Writer.Keyword(
                                    expressionType == ExpressionType.AndAlso
                                        ? SqlKeyword.AND
                                        : SqlKeyword.OR
                                );
                            }

                            domain.Flyback();

                            Writer.OpenBrace();
                        }
                    }

                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private bool JoinBranchElementIsNull(Expression left, bool isExpressionNotEqual)
        {
            if (
                !TryGetSourceParameter(left, out var parameterExpression)
                || !TryGetSourceTableInfo(parameterExpression, out var tableInfo)
            )
            {
                return false;
            }

            bool commaFlag = false;

            if (tableInfo.Keys.Count > 0)
            {
                foreach (var key in tableInfo.Keys)
                {
                    if (!tableInfo.Fields.TryGetValue(key, out var field))
                    {
                        continue;
                    }

                    if (commaFlag)
                    {
                        if (isExpressionNotEqual)
                        {
                            Writer.Keyword(Enums.SqlKeyword.OR);
                        }
                        else
                        {
                            Writer.Keyword(Enums.SqlKeyword.AND);
                        }
                    }
                    else
                    {
                        commaFlag = true;
                    }

                    Writer.Schema(parameterExpression.Name);
                    Writer.Name(field);

                    Writer.Keyword(Enums.SqlKeyword.IS);

                    if (isExpressionNotEqual)
                    {
                        Writer.Keyword(Enums.SqlKeyword.NOT);
                    }

                    Writer.Keyword(Enums.SqlKeyword.NULL);
                }
            }
            else
            {
                foreach (var field in tableInfo.Fields.Values)
                {
                    if (commaFlag)
                    {
                        if (isExpressionNotEqual)
                        {
                            Writer.Keyword(Enums.SqlKeyword.OR);
                        }
                        else
                        {
                            Writer.Keyword(Enums.SqlKeyword.AND);
                        }
                    }
                    else
                    {
                        commaFlag = true;
                    }

                    Writer.Schema(parameterExpression.Name);
                    Writer.Name(field);

                    Writer.Keyword(Enums.SqlKeyword.IS);

                    if (isExpressionNotEqual)
                    {
                        Writer.Keyword(Enums.SqlKeyword.NOT);
                    }

                    Writer.Keyword(Enums.SqlKeyword.NULL);
                }
            }

            return true;
        }

        /// <summary>
        /// Visits the children of the <see cref="Expression"/> and process it as a condition.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void Condition(Expression node)
        {
            if (IsCondition(node))
            {
                Visit(node);
            }
            else
            {
                using (var domain = Writer.Domain())
                {
                    Visit(node);

                    if (!domain.IsEmpty)
                    {
                        Writer.Operator(SqlOperator.IsTrue);
                    }
                }
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
