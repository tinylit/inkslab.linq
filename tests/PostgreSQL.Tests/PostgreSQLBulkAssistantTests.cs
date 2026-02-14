using System;
using System.Data;
using System.Threading.Tasks;
using Npgsql;
using Inkslab.Linq.PostgreSQL;
using Xunit;

namespace PostgreSQL.Tests
{
    /// <summary>
    /// PostgreSQL 批量复制助手测试类。
    /// 测试列顺序打乱后，数据是否能正确写入数据库。
    /// </summary>
    public class PostgreSQLBulkAssistantTests
    {
        private const string ConnectionString = "Host=npgsql.local.com;Database=framework;Username=root;Password=pgsql@123";

        /// <summary>
        /// 测试打乱列顺序后批量写入是否正确（同步方法）。
        /// </summary>
        [Fact]
        public void WriteToServer_WithShuffledColumns_DataMatchesExpected()
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            // 1. 创建测试表
            var tableName = $"bulk_test_{DateTime.Now:yyyyMMddHHmmssfff}";
            CreateTestTable(connection, tableName);

            try
            {
                // 2. 创建打乱列顺序的 DataTable（数据库表顺序: id, name, age, email, amount）
                // DataTable 列顺序: email, amount, name, id, age
                var dataTable = CreateShuffledDataTable(tableName);

                // 3. 执行批量写入
                using var bulkAssistant = new PostgreSQLBulkAssistant(connection);
                bulkAssistant.BulkCopyTimeout = 30;

                int rowsAffected = bulkAssistant.WriteToServer(dataTable);

                Assert.Equal(3, rowsAffected);

                // 4. 查询数据并验证
                VerifyData(connection, tableName);
            }
            finally
            {
                // 5. 清理测试表
                DropTestTable(connection, tableName);
            }
        }

        /// <summary>
        /// 测试打乱列顺序后批量写入是否正确（异步方法）。
        /// </summary>
        [Fact]
        public async Task WriteToServerAsync_WithShuffledColumns_DataMatchesExpectedAsync()
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // 1. 创建测试表
            var tableName = $"bulk_test_{DateTime.Now:yyyyMMddHHmmssfff}";
            CreateTestTable(connection, tableName);

