using System;
using System.Threading.Tasks;
using Inkslab;
using Inkslab.Linq;
using Inkslab.Transactions;
using Xunit;

namespace Combination.Tests
{
    [Serializable]
    public class DataBaseTest
    {
        private IDatabase _database;
        private readonly IDatabase<PostgreSqlPromotionConnectionStrings> _databasePostgreSql;

        public DataBaseTest(IDatabase database, IDatabase<PostgreSqlPromotionConnectionStrings> databasePostgreSql)
        {
            _database = database;
            _databasePostgreSql = databasePostgreSql;
        }

        [Fact]
        public async Task TempTableTestAsync()
        {
            await using var transaction = new TransactionUnit();

            await UseTempTableAsync();

            await transaction.CompleteAsync();
        }

        private async Task UseTempTableAsync()
        {
            var tempTableName = $"temp{KeyGen.Id()}";

            var createTempTableSql = $"CREATE TEMPORARY TABLE {tempTableName} (Id bigint);";

            await _database.ExecuteAsync(createTempTableSql);

            try
            {
                var data = await _database.QueryAsync<long>("SELECT * FROM " + tempTableName);

                await _databasePostgreSql.ExecuteAsync(createTempTableSql);

                var data2 = await _database.QueryAsync<long>("SELECT * FROM " + tempTableName);
            }
            finally
            {
                //await _database.ExecuteAsync($"DROP TEMPORARY TABLE {tempTableName};");
            }
        }
    }
}