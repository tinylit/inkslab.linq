using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Linq.Tests
{
    public class DatabaseTests
    {
        private readonly IDatabase _database;

        public DatabaseTests(IDatabase database)
        {
            _database = database;
        }

        /// <summary>
        /// SQL预览:
        /// SELECT * FROM `user` LIMIT 1
        /// </summary>
        [Fact]
        public async Task SimpleTestAsync()
        {
            string sql = "SELECT * FROM `user` LIMIT 1";

            await _database.FirstOrDefaultAsync<User>(sql);
        }

        /// <summary>
        /// SQL预览:
        /// SELECT '1'
        /// </summary>
        [Fact]
        public async Task Striong2CharAsync()
        {
            string sql = "SELECT '1'";

            await _database.FirstOrDefaultAsync<char>(sql);
        }

        /// <summary>
        /// SQL预览:
        /// SELECT * FROM `user` WHERE id = @id
        /// </summary>
        [Fact]
        public async Task SimpleWithArgTestAsync()
        {
            string sql = "SELECT * FROM `user` WHERE id = @id";

            await _database.FirstOrDefaultAsync<User>(sql, new { id = 1 });
        }

        [Fact]
        public async Task SimpleWithArgTest2Async()
        {
            string sql = "SELECT * FROM `user` WHERE id = @id";

            await _database.FirstOrDefaultAsync<User>(sql, new Dictionary<string, object> { ["@id"] = 1 });
        }

        /// <summary>
        /// SQL预览:
        /// SELECT * FROM `user` WHERE id IN (@ids_0, @ids_1)
        /// </summary>
        [Fact]
        public async Task InlistArgTestAsync()
        {
            string sql = "SELECT * FROM `user` WHERE id IN @ids";

            await _database.FirstOrDefaultAsync<User>(sql, new { ids = new int[] { 1, 2 } });
        }

        [Fact]
        public async Task InlistArgIsNullTestAsync()
        {
            string sql = "SELECT * FROM `user` WHERE id IN @ids";

            await _database.FirstOrDefaultAsync<User>(sql, new { ids = (int[])null });
        }

        [Fact]
        public async Task InlistArgIsEmptyTestAsync()
        {
            string sql = "SELECT * FROM `user` WHERE id IN @ids";

            await _database.FirstOrDefaultAsync<User>(sql, new { ids = Array.Empty<int>() });
        }

        /// <summary>
        /// SQL预览:
        /// SELECT * FROM `user` WHERE id = 1
        /// 注意: {=id} 会被直接替换为字面量值
        /// </summary>
        [Fact]
        public void IiteralTokenTest()
        {
            string sql = "SELECT * FROM `user` WHERE id = {=id}";

            _database.FirstOrDefault<User>(sql, new { id = 1 });
        }

        [Fact]
        public void IiteralTokenInlistArgTest()
        {
            string sql = "SELECT * FROM `user` WHERE id IN {=ids}";

            _database.Query<User>(sql, new { ids = new int[] { 1, 2 } });
        }

        [Fact]
        public void IiteralTokenInlistArgIsNullTest()
        {
            string sql = "SELECT * FROM `user` WHERE id IN {=ids}";

            _database.Query<User>(sql, new { ids = (int[])null });
        }

        [Fact]
        public async Task IiteralTokenInlistArgIsEmptyTestAsync()
        {
            string sql = "SELECT * FROM `user` WHERE id IN {=ids}";

            await _database.QueryAsync<User>(sql, new { ids = Array.Empty<int>() });
        }

        /// <summary>
        /// SQL预览:
        /// SELECT * FROM `user` WHERE id IN (@ids_0, @ids_1) LIMIT 5,10;SELECT COUNT(1) FROM `user` WHERE id IN (@ids_0, @ids_1)
        /// </summary>
        [Fact]
        public void QueryMultipleTest()
        {
            string sql = "SELECT * FROM `user` WHERE id IN @ids LIMIT 5,10;SELECT COUNT(1) FROM `user` WHERE id IN @ids";

            using (var reader = _database.QueryMultiple(sql, new { ids = new int[] { 1, 2 } }))
            {
                reader.Read<User>();
                reader.Read<uint>(RowStyle.Single);
            }
        }

        [Fact]
        public async Task QueryMultipleTestAsync()
        {
            string sql = "SELECT * FROM `user` WHERE id IN @ids LIMIT 5,10;SELECT COUNT(1) FROM `user` WHERE id IN @ids";

            await using (var reader = await _database.QueryMultipleAsync(sql, new { ids = new int[] { 1, 2 } }))
            {
                await reader.ReadAsync<User>();
                await reader.ReadAsync<int>(RowStyle.Single);
            }
        }

        [Fact]
        public async Task QueryMultipleTest2Async()
        {
            string sql = "SELECT * FROM `user` WHERE id IN @ids LIMIT 5,10;SELECT COUNT(1) FROM `user` WHERE id IN @ids";

            await using (var reader = await _database.QueryMultipleAsync(sql, new Dictionary<string, object> { ["@ids"] = new int[] { 1, 2 } }))
            {
                await reader.ReadAsync<User>();
                await reader.ReadAsync<int>(RowStyle.Single);
            }
        }

        /// <summary>
        /// SQL预览:
        /// INSERT INTO `user`(`name`,`date`,`is_administrator`) VALUES(@name,@now,0);SELECT @@IDENTITY;
        /// </summary>
        [Fact]
        public void IDENTITY()
        {
            string sql = "INSERT INTO `user`(`name`,`date`,`is_administrator`) VALUES(@name,@now,0);SELECT @@IDENTITY;";

            var i = _database.Single<long?>(sql, new { name = "测试", now = DateTime.Now });
        }

        [Fact]
        public async Task CreateAndExecuteStoredProcedureWithOutputParametersAsync()
        {
/*             // Create stored procedure with input, output and return parameters
            string createProcSql = @"
                DROP PROCEDURE IF EXISTS GetUserInfo;
                
                DELIMITER $$
                CREATE PROCEDURE GetUserInfo(
                    IN UserId INT,
                    OUT UserName VARCHAR(50),
                    OUT UserCount INT
                )
                BEGIN
                    SELECT name INTO UserName FROM `user` WHERE id = UserId;
                    SELECT COUNT(*) INTO UserCount FROM `user`;
                    SELECT * FROM `user` WHERE id = UserId;
                END$$
                DELIMITER ;";

            await _database.ExecuteAsync(createProcSql); */

            var userName = new DynamicParameter
            {
                Direction = ParameterDirection.Output,
                DbType = DbType.String,
                Size = 50
            };

            var userCount = new DynamicParameter
            {
                Direction = ParameterDirection.Output,
                DbType = DbType.Int32
            };

            // Execute stored procedure with output parameters
            var parameters = new Dictionary<string, object>
            {
                ["@UserId"] = 100,
                ["@UserName"] = userName,
                ["@UserCount"] = userCount
            };

            var result = await _database.QueryAsync<User>("GetUserInfo", parameters);

            var userNameOutput = userName.Value;
            var userCountOutput = Convert.ToInt32(userCount.Value);

            Assert.NotNull(result);
            Assert.NotNull(userNameOutput);
            Assert.True(userCountOutput > 0);
        }
    }
}
