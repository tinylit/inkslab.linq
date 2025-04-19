﻿using Inkslab.Linq.Enums;
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
                    throw new NotSupportedException();
                }
            }
            else if (declaringType == Types.String)
            {
                ByString(node);
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
                throw new NotSupportedException();
            }
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
                    {
                        // 准备参数。
                        var expressions = new List<Expression>();
                        var argVisitor = new ArgExpressionVisitor(expressions);

                        argVisitor.Visit(node.Arguments[0]);

                        // 分析表达式。
                        var conditionVisitor = new ConditionExpressionVisitor(expressions, this);
                        conditionVisitor.Visit(node.Arguments[1]);

                        break;
                    }
                case nameof(Conditions.If) when node.Arguments.Count == 2 && IsPlainVariable(node.Arguments[0], true):
                    {
                        bool conditionIsValid = node.Arguments[0].GetValueFromExpression<bool>();

                        if (conditionIsValid)
                        {
                            Condition(node.Arguments[1]);
                        }

                        break;
                    }
                case nameof(Conditions.If) when node.Arguments.Count == 2:
                    {
                        using (var domain = Writer.Domain())
                        {
                            Condition(node.Arguments[0]);

                            if (domain.HasValue)
                            {
                                Writer.Keyword(SqlKeyword.THEN);

                                using (var domainSub = Writer.Domain())
                                {
                                    Condition(node.Arguments[1]);

                                    if (domainSub.IsEmpty)
                                    {
                                        Writer.Keyword(SqlKeyword.NULL);
                                    }
                                    else if (RequiresConditionalEscape() && IsCondition(node.Arguments[2]))
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

                        break;
                    }
                case nameof(Conditions.If) when node.Arguments.Count == 3 && IsPlainVariable(node.Arguments[1], true):
                    {
                        bool conditionIsValid = node.Arguments[1].GetValueFromExpression<bool>();

                        if (conditionIsValid)
                        {
                            // 准备参数。
                            var expressions = new List<Expression>();
                            var argVisitor = new ArgExpressionVisitor(expressions);

                            argVisitor.Visit(node.Arguments[0]);

                            // 分析表达式。
                            var conditionVisitor = new ConditionExpressionVisitor(expressions, this);
                            conditionVisitor.Visit(node.Arguments[2]);
                        }

                        break;
                    }
                case nameof(Conditions.If) when node.Arguments.Count == 3:
                    {                            // 准备参数。
                        var expressions = new List<Expression>();
                        var argVisitor = new ArgExpressionVisitor(expressions);

                        argVisitor.Visit(node.Arguments[0]);

                        var conditionVisitor = new ConditionExpressionVisitor(expressions, this);

                        using (var domain = Writer.Domain())
                        {
                            Condition(node.Arguments[1]);

                            if (domain.HasValue)
                            {
                                Writer.Keyword(SqlKeyword.THEN);

                                using (var domainSub = Writer.Domain())
                                {
                                    conditionVisitor.Visit(node.Arguments[2]);

                                    if (domainSub.IsEmpty)
                                    {
                                        Writer.Keyword(SqlKeyword.NULL);
                                    }
                                    else if (RequiresConditionalEscape() && IsCondition(node.Arguments[2]))
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
                        break;
                    }
                case nameof(Conditions.Conditional) when node.Arguments.Count == 3 && IsPlainVariable(node.Arguments[0], true):
                    {
                        var conditionIsValid = node.Arguments[0].GetValueFromExpression<bool>();

                        Condition(node.Arguments[conditionIsValid ? 1 : 2]);

                        break;
                    }
                case nameof(Conditions.Conditional) when node.Arguments.Count == 3:
                    {
                        using (var domain = Writer.Domain())
                        {
                            Condition(node.Arguments[0]);

                            if (domain.IsEmpty) //? 测试条件不满足是视为假。
                            {
                                Condition(node.Arguments[2]);
                            }
                            else
                            {
                                Writer.Keyword(SqlKeyword.THEN);

                                using (var domainSub = Writer.Domain())
                                {
                                    Condition(node.Arguments[1]);

                                    if (domainSub.IsEmpty)
                                    {
                                        Writer.Keyword(SqlKeyword.NULL);
                                    }
                                    else if (RequiresConditionalEscape() && IsCondition(node.Arguments[2]))
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

                                using (var domainSub = Writer.Domain())
                                {
                                    Condition(node.Arguments[2]);

                                    if (domainSub.IsEmpty)
                                    {
                                        Writer.Keyword(SqlKeyword.NULL);
                                    }
                                    else if (RequiresConditionalEscape() && IsCondition(node.Arguments[3]))
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

                        break;
                    }
                case nameof(Conditions.Conditional) when node.Arguments.Count == 4 && IsPlainVariable(node.Arguments[1], true):
                    {
                        var conditionIsValid = node.Arguments[1].GetValueFromExpression<bool>();

                        // 准备参数。
                        var expressions = new List<Expression>();
                        var argVisitor = new ArgExpressionVisitor(expressions);

                        argVisitor.Visit(node.Arguments[0]);

                        // 分析表达式。
                        var conditionVisitor = new ConditionExpressionVisitor(expressions, this);
                        conditionVisitor.Visit(node.Arguments[conditionIsValid ? 2 : 3]);
                        break;
                    }
                case nameof(Conditions.Conditional) when node.Arguments.Count == 4:
                    {
                        // 准备参数。
                        var expressions = new List<Expression>();
                        var argVisitor = new ArgExpressionVisitor(expressions);

                        argVisitor.Visit(node.Arguments[0]);

                        var conditionVisitor = new ConditionExpressionVisitor(expressions, this);

                        using (var domain = Writer.Domain())
                        {
                            Condition(node.Arguments[1]);

                            if (domain.IsEmpty) //? 测试条件不满足是视为假。
                            {
                                conditionVisitor.Visit(node.Arguments[3]);
                            }
                            else
                            {
                                Writer.Keyword(SqlKeyword.THEN);

                                using (var domainSub = Writer.Domain())
                                {
                                    conditionVisitor.Visit(node.Arguments[2]);

                                    if (domainSub.IsEmpty)
                                    {
                                        Writer.Keyword(SqlKeyword.NULL);
                                    }
                                    else if (RequiresConditionalEscape() && IsCondition(node.Arguments[2]))
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

                                using (var domainSub = Writer.Domain())
                                {
                                    conditionVisitor.Visit(node.Arguments[3]);

                                    if (domainSub.IsEmpty)
                                    {
                                        Writer.Keyword(SqlKeyword.NULL);
                                    }
                                    else if (RequiresConditionalEscape() && IsCondition(node.Arguments[3]))
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
                        break;
                    }
                default:
                    throw new NotSupportedException();
            }
        }

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
            string name = node.Method.Name;

            switch (name)
            {
                case nameof(string.Contains) when node.Arguments.Count == 1:
                case nameof(string.EndsWith) when node.Arguments.Count == 1:
                case nameof(string.StartsWith) when node.Arguments.Count == 1:

                    Visit(node.Object);

                    Writer.Keyword(SqlKeyword.LIKE);

                    if (Engine == DatabaseEngine.MySQL)
                    {
                        Writer.Write("CONCAT");
                    }

                    Writer.OpenBrace();

                    if (name is (nameof(string.Contains)) or (nameof(string.EndsWith)))
                    {
                        Writer.Write("'%'");

                        if (Engine is DatabaseEngine.PostgreSQL or DatabaseEngine.Oracle)
                        {
                            Writer.Write(" || ");
                        }
                        else if (Engine == DatabaseEngine.MySQL)
                        {
                            Writer.Delimiter();
                        }
                        else
                        {
                            Writer.Write(" + ");
                        }
                    }

                    Visit(node.Arguments[0]);

                    if (name is (nameof(string.Contains)) or (nameof(string.StartsWith)))
                    {
                        if (Engine is DatabaseEngine.PostgreSQL or DatabaseEngine.Oracle)
                        {
                            Writer.Write(" || ");
                        }
                        else if (Engine == DatabaseEngine.MySQL)
                        {
                            Writer.Delimiter();
                        }
                        else
                        {
                            Writer.Write(" + ");
                        }

                        Writer.Write("'%'");
                    }

                    Writer.CloseBrace();

                    break;
                case nameof(string.IsNullOrEmpty) when IsPlainVariable(node.Arguments[0], true):
                    {
                        var text = node.Arguments[0].GetValueFromExpression<string>();

                        if (string.IsNullOrEmpty(text))
                        {
                            Writer.AlwaysTrue();
                        }
                        else
                        {
                            Writer.AlwaysFalse();
                        }

                        break;
                    }
                case nameof(string.IsNullOrWhiteSpace) when IsPlainVariable(node.Arguments[0], true):
                    {
                        var text = node.Arguments[0].GetValueFromExpression<string>();

                        if (string.IsNullOrWhiteSpace(text))
                        {
                            Writer.AlwaysTrue();
                        }
                        else
                        {
                            Writer.AlwaysFalse();
                        }

                        break;
                    }
                case nameof(string.IsNullOrEmpty):

                    using (var domain = Writer.Domain())
                    {
                        Visit(node.Arguments[0]);

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

                        string text = domain.ToString();

                        Writer.Keyword(SqlKeyword.IS);
                        Writer.Keyword(SqlKeyword.NULL);
                        Writer.Keyword(SqlKeyword.THEN);
                        Writer.Keyword(SqlKeyword.NULL);
                        Writer.Keyword(SqlKeyword.ELSE);

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

                        Writer.Write(text); //? 截取字符串主体。

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
                            Visit(startPairt);
                        }

                        if (node.Arguments.Count > 1)
                        {
                            if (Engine == DatabaseEngine.PostgreSQL)
                            {
                                Writer.Write(" FOR ");
                            }
                            else
                            {
                                Writer.Delimiter();
                            }

                            var lengthPairt = node.Arguments[1];

                            if (IsPlainVariable(lengthPairt))
                            {
                                Writer.Constant(lengthPairt.GetValueFromExpression<int>());
                            }
                            else
                            {
                                Visit(lengthPairt);
                            }
                        }

                        domain.Flyback();

                        Writer.Keyword(SqlKeyword.CASE);
                        Writer.Keyword(SqlKeyword.WHEN);
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

                    Writer.Write(" - 1"); //? 数据库下标从“1”开始，所以需要减“1”。

                    break;
                case nameof(string.Concat) when node.Arguments.Count > 1:

                    if (Engine == DatabaseEngine.MySQL)
                    {
                        Writer.Write("CONCAT");
                    }

                    Writer.OpenBrace();

                    for (int i = 0; i < node.Arguments.Count; i++)
                    {
                        if (i > 0)
                        {
                            if (Engine is DatabaseEngine.PostgreSQL or DatabaseEngine.Oracle)
                            {
                                Writer.Write(" || ");
                            }
                            else if (Engine == DatabaseEngine.MySQL)
                            {
                                Writer.Delimiter();
                            }
                            else
                            {
                                Writer.Write(" + ");
                            }
                        }

                        Visit(node.Arguments[i]);
                    }

                    Writer.CloseBrace();

                    break;
                default:
                    throw new NotSupportedException($"字符串的“{node.Method}”方法不被支持！");
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
