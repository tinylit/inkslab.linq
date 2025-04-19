using System.Diagnostics;
using System.Linq.Expressions;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 条件。
    /// </summary>
    [DebuggerDisplay("Condition")]
    public class ConditionVisitor : CoreVisitor
    {
        private readonly bool _isGroupHaving;

        /// <inheritdoc/>
        public ConditionVisitor(CoreVisitor visitor, bool isGroupHaving = false) : base(visitor)
        {
            _isGroupHaving = isGroupHaving;
        }

        /// <summary>
        /// 启动。
        /// </summary>
        /// <param name="node"></param>
        public override void Startup(Expression node) => base.Condition(node);

        /// <inheritdoc/>
        protected override void LinqCall(MethodCallExpression node)
        {
            using (var visitor = new SelectVisitor(this))
            {
                visitor.Startup(node);
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
    }
}