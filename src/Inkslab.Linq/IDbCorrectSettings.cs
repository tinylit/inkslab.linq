namespace Inkslab.Linq
{
    /// <summary>
    /// 数据库矫正设置。
    /// </summary>
    public interface IDbCorrectSettings
    {
        /// <summary>
        /// 架构/表/字段名称。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        string Name(string name);

        /// <summary>
        /// 参数名称。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <returns></returns>
        string ParamterName(string name);

        /// <summary>
        /// T-SQL(分页)。
        /// </summary>
        /// <param name="sql">SQL，不能为 null 或空字符串。</param>
        /// <param name="take">获取“<paramref name="take"/>”条数据。</param>
        /// <param name="skip">跳过“<paramref name="skip"/>”条数据。</param>
        /// <param name="orderBy">排序，为 null 或空字符串时不拼接排序及其前置空格。</param>
        /// <returns>T-SQL（分页）。</returns>
        /// <exception cref="System.ArgumentException"><paramref name="sql"/> 为 null 或空字符串。</exception>
        string ToSQL(string sql, int take, int skip, string orderBy);
    }
}
