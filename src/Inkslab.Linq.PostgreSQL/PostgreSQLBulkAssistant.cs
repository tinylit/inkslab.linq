using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System;
using Npgsql;

namespace Inkslab.Linq.PostgreSQL
{
    /// <summary>
    /// PostgreSQL 数据库连接批量复制助手。
    /// </summary>
    public class PostgreSQLBulkAssistant : IDatabaseBulkCopy
    {
        private readonly NpgsqlConnection _connection;

        private int _bulkCopyTimeout = 30;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public PostgreSQLBulkAssistant(NpgsqlConnection connection)
        {
            _connection = connection;
        }

        /// <inheritdoc/>
        public int BulkCopyTimeout
        {
            get => _bulkCopyTimeout;
            set => _bulkCopyTimeout = value;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // PostgreSQL连接由外部管理，这里不需要释放
        }

        /// <inheritdoc/>
        public int WriteToServer(DataTable dt)
        {
            if (dt == null)
            {
                throw new ArgumentNullException(nameof(dt));
            }

            if (string.IsNullOrEmpty(dt.TableName))
            {
                throw new ArgumentException("请通过DataTable.TableName指定目标表名称！");
            }

            // 生成COPY命令
            var copyCommand = GenerateCopyCommand(dt);

            try
            {
                // 开始COPY操作
                using var writer = _connection.BeginBinaryImport(copyCommand);

                writer.Timeout = TimeSpan.FromSeconds(_bulkCopyTimeout);

                // 写入每一行数据
                foreach (DataRow row in dt.Rows)
                {
                    writer.StartRow();

                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        var value = row[i];
                        var column = dt.Columns[i];

                        if (value == null || value == DBNull.Value)
                        {
                            writer.WriteNull();
                        }
                        else
                        {
                            WriteValue(writer, value, column.DataType);
                        }
                    }
                }

                // 完成并获取影响的行数
                var rowsAffected = writer.Complete();

                return (int)rowsAffected;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"PostgreSQL批量复制失败: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<int> WriteToServerAsync(DataTable dt, CancellationToken cancellationToken = default)
        {
            if (dt == null)
            {
                throw new ArgumentNullException(nameof(dt));
            }

            if (string.IsNullOrEmpty(dt.TableName))
            {
                throw new ArgumentException("请通过DataTable.TableName指定目标表名称！");
            }

            // 生成COPY命令
            var copyCommand = GenerateCopyCommand(dt);

            try
            {
                // 开始COPY操作
                await using var writer = await _connection.BeginBinaryImportAsync(copyCommand, cancellationToken);

                writer.Timeout = TimeSpan.FromSeconds(_bulkCopyTimeout);

                // 写入每一行数据
                foreach (DataRow row in dt.Rows)
                {
                    await writer.StartRowAsync(cancellationToken);

                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        var value = row[i];
                        var column = dt.Columns[i];

                        if (value == null || value == DBNull.Value)
                        {
                            await writer.WriteNullAsync(cancellationToken);
                        }
                        else
                        {
                            await WriteValueAsync(writer, value, column.DataType, cancellationToken);
                        }
                    }
                }

                // 完成并获取影响的行数
                var rowsAffected = await writer.CompleteAsync(cancellationToken);
                return (int)rowsAffected;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"PostgreSQL批量复制失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 生成PostgreSQL COPY命令
        /// </summary>
        /// <param name="dt">数据表</param>
        /// <returns>COPY命令字符串</returns>
        private static string GenerateCopyCommand(DataTable dt)
        {
            var sb = new StringBuilder();
            sb.Append($"COPY {EscapeIdentifier(dt.TableName)} (");

            // 添加列名
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(EscapeIdentifier(dt.Columns[i].ColumnName));
            }

            sb.Append(") FROM STDIN (FORMAT BINARY)");

            return sb.ToString();
        }

        /// <summary>
        /// 转义PostgreSQL标识符
        /// </summary>
        /// <param name="identifier">标识符</param>
        /// <returns>转义后的标识符</returns>
        private static string EscapeIdentifier(string identifier)
        {
            return $"\"{identifier.Replace("\"", "\"\"")}\"";
        }

