using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Inkslab.Linq.Enums;
using Inkslab.Linq.Exceptions;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 条件。
    /// </summary>
    [DebuggerDisplay("Condition")]
    public class ConditionVisitor : CoreVisitor
    {
        /// <summary>
        /// 忽略可空类型。
        /// </summary>
        private bool ignoreNull = false;
        private bool ignoreEmptyString = false;
        private bool ignoreWhiteSpace = false;

        private readonly bool _isGroupHaving;

        /// <inheritdoc/>
        public ConditionVisitor(BaseVisitor visitor, bool isGroupHaving = false) : base(visitor)
        {
            _isGroupHaving = isGroupHaving;
        }

        /// <summary>
        /// 启动。
        /// </summary>
        /// <param name="node"></param>
        public override void Startup(Expression node)
        {
            if (!RequiresConditionalEscape() || IsCondition(node))
            {
                using (var domain = Writer.Domain())
                {
                    Visit(node);

                    if (domain.IsEmpty)
                    {
                        Writer.AlwaysTrue();
                    }
                }
            }
            else
            {
                using (var domain = Writer.Domain())
                {
                    Visit(node);

                    if (domain.HasValue)
                    {
                        Writer.Operator(SqlOperator.IsTrue);
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override void Constant(object value)
        {
            if (ignoreNull && value is null)
            {
            }
            else
            {
                base.Constant(value);
            }
        }


        /// <inheritdoc/>
        protected override void Variable(string name, object value)
        {
            if (ignoreNull && value is null)
            {
            }
            else if (ignoreEmptyString
                && value is string text
                && (ignoreWhiteSpace
                    ? string.IsNullOrWhiteSpace(text)
                    : string.IsNullOrEmpty(text)))
            {
            }
            else
            {
                base.Variable(name, value);
            }
        }

        /// <inheritdoc/>
        protected override void MethodCall(MethodCallExpression node)
        {
            ignoreNull |= false;
            ignoreEmptyString |= false;
            ignoreWhiteSpace |= false;

            var declaringType = node.Method.DeclaringType;

            if (declaringType == Types.String)
            {
                ByString(node);
            }
            else if (RequiresConditionalEscape() && node.Type.IsBoolean())
            {
                using (var domain = Writer.Domain())
                {
                    base.MethodCall(node);

                    if (domain.HasValue)
                    {
                        Writer.Operator(SqlOperator.IsTrue);
                    }
                }
            }
            else
            {
                base.MethodCall(node);
            }
        }

        /// <inheritdoc/>
        protected override void ByString(MethodCallExpression node)
        {
            string name = node.Method.Name;

            switch (name)
            {
                case nameof(string.Contains) when node.Arguments.Count == 1:
                case nameof(string.EndsWith) when node.Arguments.Count == 1:
                case nameof(string.StartsWith) when node.Arguments.Count == 1:

                    using (var domain = Writer.Domain())
                    {
                        ignoreNull = true;
                        ignoreEmptyString = true;

                        Visit(node.Arguments[0]);

                        ignoreEmptyString = false;
                        ignoreNull = false;

                        if (domain.IsEmpty)
                        {
                            Writer.AlwaysTrue();

                            break;
                        }

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

                        domain.Flyback();

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
                    }

                    break;
                case nameof(string.IsNullOrEmpty):

                    using (var domain = Writer.Domain())
                    {
                        ignoreNull = true;
                        ignoreEmptyString = true;

                        Visit(node.Arguments[0]);

                        ignoreEmptyString = false;
                        ignoreNull = false;

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
                        ignoreNull = true;
                        ignoreEmptyString = true;
                        ignoreWhiteSpace = true;

                        Visit(node.Arguments[0]);

                        ignoreWhiteSpace = false;
                        ignoreEmptyString = false;
                        ignoreNull = false;

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
                default:

                    base.ByString(node);

                    break;
            }
        }

        /// <inheritdoc/>
        protected override void Condition(Expression node) => Visit(node);

        /// <inheritdoc/>
        protected override void Binary(BinaryExpression node) => Binary(node.Left, node.NodeType, node.Right);

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

            if (IsNewEquals(right) || IsNewEquals(left))
            {
                var compareExpressions = new Dictionary<MemberInfo, Expression>();

                var visitor = new MyNewExpressionVisitor(compareExpressions);

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
                            domain.Flyback();

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

        /// <inheritdoc/>
        protected virtual void BinaryCore(Expression left,
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
                case ExpressionType.Equal when IsConvertToNull(left) ^ IsConvertToNull(right):
                case ExpressionType.NotEqual when IsConvertToNull(left) ^ IsConvertToNull(right):
                case ExpressionType.GreaterThan when IsConvertToNull(left) ^ IsConvertToNull(right):
                case ExpressionType.LessThan when IsConvertToNull(left) ^ IsConvertToNull(right):
                case ExpressionType.LessThanOrEqual when IsConvertToNull(left) ^ IsConvertToNull(right):
                case ExpressionType.GreaterThanOrEqual when IsConvertToNull(left) ^ IsConvertToNull(right):

                    //? 忽略数据库非可空类型的字段与 NULL 的比较表达式。
                    using (var domain = Writer.Domain())
                    {
                        ignoreNull = true;

                        Visit(right);

                        ignoreNull = false;

                        if (domain.IsEmpty)
                        {
                            break;
                        }

                        domain.Flyback();

                        int length = Writer.Length;

                        ignoreNull = true;

                        Visit(left);

                        ignoreNull = false;

                        if (Writer.Length == length)
                        {
                            domain.Discard();
                        }
                        else
                        {
                            Writer.Operator(expressionType.GetOperator());
                        }
                    }
                    break;
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:

                    bool writeNull = false;

                    using (var domain = Writer.Domain())
                    {
                        ignoreNull = true;

                        Visit(right);

                        ignoreNull = false;

                        if (domain.IsEmpty)
                        {
                            ignoreNull = true;

                            Visit(left);

                            ignoreNull = false;

                            if (domain.IsEmpty)
                            {
                                break;
                            }

                            writeNull = true;
                        }
                        else
                        {
                            domain.Flyback();

                            int length = Writer.Length;

                            ignoreNull = true;

                            Visit(left);

                            ignoreNull = false;

                            if (Writer.Length > length)
                            {
                                Writer.Operator(expressionType.GetOperator());

                                break;
                            }

                            writeNull = true;
                        }
                    }

                    if (writeNull)
                    {
                        Writer.Keyword(SqlKeyword.IS);

                        if (expressionType == ExpressionType.NotEqual)
                        {
                            Writer.Keyword(SqlKeyword.NOT);
                        }

                        Writer.Keyword(SqlKeyword.NULL);
                    }

                    break;
                case ExpressionType.GreaterThan:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThanOrEqual:

                    Visit(left);

                    Writer.Operator(expressionType.GetOperator());

                    Visit(right);

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
                        && Engine is DatabaseEngine.MySQL or DatabaseEngine.PostgreSQL or DatabaseEngine.Oracle
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
                                if (Engine is DatabaseEngine.PostgreSQL or DatabaseEngine.Oracle)
                                {
                                    Writer.Write(" || ");
                                }
                                else
                                {
                                    Writer.Delimiter();
                                }
                            }
                            else
                            {
                                Writer.Operator(expressionType.GetOperator());
                            }
                        }

                        domain.Flyback();

                        if (isStringConcat && Engine == DatabaseEngine.MySQL)
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
                            if (Writer.IsConditionReversal
                                ? expressionType == ExpressionType.OrElse
                                : expressionType == ExpressionType.AndAlso)
                            {
                                Condition(right);
                            }
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


        /// <summary>
        /// <see cref="ExpressionType.Coalesce"/>.
        /// </summary>
        protected virtual void Coalesce(Expression left, Expression right)
        {
            switch (Engine)
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
                        ignoreNull = true;

                        Visit(left);

                        ignoreNull = false;

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

                            switch (Engine)
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
                        ignoreNull = true;

                        Visit(left);

                        ignoreNull = false;

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

        /// <inheritdoc/>
        protected override void LinqCall(MethodCallExpression node)
        {
            using (var visitor = new SelectVisitor(this))
            {
                visitor.Startup(node);
            }
        }

        /// <inheritdoc/>
        protected override void ByEnumerable(MethodCallExpression node)
        {
            if (_isGroupHaving && node.IsGrouping())
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

        #region private

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

        private static bool IsConvertToNull(Expression node)
        {
            if (node.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.TypeAs) //? 类型转换后，结果反转。
            {
                do
                {
                    if (node is UnaryExpression unary)
                    {
                        node = unary.Operand;

                        continue;
                    }

                    return !node.Type.IsNullable();

                } while (node.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.TypeAs);
            }

            return node.Type.IsNullable();
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
                            Writer.Keyword(SqlKeyword.OR);
                        }
                        else
                        {
                            Writer.Keyword(SqlKeyword.AND);
                        }
                    }
                    else
                    {
                        commaFlag = true;
                    }

                    Writer.Schema(parameterExpression.Name);
                    Writer.Name(field);

                    Writer.Keyword(SqlKeyword.IS);

                    if (isExpressionNotEqual)
                    {
                        Writer.Keyword(SqlKeyword.NOT);
                    }

                    Writer.Keyword(SqlKeyword.NULL);
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
                            Writer.Keyword(SqlKeyword.OR);
                        }
                        else
                        {
                            Writer.Keyword(SqlKeyword.AND);
                        }
                    }
                    else
                    {
                        commaFlag = true;
                    }

                    Writer.Schema(parameterExpression.Name);
                    Writer.Name(field);

                    Writer.Keyword(SqlKeyword.IS);

                    if (isExpressionNotEqual)
                    {
                        Writer.Keyword(SqlKeyword.NOT);
                    }

                    Writer.Keyword(SqlKeyword.NULL);
                }
            }

            return true;
        }
        #endregion

        #region nested
        private class MyNewExpressionVisitor : ExpressionVisitor
        {
            private readonly Dictionary<MemberInfo, Expression> _keyValues;

            public MyNewExpressionVisitor(
                Dictionary<MemberInfo, Expression> keyValues
            )
            {
                _keyValues = keyValues;
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
        #endregion
    }
}