using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using Inkslab.Linq.MySql;
using Inkslab.Linq.PostgreSQL;
using Inkslab.Linq.SqlServer;
using Xunit;

namespace Inkslab.Linq.Tests
{
    public class FrameworkCompatibilityTests
    {
        [Fact]
        public void Libraries_UseTheCurrentTargetFramework()
        {
            var expected = $".NETCoreApp,Version=v{Environment.Version.Major}.0";
            var assemblies = new[]
            {
                typeof(CommandSql).Assembly,
                typeof(MySqlCorrectSettings).Assembly,
                typeof(PostgreSQLCorrectSettings).Assembly,
                typeof(SqlServerCorrectSettings).Assembly,
                typeof(Inkslab.Transactions.TransactionUnit).Assembly
            };

            foreach (var assembly in assemblies)
            {
                Assert.Equal(expected, assembly.GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName);
            }
        }

        [Theory]
        [InlineData("en-US")]
        [InlineData("tr-TR")]
        [InlineData("ar-SA")]
        public void Pagination_PreservesSqlAndCurrentCulture(string culture)
        {
            var previous = CultureInfo.CurrentCulture;
            var custom = (CultureInfo)CultureInfo.GetCultureInfo(culture).Clone();
            custom.NumberFormat.NegativeSign = "~";

            try
            {
                CultureInfo.CurrentCulture = custom;
                var mysql = new MySqlCorrectSettings();
                var postgres = new PostgreSQLCorrectSettings();
                var sqlServer = new SqlServerCorrectSettings();

                Assert.Equal("SELECT 1 ORDER BY 1 LIMIT 3,7", mysql.ToSQL("SELECT 1", 7, 3, "ORDER BY 1"));
                Assert.Equal("SELECT 1 LIMIT ~1", mysql.ToSQL("SELECT 1", -1, 0, ""));
                Assert.Equal("SELECT 1 ORDER BY 1 LIMIT 7 OFFSET 3", postgres.ToSQL("SELECT 1", 7, 3, "ORDER BY 1"));
                Assert.Equal("SELECT 1 LIMIT ~1", postgres.ToSQL("SELECT 1", -1, 0, ""));
                Assert.Equal("SELECT 1 ORDER BY 1 OFFSET 3 ROWS FETCH NEXT 7 ROWS ONLY", sqlServer.ToSQL("SELECT 1", 7, 3, "ORDER BY 1"));
                Assert.Equal("SELECT 1 ORDER BY 1 OFFSET ~1 ROWS", sqlServer.ToSQL("SELECT 1", 0, -1, "ORDER BY 1"));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Pagination_EmptySql_ThrowsArgumentException(string sql)
        {
            IDbCorrectSettings[] settings =
            {
                new MySqlCorrectSettings(), new PostgreSQLCorrectSettings(), new SqlServerCorrectSettings()
            };

            foreach (var adapter in settings)
            {
                Assert.Equal("sql", Assert.Throws<ArgumentException>(() => adapter.ToSQL(sql, 7, 3, "ORDER BY 1")).ParamName);
                Assert.Equal("sql", Assert.Throws<ArgumentException>(() => adapter.ToSQL(sql, 7, 3, null)).ParamName);
            }
        }

        [Theory]
        [InlineData(null, 7, 3, "SELECT 1 LIMIT 3,7", "SELECT 1 LIMIT 7 OFFSET 3", "SELECT 1 OFFSET 3 ROWS FETCH NEXT 7 ROWS ONLY")]
        [InlineData("", 7, 3, "SELECT 1 LIMIT 3,7", "SELECT 1 LIMIT 7 OFFSET 3", "SELECT 1 OFFSET 3 ROWS FETCH NEXT 7 ROWS ONLY")]
        [InlineData(null, 0, 0, "SELECT 1 LIMIT 0", "SELECT 1 LIMIT 0", "SELECT 1 OFFSET 0 ROWS")]
        [InlineData("", 0, 0, "SELECT 1 LIMIT 0", "SELECT 1 LIMIT 0", "SELECT 1 OFFSET 0 ROWS")]
        public void Pagination_EmptyOrderBy_DoesNotAddExtraSpace(string orderBy, int take, int skip, string mysql, string postgres, string sqlServer)
        {
            Assert.Equal(mysql, new MySqlCorrectSettings().ToSQL("SELECT 1", take, skip, orderBy));
            Assert.Equal(postgres, new PostgreSQLCorrectSettings().ToSQL("SELECT 1", take, skip, orderBy));
            Assert.Equal(sqlServer, new SqlServerCorrectSettings().ToSQL("SELECT 1", take, skip, orderBy));
        }

        [Theory]
        [InlineData("en-US")]
        [InlineData("tr-TR")]
        public void CommandFormatting_PreservesUnicodeAndParameterBoundaries(string culture)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                var command = new CommandSql("SELECT @编号, :id, ?I, @@id, x@id, @missing", new Dictionary<string, object>
                {
                    ["编号"] = 7,
                    ["id"] = 8,
                    ["I"] = 9
                });

                Assert.Equal("SELECT 7, 8, 9, @@id, x@id, @missing", command.ToString());
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }
    }
}