        /// <summary>
        /// 写入值到二进制导入器（同步）
        /// </summary>
        /// <param name="writer">二进制导入器</param>
        /// <param name="value">值</param>
        /// <param name="dataType">数据类型</param>
        private static void WriteValue(NpgsqlBinaryImporter writer, object value, Type dataType)
        {
            if (value is JsonbPayload jsonbPayload)
            {
                writer.Write(jsonbPayload.ToString(), NpgsqlTypes.NpgsqlDbType.Jsonb);

                return;
            }

            if (value is JsonPayload jsonPayload)
            {
                writer.Write(jsonPayload.ToString(), NpgsqlTypes.NpgsqlDbType.Json);

                return;
            }

            // 根据数据类型写入相应的值，明确指定NpgsqlDbType以确保类型严格性
            switch (Type.GetTypeCode(dataType))
            {
                case TypeCode.Boolean:
                    writer.Write((bool)value, NpgsqlTypes.NpgsqlDbType.Boolean);
                    break;
                case TypeCode.Byte:
                    writer.Write((short)(byte)value, NpgsqlTypes.NpgsqlDbType.Smallint);
                    break;
                case TypeCode.SByte:
                    writer.Write((short)(sbyte)value, NpgsqlTypes.NpgsqlDbType.Smallint);
                    break;
                case TypeCode.Int16:
                    writer.Write((short)value, NpgsqlTypes.NpgsqlDbType.Smallint);
                    break;
                case TypeCode.UInt16:
                    writer.Write((int)(ushort)value, NpgsqlTypes.NpgsqlDbType.Integer);
                    break;
                case TypeCode.Int32:
                    writer.Write((int)value, NpgsqlTypes.NpgsqlDbType.Integer);
                    break;
                case TypeCode.UInt32:
                    writer.Write((long)(uint)value, NpgsqlTypes.NpgsqlDbType.Bigint);
                    break;
                case TypeCode.Int64:
                    writer.Write((long)value, NpgsqlTypes.NpgsqlDbType.Bigint);
                    break;
                case TypeCode.UInt64:
                    writer.Write((decimal)(ulong)value, NpgsqlTypes.NpgsqlDbType.Numeric);
                    break;
                case TypeCode.Single:
                    writer.Write((float)value, NpgsqlTypes.NpgsqlDbType.Real);
                    break;
                case TypeCode.Double:
                    writer.Write((double)value, NpgsqlTypes.NpgsqlDbType.Double);
                    break;
                case TypeCode.Decimal:
                    writer.Write((decimal)value, NpgsqlTypes.NpgsqlDbType.Numeric);
                    break;
                case TypeCode.DateTime:
                    writer.Write((DateTime)value, NpgsqlTypes.NpgsqlDbType.Timestamp);
                    break;
                case TypeCode.String:
                    writer.Write((string)value, NpgsqlTypes.NpgsqlDbType.Text);
                    break;
                default:
                    // 处理特殊类型
                    if (dataType == typeof(Guid))
                    {
                        writer.Write((Guid)value, NpgsqlTypes.NpgsqlDbType.Uuid);
                    }
                    else if (dataType == typeof(DateTimeOffset))
                    {
                        writer.Write((DateTimeOffset)value, NpgsqlTypes.NpgsqlDbType.TimestampTz);
                    }
                    else if (dataType == typeof(TimeSpan))
                    {
                        writer.Write((TimeSpan)value, NpgsqlTypes.NpgsqlDbType.Interval);
                    }
                    else if (dataType == typeof(byte[]))
                    {
                        writer.Write((byte[])value, NpgsqlTypes.NpgsqlDbType.Bytea);
                    }
                    else
                    {
                        // 默认转换为字符串
                        writer.Write(value.ToString(), NpgsqlTypes.NpgsqlDbType.Text);
                    }
                    break;
            }
        }

