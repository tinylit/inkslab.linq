using System.Data;
using Microsoft.Data.SqlClient;
using Xunit;

namespace SqlServer.Tests
{
    /// <summary>
    /// 验证区分 varchar/nvarchar 的引擎下，ASCII string 参数使用 AnsiString (varchar) 避免隐式转换导致索引失效，
    /// 并验证 Size 归一化策略（AnsiString → 8000, String → 4000）以促进执行计划缓存复用。
    /// </summary>
    public class LookupDbAnsiStringTests
    {
        #region SqlServer — DbType 与 Size 验证

        [Fact]
        public void AddParameterAuto_SqlServer_AsciiString_UsesAnsiStringWithSize8000()
        {
            using var command = new SqlCommand();

            Inkslab.Linq.LookupDb.AddParameterAuto(command, Inkslab.Linq.DatabaseEngine.SqlServer, "name", "Hello");

            var param = (SqlParameter)command.Parameters[0];

            Assert.Equal(DbType.AnsiString, param.DbType);
            Assert.Equal(8000, param.Size);
        }

        [Fact]
        public void AddParameterAuto_SqlServer_UnicodeString_UsesStringWithSize4000()
        {
            using var command = new SqlCommand();

            Inkslab.Linq.LookupDb.AddParameterAuto(command, Inkslab.Linq.DatabaseEngine.SqlServer, "name", "你好世界");

            var param = (SqlParameter)command.Parameters[0];

            Assert.Equal(DbType.String, param.DbType);
            Assert.Equal(4000, param.Size);
        }

        [Fact]
        public void AddParameterAuto_SqlServer_AsciiStringBetween4000And8000_UsesAnsiStringWithSize8000()
        {
            using var command = new SqlCommand();

            var text = new string('A', 5000);

            Inkslab.Linq.LookupDb.AddParameterAuto(command, Inkslab.Linq.DatabaseEngine.SqlServer, "name", text);

            var param = (SqlParameter)command.Parameters[0];

            Assert.Equal(DbType.AnsiString, param.DbType);
            Assert.Equal(8000, param.Size);
        }

        [Fact]
        public void AddParameterAuto_SqlServer_AsciiStringOver8000_UsesAnsiStringNoSizeNormalization()
        {
            using var command = new SqlCommand();

            var text = new string('A', 9000);

            Inkslab.Linq.LookupDb.AddParameterAuto(command, Inkslab.Linq.DatabaseEngine.SqlServer, "name", text);

            var param = (SqlParameter)command.Parameters[0];

            Assert.Equal(DbType.AnsiString, param.DbType);
            Assert.NotEqual(8000, param.Size);
        }

        [Fact]
        public void AddParameterAuto_SqlServer_UnicodeStringOver4000_UsesStringNoSizeNormalization()
        {
            using var command = new SqlCommand();

            var text = new string('中', 5000);

            Inkslab.Linq.LookupDb.AddParameterAuto(command, Inkslab.Linq.DatabaseEngine.SqlServer, "name", text);

            var param = (SqlParameter)command.Parameters[0];

            Assert.Equal(DbType.String, param.DbType);
            Assert.NotEqual(4000, param.Size);
        }

        [Fact]
        public void AddParameterAuto_SqlServer_EmptyString_UsesAnsiStringWithSize8000()
        {
            using var command = new SqlCommand();

            Inkslab.Linq.LookupDb.AddParameterAuto(command, Inkslab.Linq.DatabaseEngine.SqlServer, "name", "");

            var param = (SqlParameter)command.Parameters[0];

            Assert.Equal(DbType.AnsiString, param.DbType);
            Assert.Equal(8000, param.Size);
        }

        [Fact]
        public void AddParameterAuto_SqlServer_MixedAsciiUnicode_UsesStringWithSize4000()
        {
            using var command = new SqlCommand();

            Inkslab.Linq.LookupDb.AddParameterAuto(command, Inkslab.Linq.DatabaseEngine.SqlServer, "name", "Hello你好");

            var param = (SqlParameter)command.Parameters[0];

            Assert.Equal(DbType.String, param.DbType);
            Assert.Equal(4000, param.Size);
        }

        #endregion

        #region Oracle / DB2 / Sybase — 同样区分 varchar/nvarchar 的引擎

        [Theory]
        [InlineData(Inkslab.Linq.DatabaseEngine.Oracle)]
        [InlineData(Inkslab.Linq.DatabaseEngine.DB2)]
        [InlineData(Inkslab.Linq.DatabaseEngine.Sybase)]
        public void AddParameterAuto_AnsiPreferredEngine_AsciiString_UsesAnsiStringWithSize8000(Inkslab.Linq.DatabaseEngine engine)
        {
            using var command = new SqlCommand();

            Inkslab.Linq.LookupDb.AddParameterAuto(command, engine, "name", "ID00001");

            var param = (SqlParameter)command.Parameters[0];

            Assert.Equal(DbType.AnsiString, param.DbType);
            Assert.Equal(8000, param.Size);
        }

        [Theory]
        [InlineData(Inkslab.Linq.DatabaseEngine.Oracle)]
        [InlineData(Inkslab.Linq.DatabaseEngine.DB2)]
        [InlineData(Inkslab.Linq.DatabaseEngine.Sybase)]
        public void AddParameterAuto_AnsiPreferredEngine_UnicodeString_UsesStringWithSize4000(Inkslab.Linq.DatabaseEngine engine)
        {
            using var command = new SqlCommand();

            Inkslab.Linq.LookupDb.AddParameterAuto(command, engine, "name", "中文测试");

            var param = (SqlParameter)command.Parameters[0];

            Assert.Equal(DbType.String, param.DbType);
            Assert.Equal(4000, param.Size);
        }

        #endregion

        #region MySQL / PostgreSQL / SQLite — 不区分 varchar/nvarchar 的引擎，保持 DbType.String + Size 4000

        [Theory]
        [InlineData(Inkslab.Linq.DatabaseEngine.MySQL)]
        [InlineData(Inkslab.Linq.DatabaseEngine.PostgreSQL)]
        [InlineData(Inkslab.Linq.DatabaseEngine.SQLite)]
        public void AddParameterAuto_NonAnsiEngine_AsciiString_KeepsStringWithSize4000(Inkslab.Linq.DatabaseEngine engine)
        {
            using var command = new SqlCommand();

            Inkslab.Linq.LookupDb.AddParameterAuto(command, engine, "name", "Hello");

            var param = (SqlParameter)command.Parameters[0];

            Assert.Equal(DbType.String, param.DbType);
            Assert.Equal(4000, param.Size);
        }

        #endregion
    }
}
