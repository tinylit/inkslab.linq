using System.Collections.Generic;
using System.Linq.Expressions;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 替换表达式访问器。
    /// </summary>
    internal class ReplaceManyExpressionVisitor : ExpressionVisitor
    {
        private readonly IList<ParameterExpression> _oldExpression;
        private readonly IList<Expression> _newExpression;

        public ReplaceManyExpressionVisitor(IList<ParameterExpression> oldExpression, IList<Expression> newExpression)
        {
            _oldExpression = oldExpression;
            _newExpression = newExpression;
        }
        public override Expression Visit(Expression node)
        {
            if (node is null)
            {
                return null;
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
}