using System.Data.Common;

namespace Inkslab.Linq.PostgreSQL
{
    /// <summary>
    /// PostgreSQL 数据库连接批量复制工厂。
    /// </summary>
    public class PostgreSQLBulkCopyFactory : IDbConnectionBulkCopyFactory
    {
        /// <summary>
        /// 获取数据库引擎类型。
        /// </summary>
        public DatabaseEngine Engine => DatabaseEngine.PostgreSQL;

        /// <inheritdoc/>
        public IDatabaseBulkCopy Create(DbConnection connection, DbTransaction transaction = null)
        {
            return transaction is null ? new PostgreSQLBulkAssistant((Npgsql.NpgsqlConnection)connection)
                                      : new PostgreSQLBulkAssistant.PostgreSQLBulk((Npgsql.NpgsqlConnection)connection, (Npgsql.NpgsqlTransaction)transaction);
        }
    }
}
