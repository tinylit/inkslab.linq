using System.Linq.Expressions;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 基础访问器（简单委托方法片段）。
    /// </summary>
    public abstract partial class BaseVisitor
    {
        #region Sealed Delegates

        /// <inheritdoc/>
        protected sealed override Expression VisitInvocation(InvocationExpression node)
        {
            Invocation(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitBinary(BinaryExpression node)
        {
            Binary(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitConditional(ConditionalExpression node)
        {
            Conditional(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitBlock(BlockExpression node)
        {
            Block(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override CatchBlock VisitCatchBlock(CatchBlock node)
        {
            CatchBlock(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitDebugInfo(DebugInfoExpression node)
        {
            DebugInfo(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitDynamic(DynamicExpression node)
        {
            Dynamic(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override ElementInit VisitElementInit(ElementInit node)
        {
            ElementInit(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitExtension(Expression node)
        {
            Extension(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitGoto(GotoExpression node)
        {
            Goto(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitIndex(IndexExpression node)
        {
            Index(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitLabel(LabelExpression node)
        {
            Label(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override LabelTarget VisitLabelTarget(LabelTarget node)
        {
            LabelTarget(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitListInit(ListInitExpression node)
        {
            ListInit(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitLoop(LoopExpression node)
        {
            Loop(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            MemberAssignment(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override MemberBinding VisitMemberBinding(MemberBinding node)
        {
            MemberBinding(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override MemberListBinding VisitMemberListBinding(MemberListBinding node)
        {
            MemberListBinding(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override MemberMemberBinding VisitMemberMemberBinding(
            MemberMemberBinding node
        )
        {
            MemberMemberBinding(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitNewArray(NewArrayExpression node)
        {
            NewArray(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitParameter(ParameterExpression node)
        {
            Parameter(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        {
            RuntimeVariables(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitSwitch(SwitchExpression node)
        {
            Switch(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override SwitchCase VisitSwitchCase(SwitchCase node)
        {
            SwitchCase(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitTry(TryExpression node)
        {
            Try(node);

            return node;
        }

        /// <inheritdoc/>
        protected sealed override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            TypeBinary(node);

            return node;
        }

        #endregion
    }
}
