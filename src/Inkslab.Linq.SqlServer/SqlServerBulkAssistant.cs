using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Linq.SqlServer
{
    /// <summary>
    /// MySql 批处理助手。
    /// </summary>
    public class SqlServerBulkAssistant : IBulkAssistant
    {
        /// <inheritdoc/>
        public int WriteToServer(DbConnection connection, DataTable dt, int? commandTimeout = null)
        {
            using var bulkCopy = new SqlBulkCopy((SqlConnection)connection)
            {
                DestinationTableName = dt.TableName,
                BatchSize = dt.Rows.Count
            };

            if (commandTimeout.HasValue)
            {
                bulkCopy.BulkCopyTimeout = commandTimeout.Value;
            }

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                var column = dt.Columns[i];

                bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(i, column.ColumnName));
            }

            bulkCopy.WriteToServer(dt);

            return bulkCopy.RowsCopied;
        }

        /// <inheritdoc/>
        public int WriteToServer(DbConnection connection, DbTransaction transaction, DataTable dt, int? commandTimeout = null)
        {
            using var bulkCopy = new SqlBulkCopy((SqlConnection)connection, SqlBulkCopyOptions.Default, (SqlTransaction)transaction)
            {
                DestinationTableName = dt.TableName,
                BatchSize = dt.Rows.Count
            };

            if (commandTimeout.HasValue)
            {
                bulkCopy.BulkCopyTimeout = commandTimeout.Value;
            }

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                var column = dt.Columns[i];

                bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(i, column.ColumnName));
            }

            bulkCopy.WriteToServer(dt);

            return bulkCopy.RowsCopied;
        }

        /// <inheritdoc/>
        public async Task<int> WriteToServerAsync(DbConnection connection, DataTable dt, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            using var bulkCopy = new SqlBulkCopy((SqlConnection)connection)
            {
                DestinationTableName = dt.TableName,
                BatchSize = dt.Rows.Count
            };

            if (commandTimeout.HasValue)
            {
                bulkCopy.BulkCopyTimeout = commandTimeout.Value;
            }

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                var column = dt.Columns[i];

                bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(i, column.ColumnName));
            }

            await bulkCopy.WriteToServerAsync(dt, cancellationToken);

            return bulkCopy.RowsCopied;
        }

        /// <inheritdoc/>
        public async Task<int> WriteToServerAsync(DbConnection connection, DbTransaction transaction, DataTable dt, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            using var bulkCopy = new SqlBulkCopy((SqlConnection)connection, SqlBulkCopyOptions.Default, (SqlTransaction)transaction)
            {
                DestinationTableName = dt.TableName,
                BatchSize = dt.Rows.Count
            };

            if (commandTimeout.HasValue)
            {
                bulkCopy.BulkCopyTimeout = commandTimeout.Value;
            }

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                var column = dt.Columns[i];

                bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(i, column.ColumnName));
            }

            await bulkCopy.WriteToServerAsync(dt, cancellationToken);

            return bulkCopy.RowsCopied;
        }
    }
}
