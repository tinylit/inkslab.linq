using System.Data.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Inkslab.Linq.PostgreSQL
{
    /// <summary>
    /// PostgreSQL 数据库连接批量复制工厂。
    /// </summary>
    public class PostgreSQLBulkCopyFactory : IDbConnectionBulkCopyFactory
    {
        private readonly ILogger<PostgreSQLBulkAssistant> _logger;

        /// <summary>
        /// 创建批量复制工厂。
        /// </summary>
        public PostgreSQLBulkCopyFactory() : this(NullLogger<PostgreSQLBulkAssistant>.Instance)
        {
        }

        /// <summary>
        /// 使用指定日志记录器创建批量复制工厂。
        /// </summary>
        /// <param name="logger">日志记录器。</param>
        public PostgreSQLBulkCopyFactory(ILogger<PostgreSQLBulkAssistant> logger)
        {
            _logger = logger ?? NullLogger<PostgreSQLBulkAssistant>.Instance;
        }

        /// <summary>
        /// 获取数据库引擎类型。
        /// </summary>
        public DatabaseEngine Engine => DatabaseEngine.PostgreSQL;

        /// <inheritdoc/>
        public IDatabaseBulkCopy Create(DbConnection connection, DbTransaction transaction = null)
        {
            return transaction is null ? new PostgreSQLBulkAssistant((Npgsql.NpgsqlConnection)connection, _logger)
                                      : new PostgreSQLBulkAssistant.PostgreSQLBulk((Npgsql.NpgsqlConnection)connection, (Npgsql.NpgsqlTransaction)transaction, _logger);
        }
    }
}
