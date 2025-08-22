using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Inkslab.Linq.SqlServer
{
    /// <summary>
    /// MySql 数据库连接批量复制工厂。
    /// </summary>
    public class SqlServerBulkCopyFactory : IDbConnectionBulkCopyFactory
    {
        /// <summary>
        /// 获取数据库引擎类型。
        /// </summary>
        public DatabaseEngine Engine => DatabaseEngine.SqlServer;

        /// <inheritdoc/>
        public IDatabaseBulkCopy Create(DbConnection connection, DbTransaction transaction = null)
        {
            return new SqlServerBulkAssistant(new SqlBulkCopy((SqlConnection)connection, SqlBulkCopyOptions.Default, (SqlTransaction)transaction));
        }
    }
}
