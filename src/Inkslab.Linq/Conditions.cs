using System;
using System.Linq.Expressions;

namespace Inkslab.Linq
{
    /// <summary>
    /// Linq 2 SQL 扩展（仅用于表达式）。
    /// </summary>
    public static class Conditions
    {
        /// <summary>
        /// 是否满足条件。
        /// </summary>
        /// <typeparam name="T">参数类型。</typeparam>
        /// <param name="source">源。</param>
        /// <param name="predicate">条件。</param>
        /// <returns>是否满足。</returns>
        public static bool IsTrue<T>(T source, Expression<Func<T, bool>> predicate) =>
            throw new NotImplementedException("只能在条件表达式（如：where、on等）中使用！");

        /// <summary>
        /// 根据“<paramref name="test"/>”是否为真，拼接条件。
        /// </summary>
        /// <param name="test">为真时，拼接“<paramref name="test"/>”条件，否则忽略条件。</param>
        /// <param name="ifTrue">条件。</param>
        /// <returns>是否满足。</returns>
        public static bool If(bool test, bool ifTrue)
        {
            throw new NotImplementedException("只能在条件表达式（如：where、on等）中使用！");
        }

        /// <summary>
        /// 根据“<paramref name="test"/>”是否为真，拼接条件。
        /// </summary>
        /// <param name="source">源。</param>
        /// <param name="test">为真时，拼接“<paramref name="test"/>”条件，否则忽略条件。</param>
        /// <param name="ifTrue">条件。</param>
        /// <typeparam name="T">参数类型。</typeparam>
        /// <returns>是否满足。</returns>
        public static bool If<T>(T source, bool test, Expression<Func<T, bool>> ifTrue)
        {
            throw new NotImplementedException("只能在条件表达式（如：where、on等）中使用！");
        }

        /// <summary>
        /// 根据“<paramref name="test"/>”是否为真，拼接条件。
        /// </summary>
        /// <param name="test">为真时，使用“<paramref name="ifTrue"/>”条件，否则使用“<paramref name="ifTrue"/>”条件。</param>
        /// <param name="ifTrue">“<paramref name="test"/>”为真的条件。</param>
        /// <param name="ifFalse">“<paramref name="test"/>”为假的条件。</param>
        /// <returns>是否满足。</returns>
        public static bool Conditional(bool test, bool ifTrue, bool ifFalse)
        {
            throw new NotImplementedException("只能在条件表达式（如：where、on等）中使用！");
        }

        /// <summary>
        /// 根据“<paramref name="test"/>”是否为真，拼接条件。
        /// </summary>
        /// <param name="source">源。</param>
        /// <param name="test">为真时，使用“<paramref name="ifTrue"/>”条件，否则使用“<paramref name="ifTrue"/>”条件。</param>
        /// <param name="ifTrue">“<paramref name="test"/>”为真的条件。</param>
        /// <param name="ifFalse">“<paramref name="test"/>”为假的条件。</param>
        /// <typeparam name="T">参数类型。</typeparam>
        /// <returns>是否满足。</returns>
        public static bool Conditional<T>(T source, bool test, Expression<Func<T, bool>> ifTrue, Expression<Func<T, bool>> ifFalse)
        {
            throw new NotImplementedException("只能在条件表达式（如：where、on等）中使用！");
        }

        /// <summary>
        /// 获取始终为真的条件。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Expression<Func<T, bool>> True<T>() => x => true;

        /// <summary>
        /// 获取始终为真的条件。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <returns></returns>
        public static Expression<Func<T, bool>> False<T>() => x => false;

        /// <summary>
        /// 条件表达式。
        /// </summary>
        /// <typeparam name="T">参数类型。</typeparam>
        /// <param name="predicate">条件。</param>
        /// <returns>是否满足。</returns>
        public static Expression<Func<T, bool>> Fragment<T>(Expression<Func<T, bool>> predicate) => predicate ?? throw new ArgumentNullException(nameof(predicate));

        /// <summary>
        /// 且。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <param name="leftNode">左节点。</param>
        /// <param name="rightNode">右节点。</param>
        /// <returns></returns>
        public static Expression<Func<T, bool>> And<T>(
            this Expression<Func<T, bool>> leftNode,
            Expression<Func<T, bool>> rightNode
        )
        {
            if (leftNode is null)
            {
                return rightNode;
            }

            if (rightNode is null)
            {
                return leftNode;
            }

            if (
                leftNode.Body.NodeType == ExpressionType.Constant
                && leftNode.Body is ConstantExpression constant
            )
            {
                if (Equals(constant.Value, true))
                {
                    return rightNode;
                }

                return leftNode;
            }

            return Expression.Lambda<Func<T, bool>>(
                Expression.AndAlso(
                    leftNode.Body,
                    new ReplaceExpressionVisitor(
                        rightNode.Parameters[0],
                        leftNode.Parameters[0]
                    ).Visit(rightNode.Body)
                ),
                leftNode.Parameters
            );
        }

        /// <summary>
        /// 或。
        /// </summary>
        /// <typeparam name="T">类型。</typeparam>
        /// <param name="leftNode">左节点。</param>
        /// <param name="rightNode">右节点。</param>
        /// <returns></returns>
        public static Expression<Func<T, bool>> Or<T>(
            this Expression<Func<T, bool>> leftNode,
            Expression<Func<T, bool>> rightNode
        )
        {
            if (leftNode is null)
            {
                return rightNode;
            }

            if (rightNode is null)
            {
                return leftNode;
            }

            if (
                leftNode.Body.NodeType == ExpressionType.Constant
                && leftNode.Body is ConstantExpression constant
            )
            {
                if (Equals(constant.Value, false))
                {
                    return rightNode;
                }

                return leftNode;
            }

            return Expression.Lambda<Func<T, bool>>(
                Expression.OrElse(
                    leftNode.Body,
                    new ReplaceExpressionVisitor(
                        rightNode.Parameters[0],
                        leftNode.Parameters[0]
                    ).Visit(rightNode.Body)
                ),
                leftNode.Parameters
            );
        }

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
    }
}
