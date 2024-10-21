namespace Inkslab.Linq
{
    /// <summary>
    /// 执行器访问器。
    /// </summary>
    public interface IExecutorVisitor : IStartupVisitor
    {
        /// <summary>
        /// SQL语句。
        /// </summary>
        CommandSql ToSQL();
    }
}