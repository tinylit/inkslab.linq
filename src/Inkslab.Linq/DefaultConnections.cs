using System.Data.Common;

namespace Inkslab.Linq
{
    /// <summary>
    /// 管理连接。
    /// </summary>
    public class DefaultConnections : IConnections
    {
        /// <inheritdoc/>
        public DbConnection Get(string connectionString, IDbConnectionFactory factory) => factory.Create(connectionString);
    }
}
