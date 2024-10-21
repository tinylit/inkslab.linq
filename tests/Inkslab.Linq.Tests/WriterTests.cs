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

                if (domain.Length > 0)
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

                if (transaction.Length > 0)
                {
                    transaction.Flyback();

                    writer.Write(" * ");
                }
            }

            using (var tran = writer.Domain())
            {
                writer.Write("x.Id > 0 AND x.timestamp = 1024");

                if (tran.Length > 0)
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

                    if (tran.Length > 0)
                    {
                        tran.Flyback();

                        writer.Write(" WHERE ");
                    }
                }

                if (transaction.Length > 0)
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

                    if (tran.Length > 0)
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

                        if (domain.Length > 0)
                        {
                            domain.Flyback();

                            writer.Write(" AND ");
                        }
                    }

                    if (tran.Length > 0)
                    {
                        tran.Flyback();

                        writer.Write(" AND ");
                    }
                }

                if (transaction.Length > 0)
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

            Assert.Equal("NOT (", writer.ToString());
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
    }
}