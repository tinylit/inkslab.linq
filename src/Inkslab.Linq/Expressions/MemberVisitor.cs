using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 成员访问器。
    /// </summary>
    [DebuggerDisplay("MemberVisitor")]
    public class MemberVisitor : CoreVisitor
    {
        /// <inheritdoc/>
        public MemberVisitor(BaseVisitor visitor) : base(visitor)
        {

        }

        /// <inheritdoc/>
        protected override void Member(MemberInfo memberInfo, Expression node) => base.Visit(node);
    }
}