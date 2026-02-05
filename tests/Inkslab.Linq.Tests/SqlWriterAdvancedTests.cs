using System;
using Inkslab.Linq.MySql;
using Inkslab.Linq.Enums;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// SqlWriter 补充测试（更多边界情况和高级功能）
    /// </summary>
    public class SqlWriterAdvancedTests
    {
        #region 基础写入测试

        /// <summary>
        /// 测试：写入空字符串抛出异常
        /// </summary>
        [Fact]
        public void Write_EmptyString_ThrowsArgumentException()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act & Assert
            Assert.Throws<ArgumentException>(() => writer.Write(string.Empty));
        }

        /// <summary>
        /// 测试：多次写入
        /// </summary>
        [Fact]
        public void Write_MultipleTimes_ConcatenatesStrings()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Write("SELECT");
            writer.Write(" * ");
            writer.Write("FROM users");

            // Assert
            Assert.Equal("SELECT * FROM users", writer.ToString());
        }

        /// <summary>
        /// 测试：写入空格
        /// </summary>
        [Fact]
        public void WhiteSpace_WritesSpace()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Write("SELECT");
            writer.WhiteSpace();
            writer.Write("*");

            // Assert
            Assert.Equal("SELECT *", writer.ToString());
        }

        #endregion

        #region 关键字测试

        /// <summary>
        /// 测试：INSERT 关键字
        /// </summary>
        [Fact]
        public void Keyword_INSERT_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.INSERT);
            writer.Keyword(SqlKeyword.INTO);
            writer.Name("users");

            // Assert
            Assert.Equal("INSERT INTO `users`", writer.ToString());
        }

        /// <summary>
        /// 测试：UPDATE 关键字
        /// </summary>
        [Fact]
        public void Keyword_UPDATE_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.UPDATE);
            writer.Name("users");
            writer.Keyword(SqlKeyword.SET);
            writer.Name("name");
            writer.Write(" = ?name");

            // Assert
            Assert.StartsWith("UPDATE `users` SET `name`", writer.ToString());
        }

        /// <summary>
        /// 测试：IGNORE 关键字
        /// </summary>
        [Fact]
        public void Keyword_IGNORE_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.INSERT);
            writer.Keyword(SqlKeyword.IGNORE);
            writer.Keyword(SqlKeyword.INTO);
            writer.Name("users");

            // Assert
            Assert.Equal("INSERT IGNORE INTO `users`", writer.ToString());
        }

        /// <summary>
        /// 测试：VALUES 关键字
        /// </summary>
        [Fact]
        public void Keyword_VALUES_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.INSERT);
            writer.Keyword(SqlKeyword.INTO);
            writer.Name("users");
            writer.OpenBrace();
            writer.Name("name");
            writer.CloseBrace();
            writer.Keyword(SqlKeyword.VALUES);
            writer.OpenBrace();
            writer.Write("?name");
            writer.CloseBrace();

            // Assert
            Assert.Equal("INSERT INTO `users`(`name`) VALUES(?name)", writer.ToString());
        }

        /// <summary>
        /// 测试：GROUP BY 关键字
        /// </summary>
        [Fact]
        public void Keyword_GROUPBY_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.SELECT);
            writer.Name("user_id");
            writer.Write(", COUNT(1)");
            writer.Keyword(SqlKeyword.FROM);
            writer.Name("orders");
            writer.Keyword(SqlKeyword.GROUP);
            writer.Keyword(SqlKeyword.BY);
            writer.Name("user_id");

            // Assert
            Assert.Equal("SELECT `user_id`, COUNT(1) FROM `orders` GROUP BY `user_id`", writer.ToString());
        }

        /// <summary>
        /// 测试：ORDER BY 关键字
        /// </summary>
        [Fact]
        public void Keyword_ORDERBY_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(SqlKeyword.FROM);
            writer.Name("users");
            writer.Keyword(SqlKeyword.ORDER);
            writer.Keyword(SqlKeyword.BY);
            writer.Name("id");
            writer.Keyword(SqlKeyword.ASC);

            // Assert
            Assert.Equal("SELECT * FROM `users` ORDER BY `id` ASC", writer.ToString());
        }

        /// <summary>
        /// 测试：DESC 关键字
        /// </summary>
        [Fact]
        public void Keyword_DESC_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(SqlKeyword.FROM);
            writer.Name("users");
            writer.Keyword(SqlKeyword.ORDER);
            writer.Keyword(SqlKeyword.BY);
            writer.Name("created_at");
            writer.Keyword(SqlKeyword.DESC);

            // Assert
            Assert.Equal("SELECT * FROM `users` ORDER BY `created_at` DESC", writer.ToString());
        }

        /// <summary>
        /// 测试：HAVING 关键字
        /// </summary>
        [Fact]
        public void Keyword_HAVING_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.SELECT);
            writer.Name("user_id");
            writer.Write(", COUNT(1) as cnt");
            writer.Keyword(SqlKeyword.FROM);
            writer.Name("orders");
            writer.Keyword(SqlKeyword.GROUP);
            writer.Keyword(SqlKeyword.BY);
            writer.Name("user_id");
            writer.Keyword(SqlKeyword.HAVING);
            writer.Write("cnt > 10");

            // Assert
            Assert.Equal("SELECT `user_id`, COUNT(1) as cnt FROM `orders` GROUP BY `user_id` HAVING cnt > 10", writer.ToString());
        }

        /// <summary>
        /// 测试：CASE WHEN 关键字
        /// </summary>
        [Fact]
        public void Keyword_CASEWHEN_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.SELECT);
            writer.Keyword(SqlKeyword.CASE);
            writer.Keyword(SqlKeyword.WHEN);
            writer.Name("status");
            writer.Write(" = 1");
            writer.Keyword(SqlKeyword.THEN);
            writer.Write("'Active'");
            writer.Keyword(SqlKeyword.ELSE);
            writer.Write("'Inactive'");
            writer.Keyword(SqlKeyword.END);
            writer.Keyword(SqlKeyword.FROM);
            writer.Name("users");

            // Assert
            var result = writer.ToString();
            Assert.Contains("CASE", result);
            Assert.Contains("WHEN", result);
            Assert.Contains("THEN", result);
            Assert.Contains("ELSE", result);
            Assert.Contains("END", result);
        }

        /// <summary>
        /// 测试：INTERSECT 关键字
        /// </summary>
        [Fact]
        public void Keyword_INTERSECT_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.OpenBrace();
            writer.Keyword(SqlKeyword.SELECT);
            writer.Write("1");
            writer.CloseBrace();
            writer.Keyword(SqlKeyword.INTERSECT);
            writer.OpenBrace();
            writer.Keyword(SqlKeyword.SELECT);
            writer.Write("2");
            writer.CloseBrace();

            // Assert
            Assert.Equal("(SELECT 1) INTERSECT (SELECT 2)", writer.ToString());
        }

        /// <summary>
        /// 测试：EXCEPT 关键字
        /// </summary>
        [Fact]
        public void Keyword_EXCEPT_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.OpenBrace();
            writer.Keyword(SqlKeyword.SELECT);
            writer.Write("1");
            writer.CloseBrace();
            writer.Keyword(SqlKeyword.EXCEPT);
            writer.OpenBrace();
            writer.Keyword(SqlKeyword.SELECT);
            writer.Write("2");
            writer.CloseBrace();

            // Assert
            Assert.Equal("(SELECT 1) EXCEPT (SELECT 2)", writer.ToString());
        }

        #endregion

        #region JOIN 测试

        /// <summary>
        /// 测试：INNER JOIN 关键字
        /// </summary>
        [Fact]
        public void Keyword_INNERJOIN_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(SqlKeyword.FROM);
            writer.Name("users");
            writer.WhiteSpace();
            writer.Name("u");
            writer.Keyword(SqlKeyword.INNER);
            writer.Keyword(SqlKeyword.JOIN);
            writer.Name("orders");
            writer.WhiteSpace();
            writer.Name("o");
            writer.Keyword(SqlKeyword.ON);
            writer.Name("u");
            writer.Write(".");
            writer.Name("id");
            writer.Write(" = ");
            writer.Name("o");
            writer.Write(".");
            writer.Name("user_id");

            // Assert
            var result = writer.ToString();
            Assert.Contains("INNER JOIN", result);
            Assert.Contains("ON", result);
        }

        /// <summary>
        /// 测试：LEFT JOIN 关键字
        /// </summary>
        [Fact]
        public void Keyword_LEFTJOIN_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(SqlKeyword.FROM);
            writer.Name("users");
            writer.Keyword(SqlKeyword.LEFT);
            writer.Keyword(SqlKeyword.JOIN);
            writer.Name("orders");
            writer.Keyword(SqlKeyword.ON);
            writer.Write("users.id = orders.user_id");

            // Assert
            var result = writer.ToString();
            Assert.Contains("LEFT JOIN", result);
        }

        /// <summary>
        /// 测试：RIGHT JOIN 关键字
        /// </summary>
        [Fact]
        public void Keyword_RIGHTJOIN_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(SqlKeyword.FROM);
            writer.Name("users");
            writer.Keyword(SqlKeyword.RIGHT);
            writer.Keyword(SqlKeyword.JOIN);
            writer.Name("orders");
            writer.Keyword(SqlKeyword.ON);
            writer.Write("users.id = orders.user_id");

            // Assert
            var result = writer.ToString();
            Assert.Contains("RIGHT JOIN", result);
        }

        /// <summary>
        /// 测试：CROSS JOIN 关键字
        /// </summary>
        [Fact]
        public void Keyword_CROSSJOIN_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(SqlKeyword.FROM);
            writer.Name("a");
            writer.Keyword(SqlKeyword.CROSS);
            writer.Keyword(SqlKeyword.JOIN);
            writer.Name("b");

            // Assert
            var result = writer.ToString();
            Assert.Contains("CROSS JOIN", result);
        }

        /// <summary>
        /// 测试：LEFT OUTER JOIN 关键字
        /// </summary>
        [Fact]
        public void Keyword_LEFTOUTERJOIN_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(SqlKeyword.FROM);
            writer.Name("users");
            writer.Keyword(SqlKeyword.LEFT);
            writer.Keyword(SqlKeyword.OUTER);
            writer.Keyword(SqlKeyword.JOIN);
            writer.Name("orders");
            writer.Keyword(SqlKeyword.ON);
            writer.Write("users.id = orders.user_id");

            // Assert
            var result = writer.ToString();
            Assert.Contains("LEFT OUTER JOIN", result);
        }

        #endregion

        #region 条件测试

        /// <summary>
        /// 测试：LIKE 关键字
        /// </summary>
        [Fact]
        public void Keyword_LIKE_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(SqlKeyword.FROM);
            writer.Name("users");
            writer.Keyword(SqlKeyword.WHERE);
            writer.Name("name");
            writer.Keyword(SqlKeyword.LIKE);
            writer.Write("'%test%'");

            // Assert
            Assert.Equal("SELECT * FROM `users` WHERE `name` LIKE '%test%'", writer.ToString());
        }

        /// <summary>
        /// 测试：IN 关键字
        /// </summary>
        [Fact]
        public void Keyword_IN_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(SqlKeyword.FROM);
            writer.Name("users");
            writer.Keyword(SqlKeyword.WHERE);
            writer.Name("id");
            writer.Keyword(SqlKeyword.IN);
            writer.OpenBrace();
            writer.Write("1, 2, 3");
            writer.CloseBrace();

            // Assert
            Assert.Equal("SELECT * FROM `users` WHERE `id` IN(1, 2, 3)", writer.ToString());
        }

        /// <summary>
        /// 测试：NOT IN 关键字
        /// </summary>
        [Fact]
        public void Keyword_NOTIN_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(SqlKeyword.FROM);
            writer.Name("users");
            writer.Keyword(SqlKeyword.WHERE);
            writer.Name("id");
            writer.Keyword(SqlKeyword.NOT);
            writer.Keyword(SqlKeyword.IN);
            writer.OpenBrace();
            writer.Write("1, 2, 3");
            writer.CloseBrace();

            // Assert
            var result = writer.ToString();
            Assert.Contains("NOT IN", result);
            Assert.Contains("(1, 2, 3)", result);
        }

        /// <summary>
        /// 测试：NOT LIKE 关键字
        /// </summary>
        [Fact]
        public void Keyword_NOTLIKE_WritesCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(SqlKeyword.FROM);
            writer.Name("users");
            writer.Keyword(SqlKeyword.WHERE);
            writer.Name("name");
            writer.Keyword(SqlKeyword.NOT);
            writer.Keyword(SqlKeyword.LIKE);
            writer.Write("'%test%'");

            // Assert
            var result = writer.ToString();
            Assert.Contains("NOT LIKE", result);
        }

        #endregion

        #region 参数测试

        /// <summary>
        /// 测试：添加变量
        /// </summary>
        [Fact]
        public void Variable_AddsParameter()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(SqlKeyword.FROM);
            writer.Name("users");
            writer.Keyword(SqlKeyword.WHERE);
            writer.Name("id");
            writer.Write(" = ");
            writer.Variable("id", 1);

            // Assert
            Assert.Contains("?id", writer.ToString());
            Assert.Single(writer.Parameters);
            Assert.Equal(1, writer.Parameters["id"]);
        }

        /// <summary>
        /// 测试：添加多个变量
        /// </summary>
        [Fact]
        public void Variable_AddsMultipleParameters()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(SqlKeyword.FROM);
            writer.Name("users");
            writer.Keyword(SqlKeyword.WHERE);
            writer.Name("id");
            writer.Write(" = ");
            writer.Variable("id", 1);
            writer.Keyword(SqlKeyword.AND);
            writer.Name("status");
            writer.Write(" = ");
            writer.Variable("status", "active");

            // Assert
            Assert.Equal(2, writer.Parameters.Count);
            Assert.Equal(1, writer.Parameters["id"]);
            Assert.Equal("active", writer.Parameters["status"]);
        }

        #endregion

        #region Domain 测试

        /// <summary>
        /// 测试：Domain 空检查
        /// </summary>
        [Fact]
        public void Domain_IsEmpty_WhenNothingWritten()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());
            writer.Write("SELECT");

            // Act
            using (var domain = writer.Domain())
            {
                // 不写入任何内容
                Assert.True(domain.IsEmpty);
                Assert.False(domain.HasValue);
            }
        }

        /// <summary>
        /// 测试：Domain HasValue 当有内容时
        /// </summary>
        [Fact]
        public void Domain_HasValue_WhenContentWritten()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());
            writer.Write("SELECT");

            // Act & Assert
            using (var domain = writer.Domain())
            {
                writer.Write(" * FROM users");

                Assert.False(domain.IsEmpty);
                Assert.True(domain.HasValue);
            }
        }

        /// <summary>
        /// 测试：Domain Discard 丢弃内容
        /// </summary>
        [Fact]
        public void Domain_Discard_RemovesContent()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());
            writer.Write("SELECT *");

            // Act
            using (var domain = writer.Domain())
            {
                writer.Write(" FROM users");
                domain.Discard();
            }

            // Assert
            Assert.Equal("SELECT *", writer.ToString());
        }

        /// <summary>
        /// 测试：嵌套 Domain
        /// </summary>
        [Fact]
        public void Domain_Nested_WorksCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Write("SELECT");

            using (var outer = writer.Domain())
            {
                writer.Write("FROM table");

                using (var inner = writer.Domain())
                {
                    writer.Write("x.Id > 0");

                    if (inner.HasValue)
                    {
                        inner.Flyback();
                        writer.Write(" WHERE ");
                    }
                }

                if (outer.HasValue)
                {
                    outer.Flyback();
                    writer.Write(" * ");
                }
            }

            // Assert
            Assert.Equal("SELECT * FROM table WHERE x.Id > 0", writer.ToString());
        }

        #endregion

        #region ConditionReversal 测试

        /// <summary>
        /// 测试：ConditionReversal 反转 NULL 条件
        /// </summary>
        [Fact]
        public void ConditionReversal_ReversesNullCondition()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            using (writer.ConditionReversal())
            {
                writer.Keyword(SqlKeyword.IS);
                writer.Keyword(SqlKeyword.NULL);
            }

            // Assert
            Assert.Equal(" IS NOT NULL", writer.ToString());
        }

        /// <summary>
        /// 测试：ConditionReversal 反转 NOT NULL 条件
        /// </summary>
        [Fact]
        public void ConditionReversal_ReversesNotNullCondition()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            using (writer.ConditionReversal())
            {
                writer.Keyword(SqlKeyword.IS);
                writer.Keyword(SqlKeyword.NOT);
                writer.Keyword(SqlKeyword.NULL);
            }

            // Assert
            Assert.Equal(" IS NULL", writer.ToString());
        }

        #endregion

        #region 括号测试

        /// <summary>
        /// 测试：括号正确匹配
        /// </summary>
        [Fact]
        public void Braces_MatchCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Keyword(SqlKeyword.SELECT);
            writer.OpenBrace();
            writer.Write("1 + 2");
            writer.CloseBrace();
            writer.Write(" * 3");

            // Assert
            Assert.Equal("SELECT (1 + 2) * 3", writer.ToString());
        }

        /// <summary>
        /// 测试：嵌套括号
        /// </summary>
        [Fact]
        public void Braces_NestedCorrectly()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.OpenBrace();
            writer.OpenBrace();
            writer.Write("1 + 2");
            writer.CloseBrace();
            writer.Write(" * ");
            writer.OpenBrace();
            writer.Write("3 + 4");
            writer.CloseBrace();
            writer.CloseBrace();

            // Assert
            Assert.Equal("((1 + 2) * (3 + 4))", writer.ToString());
        }

        #endregion

        #region Dispose 测试

        /// <summary>
        /// 测试：Dispose 后可以重新使用
        /// </summary>
        [Fact]
        public void Dispose_AllowsReuse()
        {
            // Arrange
            var settings = new MySqlCorrectSettings();
            var writer = new SqlWriter(settings);

            // Act
            writer.Write("SELECT 1");
            writer.Dispose();

            // Assert - Dispose 后应该清空
            Assert.Equal(string.Empty, writer.ToString());
        }

        #endregion

        #region 属性测试

        /// <summary>
        /// 测试：Settings 属性
        /// </summary>
        [Fact]
        public void Settings_ReturnsCorrectSettings()
        {
            // Arrange
            var settings = new MySqlCorrectSettings();
            var writer = new SqlWriter(settings);

            // Assert
            Assert.Same(settings, writer.Settings);
        }

        /// <summary>
        /// 测试：Length 属性
        /// </summary>
        [Fact]
        public void Length_ReturnsCorrectLength()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Act
            writer.Write("SELECT");

            // Assert
            Assert.Equal(6, writer.Length);
        }

        /// <summary>
        /// 测试：Parameters 属性初始化
        /// </summary>
        [Fact]
        public void Parameters_InitializedEmpty()
        {
            // Arrange
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Assert
            Assert.NotNull(writer.Parameters);
            Assert.Empty(writer.Parameters);
        }

        #endregion
    }
}
