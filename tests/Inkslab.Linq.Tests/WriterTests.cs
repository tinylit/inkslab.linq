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
    }
}