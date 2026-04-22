using System;
using System.Linq;
using Inkslab.Linq.MySql;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// <see cref="LinqAnalyzer"/> 单元测试。
    /// </summary>
    public class LinqAnalyzerTests
    {
        private static IQueryable<User> CreateMySqlUsers()
            => LinqAnalyzer.From<User>(DatabaseEngine.MySQL, new MySqlAdapter());

        [Fact]
        public void From_ReturnsQueryable()
        {
            var users = CreateMySqlUsers();

            Assert.NotNull(users);
            Assert.Equal(typeof(User), users.ElementType);
            Assert.NotNull(users.Provider);
            Assert.NotNull(users.Expression);
        }

        [Fact]
        public void From_WithNullAdapter_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => LinqAnalyzer.From<User>(DatabaseEngine.MySQL, null));

            Assert.Throws<ArgumentNullException>(
                () => LinqAnalyzer.From<User>((DbStrictAdapter)null));
        }

        [Fact]
        public void ToSql_SimpleWhere_ReturnsExpectedSql()
        {
            var cmd = CreateMySqlUsers()
                .Where(x => x.Id == 100)
                .ToSql();

            Assert.NotNull(cmd);
            Assert.False(string.IsNullOrWhiteSpace(cmd.Text));
            Assert.Contains("`user`", cmd.Text);
            Assert.Contains("`id`", cmd.Text);
            Assert.Contains("100", cmd.Text);
            Assert.NotNull(cmd.Parameters);
        }

        [Fact]
        public void ToSql_ProjectionAndOrdering_ProducesExpectedSql()
        {
            var query = from x in CreateMySqlUsers()
                        where x.Id == 100
                        orderby x.DateAt, x.Name
                        select x.Id;

            var cmd = query.ToSql();

            Assert.Contains("SELECT", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ORDER BY", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("100", cmd.Text);
        }

        [Fact]
        public void ToSql_ParameterizedVariable_AppearsInParameters()
        {
            int id = 100;

            var cmd = CreateMySqlUsers()
                .Where(x => x.Id == id)
                .ToSql();

            Assert.Contains(cmd.Parameters.Values, v => Equals(v, 100));
        }

        [Fact]
        public void ToSql_TakeWithOrdering_ProducesLimit()
        {
            var cmd = CreateMySqlUsers()
                .Where(x => x.Id == 1)
                .OrderBy(x => x.Id)
                .Take(1)
                .ToSql();

            Assert.False(string.IsNullOrWhiteSpace(cmd.Text));
            Assert.Contains("1", cmd.Text);
        }

        [Fact]
        public void ToSql_WithNullSource_Throws()
        {
            IQueryable<User> source = null;

            Assert.Throws<ArgumentNullException>(() => source.ToSql());
        }

        [Fact]
        public void ToSql_ForeignQueryable_Throws()
        {
            IQueryable<User> foreign = Enumerable.Empty<User>().AsQueryable();

            Assert.Throws<NotSupportedException>(() => foreign.ToSql());
        }

        [Fact]
        public void Enumerate_Throws_AnalyzeOnly()
        {
            var users = CreateMySqlUsers();

            Assert.Throws<NotSupportedException>(() => users.GetEnumerator());
        }

        [Fact]
        public void Execute_ViaProvider_Throws()
        {
            var users = CreateMySqlUsers();

            Assert.Throws<NotSupportedException>(() => users.Count());
        }

        [Fact]
        public void ToSql_ReuseAdapter_ProducesIndependentCommands()
        {
            var adapter = new DbStrictAdapter(DatabaseEngine.MySQL, new MySqlAdapter());

            int a = 1;
            int b = 2;

            var cmd1 = LinqAnalyzer.From<User>(adapter).Where(x => x.Id == a).ToSql();
            var cmd2 = LinqAnalyzer.From<User>(adapter).Where(x => x.Id == b).ToSql();

            Assert.Contains(cmd1.Parameters.Values, v => Equals(v, 1));
            Assert.Contains(cmd2.Parameters.Values, v => Equals(v, 2));
        }

        #region ToSqlString

        [Fact]
        public void ToSqlString_InlinesParameters()
        {
            int id = 100;

            string sql = CreateMySqlUsers()
                .Where(x => x.Id == id)
                .ToSqlString();

            Assert.False(string.IsNullOrWhiteSpace(sql));
            Assert.Contains("`user`", sql);
            Assert.Contains("100", sql);
            Assert.DoesNotContain("?__", sql);
            Assert.DoesNotContain("@__", sql);
        }

        [Fact]
        public void ToSqlString_EqualsCommandSqlToString()
        {
            var query = CreateMySqlUsers().Where(x => x.Id == 42);

            string fromString = query.ToSqlString();
            string fromCommand = query.ToSql().ToString();

            Assert.Equal(fromCommand, fromString);
        }

        [Fact]
        public void ToSqlString_NullSource_Throws()
        {
            IQueryable<User> source = null;

            Assert.Throws<ArgumentNullException>(() => source.ToSqlString());
        }

        [Fact]
        public void ToSqlString_ForeignQueryable_Throws()
        {
            IQueryable<User> foreign = Enumerable.Empty<User>().AsQueryable();

            Assert.Throws<NotSupportedException>(() => foreign.ToSqlString());
        }

        #endregion

        #region ToSql with terminal operations

        [Fact]
        public void ToSql_WithCountTerminal_ProducesCountSql()
        {
            var cmd = CreateMySqlUsers().ToSql(q => q.Where(x => x.Id > 0).Count());

            Assert.NotNull(cmd);
            Assert.Contains("COUNT", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("`user`", cmd.Text);
        }

        [Fact]
        public void ToSql_WithSumViaGroupBy_ProducesSumSql()
        {
            var cmd = CreateMySqlUsers()
                .GroupBy(x => x.IsAdministrator)
                .Select(g => g.Sum(x => x.Id))
                .ToSql();

            Assert.NotNull(cmd);
            Assert.Contains("SUM", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("`id`", cmd.Text);
        }

        [Fact]
        public void ToSql_WithMinMaxViaGroupBy_ProducesAggregation()
        {
            var minCmd = CreateMySqlUsers()
                .GroupBy(x => x.IsAdministrator)
                .Select(g => g.Min(x => x.Id))
                .ToSql();
            var maxCmd = CreateMySqlUsers()
                .GroupBy(x => x.IsAdministrator)
                .Select(g => g.Max(x => x.Id))
                .ToSql();

            Assert.Contains("MIN", minCmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("MAX", maxCmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ToSql_WithFirstTerminal_ProducesLimitedSql()
        {
            var cmd = CreateMySqlUsers()
                .ToSql(q => q.OrderBy(x => x.Id).First());

            Assert.NotNull(cmd);
            Assert.Contains("SELECT", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("`user`", cmd.Text);
        }

        [Fact]
        public void ToSql_WithFirstOrDefaultOrdered_ProducesSql()
        {
            var cmd = CreateMySqlUsers()
                .ToSql(q => q.OrderBy(x => x.Id).FirstOrDefault(x => x.Id == 5));

            Assert.NotNull(cmd);
            Assert.Contains("5", cmd.Text);
        }

        [Fact]
        public void ToSql_WithSingleTerminal_ProducesSql()
        {
            var cmd = CreateMySqlUsers().ToSql(q => q.Single(x => x.Id == 7));

            Assert.NotNull(cmd);
            Assert.Contains("7", cmd.Text);
        }

        [Fact]
        public void ToSql_WithAnyTerminal_ProducesSql()
        {
            var cmd = CreateMySqlUsers().ToSql(q => q.Any(x => x.Id > 0));

            Assert.NotNull(cmd);
            Assert.False(string.IsNullOrWhiteSpace(cmd.Text));
        }

        [Fact]
        public void ToSql_WithLastOrderedTerminal_ProducesSql()
        {
            var cmd = CreateMySqlUsers()
                .ToSql(q => q.OrderBy(x => x.Id).Last());

            Assert.NotNull(cmd);
            Assert.Contains("SELECT", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ToSql_WithNullQuery_Throws()
        {
            var users = CreateMySqlUsers();

            Assert.Throws<ArgumentNullException>(
                () => users.ToSql<User, int>(null));
        }

        [Fact]
        public void ToSql_WithNullSourceAndQuery_Throws()
        {
            IQueryable<User> users = null;

            Assert.Throws<ArgumentNullException>(
                () => users.ToSql(q => q.Count()));
        }

        [Fact]
        public void ToSqlString_WithTerminal_InlinesParameters()
        {
            string sql = CreateMySqlUsers().ToSqlString(q => q.Count(x => x.Id == 123));

            Assert.False(string.IsNullOrWhiteSpace(sql));
            Assert.Contains("123", sql);
            Assert.Contains("COUNT", sql, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
