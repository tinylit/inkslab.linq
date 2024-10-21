using Inkslab.Linq;
using Inkslab.Linq.Enums;

namespace System.Linq.Expressions
{
    /// <summary>
    /// 表达式拓展类。
    /// </summary>
    internal static class ExpressionExtensions
    {
        /// <summary>
        /// 是否为boolean表达式。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static bool IsBoolean(this Expression node)
        {
            if (node is null)
            {
                return false;
            }

            return node.Type.IsBoolean();
        }

        /// <summary>
        /// 是否是HasValue属性。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static bool IsHasValue(this MemberExpression node)
        {
            if (node is null || node.Expression is null)
            {
                return false;
            }

            return node.Member.Name == "HasValue" && node.Expression.IsNullable();
        }

        /// <summary>
        /// 是否是Value属性。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static bool IsValue(this MemberExpression node)
        {
            if (node is null || node.Expression is null)
            {
                return false;
            }

            return node.Member.Name == "Value" && node.Expression.IsNullable();
        }

        /// <summary>
        /// 是否是Length属性。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static bool IsLength(this MemberExpression node)
        {
            if (node is null)
            {
                return false;
            }

            return node.Member.Name == "Length" && node.Member.DeclaringType == Types.String;
        }

        /// <summary>
        /// 是否为可空类型。
        /// </summary>
        /// <param name="member">表达式。</param>
        /// <returns></returns>
        internal static bool IsNullable(this Expression member)
        {
            if (member is null)
            {
                return false;
            }

            return member.Type.IsNullable();
        }

        /// <summary>
        /// Where条件（不包含And或Or）。
        /// </summary>
        /// <param name="nodeType">节点类型。</param>
        /// <returns></returns>
        internal static ExpressionType Reverse(this ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.GreaterThan:
                    return ExpressionType.LessThanOrEqual;
                case ExpressionType.GreaterThanOrEqual:
                    return ExpressionType.LessThan;
                case ExpressionType.LessThan:
                    return ExpressionType.GreaterThanOrEqual;
                case ExpressionType.LessThanOrEqual:
                    return ExpressionType.GreaterThan;
                case ExpressionType.Equal:
                    return ExpressionType.NotEqual;
                case ExpressionType.NotEqual:
                    return ExpressionType.Equal;
                default:
                    return nodeType;
            }
        }

