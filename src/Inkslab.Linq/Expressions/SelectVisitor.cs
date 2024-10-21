namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 查询访问器（负责独立查询语句功能）。
    /// </summary>
    public class SelectVisitor : BaseSelectVisitor
    {
        /// <inheritdoc/>
        public SelectVisitor(IDbAdapter adapter) : base(adapter) { }

        /// <inheritdoc/>
        public SelectVisitor(BaseVisitor visitor, bool showAs = false) : base(visitor, showAs) { }

        /// <inheritdoc/>
        protected SelectVisitor(BaseVisitor visitor, ConditionType conditionType, bool isNewWriter = false, bool showAs = false) : base(visitor, conditionType, isNewWriter, showAs) { }
    }
}
