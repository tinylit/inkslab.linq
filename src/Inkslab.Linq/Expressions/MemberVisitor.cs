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
        private readonly bool _isGroupHaving;

        /// <inheritdoc/>
        public MemberVisitor(BaseVisitor visitor, bool isGroupHaving = false) : base(visitor)
        {
            _isGroupHaving = isGroupHaving;
        }

        /// <inheritdoc/>
        protected override void Member(MemberInfo memberInfo, Expression node) => base.Visit(node);

        /// <inheritdoc/>
        protected override void Condition(Expression node)
        {
            using (var visitor = new ConditionVisitor(this, _isGroupHaving))
            {
                visitor.Startup(node);
            }
        }
    }
}