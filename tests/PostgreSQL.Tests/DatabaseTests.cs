using System;
using System.Data;
using System.Threading.Tasks;
using Inkslab.Linq;
using Xunit;

namespace PostgreSQL.Tests
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
            var utc = DateTime.Now.ToUniversalTime();

            var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);

            string sql = "SELECT * FROM \"user_contents\" LIMIT 1";

            var result = await _database.FirstOrDefaultAsync<UserContentsOfJsonbPayload>(sql);
        }

        [Fact]
        public async Task SimpleWithArgTestAsync()
        {
            string sql = "SELECT * FROM \"user_contents\" WHERE id = @id";

            var result = await _database.FirstOrDefaultAsync<UserContentsOfJsonbPayload>(sql, new { id = 1 });
        }

        [Fact]
        public void IDENTITY()
        {
            string sql = "INSERT INTO \"user_contents\"(\"content\") VALUES(@content::jsonb) RETURNING id;";

            var i = _database.Single<long?>(sql, new { content = "{\"name\":\"测试\"}" });
        }

        [Fact]
        public void Insert()
        {
            string sql = "INSERT INTO \"user_contents\"(\"content\") VALUES(@content) RETURNING id;";

            var i = _database.Single<long?>(sql, new
            {
                content = new DynamicParameter
                {
                    Value = "{\"name\":\"测试2\"}",
                    DbType = LookupDb.JsonbDbType,
                    Direction = ParameterDirection.Input
                }
            });
        }
    }
}
