using MySqlConnector;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Linq.MySql
{
    /// <summary>
    /// MySql 批处理助手。
    /// </summary>
    public class MySqlBulkAssistant : IDatabaseBulkCopy
    {
        private readonly MySqlBulkCopy _bulkCopy;

        /// <summary>
        /// Initializes a new instance of the <see cref="MySqlBulkAssistant"/> class.
        /// </summary>
        /// <param name="bulkCopy">The MySql bulk copy.</param>
        public MySqlBulkAssistant(MySqlBulkCopy bulkCopy)
        {
            _bulkCopy = bulkCopy;
        }

        /// <inheritdoc/>
        public int BulkCopyTimeout { get => _bulkCopy.BulkCopyTimeout; set => _bulkCopy.BulkCopyTimeout = value; }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        /// <remarks>使用须知：<br/>
        /// 1. 仅支持 MySQL 数据库。<br/>
        /// 2. 请使用命令“SHOW GLOBAL VARIABLES LIKE 'local_infile'”检查变量状态是否开启，若未开启，使用“SET GLOBAL local_infile = 1”设置启用。<br/>
        /// 3. 请在数据库连接字符串中添加“AllowLoadLocalInfile=True”，以确保批量处理正常运行。
        /// </remarks>
        public int WriteToServer(DataTable dt)
        {
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                var column = dt.Columns[i];

                _bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(i, column.ColumnName));
            }

            var result = _bulkCopy.WriteToServer(dt);

            return result.RowsInserted;
        }

        /// <inheritdoc/>
        /// <remarks>使用须知：<br/>
        /// 1. 仅支持 MySQL 数据库。<br/>
        /// 2. 请使用命令“SHOW GLOBAL VARIABLES LIKE 'local_infile'”检查变量状态是否开启，若未开启，使用“SET GLOBAL local_infile = 1”设置启用。<br/>
        /// 3. 请在数据库连接字符串中添加“AllowLoadLocalInfile=True”，以确保批量处理正常运行。
        /// </remarks>
        public async Task<int> WriteToServerAsync(DataTable dt, CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                var column = dt.Columns[i];

                _bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(i, column.ColumnName));
            }

            var result = await _bulkCopy.WriteToServerAsync(dt, cancellationToken);

            return result.RowsInserted;
        }
    }
}