        /// <summary>
        /// 写入值到二进制导入器（异步）
        /// </summary>
        /// <param name="writer">二进制导入器</param>
        /// <param name="value">值</param>
        /// <param name="dataType">数据类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        private static async Task WriteValueAsync(NpgsqlBinaryImporter writer, object value, Type dataType, CancellationToken cancellationToken)
        {
            if (value is JsonbPayload jsonbPayload)
            {
                await writer.WriteAsync(jsonbPayload.ToString(), NpgsqlTypes.NpgsqlDbType.Jsonb, cancellationToken);

                return;
            }

            if (value is JsonPayload jsonPayload)
            {
                await writer.WriteAsync(jsonPayload.ToString(), NpgsqlTypes.NpgsqlDbType.Json, cancellationToken);

                return;
            }

            // 根据数据类型写入相应的值，明确指定NpgsqlDbType以确保类型严格性
            switch (Type.GetTypeCode(dataType))
            {
                case TypeCode.Boolean:
                    await writer.WriteAsync((bool)value, NpgsqlTypes.NpgsqlDbType.Boolean, cancellationToken);
                    break;
                case TypeCode.Byte:
                    await writer.WriteAsync((short)(byte)value, NpgsqlTypes.NpgsqlDbType.Smallint, cancellationToken);
                    break;
                case TypeCode.SByte:
                    await writer.WriteAsync((short)(sbyte)value, NpgsqlTypes.NpgsqlDbType.Smallint, cancellationToken);
                    break;
                case TypeCode.Int16:
                    await writer.WriteAsync((short)value, NpgsqlTypes.NpgsqlDbType.Smallint, cancellationToken);
                    break;
                case TypeCode.UInt16:
                    await writer.WriteAsync((int)(ushort)value, NpgsqlTypes.NpgsqlDbType.Integer, cancellationToken);
                    break;
                case TypeCode.Int32:
                    await writer.WriteAsync((int)value, NpgsqlTypes.NpgsqlDbType.Integer, cancellationToken);
                    break;
                case TypeCode.UInt32:
                    await writer.WriteAsync((long)(uint)value, NpgsqlTypes.NpgsqlDbType.Bigint, cancellationToken);
                    break;
                case TypeCode.Int64:
                    await writer.WriteAsync((long)value, NpgsqlTypes.NpgsqlDbType.Bigint, cancellationToken);
                    break;
                case TypeCode.UInt64:
                    await writer.WriteAsync((decimal)(ulong)value, NpgsqlTypes.NpgsqlDbType.Numeric, cancellationToken);
                    break;
                case TypeCode.Single:
                    await writer.WriteAsync((float)value, NpgsqlTypes.NpgsqlDbType.Real, cancellationToken);
                    break;
                case TypeCode.Double:
                    await writer.WriteAsync((double)value, NpgsqlTypes.NpgsqlDbType.Double, cancellationToken);
                    break;
                case TypeCode.Decimal:
                    await writer.WriteAsync((decimal)value, NpgsqlTypes.NpgsqlDbType.Numeric, cancellationToken);
                    break;
                case TypeCode.DateTime:
                    await writer.WriteAsync((DateTime)value, NpgsqlTypes.NpgsqlDbType.Timestamp, cancellationToken);
                    break;
                case TypeCode.String:
                    await writer.WriteAsync((string)value, NpgsqlTypes.NpgsqlDbType.Text, cancellationToken);
                    break;
                default:
                    // 处理特殊类型
                    if (dataType == typeof(Guid))
                    {
                        await writer.WriteAsync((Guid)value, NpgsqlTypes.NpgsqlDbType.Uuid, cancellationToken);
                    }
                    else if (dataType == typeof(DateTimeOffset))
                    {
                        await writer.WriteAsync((DateTimeOffset)value, NpgsqlTypes.NpgsqlDbType.TimestampTz, cancellationToken);
                    }
                    else if (dataType == typeof(TimeSpan))
                    {
                        await writer.WriteAsync((TimeSpan)value, NpgsqlTypes.NpgsqlDbType.Interval, cancellationToken);
                    }
                    else if (dataType == typeof(byte[]))
                    {
                        await writer.WriteAsync((byte[])value, NpgsqlTypes.NpgsqlDbType.Bytea, cancellationToken);
                    }
                    else
                    {
                        // 默认转换为字符串
                        await writer.WriteAsync(value.ToString(), NpgsqlTypes.NpgsqlDbType.Text, cancellationToken);
                    }
                    break;
            }
        }

