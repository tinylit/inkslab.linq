using System.Data.Common;

namespace Inkslab.Linq
{
    /// <summary>
    /// 连接池。
    /// </summary>
    public interface IConnections
    {
        /// <summary>
        /// 获取数据库连接。
        /// </summary>
        /// <param name="databaseStrings">链接字符串。</param>
        /// <returns>数据库链接。</returns>
        DbConnection Get(IConnection databaseStrings);
    }
}
