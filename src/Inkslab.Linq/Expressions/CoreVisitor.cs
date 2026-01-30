using Inkslab.Linq.Enums;
using Inkslab.Linq.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 核心访问器（负责节点分析，方法声明适配）。
    /// </summary>
    [DebuggerDisplay("Core")]
    public abstract class CoreVisitor : BaseVisitor
    {
        private readonly BaseVisitor _visitor;

        /// <inheritdoc/>
        protected CoreVisitor(DbStrictAdapter adapter) : base(adapter)
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="visitor"><inheritdoc/></param>
        /// <param name="isNewWriter"><inheritdoc/></param>
        protected CoreVisitor(BaseVisitor visitor, bool isNewWriter = false) : base(visitor, isNewWriter)
        {
            _visitor = visitor;
        }

        #region Call
        /// <inheritdoc/>
        protected override void MethodCall(MethodCallExpression node)
        {
            var declaringType = node.Method.DeclaringType;

            if (declaringType == Types.Queryable || declaringType == Types.QueryableExtentions)
            {
                LinqCall(node);
            }
            else if (declaringType == Types.Enumerable)
            {
                ByEnumerable(node);
            }
            else if (declaringType.IsGenericType)
            {
                string name = node.Method.Name;

                var typeDefinition = declaringType.GetGenericTypeDefinition();

                if (typeDefinition == typeof(List<>))
                {
                    ByList(node);
                }
                else if (name == nameof(ICollection<int>.Contains)
                    && (typeDefinition == typeof(HashSet<>) || typeDefinition.IsLike(typeof(ICollection<>), TypeLikeKind.IsGenericTypeDefinition)))
                {
                    ByContains(node);
                }
                else if (typeDefinition.IsLike(Types.IQueryableGeneric, TypeLikeKind.IsGenericTypeDefinition)) //? 自定义函数。
                {
                    LinqCall(node);
                }
                else
                {
                    throw new NotSupportedException(NotSupportedErrorMsg(node));
                }
            }
            else if (declaringType == Types.String)
            {
                ByString(node);
            }
            else if (declaringType == Types.JsonbPayload || declaringType == Types.JsonPayload)
            {
                Visit(node.Object);
            }
            else if (declaringType == Types.DateTime)
            {
                ByDateTime(node);
            }
            else if (declaringType == Types.Conditions || declaringType == Types.Ranks) //? 自定义函数。
            {
                LinqCustomCall(node);
            }
            else if (declaringType.IsGenericType && declaringType.IsLike(typeof(IQueryable<>), TypeLikeKind.IsGenericTypeDefinition)) //? 自定义函数。
            {
                LinqCall(node);
            }
            else if (declaringType.IsGenericType && declaringType.IsLike(typeof(IQueryable<>), TypeLikeKind.IsGenericTypeDefinition)) //? 自定义函数。
            {
                LinqCustomCall(node);
            }
            else if (declaringType.IsAbstract && declaringType.IsSealed && node.Method.ReturnType.IsQueryable()) //? 静态扩展。
            {
                var queryable = node.GetValueFromExpression<IQueryable>();

                Visit(queryable?.Expression);
            }
            else
            {
                throw new NotSupportedException(NotSupportedErrorMsg(node));
            }
        }

        private static string NotSupportedErrorMsg(MethodCallExpression node)
        {
            if (node.Method.IsStatic)
            {
                return $"不支持将静态方法（{node.Method.Name}）结果直接作为表达式的一部分！请参考 {node.Method.ReturnType.Name} {{variable}} = *[{node.Method.DeclaringType.Name}.{node.Method.Name}(...args)]; 然后使用 {{variable}} 替换表达式 *.{node.Method.Name}(...args)！";
            }

            return $"不支持将方法（{node.Method.Name}）结果直接作为表达式的一部分！请参考 {node.Method.ReturnType.Name} {{variable}} = *[{node.Method.Name}(...args)]; 然后使用 {{variable}} 替换表达式 *.{node.Method.Name}(...args)！";
        }

        /// <summary>
        /// Linq 方法。<see cref="Conditions"/>。
        /// </summary>
        /// <param name="node">节点。</param>
        protected virtual void LinqCustomCall(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case nameof(Conditions.IsTrue):
                    VisitWithConditionVisitor(node.Arguments[0], node.Arguments[1]);
                    break;

                case nameof(Conditions.If) when node.Arguments.Count == 2 && IsPlainVariable(node.Arguments[0], true):
                    if (node.Arguments[0].GetValueFromExpression<bool>())
                    {
                        Condition(node.Arguments[1]);
                    }
                    break;

                case nameof(Conditions.Not) when node.Arguments.Count == 2 && IsPlainVariable(node.Arguments[0], true):
                    if (!node.Arguments[0].GetValueFromExpression<bool>())
                    {
                        Condition(node.Arguments[1]);
                    }
                    break;

                case nameof(Conditions.If) when node.Arguments.Count == 2:
                    VisitIfDynamic(node.Arguments[0], node.Arguments[1], isIfBranch: true);
                    break;

                case nameof(Conditions.Not) when node.Arguments.Count == 2:
                    VisitIfDynamic(node.Arguments[0], node.Arguments[1], isIfBranch: false);
                    break;

                case nameof(Conditions.If) when node.Arguments.Count == 3 && IsPlainVariable(node.Arguments[1], true):
                    if (node.Arguments[1].GetValueFromExpression<bool>())
                    {
                        VisitWithConditionVisitor(node.Arguments[0], node.Arguments[2]);
                    }
                    break;

                case nameof(Conditions.Not) when node.Arguments.Count == 3 && IsPlainVariable(node.Arguments[1], true):
                    if (!node.Arguments[1].GetValueFromExpression<bool>())
                    {
                        VisitWithConditionVisitor(node.Arguments[0], node.Arguments[2]);
                    }
                    break;

                case nameof(Conditions.If) when node.Arguments.Count == 3:
                    VisitIfDynamicWithSource(node.Arguments[0], node.Arguments[1], node.Arguments[2], isIfBranch: true);
                    break;

                case nameof(Conditions.Not) when node.Arguments.Count == 3:
                    VisitIfDynamicWithSource(node.Arguments[0], node.Arguments[1], node.Arguments[2], isIfBranch: false);
                    break;

                case nameof(Conditions.Conditional) when node.Arguments.Count == 3 && IsPlainVariable(node.Arguments[0], true):
                    Condition(node.Arguments[node.Arguments[0].GetValueFromExpression<bool>() ? 1 : 2]);
                    break;

                case nameof(Conditions.Conditional) when node.Arguments.Count == 3:
                    VisitConditionalDynamic(node.Arguments[0], node.Arguments[1], node.Arguments[2]);
                    break;

                case nameof(Conditions.Conditional) when node.Arguments.Count == 4 && IsPlainVariable(node.Arguments[1], true):
                    VisitWithConditionVisitor(node.Arguments[0], node.Arguments[node.Arguments[1].GetValueFromExpression<bool>() ? 2 : 3]);
                    break;

                case nameof(Conditions.Conditional) when node.Arguments.Count == 4:
                    VisitConditionalDynamicWithSource(node.Arguments[0], node.Arguments[1], node.Arguments[2], node.Arguments[3]);
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        #region LinqCustomCall 辅助方法

        /// <summary>
        /// 使用条件访问器处理表达式。
        /// </summary>
        private void VisitWithConditionVisitor(Expression sourceArg, Expression predicateArg)
        {
            var expressions = new List<Expression>();
            var argVisitor = new ArgExpressionVisitor(expressions);
            argVisitor.Visit(sourceArg);

            var conditionVisitor = new ConditionExpressionVisitor(expressions, this);
            conditionVisitor.Visit(predicateArg);
        }

        /// <summary>
        /// 处理2参数版本的 If/Not 动态条件分支。
        /// </summary>
        private void VisitIfDynamic(Expression testArg, Expression branchArg, bool isIfBranch)
        {
            using (var domain = Writer.Domain())
            {
                Condition(testArg);

                // If: domain.HasValue 时处理; Not: domain.IsEmpty 时处理
                if (isIfBranch ? domain.HasValue : domain.IsEmpty)
                {
                    if (isIfBranch)
                    {
                        Writer.Keyword(SqlKeyword.THEN);
                        WriteBranchExpression(branchArg);
                        Writer.Keyword(SqlKeyword.ELSE);
                        Writer.True();
                        WriteCaseWhenFooter(domain);
                    }
                    else
                    {
                        Condition(branchArg);
                    }
                }
                else if (!isIfBranch && domain.HasValue)
                {
                    Writer.Keyword(SqlKeyword.THEN);
                    Writer.True();
                    Writer.Keyword(SqlKeyword.ELSE);
                    WriteBranchExpression(branchArg);
                    WriteCaseWhenFooter(domain);
                }
            }
        }

        /// <summary>
        /// 处理3参数版本的 If/Not 动态条件分支（带 source）。
        /// </summary>
        private void VisitIfDynamicWithSource(Expression sourceArg, Expression testArg, Expression branchArg, bool isIfBranch)
        {
            var expressions = new List<Expression>();
            var argVisitor = new ArgExpressionVisitor(expressions);
            argVisitor.Visit(sourceArg);

            var conditionVisitor = new ConditionExpressionVisitor(expressions, this);

            using (var domain = Writer.Domain())
            {
                Condition(testArg);

                if (isIfBranch ? domain.HasValue : domain.IsEmpty)
                {
                    if (isIfBranch)
                    {
                        Writer.Keyword(SqlKeyword.THEN);
                        WriteBranchExpression(branchArg, conditionVisitor);
                        Writer.Keyword(SqlKeyword.ELSE);
                        Writer.True();
                        WriteCaseWhenFooter(domain);
                    }
                    else
                    {
                        conditionVisitor.Visit(branchArg);
                    }
                }
                else if (!isIfBranch && domain.HasValue)
                {
                    Writer.Keyword(SqlKeyword.THEN);
                    Writer.True();
                    Writer.Keyword(SqlKeyword.ELSE);
                    WriteBranchExpression(branchArg, conditionVisitor);
                    WriteCaseWhenFooter(domain);
                }
            }
        }

        /// <summary>
        /// 处理3参数版本的 Conditional 动态条件。
        /// </summary>
        private void VisitConditionalDynamic(Expression testArg, Expression ifTrueArg, Expression ifFalseArg)
        {
            using (var domain = Writer.Domain())
            {
                Condition(testArg);

                if (domain.IsEmpty)
                {
                    Condition(ifFalseArg);
                }
                else
                {
                    Writer.Keyword(SqlKeyword.THEN);
                    WriteBranchExpression(ifTrueArg);
                    Writer.Keyword(SqlKeyword.ELSE);
                    WriteBranchExpression(ifFalseArg);
                    WriteCaseWhenFooter(domain);
                }
            }
        }

        /// <summary>
        /// 处理4参数版本的 Conditional 动态条件（带 source）。
        /// </summary>
        private void VisitConditionalDynamicWithSource(Expression sourceArg, Expression testArg, Expression ifTrueArg, Expression ifFalseArg)
        {
            var expressions = new List<Expression>();
            var argVisitor = new ArgExpressionVisitor(expressions);
            argVisitor.Visit(sourceArg);

            var conditionVisitor = new ConditionExpressionVisitor(expressions, this);

            using (var domain = Writer.Domain())
            {
                Condition(testArg);

                if (domain.IsEmpty)
                {
                    conditionVisitor.Visit(ifFalseArg);
                }
                else
                {
                    Writer.Keyword(SqlKeyword.THEN);
                    WriteBranchExpression(ifTrueArg, conditionVisitor);
                    Writer.Keyword(SqlKeyword.ELSE);
                    WriteBranchExpression(ifFalseArg, conditionVisitor);
                    WriteCaseWhenFooter(domain);
                }
            }
        }

        /// <summary>
        /// 写入分支表达式，处理空值和条件转义。
        /// </summary>
        private void WriteBranchExpression(Expression branchArg, ConditionExpressionVisitor conditionVisitor = null)
        {
            using (var domainSub = Writer.Domain())
            {
                if (conditionVisitor != null)
                {
                    conditionVisitor.Visit(branchArg);
                }
                else
                {
                    Condition(branchArg);
                }

                if (domainSub.IsEmpty)
                {
                    Writer.Keyword(SqlKeyword.NULL);
                }
                else if (RequiresConditionalEscape() && IsCondition(branchArg))
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

        /// <summary>
        /// 写入 CASE WHEN 结构的尾部并回溯写入头部。
        /// </summary>
        private void WriteCaseWhenFooter(ISqlDomain domain)
        {
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

        #endregion

        /// <summary>
        /// Linq 方法。<see cref="Queryable"/> 或 <seealso cref="QueryableExtentions"/>。
        /// </summary>
        /// <param name="node">节点。</param>
        protected virtual void LinqCall(MethodCallExpression node)
        {
            using (var visitor = new SelectVisitor(this))
            {
                visitor.Startup(node);
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="MethodCallExpression"/>, when <see cref="MethodCallExpression.Method"/>.DeclaringType is <see cref="Enumerable"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void ByEnumerable(MethodCallExpression node)
        {
            string name = node.Method.Name;

            var body = node.Arguments[0];

            switch (name)
            {
                case nameof(Enumerable.Any) when body.NodeType is ExpressionType.Constant or ExpressionType.MemberAccess:
                case nameof(Enumerable.All) when body.NodeType is ExpressionType.Constant or ExpressionType.MemberAccess:
                case "Exists" when body.NodeType is ExpressionType.Constant or ExpressionType.MemberAccess:
                case "TrueForAll" when body.NodeType is ExpressionType.Constant or ExpressionType.MemberAccess:
                    using (var visitor = new LinqAnyAllVisitor(_visitor ?? this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case nameof(Enumerable.Contains) when body.NodeType is ExpressionType.Constant or ExpressionType.MemberAccess:

                    ByContains(node);

                    break;
                default:
                    string variable = "{variable}";

                    var sb = new StringBuilder(50);

                    sb.Append("不支持将内存计算的方法（")
                        .Append(name)
                        .Append("）结果直接作为表达式的一部分！请参考 ")
                        .Append(node.Method.ReturnType.Name)
                        .Append(' ')
                        .Append(variable)
                        .Append(" = *[.")
                        .Append(name)
                        .Append('(');

                    if (node.Arguments.Count > 1)
                    {
                        sb.Append("..args");
                    }

                    sb.Append(')')
                        .Append("]; 然后使用 ")
                        .Append(variable)
                        .Append(" 替换表达式 *.")
                        .Append(name)
                        .Append('(');

                    if (node.Arguments.Count > 1)
                    {
                        sb.Append("..args");
                    }

                    sb.Append(')')
                        .Append('！');

                    throw new DSyntaxErrorException(sb.ToString());
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="MethodCallExpression"/>, when <see cref="MethodCallExpression.Method"/>.DeclaringType is <see cref="List{T}"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void ByList(MethodCallExpression node)
        {
            string name = node.Method.Name;

            switch (name)
            {
                case nameof(List<int>.Exists):
                case nameof(List<int>.TrueForAll):
                    using (var visitor = new LinqAnyAllVisitor(_visitor ?? this))
                    {
                        visitor.Startup(node);
                    }
                    break;
                case nameof(Enumerable.Contains):

                    ByContains(node);

                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="MethodCallExpression"/>, when <see cref="MethodCallExpression.Method"/>.DeclaringType is <see cref="string"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void ByString(MethodCallExpression node)
        {
            using (var visitor = new ByStringCallVisitor(this))
            {
                visitor.Startup(node);
            }
        }

        /// <summary>
        /// Visits the children of the <see cref="MethodCallExpression"/>, when <see cref="MethodCallExpression.Method"/>.DeclaringType is <see cref="DateTime"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        protected virtual void ByDateTime(MethodCallExpression node)
        {
            switch (Engine)
            {
                case DatabaseEngine.MySQL:
                    Writer.Write("DATE_ADD");
                    Writer.OpenBrace();

                    Visit(node.Object);

                    Writer.Delimiter();

                    Writer.Write("INTERVAL ");

                    Visit(node.Arguments[0]);

                    Writer.Write(" ");

                    switch (node.Method.Name)
                    {
                        case nameof(DateTime.AddMilliseconds):
                            Writer.Write("MICROSECOND");
                            break;
                        case nameof(DateTime.AddSeconds):
                            Writer.Write("SECOND");
                            break;
                        case nameof(DateTime.AddMinutes):
                            Writer.Write("MINUTE");
                            break;
                        case nameof(DateTime.AddHours):
                            Writer.Write("HOUR");
                            break;
                        case nameof(DateTime.AddDays):
                            Writer.Write("DAY");
                            break;
                        case nameof(DateTime.AddMonths):
                            Writer.Write("HOUR");
                            break;
                        case nameof(DateTime.AddYears):
                            Writer.Write("YEAR");
                            break;
                        default:
                            throw new NotSupportedException($"日期时间的“{node.Method.Name}”方法不被支持！");
                    }

                    Writer.CloseBrace();

                    break;
                case DatabaseEngine.SqlServer:
                    Writer.Write("DATEADD");
                    Writer.OpenBrace();
                    switch (node.Method.Name)
                    {
                        case nameof(DateTime.AddMilliseconds):
                            Writer.Write("ms");
                            break;
                        case nameof(DateTime.AddSeconds):
                            Writer.Write("ss");
                            break;
                        case nameof(DateTime.AddMinutes):
                            Writer.Write("mi");
                            break;
                        case nameof(DateTime.AddHours):
                            Writer.Write("hh");
                            break;
                        case nameof(DateTime.AddDays):
                            Writer.Write("dd");
                            break;
                        case nameof(DateTime.AddMonths):
                            Writer.Write("mm");
                            break;
                        case nameof(DateTime.AddYears):
                            Writer.Write("yy");
                            break;
                        default:
                            throw new NotSupportedException($"日期时间的“{node.Method.Name}”方法不被支持！");
                    }
                    Writer.Delimiter();
                    Visit(node.Arguments[0]);
                    Writer.Delimiter();
                    Visit(node.Object);
                    Writer.CloseBrace();
                    break;
                case DatabaseEngine.PostgreSQL:

                    Visit(node.Object);

                    Writer.Operator(SqlOperator.Add);

                    Writer.Write("INTERVAL ");

                    switch (node.Method.Name)
                    {
                        case nameof(DateTime.AddMilliseconds):
                            Writer.Write("'1 millisecond'");
                            break;
                        case nameof(DateTime.AddSeconds):
                            Writer.Write("'1 second'");
                            break;
                        case nameof(DateTime.AddMinutes):
                            Writer.Write("'1 minute'");
                            break;
                        case nameof(DateTime.AddHours):
                            Writer.Write("'1 hour'");
                            break;
                        case nameof(DateTime.AddDays):
                            Writer.Write("'1 day'");
                            break;
                        case nameof(DateTime.AddMonths):
                            Writer.Write("'1 month'");
                            break;
                        case nameof(DateTime.AddYears):
                            Writer.Write("'1 year'");
                            break;
                        default:
                            throw new NotSupportedException($"日期时间的“{node.Method.Name}”方法不被支持！");
                    }

                    Writer.Operator(SqlOperator.Multiply);

                    Visit(node.Arguments[0]);

                    break;
                default:
                    throw new NotSupportedException($"日期时间的“{node.Method.Name}”方法不被支持！");
            }
        }

        private void ByContains(MethodCallExpression node)
        {
            var @object = node.Method.IsStatic
                ? node.Arguments[0]
                : node.Object;

            IEnumerable valueSet = @object.GetValueFromExpression<IEnumerable>() ?? Enumerable.Empty<object>();

            var enumerator = valueSet.GetEnumerator();

            if (enumerator.MoveNext())
            {
                Visit(node.Arguments[^1]);

                int parameterCount = 1;
                var maxParamterCount = Engine switch
                {
                    DatabaseEngine.Oracle => 256,
                    DatabaseEngine.SQLite => 1000,
                    DatabaseEngine.SqlServer => 10000,
                    DatabaseEngine.MySQL or DatabaseEngine.PostgreSQL => 20000,
                    _ => 128,
                };
                Writer.Keyword(SqlKeyword.IN);

                Writer.OpenBrace();

                Writer.Constant(enumerator.Current);

                while (enumerator.MoveNext())
                {
                    if (parameterCount < maxParamterCount)
                    {
                        parameterCount++;

                        Writer.Delimiter();
                    }
                    else
                    {
                        parameterCount = 1;

                        Writer.CloseBrace();
                        Writer.WhiteSpace();

                        Writer.Keyword(SqlKeyword.OR);

                        Writer.WhiteSpace();

                        Visit(node.Arguments[^1]);

                        Writer.Keyword(SqlKeyword.IN);
                        Writer.OpenBrace();
                    }

                    Writer.Constant(enumerator.Current);
                }

                Writer.CloseBrace();
            }
            else
            {
                Writer.AlwaysFalse();
            }
        }

        #endregion

        #region 内嵌类。

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

        private class ConditionExpressionVisitor : ExpressionVisitor
        {
            private readonly List<Expression> _expressions;
            private readonly CoreVisitor _visitor;

            public ConditionExpressionVisitor(List<Expression> expressions, CoreVisitor visitor)
            {
                _expressions = expressions;
                _visitor = visitor;
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                var replaceVisitor = new ReplaceManyExpressionVisitor(node.Parameters, _expressions);

                _visitor.Condition(replaceVisitor.Visit(node.Body));

                return node;
            }
        }

        private class LinqAnyAllVisitor : BaseVisitor
        {
            private object valueCurrent;

            public LinqAnyAllVisitor(BaseVisitor visitor) : base(visitor)
            {
            }

            protected override void Startup(MethodCallExpression node)
            {
                var @object = node.Method.IsStatic
                    ? node.Arguments[0]
                    : node.Object;

                IEnumerable valueSet = @object.GetValueFromExpression<IEnumerable>() ?? Enumerable.Empty<object>();

                var enumerator = valueSet.GetEnumerator();

                if (node.Arguments.Count == 1)
                {
                    if (enumerator.MoveNext())
                    {
                        Writer.AlwaysTrue();
                    }
                    else
                    {
                        Writer.AlwaysFalse();
                    }
                }
                else if (enumerator.MoveNext())
                {
                    Writer.OpenBrace();

                    valueCurrent = enumerator.Current;

                    Visit(node.Arguments[^1]);

                    bool flag = node.Method.Name is (nameof(Enumerable.Any)) or (nameof(List<int>.Exists));

                    while (enumerator.MoveNext())
                    {
                        Writer.Keyword(flag ? SqlKeyword.OR : SqlKeyword.AND);

                        valueCurrent = enumerator.Current;

                        Visit(node.Arguments[^1]);
                    }

                    Writer.CloseBrace();
                }
                else
                {
                    Writer.AlwaysFalse();
                }
            }

            protected override void PreparingParameter(LambdaExpression node)
            {
                //? 不准备参数。
            }

            protected override void Lambda<T>(Expression<T> node)
            {
                var visitor = new ReplaceExpressionVisitor(node.Parameters[0], Expression.Constant(valueCurrent));

                Visit(visitor.Visit(node.Body));
            }
        }

        #endregion
    }
}