        /// <summary>
        /// PostgreSQL 批量复制配置。
        /// </summary>
        public class PostgreSQLBulk : IDatabaseBulkCopy
        {
            private readonly NpgsqlConnection _connection;
            private readonly NpgsqlTransaction _transaction;

            /// <summary>
            /// 构造函数。
            /// </summary>
            public PostgreSQLBulk(NpgsqlConnection connection, NpgsqlTransaction transaction)
            {
                _connection = connection;
                _transaction = transaction;
            }

            private int _bulkCopyTimeout = 30;

            /// <inheritdoc/>
            public int BulkCopyTimeout
            {
                get => _bulkCopyTimeout;
                set => _bulkCopyTimeout = value;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                // PostgreSQL连接和事务由外部管理，这里不需要释放
            }

            /// <inheritdoc/>
            public int WriteToServer(DataTable dt)
            {
                if (dt == null)
                {
                    throw new ArgumentNullException(nameof(dt));
                }

                if (string.IsNullOrEmpty(dt.TableName))
                {
                    throw new ArgumentException("请通过DataTable.TableName指定目标表名称！");
                }

                if (dt.Rows.Count == 0)
                {
                    return 0;
                }

                // 计算批次大小：2000 / 列数
                int batchSize = Math.Max(1, 2000 / dt.Columns.Count);
                int totalRows = dt.Rows.Count;
                int totalAffected = 0;

                try
                {
                    // 按批次处理数据
                    for (int offset = 0; offset < totalRows; offset += batchSize)
                    {
                        int currentBatchSize = Math.Min(batchSize, totalRows - offset);

                        // 生成批量INSERT语句（带参数）
                        using var command = _connection.CreateCommand();

                        command.Transaction = _transaction;
                        command.CommandTimeout = _bulkCopyTimeout;

                        GenerateBatchInsertCommand(command, dt, offset, currentBatchSize);

                        int affected = command.ExecuteNonQuery();

                        totalAffected += affected;
                    }

                    return totalAffected;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"PostgreSQL批量复制失败: {ex.Message}", ex);
                }
            }

