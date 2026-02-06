using Inkslab.Linq.MySql;
using System;
using Xunit;

namespace Inkslab.Linq.Tests
{
    public class MultiFieldSelectSpaceTest
    {
        [Fact]
        public void TestCOUNT_DISTINCT_MultipleFields()
        {
            // 模拟: SELECT COUNT(DISTINCT `x`.`id` , `x`.`name` )
            var writer = new SqlWriter(new MySqlCorrectSettings());
            
            writer.Write("SELECT COUNT(DISTINCT ");
            
            // 使用Domain+Flyback+Delimiter模式生成多个字段
            bool flag = false;
            
            // Field 1: `x`.`id`
            using (var domain = writer.Domain())
            {
                writer.Write("`x`.`id`");
                
                if (flag)
                {
                    domain.Flyback();
                    writer.Write(", ");
                }
                else
                {
                    flag = true;
                }
            }
            
            // Field 2: `x`.`name`
            using (var domain = writer.Domain())
            {
                writer.Write("`x`.`name`");
                
                if (flag)
                {
                    domain.Flyback();
                    writer.Write(", ");
                }
            }
            
            writer.Write(")");
            
            var sql = writer.ToString();
            Console.WriteLine($"生成的 SQL: {sql}");
            
            // 检查是否有错误的 ` , 模式
            Assert.DoesNotContain("` ,", sql);
            
            // 期望的正确格式
            var expected = "SELECT COUNT(DISTINCT `x`.`id`, `x`.`name`)";
            Assert.Equal(expected, sql);
        }
    }
}
