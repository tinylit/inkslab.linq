using System.Data.Common;
using MySqlConnector;

namespace Inkslab.Linq.MySql
{
    /// <summary>
    /// MySql 数据库连接批量复制工厂。
    /// </summary>
    public class MySqlBulkCopyFactory : IDbConnectionBulkCopyFactory
    {
        /// <summary>
        /// 获取数据库引擎类型。
        /// </summary>
        public DatabaseEngine Engine => DatabaseEngine.MySQL;

        /// <inheritdoc/>
        public IDatabaseBulkCopy Create(DbConnection connection, DbTransaction transaction = null)
        {
            return new MySqlBulkAssistant(new MySqlBulkCopy((MySqlConnection)connection, (MySqlTransaction)transaction));
        }
    }
}
