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
        private bool isConditionBalance = true;

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
            Visit(node);
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

                if (isConditionBalance)
                {
                    Writer.Operator(SqlOperator.IsTrue);
                }
            }
        }


        /// <inheritdoc/>
        protected override void Variable(string name, object value)
        {
            if (ignoreNull && value is null)
            {
            }
            else
            {
                base.Variable(name, value);

                if (isConditionBalance)
                {
                    Writer.Operator(SqlOperator.IsTrue);
                }
            }
        }

        /// <inheritdoc/>
        protected override void Member(MemberExpression node)
        {
            base.Member(node);

            if (isConditionBalance)
            {
                Writer.Operator(SqlOperator.IsTrue);
            }
        }

        /// <inheritdoc/>
        protected override void Member(MemberInfo memberInfo, Expression node)
        {
            base.Member(memberInfo, node);

            if (isConditionBalance)
            {
                Writer.Operator(SqlOperator.IsTrue);
            }
        }

        /// <inheritdoc/>
        protected override void Member(string schema, string field, string name)
        {
            base.Member(schema, field, name);

            if (isConditionBalance)
            {
                Writer.Operator(SqlOperator.IsTrue);
            }
        }

        /// <inheritdoc/>
        protected override void MemberHasValue(Expression node)
        {
            if (isConditionBalance)
            {
                isConditionBalance = false;

                base.MemberHasValue(node);

                isConditionBalance = true;
            }
            else
            {
                base.MemberHasValue(node);
            }
        }

        /// <inheritdoc/>
        protected override void MethodCall(MethodCallExpression node)
        {
            ignoreNull &= false;
            isConditionBalance &= false;

            var declaringType = node.Method.DeclaringType;

            if (declaringType == Types.String)
            {
                ByString(node);
            }
            else
            {
                base.MethodCall(node);
            }
        }

        /// <inheritdoc/>
        protected override void ByString(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case nameof(string.Contains):
                case nameof(string.EndsWith):
                case nameof(string.StartsWith):
                case nameof(string.IsNullOrEmpty):
                case nameof(string.IsNullOrWhiteSpace):

                    using (var domain = Writer.Domain()) //? 字符串为空时，处理为真。
                    {
                        base.ByString(node);

                        if (domain.IsEmpty)
                        {
                            Writer.AlwaysTrue();
                        }
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
        protected override void Binary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Coalesce)
            {
                base.Coalesce(node);
            }
            else
            {
                Binary(node.Left, node.NodeType, node.Right);
            }
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
                case ExpressionType.Equal
                    when right.NodeType == ExpressionType.Constant
                        && (left.NodeType == ExpressionType.MemberAccess || left.NodeType == ExpressionType.Parameter)
                        && !left.Type.IsCell():
                case ExpressionType.NotEqual
                    when right.NodeType == ExpressionType.Constant
                        && (left.NodeType == ExpressionType.MemberAccess || left.NodeType == ExpressionType.Parameter)
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
                        && (right.NodeType == ExpressionType.MemberAccess || right.NodeType == ExpressionType.Parameter)
                        && !right.Type.IsCell():
                case ExpressionType.NotEqual
                    when left.NodeType == ExpressionType.Constant
                        && (right.NodeType == ExpressionType.MemberAccess || right.NodeType == ExpressionType.Parameter)
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

                    //? 重置对称条件。
                    isConditionBalance &= false;

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

                    //? 重置对称条件。
                    isConditionBalance &= false;

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

                    //? 重置对称条件。
                    isConditionBalance &= false;

                    Visit(left);

                    Writer.Operator(expressionType.GetOperator());

                    Visit(right);

                    break;
                case ExpressionType.And:
                case ExpressionType.Or:
                case ExpressionType.ExclusiveOr:


                    //? 外存。
                    var conditionBalanceFlag = isConditionBalance;

                    isConditionBalance &= false;

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

                            Writer.Operator(expressionType.GetOperator());
                        }

                        if (conditionBalanceFlag)
                        {
                            Writer.Operator(SqlOperator.IsTrue);
                        }

                        domain.Flyback();

                        Writer.OpenBrace();
                    }

                    break;
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

                    isConditionBalance &= false;

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

                    //? 重置对称条件。
                    isConditionBalance &= false;

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
                        isConditionBalance = RequiresConditionalEscape();

                        Condition(left);

                        isConditionBalance = false;

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

                                isConditionBalance = RequiresConditionalEscape();

                                Condition(right);

                                isConditionBalance = false;

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