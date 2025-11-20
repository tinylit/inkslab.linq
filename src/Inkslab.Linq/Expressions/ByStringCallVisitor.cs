using System;
using System.Linq;
using System.Linq.Expressions;
using Inkslab.Linq.Enums;

namespace Inkslab.Linq.Expressions
{
    /// <summary>
    /// 字符串访问器。
    /// </summary>
    public class ByStringCallVisitor : BaseVisitor
    {
        private bool ignoreNull = false;
        private bool ignoreEmptyString = false;
        private bool ignoreWhiteSpace = false;

        /// <inheritdoc/>
        public ByStringCallVisitor(BaseVisitor visitor) : base(visitor)
        {
        }

        /// <inheritdoc/>
        protected override void Startup(MethodCallExpression node)
        {
            string name = node.Method.Name;

            switch (name)
            {
                case nameof(string.Contains) when node.Arguments.Count == 1:
                case nameof(string.EndsWith) when node.Arguments.Count == 1:
                case nameof(string.StartsWith) when node.Arguments.Count == 1:

                    using (var domain = Writer.Domain())
                    {
                        ignoreNull = true;
                        ignoreEmptyString = true;

                        Visit(node.Arguments[0]);

                        ignoreEmptyString = false;
                        ignoreNull = false;

                        if (domain.IsEmpty)
                        {
                            break;
                        }

                        if (name is (nameof(string.Contains)) or (nameof(string.StartsWith)))
                        {
                            if (Engine is DatabaseEngine.PostgreSQL or DatabaseEngine.Oracle)
                            {
                                Writer.Write(" || ");
                            }
                            else if (Engine == DatabaseEngine.MySQL)
                            {
                                Writer.Delimiter();
                            }
                            else
                            {
                                Writer.Write(" + ");
                            }

                            Writer.Write("'%'");
                        }

                        Writer.CloseBrace();

                        domain.Flyback();

                        Visit(node.Object);

                        Writer.Keyword(SqlKeyword.LIKE);

                        if (Engine == DatabaseEngine.MySQL)
                        {
                            Writer.Write("CONCAT");
                        }

                        Writer.OpenBrace();

                        if (name is (nameof(string.Contains)) or (nameof(string.EndsWith)))
                        {
                            Writer.Write("'%'");

                            if (Engine is DatabaseEngine.PostgreSQL or DatabaseEngine.Oracle)
                            {
                                Writer.Write(" || ");
                            }
                            else if (Engine == DatabaseEngine.MySQL)
                            {
                                Writer.Delimiter();
                            }
                            else
                            {
                                Writer.Write(" + ");
                            }
                        }
                    }

                    break;
                case nameof(string.IsNullOrEmpty):

                    using (var domain = Writer.Domain())
                    {
                        ignoreNull = true;
                        ignoreEmptyString = true;

                        Visit(node.Arguments[0]);

                        ignoreEmptyString = false;
                        ignoreNull = false;

                        if (domain.IsEmpty)
                        {
                            break;
                        }

                        string text = domain.ToString();

                        Writer.Keyword(SqlKeyword.IS);
                        Writer.Keyword(SqlKeyword.NULL);

                        Writer.Keyword(SqlKeyword.OR);

                        Writer.Write(text);

                        Writer.Operator(SqlOperator.Equal);

                        Writer.EmptyString();

                        Writer.CloseBrace();

                        domain.Flyback();

                        Writer.OpenBrace();
                    }

                    break;
                case nameof(string.IsNullOrWhiteSpace):

                    using (var domain = Writer.Domain())
                    {
                        ignoreNull = true;
                        ignoreEmptyString = true;
                        ignoreWhiteSpace = true;

                        Visit(node.Arguments[0]);

                        ignoreWhiteSpace = false;
                        ignoreEmptyString = false;
                        ignoreNull = false;

                        if (domain.IsEmpty)
                        {
                            break;
                        }

                        string text = domain.ToString();

                        Writer.Keyword(SqlKeyword.IS);
                        Writer.Keyword(SqlKeyword.NULL);

                        Writer.Keyword(SqlKeyword.OR);

                        if (Engine == DatabaseEngine.PostgreSQL)
                        {
                            Writer.Write("TRIM");
                            Writer.OpenBrace();
                            Writer.Write("BOTH FROM ");

                            Writer.Write(text);
                        }
                        else
                        {
                            Writer.Write("LTRIM");
                            Writer.OpenBrace();
                            Writer.Write("RTRIM");
                            Writer.OpenBrace();

                            Writer.Write(text);

                            Writer.CloseBrace();
                        }

                        Writer.CloseBrace();

                        Writer.Operator(SqlOperator.Equal);

                        Writer.EmptyString();

                        Writer.CloseBrace();

                        domain.Flyback();

                        Writer.OpenBrace();
                    }

                    break;
                case nameof(string.Replace) when node.Arguments.Count == 2:

                    Writer.Write("REPLACE");

                    Writer.OpenBrace();

                    Visit(node.Object);

                    foreach (Expression item in node.Arguments)
                    {
                        Writer.Delimiter();

                        Visit(item);
                    }

                    Writer.CloseBrace();

                    break;
                case nameof(string.Substring):

                    using (var domain = Writer.Domain())
                    {
                        Visit(node.Object);

                        string text = domain.ToString();

                        Writer.Keyword(SqlKeyword.IS);
                        Writer.Keyword(SqlKeyword.NULL);
                        Writer.Keyword(SqlKeyword.THEN);
                        Writer.Keyword(SqlKeyword.NULL);
                        Writer.Keyword(SqlKeyword.ELSE);

                        switch (Engine)
                        {
                            case DatabaseEngine.Sybase:
                            case DatabaseEngine.SqlServer:
                            case DatabaseEngine.PostgreSQL:
                                Writer.Write("SUBSTRING");
                                break;
                            case DatabaseEngine.Access:
                                Writer.Write("MID");
                                break;
                            case DatabaseEngine.SQLite:
                            case DatabaseEngine.DB2:
                            case DatabaseEngine.Oracle:
                            case DatabaseEngine.MySQL:
                            default:
                                Writer.Write("SUBSTR");
                                break;
                        }

                        Writer.OpenBrace();

                        Writer.Write(text); //? 截取字符串主体。

                        var startPairt = node.Arguments[0];

                        if (Engine == DatabaseEngine.PostgreSQL)
                        {
                            Writer.Write("FROM ");
                        }
                        else
                        {
                            Writer.Delimiter();
                        }

                        if (IsPlainVariable(startPairt))
                        {
                            Writer.Constant(startPairt.GetValueFromExpression<int>() + 1);
                        }
                        else
                        {
                            Visit(startPairt);
                        }

                        if (node.Arguments.Count > 1)
                        {
                            if (Engine == DatabaseEngine.PostgreSQL)
                            {
                                Writer.Write(" FOR ");
                            }
                            else
                            {
                                Writer.Delimiter();
                            }

                            var lengthPairt = node.Arguments[1];

                            if (IsPlainVariable(lengthPairt))
                            {
                                Writer.Constant(lengthPairt.GetValueFromExpression<int>());
                            }
                            else
                            {
                                Visit(lengthPairt);
                            }
                        }

                        domain.Flyback();

                        Writer.Keyword(SqlKeyword.CASE);
                        Writer.Keyword(SqlKeyword.WHEN);
                    }

                    Writer.CloseBrace();

                    Writer.Keyword(SqlKeyword.END);

                    break;
                case nameof(string.ToUpper) when node.Arguments.Count == 0:

                    Writer.Write("UPPER");
                    Writer.OpenBrace();

                    Visit(node.Object);

                    Writer.CloseBrace();

                    break;
                case nameof(string.ToLower) when node.Arguments.Count == 0:

                    Writer.Write("LOWER");
                    Writer.OpenBrace();

                    Visit(node.Object);

                    Writer.CloseBrace();

                    break;
                case nameof(string.TrimStart) when node.Arguments.Count == 0:

                    if (Engine == DatabaseEngine.PostgreSQL)
                    {
                        Writer.Write("TRIM");
                        Writer.OpenBrace();
                        Writer.Write("LEADING FROM ");
                    }
                    else
                    {
                        Writer.Write("LTRIM");
                        Writer.OpenBrace();
                    }

                    Visit(node.Object);

                    Writer.CloseBrace();

                    break;
                case nameof(string.TrimEnd) when node.Arguments.Count == 0:

                    if (Engine == DatabaseEngine.PostgreSQL)
                    {
                        Writer.Write("TRIM");
                        Writer.OpenBrace();
                        Writer.Write("TRAILING FROM ");
                    }
                    else
                    {
                        Writer.Write("RTRIM");
                        Writer.OpenBrace();
                    }

                    Visit(node.Object);

                    Writer.CloseBrace();

                    break;
                case nameof(string.Trim) when node.Arguments.Count == 0:

                    if (Engine == DatabaseEngine.PostgreSQL)
                    {
                        Writer.Write("TRIM");
                        Writer.OpenBrace();
                        Writer.Write("BOTH FROM ");
                    }
                    else
                    {
                        Writer.Write("LTRIM");
                        Writer.OpenBrace();

                        Writer.Write("RTRIM");
                        Writer.OpenBrace();
                    }


                    Visit(node.Object);

                    Writer.CloseBrace();

                    if (Engine == DatabaseEngine.PostgreSQL)
                    {
                        break;
                    }

                    Writer.CloseBrace();

                    break;
                case nameof(string.IndexOf) when node.Arguments.All(x => x.Type == Types.String || x.Type == Types.Int32):

                    switch (Engine)
                    {
                        case DatabaseEngine.Oracle when node.Arguments.Count <= 2:
                            Writer.Write("INSTR");

                            Writer.OpenBrace();

                            Visit(node.Object);

                            Writer.Delimiter();

                            Visit(node.Arguments[0]);

                            if (node.Arguments.Count > 1)
                            {
                                Writer.Delimiter();

                                Visit(node.Arguments[1]);
                            }

                            Writer.CloseBrace();

                            break;
                        case DatabaseEngine.MySQL when node.Arguments.Count <= 2:

                            Writer.Write("LOCATE");

                            Writer.OpenBrace();

                            Visit(node.Arguments[0]);

                            Writer.Delimiter();

                            Visit(node.Object);

                            if (node.Arguments.Count > 1)
                            {
                                Writer.Delimiter();

                                Visit(node.Arguments[1]);
                            }

                            Writer.CloseBrace();

                            break;
                        case DatabaseEngine.PostgreSQL when node.Arguments.Count == 1:

                            Writer.Write("POSITION");

                            Writer.OpenBrace();

                            Visit(node.Arguments[0]);

                            Writer.Write(" IN ");

                            Visit(node.Object);

                            Writer.CloseBrace();

                            break;
                        case DatabaseEngine.DB2 when node.Arguments.Count <= 2:
                        case DatabaseEngine.Sybase when node.Arguments.Count == 1:
                        case DatabaseEngine.SqlServer when node.Arguments.Count <= 2:
                            Writer.Write("CHARINDEX");

                            Writer.OpenBrace();

                            Visit(node.Arguments[0]);

                            Writer.Delimiter();

                            Visit(node.Object);

                            if (node.Arguments.Count > 1)
                            {
                                Writer.Delimiter();

                                Visit(node.Arguments[1]);
                            }

                            Writer.CloseBrace();
                            break;
                        case DatabaseEngine.Access when node.Arguments.Count <= 2:

                            Writer.Write("InStr");

                            Writer.OpenBrace();

                            if (node.Arguments.Count > 1)
                            {
                                Visit(node.Arguments[1]);

                                Writer.Delimiter();
                            }

                            Visit(node.Object);

                            Writer.Delimiter();

                            Visit(node.Arguments[0]);

                            Writer.CloseBrace();

                            break;
                        case DatabaseEngine.SQLite when node.Arguments.Count == 1:

                            Writer.Write("InStr");

                            Writer.OpenBrace();

                            Visit(node.Object);

                            Writer.Delimiter();

                            Visit(node.Arguments[0]);

                            Writer.CloseBrace();

                            break;
                        default:
                            throw new NotSupportedException();
                    }

                    Writer.Write(" - 1"); //? 数据库下标从“1”开始，所以需要减“1”。

                    break;
                case nameof(string.Concat) when node.Arguments.Count > 1:

                    if (Engine == DatabaseEngine.MySQL)
                    {
                        Writer.Write("CONCAT");
                    }

                    Writer.OpenBrace();

                    for (int i = 0; i < node.Arguments.Count; i++)
                    {
                        if (i > 0)
                        {
                            if (Engine is DatabaseEngine.PostgreSQL or DatabaseEngine.Oracle)
                            {
                                Writer.Write(" || ");
                            }
                            else if (Engine == DatabaseEngine.MySQL)
                            {
                                Writer.Delimiter();
                            }
                            else
                            {
                                Writer.Write(" + ");
                            }
                        }

                        Visit(node.Arguments[i]);
                    }

                    Writer.CloseBrace();

                    break;
                default:
                    throw new NotSupportedException($"字符串的“{node.Method}”方法不被支持！");
            }
        }


        /// <inheritdoc/>
        protected override void Constant(object value)
        {
            if (ignoreNull && value is null)
            {
            }
            else if ((ignoreEmptyString || ignoreWhiteSpace)
                && value is string text
                && (ignoreWhiteSpace
                    ? string.IsNullOrWhiteSpace(text)
                    : string.IsNullOrEmpty(text)))
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
            if (ignoreNull && value is null)
            {
                return;
            }
            else if ((ignoreEmptyString || ignoreWhiteSpace)
                && value is string text
                && (ignoreWhiteSpace
                    ? string.IsNullOrWhiteSpace(text)
                    : string.IsNullOrEmpty(text)))
            {

            }
            else
            {
                base.Variable(name, value);
            }
        }
    }
}