            try
            {
                // 2. 创建打乱列顺序的 DataTable（数据库表顺序: id, name, age, email, amount）
                // DataTable 列顺序: email, amount, name, id, age
                var dataTable = CreateShuffledDataTable(tableName);

                // 3. 执行批量写入
                using var bulkAssistant = new PostgreSQLBulkAssistant(connection);
                bulkAssistant.BulkCopyTimeout = 30;

                int rowsAffected = await bulkAssistant.WriteToServerAsync(dataTable);

                Assert.Equal(3, rowsAffected);

                // 4. 查询数据并验证
                VerifyData(connection, tableName);
            }
            finally
            {
                // 5. 清理测试表
                DropTestTable(connection, tableName);
            }
        }

        /// <summary>
        /// 测试列顺序完全相反的情况。
        /// </summary>
        [Fact]
        public async Task WriteToServerAsync_WithReversedColumns_DataMatchesExpectedAsync()
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var tableName = $"bulk_test_rev_{DateTime.Now:yyyyMMddHHmmssfff}";
            CreateTestTable(connection, tableName);

            try
            {
                // 创建列顺序完全相反的 DataTable
                // 数据库表顺序: id, name, age, email, amount
                // DataTable 顺序: amount, email, age, name, id
                var dataTable = CreateReversedDataTable(tableName);

                using var bulkAssistant = new PostgreSQLBulkAssistant(connection);

                int rowsAffected = await bulkAssistant.WriteToServerAsync(dataTable);

                Assert.Equal(3, rowsAffected);
                VerifyData(connection, tableName);
            }
            finally
            {
                DropTestTable(connection, tableName);
            }
        }

        /// <summary>
        /// 测试仅部分列的批量写入。
        /// </summary>
        [Fact]
        public async Task WriteToServerAsync_WithPartialColumns_DataMatchesExpectedAsync()
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var tableName = $"bulk_test_partial_{DateTime.Now:yyyyMMddHHmmssfff}";
            CreateTestTableWithDefaults(connection, tableName);

            try
            {
                // 只写入部分列（id, name），其他列使用默认值
                var dataTable = CreatePartialDataTable(tableName);

                using var bulkAssistant = new PostgreSQLBulkAssistant(connection);

                int rowsAffected = await bulkAssistant.WriteToServerAsync(dataTable);

                Assert.Equal(2, rowsAffected);
                VerifyPartialData(connection, tableName);
            }
            finally
            {
                DropTestTable(connection, tableName);
            }
        }

        /// <summary>
        /// 测试当数据库表有可空字段但 DataTable 中不包含该列时的批量写入。
        /// </summary>
        [Fact]
        public async Task WriteToServerAsync_WithNullableColumnNotInDataTable_DataMatchesExpectedAsync()
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var tableName = $"bulk_test_nullable_{DateTime.Now:yyyyMMddHHmmssfff}";
            CreateTestTableWithNullableColumns(connection, tableName);

            try
            {
                // DataTable 不包含可空字段 remark 和 score
                // 数据库表有: id, name, age, remark(nullable), score(nullable)
                // DataTable 只有: name, id, age（打乱顺序）
                var dataTable = CreateDataTableWithoutNullableColumns(tableName);

                using var bulkAssistant = new PostgreSQLBulkAssistant(connection);

                int rowsAffected = await bulkAssistant.WriteToServerAsync(dataTable);

                Assert.Equal(3, rowsAffected);
                VerifyDataWithNullableColumns(connection, tableName);
            }
            finally
            {
                DropTestTable(connection, tableName);
            }
        }

        #region Helper Methods

        private static void CreateTestTable(NpgsqlConnection connection, string tableName)
        {
            var sql = $@"
                CREATE TABLE ""{tableName}"" (
                    ""id"" INT NOT NULL,
                    ""name"" VARCHAR(100) NOT NULL,
                    ""age"" INT NOT NULL,
                    ""email"" VARCHAR(255) NOT NULL,
                    ""amount"" DECIMAL(10,2) NOT NULL,
                    PRIMARY KEY (""id"")
                )";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.ExecuteNonQuery();
        }

        private static void CreateTestTableWithDefaults(NpgsqlConnection connection, string tableName)
        {
            var sql = $@"
                CREATE TABLE ""{tableName}"" (
                    ""id"" INT NOT NULL,
                    ""name"" VARCHAR(100) NOT NULL,
                    ""age"" INT DEFAULT 0,
                    ""email"" VARCHAR(255) DEFAULT '',
                    ""amount"" DECIMAL(10,2) DEFAULT 0.00,
                    PRIMARY KEY (""id"")
                )";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.ExecuteNonQuery();
        }

        private static void CreateTestTableWithNullableColumns(NpgsqlConnection connection, string tableName)
        {
            var sql = $@"
                CREATE TABLE ""{tableName}"" (
                    ""id"" INT NOT NULL,
                    ""name"" VARCHAR(100) NOT NULL,
                    ""age"" INT NOT NULL,
                    ""remark"" VARCHAR(500) NULL,
                    ""score"" DECIMAL(5,2) NULL,
                    PRIMARY KEY (""id"")
                )";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.ExecuteNonQuery();
        }

        private static void DropTestTable(NpgsqlConnection connection, string tableName)
        {
            var sql = $@"DROP TABLE IF EXISTS ""{tableName}""";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 创建打乱列顺序的 DataTable。
        /// 数据库表顺序: id, name, age, email, amount
        /// DataTable 顺序: email, amount, name, id, age
        /// </summary>
        private static DataTable CreateShuffledDataTable(string tableName)
        {
            var dt = new DataTable(tableName);

            // 按打乱的顺序添加列
            dt.Columns.Add("email", typeof(string));
            dt.Columns.Add("amount", typeof(decimal));
            dt.Columns.Add("name", typeof(string));
            dt.Columns.Add("id", typeof(int));
            dt.Columns.Add("age", typeof(int));

            // 添加测试数据（按 DataTable 的列顺序）
            dt.Rows.Add("alice@test.com", 1000.50m, "Alice", 1, 25);
            dt.Rows.Add("bob@test.com", 2000.75m, "Bob", 2, 30);
            dt.Rows.Add("charlie@test.com", 3000.00m, "Charlie", 3, 35);

            return dt;
        }

        /// <summary>
        /// 创建列顺序完全相反的 DataTable。
        /// 数据库表顺序: id, name, age, email, amount
        /// DataTable 顺序: amount, email, age, name, id
        /// </summary>
        private static DataTable CreateReversedDataTable(string tableName)
        {
            var dt = new DataTable(tableName);

            // 按相反顺序添加列
            dt.Columns.Add("amount", typeof(decimal));
            dt.Columns.Add("email", typeof(string));
            dt.Columns.Add("age", typeof(int));
            dt.Columns.Add("name", typeof(string));
            dt.Columns.Add("id", typeof(int));

            // 添加测试数据（按 DataTable 的列顺序）
            dt.Rows.Add(1000.50m, "alice@test.com", 25, "Alice", 1);
            dt.Rows.Add(2000.75m, "bob@test.com", 30, "Bob", 2);
            dt.Rows.Add(3000.00m, "charlie@test.com", 35, "Charlie", 3);

            return dt;
        }

        /// <summary>
        /// 创建只包含部分列的 DataTable。
        /// </summary>
        private static DataTable CreatePartialDataTable(string tableName)
        {
            var dt = new DataTable(tableName);

            // 只添加 id 和 name 列
            dt.Columns.Add("name", typeof(string));
            dt.Columns.Add("id", typeof(int));

            dt.Rows.Add("Alice", 1);
            dt.Rows.Add("Bob", 2);

            return dt;
        }

        /// <summary>
        /// 创建不包含可空字段的 DataTable。
        /// 数据库表有: id, name, age, remark(nullable), score(nullable)
        /// DataTable 只有: name, id, age（打乱顺序，不包含 remark 和 score）
        /// </summary>
        private static DataTable CreateDataTableWithoutNullableColumns(string tableName)
        {
            var dt = new DataTable(tableName);

            // 打乱顺序，且不包含可空字段
            dt.Columns.Add("name", typeof(string));
            dt.Columns.Add("id", typeof(int));
            dt.Columns.Add("age", typeof(int));

            dt.Rows.Add("Alice", 1, 25);
            dt.Rows.Add("Bob", 2, 30);
            dt.Rows.Add("Charlie", 3, 35);

            return dt;
        }

        /// <summary>
        /// 验证数据是否正确写入（完整列）。
        /// </summary>
        private static void VerifyData(NpgsqlConnection connection, string tableName)
        {
            var sql = $@"SELECT ""id"", ""name"", ""age"", ""email"", ""amount"" FROM ""{tableName}"" ORDER BY ""id""";
            using var cmd = new NpgsqlCommand(sql, connection);
            using var reader = cmd.ExecuteReader();

            // 验证第一行
            Assert.True(reader.Read());
            Assert.Equal(1, reader.GetInt32(reader.GetOrdinal("id")));
            Assert.Equal("Alice", reader.GetString(reader.GetOrdinal("name")));
            Assert.Equal(25, reader.GetInt32(reader.GetOrdinal("age")));
            Assert.Equal("alice@test.com", reader.GetString(reader.GetOrdinal("email")));
            Assert.Equal(1000.50m, reader.GetDecimal(reader.GetOrdinal("amount")));

            // 验证第二行
            Assert.True(reader.Read());
            Assert.Equal(2, reader.GetInt32(reader.GetOrdinal("id")));
            Assert.Equal("Bob", reader.GetString(reader.GetOrdinal("name")));
            Assert.Equal(30, reader.GetInt32(reader.GetOrdinal("age")));
            Assert.Equal("bob@test.com", reader.GetString(reader.GetOrdinal("email")));
            Assert.Equal(2000.75m, reader.GetDecimal(reader.GetOrdinal("amount")));

            // 验证第三行
            Assert.True(reader.Read());
            Assert.Equal(3, reader.GetInt32(reader.GetOrdinal("id")));
            Assert.Equal("Charlie", reader.GetString(reader.GetOrdinal("name")));
            Assert.Equal(35, reader.GetInt32(reader.GetOrdinal("age")));
            Assert.Equal("charlie@test.com", reader.GetString(reader.GetOrdinal("email")));
            Assert.Equal(3000.00m, reader.GetDecimal(reader.GetOrdinal("amount")));

            Assert.False(reader.Read());
        }

        /// <summary>
        /// 验证部分列数据是否正确写入。
        /// </summary>
        private static void VerifyPartialData(NpgsqlConnection connection, string tableName)
        {
            var sql = $@"SELECT ""id"", ""name"", ""age"", ""email"", ""amount"" FROM ""{tableName}"" ORDER BY ""id""";
            using var cmd = new NpgsqlCommand(sql, connection);
            using var reader = cmd.ExecuteReader();

            // 验证第一行
            Assert.True(reader.Read());
            Assert.Equal(1, reader.GetInt32(reader.GetOrdinal("id")));
            Assert.Equal("Alice", reader.GetString(reader.GetOrdinal("name")));
            Assert.Equal(0, reader.GetInt32(reader.GetOrdinal("age"))); // 默认值
            Assert.Equal("", reader.GetString(reader.GetOrdinal("email"))); // 默认值
            Assert.Equal(0.00m, reader.GetDecimal(reader.GetOrdinal("amount"))); // 默认值

            // 验证第二行
            Assert.True(reader.Read());
            Assert.Equal(2, reader.GetInt32(reader.GetOrdinal("id")));
            Assert.Equal("Bob", reader.GetString(reader.GetOrdinal("name")));

            Assert.False(reader.Read());
        }

        /// <summary>
        /// 验证包含可空字段的数据是否正确写入（可空字段应为 NULL）。
        /// </summary>
        private static void VerifyDataWithNullableColumns(NpgsqlConnection connection, string tableName)
        {
            var sql = $@"SELECT ""id"", ""name"", ""age"", ""remark"", ""score"" FROM ""{tableName}"" ORDER BY ""id""";
            using var cmd = new NpgsqlCommand(sql, connection);
            using var reader = cmd.ExecuteReader();

            // 验证第一行
            Assert.True(reader.Read());
            Assert.Equal(1, reader.GetInt32(reader.GetOrdinal("id")));
            Assert.Equal("Alice", reader.GetString(reader.GetOrdinal("name")));
            Assert.Equal(25, reader.GetInt32(reader.GetOrdinal("age")));
            Assert.True(reader.IsDBNull(reader.GetOrdinal("remark"))); // 应为 NULL
            Assert.True(reader.IsDBNull(reader.GetOrdinal("score")));  // 应为 NULL

            // 验证第二行
            Assert.True(reader.Read());
            Assert.Equal(2, reader.GetInt32(reader.GetOrdinal("id")));
            Assert.Equal("Bob", reader.GetString(reader.GetOrdinal("name")));
            Assert.Equal(30, reader.GetInt32(reader.GetOrdinal("age")));
            Assert.True(reader.IsDBNull(reader.GetOrdinal("remark")));
            Assert.True(reader.IsDBNull(reader.GetOrdinal("score")));

            // 验证第三行
            Assert.True(reader.Read());
            Assert.Equal(3, reader.GetInt32(reader.GetOrdinal("id")));
            Assert.Equal("Charlie", reader.GetString(reader.GetOrdinal("name")));
            Assert.Equal(35, reader.GetInt32(reader.GetOrdinal("age")));
            Assert.True(reader.IsDBNull(reader.GetOrdinal("remark")));
            Assert.True(reader.IsDBNull(reader.GetOrdinal("score")));

            Assert.False(reader.Read());
        }

        #endregion
    }
}
