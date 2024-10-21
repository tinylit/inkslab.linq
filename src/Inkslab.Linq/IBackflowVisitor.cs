using System.Linq.Expressions;

namespace Inkslab.Linq
{
    /// <summary>
    /// 表达式回流，在分支表达式无法解析时，回流到主分支尝试解析。
    /// </summary>
    public interface IBackflowVisitor
    {
        /// <summary>
        /// 链路回流。
        /// </summary>
        /// <param name="visitor">当前访问器。</param>
        /// <param name="node">节点。</param>
        void Backflow(ExpressionVisitor visitor, MethodCallExpression node);
    }
}
