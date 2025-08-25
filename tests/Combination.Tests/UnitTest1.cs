using System.Linq;
using Inkslab.Linq;
using Xunit;

namespace Combination.Tests
{
    public class UnitTest1
    {
        private readonly IQueryable<User> _users;
        private readonly IDatabase<SqlServerPromotionConnectionStrings> _database;

        public UnitTest1(IQueryable<User> users, IDatabase<SqlServerPromotionConnectionStrings> database)
        {
            _users = users;
            _database = database;
        }

        [Fact]
        public void Test1()
        {
            var linq = from x in _users where x.Id == 100 orderby x.DateAt, x.Name select x.Id;

            var results = linq.ToList();

            var datas = _database.Query<(long Id, string Name)>("SELECT Id, Name FROM Activity WHERE Id > @id ORDER BY CreateTime DESC OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY", new { id = 100 });
        }
    }
}