        /// <summary>
        /// 获取操作符。
        /// </summary>
        /// <param name="expressionType">表达式类型。</param>
        /// <returns>操作符。</returns>
        internal static SqlOperator GetOperator(this ExpressionType expressionType)
        {
            if (TryGetOperator(expressionType, out SqlOperator @operator))
            {
                return @operator;
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// 尝试获取操作符。
        /// </summary>
        /// <param name="expressionType">表达式类型。</param>
        /// <param name="operator">操作符。</param>
        /// <returns>是否成功获取。</returns>
        internal static bool TryGetOperator(
            this ExpressionType expressionType,
            out SqlOperator @operator
        )
        {
            switch (expressionType)
            {
                case ExpressionType.And:
                    @operator = SqlOperator.And;
                    return true;
                case ExpressionType.Or:
                    @operator = SqlOperator.Or;
                    return true;
                case ExpressionType.Equal:
                    @operator = SqlOperator.Equal;
                    return true;
                case ExpressionType.NotEqual:
                    @operator = SqlOperator.NotEqual;
                    return true;
                case ExpressionType.LessThan:
                    @operator = SqlOperator.LessThan;
                    return true;
                case ExpressionType.LessThanOrEqual:
                    @operator = SqlOperator.LessThanOrEqual;
                    return true;
                case ExpressionType.GreaterThan:
                    @operator = SqlOperator.GreaterThan;
                    return true;
                case ExpressionType.GreaterThanOrEqual:
                    @operator = SqlOperator.GreaterThanOrEqual;
                    return true;
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    @operator = SqlOperator.Add;
                    return true;
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    @operator = SqlOperator.Subtract;
                    return true;
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    @operator = SqlOperator.Multiply;
                    return true;
                case ExpressionType.Divide:
                    @operator = SqlOperator.Divide;
                    return true;
                case ExpressionType.Modulo:
                    @operator = SqlOperator.Modulo;
                    return true;
                case ExpressionType.ExclusiveOr:
                    @operator = SqlOperator.ExclusiveOr;
                    return true;
                case ExpressionType.LeftShift:
                    @operator = SqlOperator.LeftShift;
                    return true;
                case ExpressionType.RightShift:
                    @operator = SqlOperator.RightShift;
                    return true;
                case ExpressionType.Not:
                case ExpressionType.OnesComplement:
                    @operator = SqlOperator.OnesComplement;
                    return true;
                case ExpressionType.UnaryPlus:
                    @operator = SqlOperator.UnaryPlus;
                    return true;
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    @operator = SqlOperator.Negate;
                    return true;
                default:
                    @operator = SqlOperator.IsFalse;
                    return false;
            }
        }

        /// <summary>
        /// 获取表达式值。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static object GetValueFromExpression(this Expression node)
        {
            if (node is null)
            {
                return null;
            }

            switch (node)
            {
                case ConstantExpression constant:
                    return constant.Value;
                case LambdaExpression lambda when lambda.Parameters.Count > 0:
                    throw new NotSupportedException();
                case LambdaExpression lambda:
                    return lambda.Body is ConstantExpression body
                        ? body.Value
                        : lambda.Compile().DynamicInvoke();
                case UnaryExpression unary when unary.NodeType == ExpressionType.Quote:
                    return unary.Operand.GetValueFromExpression();
                default:
                    return Expression.Lambda(node).Compile().DynamicInvoke();
            }
        }

        /// <summary>
        /// 获取表达式值。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <returns></returns>
        internal static T GetValueFromExpression<T>(this Expression node)
        {
            if (node is null)
            {
                return default;
            }

            switch (node)
            {
                case ConstantExpression constant:
                    return (T)constant.Value;
                case Expression<Func<T>> lambda:
                    return lambda.Compile().Invoke();
                case LambdaExpression lambda when lambda.Parameters.Count > 0:
                    throw new NotSupportedException();
                case LambdaExpression lambda when lambda.Body.NodeType == ExpressionType.Constant:
                    return lambda.Body.GetValueFromExpression<T>();
                case UnaryExpression unary when unary.NodeType == ExpressionType.Quote:
                    return unary.Operand.GetValueFromExpression<T>();
                default:
                    return Expression.Lambda<Func<T>>(node)
                        .Compile()
                        .Invoke();
            }
        }

        /// <summary>
        /// 是否是<see cref="IGrouping{TKey, TElement}"/>
        /// </summary>
        /// <returns></returns>
        internal static bool IsGrouping(this Expression node, bool isAnalyzeOnlyOneself = false)
        {
            switch (node)
            {
                case MethodCallExpression methodCall:
                    return methodCall.IsGrouping(isAnalyzeOnlyOneself);
                case MemberExpression member:
                    return member.IsGrouping(isAnalyzeOnlyOneself);
                default:
                    return node.Type.IsGrouping();
            }
        }

        /// <summary>
        /// 是否是<see cref="IGrouping{TKey, TElement}"/>
        /// </summary>
        /// <returns></returns>
        internal static bool IsGrouping(
            this MemberExpression node,
            bool isAnalyzeOnlyOneself = false
        )
        {
            if (node.Expression is null)
            {
                return node.Type.IsGrouping();
            }

            return node.Member.Name == "Key"
                ? node.Expression.Type.IsGrouping()
                : isAnalyzeOnlyOneself
                    ? node.Type.IsGrouping()
                    : node.Expression is MemberExpression member
                        && member.Member.Name == "Key"
                        && member.IsGrouping();
        }

        /// <summary>
        /// 是否是<see cref="IGrouping{TKey, TElement}"/>
        /// </summary>
        /// <returns></returns>
        internal static bool IsGrouping(
            this MethodCallExpression node,
            bool isAnalyzeOnlyOneself = false
        )
        {
            if (node.Method.IsStatic)
            {
                if (isAnalyzeOnlyOneself)
                {
                    return node.Arguments.Count > 1
                        ? IsGrouping(node.Arguments[1])
                        : node.Arguments[0].Type.IsGrouping();
                }

                return node.Arguments.Count > 1 && IsGrouping(node.Arguments[1])
                    || node.Arguments[0].IsGrouping();
            }

            if (isAnalyzeOnlyOneself)
            {
                return node.Arguments.Count > 0
                    ? IsGrouping(node.Arguments[0])
                    : node.Object.Type.IsGrouping();
            }

            return node.Arguments.Count > 0 && IsGrouping(node.Arguments[0])
                || node.Object.IsGrouping();
        }

        private static bool IsGrouping(Expression node)
        {
            switch (node)
            {
                case UnaryExpression unary:
                    return IsGrouping(unary.Operand);
                case LambdaExpression lambda when lambda.Parameters.Count > 0:
                    return IsGrouping(lambda.Parameters[0]);
                case ConstantExpression constant:
                    return constant.Type.IsGroupingQueryable();
                case ParameterExpression parameter:
                    return parameter.Type.IsGrouping();
                default:
                    return false;
            }
        }
    }
}
