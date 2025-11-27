using System;
using System.Data;
using System.Threading.Tasks;
using Npgsql;
using Inkslab.Linq.PostgreSQL;
using Xunit;
using System.Text.Json;

namespace PostgreSQL.Tests
{
    /// <summary>
    /// PostgreSQL 批量复制使用示例
    /// </summary>
    public class PostgreSQLBulkCopyExample
    {
        private const string ConnectionString = "Host=npgsql.local.com;Database=framework;Username=root;Password=pgsql@123";

        /// <summary>
        /// 演示基本的批量数据插入功能
        /// </summary>
        [Fact]
        public static async Task BasicBulkInsertExampleAsync()
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // 创建测试表（如果不存在）
            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS users (
                    id SERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    email VARCHAR(255) UNIQUE NOT NULL,
                    age INTEGER,
                    salary DECIMAL(10,2),
                    is_active BOOLEAN DEFAULT true,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            using (var cmd = new NpgsqlCommand(createTableSql, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // 准备测试数据
            var dataTable = CreateTestDataTable();

            // 使用 PostgreSQL 批量复制
            using var bulkAssistant = new PostgreSQLBulkAssistant(connection);

            try
            {
                // 设置超时时间
                bulkAssistant.BulkCopyTimeout = 30;

                // 执行批量插入
                Console.WriteLine($"开始插入 {dataTable.Rows.Count} 行数据到表 '{dataTable.TableName}'...");

                var startTime = DateTime.Now;
                int rowsAffected = await bulkAssistant.WriteToServerAsync(dataTable);
                var duration = DateTime.Now - startTime;

                Console.WriteLine($"批量插入完成！");
                Console.WriteLine($"- 插入行数: {rowsAffected}");
                Console.WriteLine($"- 耗时: {duration.TotalMilliseconds:F2} 毫秒");
                Console.WriteLine($"- 平均速度: {rowsAffected / duration.TotalSeconds:F0} 行/秒");

                // 验证插入结果
                await VerifyInsertedDataAsync(connection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"批量插入失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 演示基本的批量数据插入功能
        /// </summary>
        [Fact]
        public static async Task BasicBulkInsertJsonAsync()
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // 创建测试表（如果不存在）
            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS user_contents (
                    id SERIAL PRIMARY KEY,
                    content        jsonb  NOT NULL
                )";

            using (var cmd = new NpgsqlCommand(createTableSql, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            var dataTable = new DataTable("user_contents");

            // 定义列结构
            dataTable.Columns.Add("content", typeof(JsonDocument));

            // 添加测试数据
            var random = new Random();
            var now = DateTime.Now;

            for (int i = 1; i <= 1000; i++)
            {
                dataTable.Rows.Add(
                    JsonDocument.Parse($@"{{
                        ""id"": {i},
                        ""name"": ""用户{i:D4}"",
                        ""age"": {random.Next(18, 65)},
                        ""salary"": {Math.Round((decimal)(random.NextDouble() * 50000 + 30000), 2)},
                        ""is_active"": {(random.Next(2) == 1).ToString().ToLower()},
                        ""created_at"": ""{now.AddMinutes(-random.Next(0, 525600)):o}""
                    }}")
                );
            }

            // 使用 PostgreSQL 批量复制
            using var bulkAssistant = new PostgreSQLBulkAssistant(connection);

            try
            {
                // 设置超时时间
                bulkAssistant.BulkCopyTimeout = 30;

                // 执行批量插入
                Console.WriteLine($"开始插入 {dataTable.Rows.Count} 行数据到表 '{dataTable.TableName}'...");

                var startTime = DateTime.Now;
                int rowsAffected = await bulkAssistant.WriteToServerAsync(dataTable);
                var duration = DateTime.Now - startTime;

                Console.WriteLine($"批量插入完成！");
                Console.WriteLine($"- 插入行数: {rowsAffected}");
                Console.WriteLine($"- 耗时: {duration.TotalMilliseconds:F2} 毫秒");
                Console.WriteLine($"- 平均速度: {rowsAffected / duration.TotalSeconds:F0} 行/秒");

                // 验证插入结果
                await VerifyInsertedDataAsync(connection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"批量插入失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 演示基本的批量数据插入功能
        /// </summary>
        [Fact]
        public static async Task BasicBulkInsertTransactionAsync()
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // 创建测试表（如果不存在）
            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS users (
                    id SERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    email VARCHAR(255) UNIQUE NOT NULL,
                    age INTEGER,
                    salary DECIMAL(10,2),
                    is_active BOOLEAN DEFAULT true,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            using (var cmd = new NpgsqlCommand(createTableSql, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // 准备测试数据
            var dataTable = CreateTestDataTable("user_tx_");

            await using var transaction = await connection.BeginTransactionAsync();

            // 使用 PostgreSQL 批量复制
            using var bulkAssistant = new PostgreSQLBulkAssistant.PostgreSQLBulk(connection, transaction);

            try
            {
                // 设置超时时间
                bulkAssistant.BulkCopyTimeout = 30;

                // 执行批量插入
                Console.WriteLine($"开始插入 {dataTable.Rows.Count} 行数据到表 '{dataTable.TableName}'...");

                var startTime = DateTime.Now;
                int rowsAffected = await bulkAssistant.WriteToServerAsync(dataTable);
                var duration = DateTime.Now - startTime;

                Console.WriteLine($"批量插入完成！");
                Console.WriteLine($"- 插入行数: {rowsAffected}");
                Console.WriteLine($"- 耗时: {duration.TotalMilliseconds:F2} 毫秒");
                Console.WriteLine($"- 平均速度: {rowsAffected / duration.TotalSeconds:F0} 行/秒");

                // 验证插入结果
                await VerifyInsertedDataAsync(connection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"批量插入失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 创建测试数据表
        /// </summary>
        private static DataTable CreateTestDataTable(string emailPrefix = "user")
        {
            var dataTable = new DataTable("users");

            // 定义列结构
            dataTable.Columns.Add("name", typeof(string));
            dataTable.Columns.Add("email", typeof(string));
            dataTable.Columns.Add("age", typeof(int));
            dataTable.Columns.Add("salary", typeof(decimal));
            dataTable.Columns.Add("is_active", typeof(bool));
            dataTable.Columns.Add("created_at", typeof(DateTime));

            // 添加测试数据
            var random = new Random();
            var now = DateTime.Now;

            for (int i = 1; i <= 1000; i++)
            {
                dataTable.Rows.Add(
                    $"用户{i:D4}",                           // name
                    $"{emailPrefix}{i:D4}.{now.Ticks}@example.com",              // email
                    random.Next(18, 65),                    // age
                    Math.Round((decimal)(random.NextDouble() * 50000 + 30000), 2), // salary
                    random.Next(2) == 1,                    // is_active
                    now.AddMinutes(-random.Next(0, 525600)) // created_at (随机过去一年内的时间)
                );
            }

            return dataTable;
        }

        /// <summary>
        /// 演示基本的批量数据插入功能
        /// </summary>
        [Fact]
        public static async Task BasicBulkInsertJsonTransactionAsync()
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // 创建测试表（如果不存在）
            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS user_contents (
                    id SERIAL PRIMARY KEY,
                    content        jsonb  NOT NULL
                )";

            using (var cmd = new NpgsqlCommand(createTableSql, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            var dataTable = new DataTable("user_contents");

            // 定义列结构
            dataTable.Columns.Add("content", typeof(JsonDocument));

            // 添加测试数据
            var random = new Random();
            var now = DateTime.Now;

            for (int i = 1; i <= 1000; i++)
            {
                dataTable.Rows.Add(
                    JsonDocument.Parse($@"{{
                        ""id"": {i},
                        ""name"": ""用户{i:D4}"",
                        ""age"": {random.Next(18, 65)},
                        ""salary"": {Math.Round((decimal)(random.NextDouble() * 50000 + 30000), 2)},
                        ""is_active"": {(random.Next(2) == 1).ToString().ToLower()},
                        ""created_at"": ""{now.AddMinutes(-random.Next(0, 525600)):o}""
                    }}")
                );
            }


            await using var transaction = await connection.BeginTransactionAsync();

            // 使用 PostgreSQL 批量复制
            using var bulkAssistant = new PostgreSQLBulkAssistant.PostgreSQLBulk(connection, transaction);

            try
            {
                // 设置超时时间
                bulkAssistant.BulkCopyTimeout = 30;

                // 执行批量插入
                Console.WriteLine($"开始插入 {dataTable.Rows.Count} 行数据到表 '{dataTable.TableName}'...");

                var startTime = DateTime.Now;
                int rowsAffected = await bulkAssistant.WriteToServerAsync(dataTable);
                var duration = DateTime.Now - startTime;

                Console.WriteLine($"批量插入完成！");
                Console.WriteLine($"- 插入行数: {rowsAffected}");
                Console.WriteLine($"- 耗时: {duration.TotalMilliseconds:F2} 毫秒");
                Console.WriteLine($"- 平均速度: {rowsAffected / duration.TotalSeconds:F0} 行/秒");

                // 验证插入结果
                await VerifyInsertedDataAsync(connection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"批量插入失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 验证插入的数据
        /// </summary>
        private static async Task VerifyInsertedDataAsync(NpgsqlConnection connection)
        {
            var verifySql = @"
                SELECT 
                    COUNT(*) as total_count,
                    COUNT(CASE WHEN is_active = true THEN 1 END) as active_count,
                    AVG(age) as avg_age,
                    AVG(salary) as avg_salary
                FROM users";

            using var cmd = new NpgsqlCommand(verifySql, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                Console.WriteLine("\n验证结果:");
                Console.WriteLine($"- 总记录数: {reader.GetInt64("total_count")}");
                Console.WriteLine($"- 活跃用户数: {reader.GetInt64("active_count")}");
                Console.WriteLine($"- 平均年龄: {reader.GetDecimal("avg_age"):F1}");
                Console.WriteLine($"- 平均薪资: {reader.GetDecimal("avg_salary"):F2}");
            }
        }

        /// <summary>
        /// 演示处理空值的情况
        /// </summary>
        [Fact]
        public static async Task NullValueHandlingExampleAsync()
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // 创建包含空值的测试数据
            var dataTable = new DataTable("users");
            dataTable.Columns.Add("name", typeof(string));
            dataTable.Columns.Add("email", typeof(string));
            dataTable.Columns.Add("age", typeof(int));
            dataTable.Columns.Add("salary", typeof(decimal));

            // 添加包含空值的数据
            dataTable.Rows.Add("张三", "zhangsan@test.com", 25, 50000m);
            dataTable.Rows.Add("李四", "lisi@test.com", DBNull.Value, 60000m);  // age 为空
            dataTable.Rows.Add("王五", "wangwu@test.com", 30, DBNull.Value);     // salary 为空
            dataTable.Rows.Add("赵六", null, 35, 70000m);                       // email 为空

            using var bulkAssistant = new PostgreSQLBulkAssistant(connection);

            try
            {
                int rowsAffected = await bulkAssistant.WriteToServerAsync(dataTable);
                Console.WriteLine($"成功插入包含空值的 {rowsAffected} 行数据");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理空值时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 演示数据库表名和列名包含特殊字符的情况
        /// </summary>
        [Fact]
        public static async Task SpecialCharactersExampleAsync()
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // 创建包含特殊字符的表
            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS ""user data"" (
                    ""user id"" SERIAL PRIMARY KEY,
                    ""full name"" VARCHAR(100),
                    ""e-mail address"" VARCHAR(255),
                    ""salary/month"" DECIMAL(10,2)
                )";

            using (var cmd = new NpgsqlCommand(createTableSql, connection))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // 创建对应的 DataTable
            var dataTable = new DataTable("user data");  // 表名包含空格
            dataTable.Columns.Add("full name", typeof(string));        // 列名包含空格
            dataTable.Columns.Add("e-mail address", typeof(string));   // 列名包含连字符
            dataTable.Columns.Add("salary/month", typeof(decimal));    // 列名包含斜杠

            // 添加测试数据
            dataTable.Rows.Add("张三", "zhangsan@company.com", 8500.50m);
            dataTable.Rows.Add("李四", "lisi@company.com", 9200.75m);

            using var bulkAssistant = new PostgreSQLBulkAssistant(connection);

            try
            {
                int rowsAffected = await bulkAssistant.WriteToServerAsync(dataTable);
                Console.WriteLine($"成功插入到包含特殊字符的表中，影响 {rowsAffected} 行");

                // 显示生成的 COPY 命令（用于调试）
                Console.WriteLine("生成的 COPY 命令示例:");
                Console.WriteLine("COPY \"user data\" (\"full name\", \"e-mail address\", \"salary/month\") FROM STDIN (FORMAT BINARY)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理特殊字符时出错: {ex.Message}");
            }
        }
    }
}