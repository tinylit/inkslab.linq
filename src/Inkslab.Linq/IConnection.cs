namespace Inkslab.Linq
{
    /// <summary>
    /// 数据库连接字符串。
    /// </summary>
    public interface IConnection : IConnectionStrings
    {
        /// <summary>
        /// 数据库引擎。
        /// </summary>
        DatabaseEngine Engine { get; }
    }
}