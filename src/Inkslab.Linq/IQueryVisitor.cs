namespace Inkslab.Linq
{
    /// <summary>
    /// 查询访问器。
    /// </summary>
    public interface IQueryVisitor : IStartupVisitor
    {
        /// <summary>
        /// SQL语句。
        /// </summary>
        /// <typeparam name="TElement">元素类型。</typeparam>
        /// <returns></returns>
        CommandSql<TElement> ToSQL<TElement>();
    }
}