using System.Linq.Expressions;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 替换表达式访问器。
    /// </summary>
    internal class ReplaceExpressionVisitor : ExpressionVisitor
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