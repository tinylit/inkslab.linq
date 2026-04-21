using System;
using System.Reflection;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// <see cref="LookupDb.AppendJsonCast"/> 的纯字符串扫描替换行为测试。
    /// </summary>
    public class LookupDbAppendJsonCastTests
    {
        private static readonly MethodInfo _appendJsonCast = typeof(LookupDb)
            .GetMethod("AppendJsonCast", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("未找到 LookupDb.AppendJsonCast。");

        private static string Invoke(string sql, string name, string cast)
            => (string)_appendJsonCast.Invoke(null, new object[] { sql, name, cast });

        [Fact]
        public void AppendJsonCast_SingleAtParameter_AppendsCast()
        {
            var result = Invoke("INSERT INTO tb(payload) VALUES(@p1)", "p1", "::json");

            Assert.Equal("INSERT INTO tb(payload) VALUES(@p1::json)", result);
        }

        [Fact]
        public void AppendJsonCast_ColonParameter_AppendsCast()
        {
            var result = Invoke("UPDATE tb SET payload = :doc WHERE id = :id", "doc", "::jsonb");

            Assert.Equal("UPDATE tb SET payload = :doc::jsonb WHERE id = :id", result);
        }

        [Fact]
        public void AppendJsonCast_QuestionParameter_AppendsCast()
        {
            var result = Invoke("SELECT * FROM tb WHERE payload = ?doc", "doc", "::json");

            Assert.Equal("SELECT * FROM tb WHERE payload = ?doc::json", result);
        }

        [Fact]
        public void AppendJsonCast_CaseInsensitiveParameterName_AppendsCast()
        {
            var result = Invoke("SELECT @Doc", "doc", "::json");

            Assert.Equal("SELECT @Doc::json", result);
        }

        [Fact]
        public void AppendJsonCast_DoesNotMatch_WhenFollowedByAlphaNumericOrUnderscore()
        {
            // @doc1 不应命中 doc
            var sql = "SELECT @doc1";

            var result = Invoke(sql, "doc", "::json");

            Assert.Equal(sql, result);
        }

        [Fact]
        public void AppendJsonCast_DoesNotMatch_WhenFollowedByUnderscore()
        {
            var sql = "SELECT @doc_id";

            var result = Invoke(sql, "doc", "::json");

            Assert.Equal(sql, result);
        }

        [Fact]
        public void AppendJsonCast_DoesNotMatch_WhenPrecededByIdentifierChar()
        {
            // 比如邮箱字面量里出现 @doc
            var sql = "SELECT 'user@doc' AS email";

            var result = Invoke(sql, "doc", "::json");

            Assert.Equal(sql, result);
        }

        [Fact]
        public void AppendJsonCast_MultipleOccurrences_AppendsEach()
        {
            var result = Invoke("SELECT @p1, (SELECT @p1 FROM x) UNION SELECT @p1", "p1", "::jsonb");

            Assert.Equal("SELECT @p1::jsonb, (SELECT @p1::jsonb FROM x) UNION SELECT @p1::jsonb", result);
        }

        [Fact]
        public void AppendJsonCast_EmptyOrNullSql_ReturnsAsIs()
        {
            Assert.Null(Invoke(null, "p1", "::json"));
            Assert.Equal(string.Empty, Invoke(string.Empty, "p1", "::json"));
        }

        [Fact]
        public void AppendJsonCast_NoMatch_ReturnsSameReference()
        {
            var sql = "SELECT 1";

            var result = Invoke(sql, "p1", "::json");

            Assert.Same(sql, result);
        }

        [Fact]
        public void AppendJsonCast_ParameterAtEnd_AppendsCast()
        {
            var result = Invoke("SELECT @p1", "p1", "::json");

            Assert.Equal("SELECT @p1::json", result);
        }
    }
}
