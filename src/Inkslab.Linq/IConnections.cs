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
        /// <param name="connectionString">链接字符串。</param>
        /// <param name="factory">数据库工厂器。</param>
        /// <returns>数据库链接。</returns>
        DbConnection Get(string connectionString, IDbConnectionFactory factory);
    }
}
