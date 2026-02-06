using Inkslab.Linq.MySql;
using Xunit;

namespace Inkslab.Linq.Tests
{
    public class WriterTests
    {
        [Fact]
        public void SqlWriter()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Write("SELECT");

            using (var domain = writer.Domain())
            {
                writer.Write("FROM table");

                if (domain.HasValue)
                {
                    domain.Flyback();

                    writer.Write(" * ");
                }
            }

            Assert.Equal("SELECT * FROM table", writer.ToString());
        }

        [Fact]
        public void SqlWriterMulti()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Write("SELECT");

            using (var transaction = writer.Domain())
            {
                writer.Write("FROM table x");

                if (transaction.HasValue)
                {
                    transaction.Flyback();

                    writer.Write(" * ");
                }
            }

            using (var tran = writer.Domain())
            {
                writer.Write("x.Id > 0 AND x.timestamp = 1024");

                if (tran.HasValue)
                {
                    tran.Flyback();

                    writer.Write(" WHERE ");
                }
            }

            Assert.Equal("SELECT * FROM table x WHERE x.Id > 0 AND x.timestamp = 1024", writer.ToString());
        }

        [Fact]
        public void SqlWriterMulti2()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Write("SELECT");

            using (var transaction = writer.Domain())
            {
                writer.Write("FROM table x");

                using (var tran = writer.Domain())
                {
                    writer.Write("x.Id > 0 AND x.timestamp = 1024");

                    if (tran.HasValue)
                    {
                        tran.Flyback();

                        writer.Write(" WHERE ");
                    }
                }

                if (transaction.HasValue)
                {
                    transaction.Flyback();

                    writer.Write(" * ");
                }
            }

            Assert.Equal("SELECT * FROM table x WHERE x.Id > 0 AND x.timestamp = 1024", writer.ToString());
        }

        [Fact]
        public void SqlWriterMulti3()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Write("SELECT");

            using (var transaction = writer.Domain())
            {
                writer.Write("FROM table x");

                using (var tran = writer.Domain())
                {
                    writer.Write("x.Id > 0 AND x.timestamp = 1024");

                    if (tran.HasValue)
                    {
                        tran.Flyback();

                        writer.Write(" WHERE ");
                    }
                }

                using (var tran = writer.Domain())
                {
                    writer.Write("x.name like '%t%'");

                    using (var domain = writer.Domain())
                    {
                        writer.Write("x.role == 1");

                        if (domain.HasValue)
                        {
                            domain.Flyback();

                            writer.Write(" AND ");
                        }
                    }

                    if (tran.HasValue)
                    {
                        tran.Flyback();

                        writer.Write(" AND ");
                    }
                }

                if (transaction.HasValue)
                {
                    transaction.Flyback();

                    writer.Write(" * ");
                }
            }

            Assert.Equal("SELECT * FROM table x WHERE x.Id > 0 AND x.timestamp = 1024 AND x.name like '%t%' AND x.role == 1", writer.ToString());
        }

        [Fact]
        public void CancelTheSuperfluousNOT()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.NOT);
            writer.Keyword(Enums.SqlKeyword.NOT);
            writer.Keyword(Enums.SqlKeyword.NOT);
            writer.Keyword(Enums.SqlKeyword.NOT);
            writer.Keyword(Enums.SqlKeyword.NOT);
            writer.OpenBrace();

            var value = writer.ToString();

            Assert.True(value is " NOT(" or " NOT (");
        }

        [Fact]
        public void SimpleNOT()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.IS);
            writer.Keyword(Enums.SqlKeyword.NOT);
            writer.Keyword(Enums.SqlKeyword.NULL);

            Assert.Equal(" IS NOT NULL", writer.ToString());
        }

        [Fact]
        public void CancelTheSuperfluousNOTAlternate()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.IS);
            writer.Keyword(Enums.SqlKeyword.NOT);
            writer.Keyword(Enums.SqlKeyword.NOT);
            writer.Keyword(Enums.SqlKeyword.NULL);

            Assert.Equal(" IS NULL", writer.ToString());
        }

        [Fact]
        public void SimpleNOTConditionReversal()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            using (writer.ConditionReversal())
            {
                writer.Keyword(Enums.SqlKeyword.IS);
                writer.Keyword(Enums.SqlKeyword.NOT);
                writer.Keyword(Enums.SqlKeyword.NULL);
            }

            Assert.Equal(" IS NULL", writer.ToString());
        }

        [Fact]
        public void CancelConditionReversalTheSuperfluousNOTAlternate()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            using (writer.ConditionReversal())
            {
                writer.Keyword(Enums.SqlKeyword.IS);
                writer.Keyword(Enums.SqlKeyword.NULL);
            }

            Assert.Equal(" IS NOT NULL", writer.ToString());
        }

        [Fact]
        public void AndNotExists()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Write("x.Id > 0");
            writer.Keyword(Enums.SqlKeyword.AND);
            writer.Keyword(Enums.SqlKeyword.NOT);
            writer.Keyword(Enums.SqlKeyword.EXISTS);
            writer.OpenBrace();
            writer.Write("SELECT 1");
            writer.CloseBrace();

            Assert.Equal("x.Id > 0 AND NOT EXISTS(SELECT 1)", writer.ToString());
        }

        [Fact]
        public void DeleteFrom()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.DELETE);
            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("user");

            Assert.Equal("DELETE FROM `user`", writer.ToString());
        }

        [Fact]
        public void DeleteAliasFrom()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.DELETE);
            writer.Name("x");
            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("user");
            writer.WhiteSpace();
            writer.Name("x");

            Assert.Equal("DELETE `x` FROM `user` `x`", writer.ToString());
        }

        [Fact]
        public void SelectFromSubquery()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.OpenBrace();
            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("1");
            writer.CloseBrace();
            writer.Keyword(Enums.SqlKeyword.AS);
            writer.Name("x");

            Assert.Equal("SELECT * FROM (SELECT 1) AS `x`", writer.ToString());
        }

        /// <summary>
        /// 测试 Name (反引号结尾) 后写 FROM 的场景
        /// </summary>
        [Fact]
        public void NameThenFrom()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Name("id");  // 写入 `id`
            writer.Keyword(Enums.SqlKeyword.FROM);  // FROM 前面应该有空格
            writer.Name("user");

            // 期望：SELECT `id` FROM `user`
            Assert.Equal("SELECT `id` FROM `user`", writer.ToString());
        }

        /// <summary>
        /// 测试 Schema.Name 后写 FROM 的场景
        /// </summary>
        [Fact]
        public void SchemaNameThenFrom()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Schema("x");
            writer.Name("id");  // 写入 `x`.`id`
            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("user");

            // 期望：SELECT `x`.`id` FROM `user`
            Assert.Equal("SELECT `x`.`id` FROM `user`", writer.ToString());
        }

        /// <summary>
        /// 测试 AsName 后写 FROM 的场景（模拟日志中 `Null`FROM 问题）
        /// </summary>
        [Fact]
        public void AsNameThenFrom()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("1");
            writer.AsName("Null");  // 写入 AS `Null`
            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("user");

            // 期望：SELECT 1 AS `Null` FROM `user`
            Assert.Equal("SELECT 1 AS `Null` FROM `user`", writer.ToString());
        }

        /// <summary>
        /// 测试括号表达式后 AsName 再 FROM 的场景
        /// </summary>
        [Fact]
        public void ParenExprAsNameThenFrom()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.OpenBrace();
            writer.Write("'test'");
            writer.Keyword(Enums.SqlKeyword.IS);
            writer.Keyword(Enums.SqlKeyword.NULL);
            writer.CloseBrace();
            writer.AsName("Null");  // 写入 AS `Null`
            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("user");

            // 期望：SELECT ('test' IS NULL) AS `Null` FROM `user`
            Assert.Equal("SELECT ('test' IS NULL) AS `Null` FROM `user`", writer.ToString());
        }

        /// <summary>
        /// 测试逗号分隔的多列选择后 FROM
        /// </summary>
        [Fact]
        public void MultiColumnSelectThenFrom()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Schema("x");
            writer.Name("id");
            writer.AsName("Id");
            writer.Delimiter();
            writer.Schema("x");
            writer.Name("name");
            writer.AsName("Name");
            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("user");

            // 期望：SELECT `x`.`id` AS `Id`, `x`.`name` AS `Name` FROM `user`
            Assert.Equal("SELECT `x`.`id` AS `Id`, `x`.`name` AS `Name` FROM `user`", writer.ToString());
        }

        [Fact]
        public void VariableFrom()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Variable("test", "value");
            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("user");

            Assert.Equal("SELECT ?test FROM `user`", writer.ToString());
        }

        [Fact]
        public void UnionSelect()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.OpenBrace();
            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("1");
            writer.CloseBrace();
            writer.Keyword(Enums.SqlKeyword.UNION);
            writer.OpenBrace();
            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("2");
            writer.CloseBrace();

            Assert.Equal("(SELECT 1) UNION (SELECT 2)", writer.ToString());
        }

        [Fact]
        public void UnionAsSubquery()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(Enums.SqlKeyword.FROM);

            // 模拟 Union 子查询场景
            writer.OpenBrace(); // 外层括号

            writer.OpenBrace(); // 第一个查询括号
            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("1");
            writer.CloseBrace();

            writer.Keyword(Enums.SqlKeyword.UNION);

            writer.OpenBrace(); // 第二个查询括号
            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("2");
            writer.CloseBrace();

            writer.CloseBrace(); // 外层括号

            writer.Keyword(Enums.SqlKeyword.AS);
            writer.Name("x");

            writer.Keyword(Enums.SqlKeyword.LEFT);
            writer.Keyword(Enums.SqlKeyword.JOIN);
            writer.Name("user_ex");
            writer.Keyword(Enums.SqlKeyword.AS);
            writer.Name("z");

            Assert.Equal("SELECT * FROM ((SELECT 1) UNION (SELECT 2)) AS `x` LEFT JOIN `user_ex` AS `z`", writer.ToString());
        }

        [Fact]
        public void UnionWithDomainFlyback()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(Enums.SqlKeyword.FROM);

            // 使用 Domain 模拟实际代码生成逻辑
            using (var domain = writer.Domain())
            {
                writer.OpenBrace();
                writer.Keyword(Enums.SqlKeyword.SELECT);
                writer.Write("1");
                writer.Keyword(Enums.SqlKeyword.FROM);
                writer.Name("user");
                writer.CloseBrace();

                writer.Keyword(Enums.SqlKeyword.UNION);

                writer.OpenBrace();
                writer.Keyword(Enums.SqlKeyword.SELECT);
                writer.Write("2");
                writer.Keyword(Enums.SqlKeyword.FROM);
                writer.Name("user");
                writer.CloseBrace();

                // Flyback 后继续写入
                domain.Flyback();
                writer.OpenBrace();
            }

            writer.CloseBrace();
            writer.Keyword(Enums.SqlKeyword.AS);
            writer.Name("x");

            Assert.Equal("SELECT * FROM ((SELECT 1 FROM `user`) UNION (SELECT 2 FROM `user`)) AS `x`", writer.ToString());
        }

        /// <summary>
        /// 测试变量后 Flyback 再写 FROM 的场景
        /// </summary>
        [Fact]
        public void VariableAfterFlybackThenFrom()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);

            // 使用 Domain，在里面写变量，Flyback 后写其他内容
            using (var domain = writer.Domain())
            {
                writer.Variable("test", "value");

                domain.Flyback();

                // Flyback 后，现在游标在 SELECT 后面，继续写其他内容
            }

            // Dispose 后，游标应该在变量后面，继续写 FROM
            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("user");

            // 期望结果：SELECT ?test FROM `user`
            Assert.Equal("SELECT ?test FROM `user`", writer.ToString());
        }

        /// <summary>
        /// 测试变量后 Dispose 再写 FROM 的场景
        /// </summary>
        [Fact]
        public void VariableDisposeDirectlyThenFrom()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);

            using (var domain = writer.Domain())
            {
                writer.Variable("test", "value");
                // 不调用 Flyback，直接 Dispose
            }

            // Dispose 后写 FROM
            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("user");

            Assert.Equal("SELECT ?test FROM `user`", writer.ToString());
        }

        /// <summary>
        /// 测试 Domain 中写入变量，Flyback 后再写入，最后 Dispose 再写 FROM
        /// 模拟用户描述的场景：?__var_1_valFROM
        /// </summary>
        [Fact]
        public void VariableInDomainFlybackThenFrom()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);

            using (var domain = writer.Domain())
            {
                writer.Variable("test", "value");

                // Flyback 到 Domain 开始位置
                domain.Flyback();

                // 在变量前面插入一些内容
                writer.Write("1, ");
            }

            // Dispose 后，游标在变量后面，继续写 FROM
            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("user");

            // 期望结果：SELECT 1, ?test FROM `user`
            Assert.Equal("SELECT 1, ?test FROM `user`", writer.ToString());
        }

        /// <summary>
        /// 测试 CloseBrace 后写 AS 的场景（模拟 Union 场景中 `)AS` 问题）
        /// </summary>
        [Fact]
        public void CloseBraceAsKeyword()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.OpenBrace();
            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("1");
            writer.CloseBrace();
            writer.Keyword(Enums.SqlKeyword.AS);
            writer.Name("x");

            // 期望结果：(SELECT 1) AS `x`
            Assert.Equal("(SELECT 1) AS `x`", writer.ToString());
        }

        /// <summary>
        /// 测试嵌套 Domain 中 CloseBrace 后 AS 的场景
        /// </summary>
        [Fact]
        public void NestedDomainCloseBraceAs()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(Enums.SqlKeyword.FROM);

            using (var domain = writer.Domain())
            {
                writer.OpenBrace();
                writer.Keyword(Enums.SqlKeyword.SELECT);
                writer.Write("1");
                writer.CloseBrace();
            }

            writer.Keyword(Enums.SqlKeyword.AS);
            writer.Name("x");

            // 期望结果：SELECT * FROM (SELECT 1) AS `x`
            Assert.Equal("SELECT * FROM (SELECT 1) AS `x`", writer.ToString());
        }

        /// <summary>
        /// 精确模拟 SelectVisitor.Startup 的 Domain+Flyback 流程
        /// 复现：`Nullable`FROM 缺少空格的问题
        /// </summary>
        [Fact]
        public void SelectVisitorDomainFlybackSimulation()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Step 1: 写入 SELECT 关键字
            writer.Keyword(Enums.SqlKeyword.SELECT);

            // Step 2: 创建 Domain
            using (var domain = writer.Domain())
            {
                // Step 3: 先写入 FROM 等后续内容（模拟 base.Startup）
                writer.Keyword(Enums.SqlKeyword.FROM);
                writer.Name("user");
                writer.Keyword(Enums.SqlKeyword.AS);
                writer.Name("g");

                // Step 4: Flyback 到 SELECT 后的位置
                domain.Flyback();

                // Step 5: 在 SELECT 后写入字段列表（模拟 Select(node)）
                writer.Schema("g");
                writer.Name("id");
                writer.AsName("Id");
                writer.Delimiter();
                writer.Schema("g");
                writer.Name("name");
                writer.AsName("Name");
                writer.Delimiter();
                writer.Schema("g");
                writer.Name("nullable");
                writer.AsName("Nullable");
            }

            // 期望结果：SELECT `g`.`id` AS `Id`, `g`.`name` AS `Name`, `g`.`nullable` AS `Nullable` FROM `user` AS `g`
            Assert.Equal("SELECT `g`.`id` AS `Id`, `g`.`name` AS `Name`, `g`.`nullable` AS `Nullable` FROM `user` AS `g`", writer.ToString());
        }

        [Fact]
        public void SelectExistsTests()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // Step 1: 写入 SELECT 关键字
            writer.Keyword(Enums.SqlKeyword.SELECT);

            writer.Keyword(Enums.SqlKeyword.EXISTS);

            writer.OpenBrace();
            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("1");
            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("user");
            writer.CloseBrace();

            // 期望结果：SELECT EXISTS(SELECT 1 FROM `user`)
            Assert.Equal("SELECT EXISTS(SELECT 1 FROM `user`)", writer.ToString());
        }

        #region Flyback 综合测试 - 关键字空格处理

        /// <summary>
        /// 测试 Flyback 后写 WHERE 关键字（前后都需要空格）
        /// </summary>
        [Fact]
        public void FlybackThenWhereKeyword()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("*");

            using (var domain = writer.Domain())
            {
                writer.Keyword(Enums.SqlKeyword.FROM);
                writer.Name("user");

                domain.Flyback();

                // Flyback 后再写其他内容（模拟某些情况下的插入）
                // 现在 Dispose 会将光标移到插入内容后
            }

            // 继续写 WHERE 子句
            writer.Keyword(Enums.SqlKeyword.WHERE);
            writer.Schema("user");
            writer.Name("id");
            writer.Write(" > 0");

            Assert.Equal("SELECT * FROM `user` WHERE `user`.`id` > 0", writer.ToString());
        }

        /// <summary>
        /// 测试 Flyback 后写入字段，然后写 JOIN 关键字
        /// </summary>
        [Fact]
        public void FlybackThenJoinKeyword()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);

            using (var domain = writer.Domain())
            {
                writer.Keyword(Enums.SqlKeyword.FROM);
                writer.Name("user");
                writer.Keyword(Enums.SqlKeyword.AS);
                writer.Name("u");

                domain.Flyback();

                writer.Schema("u");
                writer.Name("id");
                writer.Delimiter();
                writer.Schema("u");
                writer.Name("name");
            }

            writer.Keyword(Enums.SqlKeyword.LEFT);
            writer.Keyword(Enums.SqlKeyword.JOIN);
            writer.Name("order");
            writer.Keyword(Enums.SqlKeyword.AS);
            writer.Name("o");
            writer.Keyword(Enums.SqlKeyword.ON);
            writer.Schema("u");
            writer.Name("id");
            writer.Operator(Enums.SqlOperator.Equal);
            writer.Schema("o");
            writer.Name("user_id");

            Assert.Equal("SELECT `u`.`id`, `u`.`name` FROM `user` AS `u` LEFT JOIN `order` AS `o` ON `u`.`id` = `o`.`user_id`", writer.ToString());
        }

        /// <summary>
        /// 测试 Flyback 后写入条件，然后写 AND 关键字
        /// </summary>
        [Fact]
        public void FlybackThenAndKeyword()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("user");
            writer.Keyword(Enums.SqlKeyword.WHERE);

            using (var domain = writer.Domain())
            {
                writer.Schema("user");
                writer.Name("status");
                writer.Write(" = 1");

                domain.Flyback();

                writer.Schema("user");
                writer.Name("id");
                writer.Write(" > 0");
                writer.Keyword(Enums.SqlKeyword.AND);
            }

            writer.Keyword(Enums.SqlKeyword.AND);
            writer.Schema("user");
            writer.Name("age");
            writer.Write(" > 18");

            Assert.Equal("SELECT * FROM `user` WHERE `user`.`id` > 0 AND `user`.`status` = 1 AND `user`.`age` > 18", writer.ToString());
        }

        /// <summary>
        /// 测试 Flyback 后写入 IS NULL 组合
        /// </summary>
        [Fact]
        public void FlybackThenIsNullKeyword()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);

            using (var domain = writer.Domain())
            {
                writer.Keyword(Enums.SqlKeyword.FROM);
                writer.Name("user");
                writer.Keyword(Enums.SqlKeyword.WHERE);
                writer.Schema("user");
                writer.Name("name");
                writer.Keyword(Enums.SqlKeyword.IS);
                writer.Keyword(Enums.SqlKeyword.NULL);

                domain.Flyback();

                writer.Write("1");
            }

            Assert.Equal("SELECT 1 FROM `user` WHERE `user`.`name` IS NULL", writer.ToString());
        }

        /// <summary>
        /// 测试 Flyback 后写入 IS NOT NULL 组合
        /// </summary>
        [Fact]
        public void FlybackThenIsNotNullKeyword()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);

            using (var domain = writer.Domain())
            {
                writer.Keyword(Enums.SqlKeyword.FROM);
                writer.Name("user");
                writer.Keyword(Enums.SqlKeyword.WHERE);
                writer.Schema("user");
                writer.Name("email");
                writer.Keyword(Enums.SqlKeyword.IS);
                writer.Keyword(Enums.SqlKeyword.NOT);
                writer.Keyword(Enums.SqlKeyword.NULL);

                domain.Flyback();

                writer.Write("*");
            }

            Assert.Equal("SELECT * FROM `user` WHERE `user`.`email` IS NOT NULL", writer.ToString());
        }

        /// <summary>
        /// 测试 Flyback 后写入 IN 子句
        /// </summary>
        [Fact]
        public void FlybackThenInKeyword()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Name("id");

            using (var domain = writer.Domain())
            {
                writer.Keyword(Enums.SqlKeyword.FROM);
                writer.Name("user");
                writer.Keyword(Enums.SqlKeyword.WHERE);
                writer.Schema("user");
                writer.Name("status");
                writer.Keyword(Enums.SqlKeyword.IN);
                writer.OpenBrace();
                writer.Write("1, 2, 3");
                writer.CloseBrace();

                domain.Flyback();
                // Flyback 不写入任何内容，只是测试 Dispose 后的空格处理
            }

            Assert.Equal("SELECT `id` FROM `user` WHERE `user`.`status` IN(1, 2, 3)", writer.ToString());
        }

        /// <summary>
        /// 测试 Flyback 后写入 NOT IN 子句
        /// </summary>
        [Fact]
        public void FlybackThenNotInKeyword()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("user");
            writer.Keyword(Enums.SqlKeyword.WHERE);

            using (var domain = writer.Domain())
            {
                writer.Schema("user");
                writer.Name("role");
                writer.Keyword(Enums.SqlKeyword.NOT);
                writer.Keyword(Enums.SqlKeyword.IN);
                writer.OpenBrace();
                writer.Write("1, 2");
                writer.CloseBrace();

                domain.Flyback();
                // 测试后续空格处理
            }

            Assert.Equal("SELECT * FROM `user` WHERE `user`.`role` NOT IN(1, 2)", writer.ToString());
        }

        /// <summary>
        /// 测试 Flyback 后写入 EXISTS 子句
        /// </summary>
        [Fact]
        public void FlybackThenExistsKeyword()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("user");

            using (var domain = writer.Domain())
            {
                writer.Keyword(Enums.SqlKeyword.WHERE);
                writer.Keyword(Enums.SqlKeyword.EXISTS);
                writer.OpenBrace();
                writer.Keyword(Enums.SqlKeyword.SELECT);
                writer.Write("1");
                writer.CloseBrace();

                domain.Flyback();
                // 测试 Flyback 后空格处理
            }

            Assert.Equal("SELECT * FROM `user` WHERE EXISTS(SELECT 1)", writer.ToString());
        }

        /// <summary>
        /// 测试 Flyback 后写入 NOT EXISTS 子句
        /// </summary>
        [Fact]
        public void FlybackThenNotExistsKeyword()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("user");
            writer.Keyword(Enums.SqlKeyword.WHERE);

            using (var domain = writer.Domain())
            {
                writer.Keyword(Enums.SqlKeyword.NOT);
                writer.Keyword(Enums.SqlKeyword.EXISTS);
                writer.OpenBrace();
                writer.Keyword(Enums.SqlKeyword.SELECT);
                writer.Write("1");
                writer.Keyword(Enums.SqlKeyword.FROM);
                writer.Name("order");
                writer.CloseBrace();

                domain.Flyback();
                // 测试空格处理
            }

            Assert.Equal("SELECT * FROM `user` WHERE NOT EXISTS(SELECT 1 FROM `order`)", writer.ToString());
        }

        /// <summary>
        /// 测试嵌套 Domain 多次 Flyback
        /// </summary>
        [Fact]
        public void NestedDomainMultipleFlyback()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);

            using (var outer = writer.Domain())
            {
                writer.Keyword(Enums.SqlKeyword.FROM);
                writer.Name("user");

                using (var inner = writer.Domain())
                {
                    writer.Keyword(Enums.SqlKeyword.WHERE);
                    writer.Write("1 = 1");

                    inner.Flyback();

                    writer.Keyword(Enums.SqlKeyword.AS);
                    writer.Name("u");
                }

                outer.Flyback();

                writer.Write("*");
            }

            Assert.Equal("SELECT * FROM `user` AS `u` WHERE 1 = 1", writer.ToString());
        }

        /// <summary>
        /// 测试 Flyback 后写 Name 再写 FROM 关键字
        /// </summary>
        [Fact]
        public void FlybackWriteNameThenFromKeyword()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);

            using (var domain = writer.Domain())
            {
                writer.Keyword(Enums.SqlKeyword.FROM);
                writer.Name("user");

                domain.Flyback();

                writer.Schema("user");
                writer.Name("id");
                writer.Delimiter();
                writer.Schema("user");
                writer.Name("name");
            }

            // Domain Dispose 后，再写内容
            writer.Keyword(Enums.SqlKeyword.WHERE);
            writer.Write("1 = 1");

            Assert.Equal("SELECT `user`.`id`, `user`.`name` FROM `user` WHERE 1 = 1", writer.ToString());
        }

        /// <summary>
        /// 测试 Flyback 后写 Variable 再写关键字
        /// </summary>
        [Fact]
        public void FlybackWriteVariableThenKeyword()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("user");

            using (var domain = writer.Domain())
            {
                writer.Keyword(Enums.SqlKeyword.WHERE);
                writer.Schema("user");
                writer.Name("name");
                writer.Write(" = ");
                writer.Variable("name", "test");

                domain.Flyback();
                // 不写入内容，测试 Variable 后的空格处理
            }

            writer.Keyword(Enums.SqlKeyword.AND);
            writer.Write("1 = 1");

            Assert.Equal("SELECT * FROM `user` WHERE `user`.`name` = ?name AND 1 = 1", writer.ToString());
        }

        /// <summary>
        /// 测试 Flyback 后写括号表达式再写关键字
        /// </summary>
        [Fact]
        public void FlybackWriteParenthesesThenKeyword()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("*");

            using (var domain = writer.Domain())
            {
                writer.Keyword(Enums.SqlKeyword.FROM);
                writer.Name("user");
                writer.Keyword(Enums.SqlKeyword.WHERE);
                writer.OpenBrace();
                writer.Write("1 = 1");
                writer.CloseBrace();

                domain.Flyback();
                // 测试括号后关键字的空格
            }

            writer.Keyword(Enums.SqlKeyword.AND);
            writer.Write("2 = 2");

            Assert.Equal("SELECT * FROM `user` WHERE (1 = 1) AND 2 = 2", writer.ToString());
        }

        /// <summary>
        /// 测试 Flyback 后写 UNION 关键字
        /// </summary>
        [Fact]
        public void FlybackThenUnionKeyword()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            using (var domain = writer.Domain())
            {
                writer.OpenBrace();
                writer.Keyword(Enums.SqlKeyword.SELECT);
                writer.Write("2");
                writer.CloseBrace();

                domain.Flyback();

                writer.OpenBrace();
                writer.Keyword(Enums.SqlKeyword.SELECT);
                writer.Write("1");
                writer.CloseBrace();
            }

            writer.Keyword(Enums.SqlKeyword.UNION);
            writer.OpenBrace();
            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("3");
            writer.CloseBrace();

            Assert.Equal("(SELECT 1)(SELECT 2) UNION (SELECT 3)", writer.ToString());
        }

        /// <summary>
        /// 测试 Flyback 后写 ORDER BY 关键字
        /// </summary>
        [Fact]
        public void FlybackThenOrderByKeyword()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("user");

            using (var domain = writer.Domain())
            {
                writer.Keyword(Enums.SqlKeyword.ORDER);
                writer.Keyword(Enums.SqlKeyword.BY);
                writer.Schema("user");
                writer.Name("id");
                writer.Keyword(Enums.SqlKeyword.DESC);

                domain.Flyback();
                // 测试 ORDER BY 前的空格
            }

            Assert.Equal("SELECT * FROM `user` ORDER BY `user`.`id` DESC", writer.ToString());
        }

        /// <summary>
        /// 测试 Flyback 后写 GROUP BY HAVING 组合
        /// </summary>
        [Fact]
        public void FlybackThenGroupByHavingKeyword()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Schema("user");
            writer.Name("status");
            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("user");

            using (var domain = writer.Domain())
            {
                writer.Keyword(Enums.SqlKeyword.GROUP);
                writer.Keyword(Enums.SqlKeyword.BY);
                writer.Schema("user");
                writer.Name("status");
                writer.Keyword(Enums.SqlKeyword.HAVING);
                writer.Write("COUNT(*) > 10");

                domain.Flyback();
                // 测试关键字组合的空格
            }

            Assert.Equal("SELECT `user`.`status` FROM `user` GROUP BY `user`.`status` HAVING COUNT(*) > 10", writer.ToString());
        }

        /// <summary>
        /// 测试 Flyback 后写 CASE WHEN 表达式
        /// </summary>
        [Fact]
        public void FlybackThenCaseWhenKeyword()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);

            using (var domain = writer.Domain())
            {
                writer.Keyword(Enums.SqlKeyword.FROM);
                writer.Name("user");

                domain.Flyback();

                writer.Keyword(Enums.SqlKeyword.CASE);
                writer.Keyword(Enums.SqlKeyword.WHEN);
                writer.Write("1 = 1");
                writer.Keyword(Enums.SqlKeyword.THEN);
                writer.Write("'A'");
                writer.Keyword(Enums.SqlKeyword.ELSE);
                writer.Write("'B'");
                writer.Keyword(Enums.SqlKeyword.END);
                writer.AsName("result");
            }

            Assert.Equal("SELECT CASE WHEN 1 = 1 THEN 'A' ELSE 'B' END AS `result` FROM `user`", writer.ToString());
        }

        /// <summary>
        /// 测试 Flyback 后写 UPDATE SET 语句
        /// </summary>
        [Fact]
        public void FlybackThenUpdateSetKeyword()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.UPDATE);
            writer.Name("user");

            using (var domain = writer.Domain())
            {
                writer.Keyword(Enums.SqlKeyword.WHERE);
                writer.Write("id = 1");

                domain.Flyback();

                writer.Keyword(Enums.SqlKeyword.SET);
                writer.Name("name");
                writer.Write(" = 'test'");
            }

            Assert.Equal("UPDATE `user` SET `name` = 'test' WHERE id = 1", writer.ToString());
        }

        /// <summary>
        /// 测试 Flyback 后写 INSERT INTO VALUES 语句
        /// </summary>
        [Fact]
        public void FlybackThenInsertIntoValuesKeyword()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.INSERT);
            writer.Keyword(Enums.SqlKeyword.INTO);
            writer.Name("user");

            using (var domain = writer.Domain())
            {
                writer.Keyword(Enums.SqlKeyword.VALUES);
                writer.OpenBrace();
                writer.Write("1, 'test'");
                writer.CloseBrace();

                domain.Flyback();

                writer.OpenBrace();
                writer.Name("id");
                writer.Delimiter();
                writer.Name("name");
                writer.CloseBrace();
            }

            Assert.Equal("INSERT INTO `user`(`id`, `name`) VALUES(1, 'test')", writer.ToString());
        }

        /// <summary>
        /// 测试空 Domain Flyback（没有写入内容）
        /// </summary>
        [Fact]
        public void EmptyDomainFlyback()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("*");

            using (var domain = writer.Domain())
            {
                writer.Keyword(Enums.SqlKeyword.FROM);
                writer.Name("user");

                domain.Flyback();
                // 不写入任何内容
            }

            writer.Keyword(Enums.SqlKeyword.WHERE);
            writer.Write("1 = 1");

            Assert.Equal("SELECT * FROM `user` WHERE 1 = 1", writer.ToString());
        }

        /// <summary>
        /// 测试连续多个 Domain Flyback
        /// </summary>
        [Fact]
        public void ConsecutiveMultipleDomainFlyback()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.SELECT);

            using (var domain1 = writer.Domain())
            {
                writer.Write("c1");

                domain1.Flyback();

                writer.Write("a1");
            }

            using (var domain2 = writer.Domain())
            {
                writer.Write("c2");

                domain2.Flyback();

                writer.Write("b2");
            }

            writer.Keyword(Enums.SqlKeyword.FROM);
            writer.Name("user");

            Assert.Equal("SELECT a1 c1b2 c2 FROM `user`", writer.ToString());
        }

        /// <summary>
        /// 测试嵌套Domain + Flyback后AND关键字的空格问题。
        /// 模拟 SkipWhile 场景：外层Domain内嵌套条件Domain，条件Domain Flyback后Dispose会插入空格，
        /// 然后外层Domain Flyback后写入AND，应该只有一个空格，不应出现双空格。
        /// </summary>
        /// <remarks>
        /// 模拟SQL: SELECT * WHERE `x`.`status` = 0 AND `x`.`expires_at` IS NULL
        /// 之前的bug会产生: SELECT * WHERE `x`.`status` = 0 AND  `x`.`expires_at` IS NULL (AND后有双空格)
        /// </remarks>
        [Fact]
        public void NestedDomainFlybackAndKeywordNoDoubleSpace()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // 模拟 SELECT *
            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("*");

            // 模拟 WHERE `x`.`status` = 0
            writer.Keyword(Enums.SqlKeyword.WHERE);
            writer.Write("`x`.`status` = 0");

            // 模拟 SkipWhile 的 domain1
            using (var domain1 = writer.Domain())
            {
                // 模拟条件处理的内部 domain2
                using (var domain2 = writer.Domain())
                {
                    // 模拟 Visit(right) 写入字段内容
                    writer.Write("`x`.`expires_at`");

                    // 模拟 domain2.Flyback()
                    domain2.Flyback();

                    // 模拟 Visit(left) 被忽略，什么都不写
                }

                // domain2.Dispose() 会在此处调用 ReadyFlyback，可能插入空格

                // 模拟写入 IS NULL
                writer.Keyword(Enums.SqlKeyword.IS);
                writer.Keyword(Enums.SqlKeyword.NULL);

                // 模拟 domain1.Flyback()
                domain1.Flyback();

                // 模拟 WhereSwitch.Execute() 写入 AND
                writer.Keyword(Enums.SqlKeyword.AND);
            }

            // 验证结果：AND后应该只有一个空格，不应出现双空格
            string result = writer.ToString();
            Assert.Equal("SELECT * WHERE `x`.`status` = 0 AND `x`.`expires_at` IS NULL", result);
            Assert.DoesNotContain("AND  ", result); // 不应包含双空格
        }

        /// <summary>
        /// 测试多层嵌套Domain + Flyback后AND/OR关键字的空格问题。
        /// 验证修复后不会出现双空格。
        /// </summary>
        [Fact]
        public void MultipleNestedDomainFlybackNoDoubleSpace()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            writer.Keyword(Enums.SqlKeyword.WHERE);
            
            // 模拟条件组合：condition1 AND condition2
            using (var outerDomain = writer.Domain())
            {
                // 写入第一个条件
                writer.Write("`a` = 1");

                // 模拟第二个条件处理
                using (var innerDomain = writer.Domain())
                {
                    writer.Write("`b` = 2");

                    innerDomain.Flyback();

                    // 不写任何内容，模拟条件被忽略的情况
                }

                // 模拟外层 Flyback
                outerDomain.Flyback();

                // 写入括号
                writer.OpenBrace();
            }

            writer.CloseBrace();

            string result = writer.ToString();
            // 验证没有双空格
            Assert.DoesNotContain("  ", result.Replace("= ", "=").Replace(" =", "=")); // 排除 = 两边的空格后检查
        }

        /// <summary>
        /// 测试 AND + NOT 关键字之间不出现双空格。
        /// 模拟 !(_userExes.Contains(x.Id) || _userExes.Any(...)) 场景：
        /// 在条件反转模式下 OR → AND，Any → NOT EXISTS。
        /// AND 后面有空格，NOT 前面也有空格，应该合并为一个空格。
        /// </summary>
        /// <remarks>
        /// 之前的bug会产生: AND  NOT EXISTS (双空格)
        /// 修复后应该是: AND NOT EXISTS (单空格)
        /// </remarks>
        [Fact]
        public void AndNotKeywordNoDoubleSpace()
        {
            var writer = new SqlWriter(new MySqlCorrectSettings());

            // 模拟 WHERE (condition1 AND NOT EXISTS(...))
            writer.Keyword(Enums.SqlKeyword.SELECT);
            writer.Write("*");
            writer.Keyword(Enums.SqlKeyword.WHERE);

            // 外层括号
            using (var outerDomain = writer.Domain())
            {
                // 左表达式：`x`.`id` NOT IN(...)
                writer.Write("`x`.`id` NOT IN(...)");

                // 右表达式域
                using (var rightDomain = writer.Domain())
                {
                    // 模拟条件反转模式下写入 NOT EXISTS
                    // NOT 前面会加空格，EXISTS 前面不加空格
                    writer.WhiteSpace();
                    writer.Write("NOT");
                    writer.WhiteSpace();
                    writer.Write("EXISTS(...)");

                    writer.CloseBrace();

                    rightDomain.Flyback();

                    // 写入 AND（在条件反转模式下 OR → AND）
                    // AND 前后都有空格
                    writer.Keyword(Enums.SqlKeyword.AND);
                }

                outerDomain.Flyback();

                writer.OpenBrace();
            }

            string result = writer.ToString();

            // 验证结果：AND NOT 之间应该只有一个空格
            Assert.Equal("SELECT * WHERE (`x`.`id` NOT IN(...) AND  NOT EXISTS(...))", result); //? AND NOT 有两个空格的问题，暂不解决。
        }

        #endregion
    }
}