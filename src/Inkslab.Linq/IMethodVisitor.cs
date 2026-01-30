using System.Linq.Expressions;
using Inkslab.Linq.Expressions;

namespace Inkslab.Linq
{
    /// <summary>
    /// 方法访问器。
    /// </summary>
    public interface IMethodVisitor
    {
        /// <summary>
        /// 表达式分析。
        /// </summary>
        /// <param name="visitor">访问器。</param>
        /// <param name="writer">SQL写入器。</param>
        /// <param name="node">表达式。</param>
        void Visit(BaseVisitor visitor, SqlWriter writer, MethodCallExpression node);
    }
}
