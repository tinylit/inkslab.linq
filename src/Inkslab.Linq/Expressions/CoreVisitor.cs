using Inkslab.Linq.Enums;
using Inkslab.Linq.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 核心访问器（负责节点分析，方法声明适配）。
    /// </summary>
    public abstract class CoreVisitor : BaseVisitor
    {
        private readonly BaseVisitor _visitor;

        /// <inheritdoc/>
        protected CoreVisitor(IDbAdapter adapter) : base(adapter)
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
                else if (name == nameof(ICollection<int>.Contains) && typeDefinition == typeof(ICollection<>))
                {
                    ByList(node);
                }
                else if (typeDefinition.IsLike(Types.IQueryableGeneric, TypeLikeKind.IsGenericTypeDefinition)) //? 自定义函数。
                {
                    LinqCall(node);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else if (declaringType == Types.String)
            {
                ByString(node);
            }
            else if (declaringType == Types.L2S)
            {
                switch (node.Method.Name)
                {
                    case nameof(L2S.Condition):
                        // 准备参数。
                        var expressions = new List<Expression>();
                        var argVisitor = new ConditionArgExpressionVisitor(expressions);
                        argVisitor.Visit(node.Arguments[0]);
                        // 分析表达式。
                        var conditionVisitor = new ConditionExpressionVisitor(expressions, this);
                        conditionVisitor.Visit(node.Arguments[1]);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            else if (declaringType.IsAbstract && declaringType.IsSealed && node.Method.ReturnType.IsQueryable()) //? 静态扩展。
            {
                var queryable = node.GetValueFromExpression<IQueryable>();

                Visit(queryable?.Expression);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Linq 方法。<see cref="Queryable"/> 或 <seealso cref="QueryableExtentions"/>。
        /// </summary>
        /// <param name="node"></param>
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
                    using (var visitor = new LinqAnyAllVisitor(this._visitor ?? this))
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
            string name = node.Method.Name;

            switch (name)
            {
                case nameof(string.Contains) when node.Arguments.Count == 1:
                case nameof(string.EndsWith) when node.Arguments.Count == 1:
                case nameof(string.StartsWith) when node.Arguments.Count == 1:

                    var fuzzyPairt = node.Arguments[0];

                    if (IsPlainVariable(fuzzyPairt))
                    {
                        var value = fuzzyPairt.GetValueFromExpression();

                        if (value is null) //? 忽略内容。
                        {
                            break;
                        }

                        if (value is string text)
                        {

                        }
                        else if (value is char c)
                        {
                            text = c.ToString();
                        }
                        else
                        {
                            goto default;
                        }

                        if (name is (nameof(string.Contains)) or (nameof(string.StartsWith)))
                        {
                            text = string.Concat("%", text);
                        }

                        if (name is (nameof(string.Contains)) or (nameof(string.EndsWith)))
                        {
                            text = string.Concat(text, "%");
                        }

                        Visit(node.Object);

                        Writer.Keyword(SqlKeyword.LIKE);

                        Writer.Constant(text);

                        break;
                    }

                    Visit(node.Object);

                    Writer.Keyword(SqlKeyword.LIKE);

                    if (name is (nameof(string.Contains)) or (nameof(string.StartsWith)))
                    {
                        if (Engine is DatabaseEngine.PostgreSQL or DatabaseEngine.Oracle)
                        {
                            Writer.Write("'%' || ");
                        }
                        else
                        {
                            Writer.Write("'%' + ");
                        }
                    }

                    Visit(fuzzyPairt);

                    if (name is (nameof(string.Contains)) or (nameof(string.EndsWith)))
                    {
                        if (Engine is DatabaseEngine.PostgreSQL or DatabaseEngine.Oracle)
                        {
                            Writer.Write(" || '%'");
                        }
                        else
                        {
                            Writer.Write(" + '%'");
                        }
                    }

                    break;
                case nameof(string.IsNullOrEmpty):

                    using (var domain = Writer.Domain())
                    {
                        Visit(node.Arguments[0]);

                        if (domain.IsEmpty)
                        {
                            Writer.AlwaysTrue();

                            break;
                        }

                        string text = domain.ToString();

                        Writer.Keyword(SqlKeyword.IS);
                        Writer.Keyword(SqlKeyword.NULL);

                        Writer.Keyword(SqlKeyword.OR);

                        Writer.Write(text);

                        Writer.Operator(SqlOperator.Equal);

                        Writer.EmptyString();

                        Writer.CloseBrace();

                        domain.Flyback();

                        Writer.OpenBrace();
                    }

                    break;
                case nameof(string.IsNullOrWhiteSpace):

                    using (var domain = Writer.Domain())
                    {
                        Visit(node.Arguments[0]);

                        if (domain.IsEmpty)
                        {
                            Writer.AlwaysTrue();

                            break;
                        }

                        string text = domain.ToString();

                        Writer.Keyword(SqlKeyword.IS);
                        Writer.Keyword(SqlKeyword.NULL);

                        Writer.Keyword(SqlKeyword.OR);

                        if (Engine == DatabaseEngine.PostgreSQL)
                        {
                            Writer.Write("TRIM");
                            Writer.OpenBrace();
                            Writer.Write("BOTH FROM ");

                            Writer.Write(text);
                        }
                        else
                        {
                            Writer.Write("LTRIM");
                            Writer.OpenBrace();
                            Writer.Write("RTRIM");
                            Writer.OpenBrace();

                            Writer.Write(text);

                            Writer.CloseBrace();
                        }

                        Writer.CloseBrace();

                        Writer.Operator(SqlOperator.Equal);

                        Writer.EmptyString();

                        Writer.CloseBrace();

                        domain.Flyback();

                        Writer.OpenBrace();
                    }

                    break;
                case nameof(string.Replace) when node.Arguments.Count == 2:

                    Writer.Write("REPLACE");

                    Writer.OpenBrace();

                    Visit(node.Object);

                    foreach (Expression item in node.Arguments)
                    {
                        Writer.Delimiter();

                        Visit(item);
                    }

                    Writer.CloseBrace();

                    break;
                case nameof(string.Substring):

                    using (var domain = Writer.Domain())
                    {
                        Visit(node.Object);

                        if (domain.IsEmpty)
                        {
                            Writer.Keyword(SqlKeyword.NULL);

                            break;
                        }

                        string text = domain.ToString();

                        Writer.Keyword(SqlKeyword.IS);
                        Writer.Keyword(SqlKeyword.NULL);
                        Writer.Keyword(SqlKeyword.THEN);
                        Writer.Keyword(SqlKeyword.NULL);
                        Writer.Keyword(SqlKeyword.WHEN);

                        switch (Engine)
                        {
                            case DatabaseEngine.Sybase:
                            case DatabaseEngine.SqlServer:
                            case DatabaseEngine.PostgreSQL:
                                Writer.Write("SUBSTRING");
                                break;
                            case DatabaseEngine.Access:
                                Writer.Write("MID");
                                break;
                            case DatabaseEngine.SQLite:
                            case DatabaseEngine.DB2:
                            case DatabaseEngine.Oracle:
                            case DatabaseEngine.MySQL:
                            default:
                                Writer.Write("SUBSTR");
                                break;
                        }

                        Writer.OpenBrace();

                        var startPairt = node.Arguments[0];

                        if (Engine == DatabaseEngine.PostgreSQL)
                        {
                            Writer.Write("FROM ");
                        }
                        else
                        {
                            Writer.Delimiter();
                        }

                        if (IsPlainVariable(startPairt))
                        {
                            Writer.Constant(startPairt.GetValueFromExpression<int>() + 1);
                        }
                        else
                        {
                            using (var domainSub = Writer.Domain())
                            {
                                Visit(startPairt);

                                if (domainSub.IsEmpty)
                                {
                                    Writer.Write('1');
                                }
                            }
                        }

                        if (node.Arguments.Count > 1)
                        {
                            using (var domainSub = Writer.Domain())
                            {
                                Visit(node.Arguments[1]);

                                if (!domainSub.IsEmpty)
                                {
                                    domainSub.Flyback();

                                    if (Engine == DatabaseEngine.PostgreSQL)
                                    {
                                        Writer.Write(" FOR ");
                                    }
                                    else
                                    {
                                        Writer.Delimiter();
                                    }
                                }
                            }
                        }
                    }

                    Writer.CloseBrace();

                    Writer.Keyword(SqlKeyword.END);

                    break;
                case nameof(string.ToUpper) when node.Arguments.Count == 0:

                    Writer.Write("UPPER");
                    Writer.OpenBrace();

                    Visit(node.Object);

                    Writer.CloseBrace();

                    break;
                case nameof(string.ToLower) when node.Arguments.Count == 0:

                    Writer.Write("LOWER");
                    Writer.OpenBrace();

                    Visit(node.Object);

                    Writer.CloseBrace();

                    break;
                case nameof(string.TrimStart) when node.Arguments.Count == 0:

                    if (Engine == DatabaseEngine.PostgreSQL)
                    {
                        Writer.Write("TRIM");
                        Writer.OpenBrace();
                        Writer.Write("LEADING FROM ");
                    }
                    else
                    {
                        Writer.Write("LTRIM");
                        Writer.OpenBrace();
                    }

                    Visit(node.Object);

                    Writer.CloseBrace();

                    break;
                case nameof(string.TrimEnd) when node.Arguments.Count == 0:

                    if (Engine == DatabaseEngine.PostgreSQL)
                    {
                        Writer.Write("TRIM");
                        Writer.OpenBrace();
                        Writer.Write("TRAILING FROM ");
                    }
                    else
                    {
                        Writer.Write("RTRIM");
                        Writer.OpenBrace();
                    }

                    Visit(node.Object);

                    Writer.CloseBrace();

                    break;
                case nameof(string.Trim) when node.Arguments.Count == 0:

                    if (Engine == DatabaseEngine.PostgreSQL)
                    {
                        Writer.Write("TRIM");
                        Writer.OpenBrace();
                        Writer.Write("BOTH FROM ");
                    }
                    else
                    {
                        Writer.Write("LTRIM");
                        Writer.OpenBrace();

                        Writer.Write("RTRIM");
                        Writer.OpenBrace();
                    }


                    Visit(node.Object);

                    Writer.CloseBrace();

                    if (Engine == DatabaseEngine.PostgreSQL)
                    {
                        break;
                    }

                    Writer.CloseBrace();

                    break;
                case nameof(string.IndexOf) when node.Arguments.All(x => x.Type == Types.String || x.Type == Types.Int32):

                    switch (Engine)
                    {
                        case DatabaseEngine.Oracle when node.Arguments.Count <= 2:
                            Writer.Write("INSTR");

                            Writer.OpenBrace();

                            Visit(node.Object);

                            Writer.Delimiter();

                            Visit(node.Arguments[0]);

                            if (node.Arguments.Count > 1)
                            {
                                Writer.Delimiter();

                                Visit(node.Arguments[1]);
                            }

                            Writer.CloseBrace();
                            break;
                        case DatabaseEngine.MySQL when node.Arguments.Count <= 2:

                            Writer.Write("LOCATE");

                            Writer.OpenBrace();

                            Visit(node.Arguments[0]);

                            Writer.Delimiter();

                            Visit(node.Object);

                            if (node.Arguments.Count > 1)
                            {
                                Writer.Delimiter();

                                Visit(node.Arguments[1]);
                            }

                            Writer.CloseBrace();

                            break;
                        case DatabaseEngine.PostgreSQL when node.Arguments.Count == 1:

                            Writer.Write("position");

                            Writer.OpenBrace();

                            Visit(node.Arguments[0]);

                            Writer.Write(" IN ");

                            Visit(node.Object);

                            Writer.CloseBrace();

                            break;
                        case DatabaseEngine.DB2 when node.Arguments.Count <= 2:
                        case DatabaseEngine.Sybase when node.Arguments.Count == 1:
                        case DatabaseEngine.SqlServer when node.Arguments.Count <= 2:
                            Writer.Write("CHARINDEX");

                            Writer.OpenBrace();

                            Visit(node.Arguments[0]);

                            Writer.Delimiter();

                            Visit(node.Object);

                            if (node.Arguments.Count > 1)
                            {
                                Writer.Delimiter();

                                Visit(node.Arguments[1]);
                            }

                            Writer.CloseBrace();
                            break;
                        case DatabaseEngine.Access when node.Arguments.Count <= 2:

                            Writer.Write("InStr");

                            Writer.OpenBrace();

                            if (node.Arguments.Count > 1)
                            {
                                Visit(node.Arguments[1]);

                                Writer.Delimiter();
                            }

                            Visit(node.Object);

                            Writer.Delimiter();

                            Visit(node.Arguments[0]);

                            Writer.CloseBrace();

                            break;
                        case DatabaseEngine.SQLite when node.Arguments.Count == 1:

                            Writer.Write("InStr");

                            Writer.OpenBrace();

                            Visit(node.Object);

                            Writer.Delimiter();

                            Visit(node.Arguments[0]);

                            Writer.CloseBrace();

                            break;
                        default:
                            throw new NotSupportedException();
                    }

                    break;
                case nameof(string.Concat) when node.Arguments.Count > 1:

                    ByConcat(0, node.Arguments);

                    break;
                default:
                    throw new NotSupportedException($"字符串的“{node.Method}”方法不被支持！");
            }
        }

        private void ByConcat(int i, System.Collections.ObjectModel.ReadOnlyCollection<Expression> arguments)
        {
            if (i < arguments.Count - 1)
            {
                Writer.Write("CONCAT");
                Writer.OpenBrace();
                Visit(arguments[i]);
                Writer.Delimiter();
                ByConcat(i + 1, arguments);
                Writer.CloseBrace();
            }
            else
            {
                Visit(arguments[i]);
            }
        }

        private void ByContains(MethodCallExpression node)
        {
            IEnumerable valueSet = node.Arguments[0].GetValueFromExpression<IEnumerable>() ?? Enumerable.Empty<object>();

            var enumerator = valueSet.GetEnumerator();

            if (enumerator.MoveNext())
            {
                Visit(node.Arguments[1]);

                int parameterCount = 1;
                var maxParamterCount = Engine switch
                {
                    DatabaseEngine.SQLite => 1000,
                    DatabaseEngine.SqlServer => 10000,
                    DatabaseEngine.MySQL => 20000,
                    DatabaseEngine.Oracle => 256,
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

                        Visit(node.Arguments[1]);

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

        private class ReplaceConditionExpressionVisitor : ExpressionVisitor
        {
            private readonly List<ParameterExpression> _oldExpression;
            private readonly List<Expression> _newExpression;

            public ReplaceConditionExpressionVisitor(List<ParameterExpression> oldExpression, List<Expression> newExpression)
            {
                _oldExpression = oldExpression;
                _newExpression = newExpression;
            }
            public override Expression Visit(Expression node)
            {
                if (node is null)
                {
                    return node;
                }

                if (node.NodeType == ExpressionType.Parameter)
                {
                    for (int i = 0; i < _oldExpression.Count; i++)
                    {
                        if (Equals(node, _oldExpression[i]))
                        {
                            return _newExpression[i];
                        }
                    }
                }

                return base.Visit(node);
            }
        }

        private class ConditionArgExpressionVisitor : ExpressionVisitor
        {
            private readonly List<Expression> _expressions;

            public ConditionArgExpressionVisitor(List<Expression> expressions)
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
                var replaceVisitor = new ReplaceConditionExpressionVisitor(new List<ParameterExpression>(node.Parameters), _expressions);

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

            public override void Startup(MethodCallExpression node)
            {
                IEnumerable valueSet = node.Arguments[0].GetValueFromExpression<IEnumerable>() ?? Enumerable.Empty<object>();

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

                    Visit(node);

                    bool flag = node.Method.Name is (nameof(Enumerable.Any)) or (nameof(List<int>.Exists));

                    while (enumerator.MoveNext())
                    {
                        Writer.Keyword(flag ? SqlKeyword.OR : SqlKeyword.AND);

                        valueCurrent = enumerator.Current;

                        Visit(node);
                    }

                    Writer.CloseBrace();
                }
                else
                {
                    Writer.AlwaysFalse();
                }
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
