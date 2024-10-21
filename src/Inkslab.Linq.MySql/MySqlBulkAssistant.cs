using MySqlConnector;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Linq.MySql
{
    /// <summary>
    /// MySql 批处理助手。
    /// </summary>
    public class MySqlBulkAssistant : IBulkAssistant
    {
        /// <inheritdoc/>
        /// <remarks>使用须知：<br/>
        /// 1. 仅支持 MySQL 数据库。<br/>
        /// 2. 请使用命令“SHOW GLOBAL VARIABLES LIKE 'local_infile'”检查变量状态是否开启，若未开启，使用“SET GLOBAL local_infile = 1”设置启用。<br/>
        /// 3. 请在数据库连接字符串中添加“AllowLoadLocalInfile=True”，以确保批量处理正常运行。
        /// </remarks>
        public int WriteToServer(DbConnection connection, DataTable dt, int? commandTimeout = null) => WriteToServer(connection, null, dt, commandTimeout);

        /// <inheritdoc/>
        /// <remarks>使用须知：<br/>
        /// 1. 仅支持 MySQL 数据库。<br/>
        /// 2. 请使用命令“SHOW GLOBAL VARIABLES LIKE 'local_infile'”检查变量状态是否开启，若未开启，使用“SET GLOBAL local_infile = 1”设置启用。<br/>
        /// 3. 请在数据库连接字符串中添加“AllowLoadLocalInfile=True”，以确保批量处理正常运行。
        /// </remarks>
        public int WriteToServer(DbConnection connection, DbTransaction transaction, DataTable dt, int? commandTimeout = null)
        {
            var bulkCopy = new MySqlBulkCopy((MySqlConnection)connection, (MySqlTransaction)transaction)
            {
                DestinationTableName = dt.TableName
            };

            if (commandTimeout.HasValue)
            {
                bulkCopy.BulkCopyTimeout = commandTimeout.Value;
            }

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                var column = dt.Columns[i];

                bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(i, column.ColumnName));
            }

            var result = bulkCopy.WriteToServer(dt);

            return result.RowsInserted;
        }

        /// <inheritdoc/>
        /// <remarks>使用须知：<br/>
        /// 1. 仅支持 MySQL 数据库。<br/>
        /// 2. 请使用命令“SHOW GLOBAL VARIABLES LIKE 'local_infile'”检查变量状态是否开启，若未开启，使用“SET GLOBAL local_infile = 1”设置启用。<br/>
        /// 3. 请在数据库连接字符串中添加“AllowLoadLocalInfile=True”，以确保批量处理正常运行。
        /// </remarks>
        public Task<int> WriteToServerAsync(DbConnection connection, DataTable dt, int? commandTimeout = null, CancellationToken cancellationToken = default) => WriteToServerAsync(connection, null, dt, commandTimeout, cancellationToken);

        /// <inheritdoc/>
        /// <remarks>使用须知：<br/>
        /// 1. 仅支持 MySQL 数据库。<br/>
        /// 2. 请使用命令“SHOW GLOBAL VARIABLES LIKE 'local_infile'”检查变量状态是否开启，若未开启，使用“SET GLOBAL local_infile = 1”设置启用。<br/>
        /// 3. 请在数据库连接字符串中添加“AllowLoadLocalInfile=True”，以确保批量处理正常运行。
        /// </remarks>
        public async Task<int> WriteToServerAsync(DbConnection connection, DbTransaction transaction, DataTable dt, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var bulkCopy = new MySqlBulkCopy((MySqlConnection)connection, (MySqlTransaction)transaction)
            {
                DestinationTableName = dt.TableName
            };

            if (commandTimeout.HasValue)
            {
                bulkCopy.BulkCopyTimeout = commandTimeout.Value;
            }

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                var column = dt.Columns[i];

                bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(i, column.ColumnName));
            }

            var result = await bulkCopy.WriteToServerAsync(dt, cancellationToken);

            return result.RowsInserted;
        }
    }
}
