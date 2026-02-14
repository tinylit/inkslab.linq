using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Linq.SqlServer
{
    /// <summary>
    /// MySql 批处理助手。
    /// </summary>
    public class SqlServerBulkAssistant : IDatabaseBulkCopy
    {
        private readonly SqlBulkCopy _bulkCopy;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerBulkAssistant"/> class.
        /// </summary>
        /// <param name="bulkCopy">The SQL Server bulk copy.</param>
        public SqlServerBulkAssistant(SqlBulkCopy bulkCopy)
        {
            _bulkCopy = bulkCopy;
        }

        /// <inheritdoc/>
        public int BulkCopyTimeout { get => _bulkCopy.BulkCopyTimeout; set => _bulkCopy.BulkCopyTimeout = value; }

        /// <inheritdoc/>
        public void Dispose()
        {
            ((IDisposable)_bulkCopy).Dispose();

            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public int WriteToServer(DataTable dt)
        {
            foreach (DataColumn column in dt.Columns)
            {
                _bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(column.ColumnName, column.ColumnName));
            }

            _bulkCopy.DestinationTableName = dt.TableName;

            _bulkCopy.WriteToServer(dt);

            return _bulkCopy.RowsCopied;
        }

        /// <inheritdoc/>
        public async Task<int> WriteToServerAsync(DataTable dt, CancellationToken cancellationToken = default)
        {
            foreach (DataColumn column in dt.Columns)
            {
                _bulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(column.ColumnName, column.ColumnName));
            }

            _bulkCopy.DestinationTableName = dt.TableName;

            await _bulkCopy.WriteToServerAsync(dt, cancellationToken);

            return _bulkCopy.RowsCopied;
        }
    }
}
