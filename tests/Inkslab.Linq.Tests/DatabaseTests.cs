using System;
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

        [Fact]
        public async Task SimpleTestAsync()
        {
            string sql = "SELECT * FROM `user` LIMIT 1";

            await _database.FirstOrDefaultAsync<User>(sql);
        }

        [Fact]
        public async Task SimpleWithArgTestAsync()
        {
            string sql = "SELECT * FROM `user` WHERE id = @id";

            await _database.FirstOrDefaultAsync<User>(sql, new { id = 1 });
        }

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

        [Fact]
        public void QueryMultipleTest()
        {
            string sql = "SELECT * FROM `user` WHERE id IN @ids LIMIT 5,10;SELECT COUNT(1) FROM `user` WHERE id IN @ids";

            using (var reader = _database.QueryMultiple(sql, new { ids = new int[] { 1, 2 } }))
            {
                reader.Read<User>();
                reader.Read<int>(RowStyle.Single);
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
        public void IDENTITY()
        {
            string sql = "INSERT INTO `user`(`name`,`date`) VALUES(@name,@now);SELECT @@IDENTITY;";

            var i = _database.Single<long?>(sql, new { name = "测试", now = DateTime.Now });
        }
    }
}
