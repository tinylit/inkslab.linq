using Inkslab.Linq.MySql;
using Inkslab.Linq;
using System;
using System.Text;
using Xunit;
using System.Linq;

namespace Inkslab.Linq.Tests
{
    public class VisitNewSpaceTest
    {
        [Fact]
        public void TestVisitNewMultipleFields()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // 模拟 BaseVisitor.VisitNew 的逻辑：处理多个 Members
            // SELECT `x`.`id` AS `Id`, `y`.`role` AS `RoleType`, 1 AS `Type`
            
            writer.Write("SELECT ");  // SELECT with trailing space
            
            bool flag = false;
            
            // Field 1: `x`.`id` AS `Id`
            using (var domain = writer.Domain())
            {
                //模拟 Member() - 没有前导空格
                writer.Write("`x`.`id`");
                writer.Write(" AS ");
                writer.Write("`Id`");
                
                if (!domain.IsEmpty && flag)
                {
                    domain.Flyback();
                    writer.Write(", ");
                }
                else
                {
                    flag = true;
                }
            }

            // Field 2: `y`.`role` AS `RoleType`
            using (var domain = writer.Domain())
            {
                // 模拟 Member() - 没有前导空格
                writer.Write("`y`.`role`");
                writer.Write(" AS ");
                writer.Write("`RoleType`");
                
                if (!domain.IsEmpty && flag)
                {
                    domain.Flyback();
                    writer.Write(", ");
                }
            }
            
            // Field 3: 1 AS `Type`
            using (var domain = writer.Domain())
            {
                // 模拟 Member() - 没有前导空格                writer.Write("1");
                writer.Write(" AS ");
                writer.Write("`Type`");
                
                if (!domain.IsEmpty && flag)
                {
                    domain.Flyback();
                    writer.Write(", 1");
                }
            }

            writer.Write(" FROM `user`");

            var sql = writer.ToString();
            
            // 打印SQL用于调试
            Console.WriteLine($"生成的 SQL: {sql}");
            Console.WriteLine($"SQL字节: {BitConverter.ToString(Encoding.UTF8.GetBytes(sql))}");
            
            // 检查是否存在错误的 ` , 模式（空格+逗号）
            Assert.DoesNotContain("` ,", sql);
            
            // 检查是否存在错误的 `,  ` 模式（逗号+两个空格）
            Assert.DoesNotContain(",  ", sql);
            
            // 检查应该包含正确的 `, ` 模式（逗号+一个空格）
            Assert.Contains(", `", sql);
            
            // 打印关键位置的字节
            int idPos = sql.IndexOf("AS `Id`");
            if (idPos > 0)
            {
                int afterIdPos = idPos + 7;  // "AS `Id`".Length = 7
                if (afterIdPos < sql.Length - 2)
                {
                    var bytes = Encoding.UTF8.GetBytes(sql.Substring(afterIdPos, 3));
                    Console.WriteLine($"`Id` 后的3个字符字节: {BitConverter.ToString(bytes)}");
                }
            }
            
            // 期望的 SQL 格式
            var expected = "SELECT `x`.`id` AS `Id`, `y`.`role` AS `RoleType`, 1 AS `Type` FROM `user`";
            Assert.Equal(expected, sql);
        }
    }
}
