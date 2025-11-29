using System;
using System.Diagnostics;
using System.Linq.Expressions;
using Inkslab.Linq.Enums;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 条件。
    /// </summary>
    [DebuggerDisplay("Coalesce")]
    public class CoalesceVisitor : CoreVisitor
    {
        /// <inheritdoc/>
        public CoalesceVisitor(BaseVisitor visitor) : base(visitor)
        {
        }

        /// <summary>
        /// 启动。
        /// </summary>
        /// <param name="node"></param>
        public override void Startup(Expression node)
        {
            if (node.NodeType != ExpressionType.Coalesce)
            {
                throw new InvalidOperationException("Node type must be Coalesce.");
            }

            Visit(node);
        }

        /// <inheritdoc/>
        protected override void Constant(object value)
        {
            if (value is null)
            {
            }
            else
            {
                base.Constant(value);
            }
        }

        /// <inheritdoc/>
        protected override void Variable(string name, object value)
        {
            if (value is null)
            {
            }
            else
            {
                base.Variable(name, value);
            }
        }

        /// <inheritdoc/>
        protected override void Binary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Coalesce)
            {
                Coalesce(node.Left, node.Right);
            }
            else
            {
                base.Binary(node);
            }
        }

        /// <summary>
        /// <see cref="ExpressionType.Coalesce"/>.
        /// </summary>
        protected virtual void Coalesce(Expression left, Expression right)
        {
            switch (Engine)
            {
                case DatabaseEngine.Oracle:
                case DatabaseEngine.DB2:
                case DatabaseEngine.Sybase:
                case DatabaseEngine.MySQL:
                case DatabaseEngine.SQLite:
                case DatabaseEngine.SqlServer:
                case DatabaseEngine.PostgreSQL:

                    using (var domain = Writer.Domain())
                    {
                        Visit(left);

                        if (domain.IsEmpty)
                        {
                            Visit(right);

                            if (domain.IsEmpty)
                            {
                                Writer.Keyword(SqlKeyword.NULL);
                            }
                        }
                        else
                        {
                            Writer.Delimiter();

                            int length = Writer.Length;

                            Visit(right);

                            if (length == Writer.Length)
                            {
                                Writer.Keyword(SqlKeyword.NULL);
                            }

                            Writer.CloseBrace();

                            domain.Flyback();

                            switch (Engine)
                            {
                                case DatabaseEngine.Oracle:
                                    Writer.Write("NVL");
                                    break;
                                case DatabaseEngine.MySQL:
                                case DatabaseEngine.SQLite:
                                    Writer.Write("IFNULL");
                                    break;
                                case DatabaseEngine.DB2:
                                case DatabaseEngine.Sybase:
                                case DatabaseEngine.SqlServer:
                                    Writer.Write("ISNULL");
                                    break;
                                case DatabaseEngine.PostgreSQL:
                                    Writer.Write("COALESCE");
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }

                            Writer.OpenBrace();
                        }
                    }

                    break;
                default:

                    using (var domain = Writer.Domain())
                    {
                        Visit(left);

                        if (domain.IsEmpty)
                        {
                            Visit(right);

                            if (domain.IsEmpty)
                            {
                                Writer.Keyword(SqlKeyword.NULL);
                            }
                        }
                        else
                        {
                            Writer.Keyword(SqlKeyword.THEN);

                            Visit(left);

                            Writer.Keyword(SqlKeyword.ELSE);

                            int length = Writer.Length;

                            Visit(right);

                            if (length == Writer.Length)
                            {
                                Writer.Keyword(SqlKeyword.NULL);
                            }

                            Writer.Keyword(SqlKeyword.END);

                            Writer.CloseBrace();

                            domain.Flyback();

                            Writer.OpenBrace();

                            Writer.Keyword(SqlKeyword.CASE);
                            Writer.Keyword(SqlKeyword.WHEN);
                        }
                    }

                    break;
            }
        }
    }
}