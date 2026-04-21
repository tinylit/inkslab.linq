using System;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// 数据库工厂测试。
    /// </summary>
    public class DatabaseFactoryTests
    {
        private readonly IDatabaseFactory _factory;

        public DatabaseFactoryTests(IDatabaseFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public void RegisteredEngines_ShouldContainMySql()
        {
            Assert.Contains(DatabaseEngine.MySQL, _factory.RegisteredEngines);
        }

        [Fact]
        public void Create_WithEmptyConnectionString_ShouldThrow()
        {
            Assert.Throws<ArgumentException>(() => _factory.Create(DatabaseEngine.MySQL, null));
            Assert.Throws<ArgumentException>(() => _factory.Create(DatabaseEngine.MySQL, string.Empty));
        }

        [Fact]
        public void Create_WithUnregisteredEngine_ShouldThrow()
        {
            Assert.Throws<InvalidOperationException>(
                () => _factory.Create(DatabaseEngine.Oracle, "fake=conn"));
        }

        [Fact]
        public void Create_ShouldReturnIndependentSessions()
        {
            var a = _factory.Create(DatabaseEngine.MySQL, "server=a;database=x;");
            var b = _factory.Create(DatabaseEngine.MySQL, "server=b;database=y;");

            Assert.NotNull(a);
            Assert.NotNull(b);
            Assert.NotSame(a, b);
            Assert.IsAssignableFrom<IDatabase>(a);
        }

        /// <summary>
        /// 集成测试：按引擎 + 连接字符串访问 MySQL。
        /// </summary>
        [Fact]
        public async Task IntegrationAsync_FirstOrDefaultAsync()
        {
            const string connStr = "server=mysql.local.com;uid=root;pwd=yyy@123*;database=framework;AllowLoadLocalInfile=true;Charset=utf8mb4;";

            var db = _factory.Create(DatabaseEngine.MySQL, connStr);

            var user = await db.FirstOrDefaultAsync<User>("SELECT * FROM `user` LIMIT 1");

            Assert.True(user is null || user.Id >= 0);
        }
    }
}
