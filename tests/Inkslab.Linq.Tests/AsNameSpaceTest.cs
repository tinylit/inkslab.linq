using Inkslab.Linq.MySql;
using Xunit;
using System.Text.RegularExpressions;
using System;
using System.Linq;

namespace Inkslab.Linq.Tests
{
    public class AsNameSpaceTest
    {
        /// <summary>
        /// 复现：AsName 后跟 Delimiter 导致空格+逗号的问题
        /// 模拟 Select 字段生成：field AS alias, field2 AS alias2
        /// </summary>
        [Fact]
        public void TestAsNameFollowedByDelimiter()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);

            // 第一个字段（不使用 Domain + Flyback）
            bool first = true;
            
            // 模拟多个字段，使用 Domain + Flyback 模式
            for (int i = 0; i < 3; i++)
            {
                using (var domain = writer.Domain())
                {
                    writer.Schema("x");
                    writer.Name($"field{i}");
                    writer.AsName($"Field{i}");

                    // 如果不是第一个，先 Flyback 再 Delimiter
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

            // 输出详细信息用于调试
            System.Console.WriteLine($"生成的 SQL: {sql}");
            System.Console.WriteLine($"SQL 长度: {sql.Length}");
            
            // 检查特定位置的字符
            int idx = sql.IndexOf("`Field0`");
            if (idx >= 0)
            {
                System.Console.WriteLine($"`Field0` 位置: {idx}");
                if (idx + 8 < sql.Length)
                {
                    string after = sql.Substring(idx + 8, Math.Min(5, sql.Length - idx - 8));
                    System.Console.WriteLine($"`Field0` 后面5个字符: '{after}'");
                    System.Console.WriteLine($"字节: {string.Join(" ", after.Select(c => ((int)c).ToString("X2")))}");
                }
            }

            // 检查是否有多余空格的模式
            // 错误模式：` ,`（空格+逗号）
            var spaceCommaPattern = new Regex(@"`\s,");
            if (spaceCommaPattern.IsMatch(sql))
            {
                var matches = spaceCommaPattern.Matches(sql);
                System.Console.WriteLine($"发现 {matches.Count} 处空格+逗号问题:");
                foreach (Match match in matches)
                {
                    System.Console.WriteLine($"  位置 {match.Index}: '{match.Value}'");
                }
                Assert.Fail("发现空格+逗号模式");
            }

            // 正确模式应该是：`, `（逗号+空格）
            Assert.Matches(new Regex(@"`,\s"), sql);
        }
    }
}
