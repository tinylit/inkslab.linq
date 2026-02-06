using Inkslab.Linq.MySql;
using Xunit;
using System.Text.RegularExpressions;

namespace Inkslab.Linq.Tests
{
    public class SpaceVerificationTest
    {
        /// <summary>
        /// 验证：字段别名后不应该有多余空格（反引号+空格+逗号的模式）
        /// </summary>
        [Fact]
        public void VerifyNoExtraSpaceAfterFieldAlias()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);

            // 模拟实际场景：多个字段，每个字段用 Domain + Flyback
            bool first = true;
            for (int i = 0; i < 3; i++)
            {
                using (var domain = writer.Domain())
                {
                    writer.Schema("x");
                    writer.Name($"field{i}");
                    writer.AsName($"Field{i}");

                    if (!first)
                    {
                        domain.Flyback();
                        writer.Delimiter();
                    }
                    else
                    {
                        first = false;
                    }
                }
            }

            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("table");

            var sql = writer.ToString();

            // 验证：不应该有 "` ,"（反引号+空格+逗号）的模式
            Assert.DoesNotMatch(new Regex(@"`\s+,"), sql);
            
            // 验证：应该有 "`, "（反引号+逗号+空格）的正确模式
            Assert.Matches(new Regex(@"`,\s"), sql);

            // 验证完整 SQL 格式
            Assert.Equal("SELECT `x`.`field0` AS `Field0`, `x`.`field1` AS `Field1`, `x`.`field2` AS `Field2` FROM `table`", sql);
        }
    }
}