            /// <inheritdoc/>
            public async Task<int> WriteToServerAsync(DataTable dt, CancellationToken cancellationToken = default)
            {
                if (dt == null)
                {
                    throw new ArgumentNullException(nameof(dt));
                }

                if (string.IsNullOrEmpty(dt.TableName))
                {
                    throw new ArgumentException("请通过DataTable.TableName指定目标表名称！");
                }

                if (dt.Rows.Count == 0)
                {
                    return 0;
                }

                // 计算批次大小：2000 / 列数
                int batchSize = Math.Max(1, 2000 / dt.Columns.Count);
                int totalRows = dt.Rows.Count;
                int totalAffected = 0;

                try
                {
                    // 按批次处理数据
                    for (int offset = 0; offset < totalRows; offset += batchSize)
                    {
                        int currentBatchSize = Math.Min(batchSize, totalRows - offset);

                        // 生成批量INSERT语句（带参数）
                        using var command = _connection.CreateCommand();

                        command.Transaction = _transaction;
                        command.CommandTimeout = _bulkCopyTimeout;

                        GenerateBatchInsertCommand(command, dt, offset, currentBatchSize);

                        int affected = await command.ExecuteNonQueryAsync(cancellationToken);
                        totalAffected += affected;
                    }

                    return totalAffected;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"PostgreSQL批量复制失败: {ex.Message}", ex);
                }
            }

            /// <summary>
            /// 生成参数化批量INSERT命令
            /// </summary>
            /// <param name="command">数据库命令对象</param>
            /// <param name="dt">数据表</param>
            /// <param name="offset">起始行偏移量</param>
            /// <param name="batchSize">批次大小</param>
            private static void GenerateBatchInsertCommand(NpgsqlCommand command, DataTable dt, int offset, int batchSize)
            {
                var sb = new StringBuilder();
                int paramIndex = 0;

                // INSERT INTO table_name (col1, col2, ...) VALUES
                sb.Append("INSERT INTO ");
                sb.Append(EscapeIdentifier(dt.TableName));
                sb.Append(" (");

                // 添加列名
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(EscapeIdentifier(dt.Columns[i].ColumnName));
                }

                sb.Append(") VALUES ");

                // 添加每一行的值（使用参数化）
                for (int rowIndex = 0; rowIndex < batchSize; rowIndex++)
                {
                    if (rowIndex > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append('(');

                    var row = dt.Rows[offset + rowIndex];

                    for (int colIndex = 0; colIndex < dt.Columns.Count; colIndex++)
                    {
                        if (colIndex > 0)
                        {
                            sb.Append(", ");
                        }

                        var value = row[colIndex];
                        var column = dt.Columns[colIndex];

                        if (value == null || value == DBNull.Value)
                        {
                            sb.Append("NULL");
                        }
                        else if (column.DataType.IsMini())
                        {
                            // 基元类型直接内联（安全）
                            sb.Append(FormatSimpleValue(value, column.DataType));
                        }
                        else
                        {
                            // 非基元类型使用参数化（防止注入）
                            string paramName = $"p{paramIndex++}";

                            sb.Append('@')
                                .Append(paramName);

                            if (value is JsonbPayload jsonbPayload)
                            {
                                sb.Append("::jsonb");

                                LookupDb.AddParameterAuto(command, DatabaseEngine.PostgreSQL, paramName, jsonbPayload.ToString());
                            }
                            else if (value is JsonPayload jsonPayload)
                            {
                                sb.Append("::json");

                                LookupDb.AddParameterAuto(command, DatabaseEngine.PostgreSQL, paramName, jsonPayload.ToString());
                            }
                            else
                            {
                                // 普通参数
                                LookupDb.AddParameterAuto(command, DatabaseEngine.PostgreSQL, paramName, value);
                            }
                        }
                    }

                    sb.Append(')');
                }

                command.CommandText = sb.ToString();
            }


            /// <summary>
            /// 格式化简单类型值为SQL字符串（仅用于数值和布尔类型）
            /// </summary>
            /// <param name="value">值</param>
            /// <param name="dataType">数据类型</param>
            /// <returns>格式化后的SQL字符串</returns>
            private static string FormatSimpleValue(object value, Type dataType)
            {
                switch (Type.GetTypeCode(dataType))
                {
                    case TypeCode.Boolean:
                        return (bool)value ? "TRUE" : "FALSE";

                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        return value.ToString();

                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        return Convert.ToDecimal(value).ToString(System.Globalization.CultureInfo.InvariantCulture);

                    default:
                        throw new ArgumentException($"类型 {dataType.Name} 不是简单类型");
                }
            }

            /// <summary>
            /// 转义PostgreSQL标识符
            /// </summary>
            /// <param name="identifier">标识符</param>
            /// <returns>转义后的标识符</returns>
            private static string EscapeIdentifier(string identifier)
            {
                return $"\"{identifier.Replace("\"", "\"\"")}\"";
            }
        }
    }
}