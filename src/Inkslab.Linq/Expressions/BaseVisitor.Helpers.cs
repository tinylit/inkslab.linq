using System;
using System.Linq;
using System.Linq.Expressions;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 基础访问器（辅助判断方法片段）。
    /// </summary>
    public abstract partial class BaseVisitor
    {
        #region Helper Methods

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
        protected bool RequiresConditionalEscape() => 
            Engine is not (DatabaseEngine.MySQL or DatabaseEngine.PostgreSQL);

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

        #endregion
    }
}
