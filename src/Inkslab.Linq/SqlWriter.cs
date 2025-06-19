﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Inkslab.Linq.Enums;

namespace Inkslab.Linq
{
    /// <summary>
    /// T-SQL 写入流。
    /// </summary>
    public class SqlWriter : IDisposable
    {
        private sealed class Writer : IDisposable
        {
            private const char WHITESPACE = ' ';

            private readonly StringBuilder _sb;
            private readonly SqlWriter _writer;

            private int cursorPosition = -1;

            //? 处理作为方法的 NOT 指令。
            private bool writtenNOT = false;

            private bool ignoreNOT = false;

            //? 刚刚写入 CASE THEN。
            private bool justWrittenWhiteCase = false;

            //? 刚刚写入 DELETE FROM。
            private bool justWrittenWhiteDelete = false;

            public Writer(SqlWriter writer, int capacity)
            {
                _sb = new StringBuilder(capacity);

                _writer = writer;
            }

            public int CursorPosition
            {
                get
                {
                    if (cursorPosition == -1)
                    {
                        return _sb.Length;
                    }

                    return cursorPosition;
                }
                private set => cursorPosition = value;
            }

            public int Length => _sb.Length;

            public void WhiteSpace() => Write(WHITESPACE);

            public void Keyword(SqlKeyword keyword)
            {
                switch (keyword)
                {
                    case SqlKeyword.DELETE:

                        Write(keyword.ToString());

                        WhiteSpace();

                        justWrittenWhiteDelete = true;

                        break;
                    case SqlKeyword.SELECT:
                    case SqlKeyword.DISTINCT:
                    case SqlKeyword.UPDATE:
                    case SqlKeyword.INSERT:
                    case SqlKeyword.IGNORE:
                    case SqlKeyword.INTO:
                    case SqlKeyword.ALL:

                        Write(keyword.ToString());
                        WhiteSpace();

                        break;
                    case SqlKeyword.CASE:

                        Write(keyword.ToString());

                        WhiteSpace();

                        justWrittenWhiteCase = true;

                        break;
                    case SqlKeyword.WHEN:

                        if (!justWrittenWhiteCase)
                        {
                            WhiteSpace();
                        }

                        Write(keyword.ToString());
                        WhiteSpace();

                        break;
                    case SqlKeyword.END:

                        WhiteSpace();
                        Write(keyword.ToString());

                        break;
                    case SqlKeyword.FROM:

                        if (!justWrittenWhiteDelete)
                        {
                            WhiteSpace();
                        }

                        Write(keyword.ToString());
                        WhiteSpace();

                        break;
                    case SqlKeyword.IS:
                    case SqlKeyword.AS:
                    case SqlKeyword.SET:
                    case SqlKeyword.JOIN:
                    case SqlKeyword.ON:
                    case SqlKeyword.WHERE:
                    case SqlKeyword.AND:
                    case SqlKeyword.OR:
                    case SqlKeyword.LIKE:
                    case SqlKeyword.BY:
                    case SqlKeyword.HAVING:
                    case SqlKeyword.THEN:
                    case SqlKeyword.ELSE:
                    case SqlKeyword.UNION:
                    case SqlKeyword.INTERSECT:
                    case SqlKeyword.EXCEPT:

                        WhiteSpace();
                        Write(keyword.ToString());
                        WhiteSpace();

                        break;
                    case SqlKeyword.NOT:

                        ignoreNOT ^= true;

                        if (writtenNOT)
                        {
                            writtenNOT = false;

                            if (!ignoreNOT)
                            {
                                Write(keyword.ToString());

                                WhiteSpace();
                            }
                        }

                        break;
                    case SqlKeyword.EXISTS:

                        if (ignoreNOT)
                        {
                            writtenNOT = true;

                            _writer.Keyword(SqlKeyword.NOT);
                        }

                        Write(keyword.ToString());
                        break;
                    case SqlKeyword.CROSS:
                    case SqlKeyword.INNER:
                    case SqlKeyword.LEFT:
                    case SqlKeyword.RIGHT:
                    case SqlKeyword.OUTER:
                    case SqlKeyword.IN:
                    case SqlKeyword.ASC:
                    case SqlKeyword.DESC:
                    case SqlKeyword.GROUP:
                    case SqlKeyword.ORDER:
                    case SqlKeyword.VALUES:

                        WhiteSpace();
                        Write(keyword.ToString());

                        break;
                    case SqlKeyword.NULL:

                        Write(keyword.ToString());
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            public void Write(char value)
            {
                justWrittenWhiteCase &= false;
                justWrittenWhiteDelete &= false;

                if (ignoreNOT)
                {
                    writtenNOT = true;

                    _writer.Keyword(SqlKeyword.NOT);
                }

                if (cursorPosition > -1)
                {
                    _sb.Insert(cursorPosition, value);

                    cursorPosition++;
                }
                else
                {
                    _sb.Append(value);
                }
            }

            public void Write(string value)
            {
                justWrittenWhiteCase &= false;
                justWrittenWhiteDelete &= false;

                if (ignoreNOT)
                {
                    writtenNOT = true;

                    _writer.Keyword(SqlKeyword.NOT);
                }

                if (cursorPosition > -1)
                {
                    _sb.Insert(cursorPosition, value);

                    cursorPosition += value.Length;
                }
                else
                {
                    _sb.Append(value);
                }
            }

            public void Insert(int index, string value)
            {
                justWrittenWhiteCase = false;

                _sb.Insert(index, value);

                if (index <= cursorPosition)
                {
                    cursorPosition += value.Length;
                }
            }

            public ISqlDomain Domain() => new SqlDomain(this, _sb, _sb.Length, cursorPosition);

            public override string ToString() => _sb.ToString();

            public void Dispose() => _sb.Clear();

            private sealed class SqlDomain : ISqlDomain
            {
                private readonly Writer _writer;
                private readonly StringBuilder _sb;
                private readonly int _length;
                private readonly int _cursorPosition;

                public SqlDomain(Writer writer, StringBuilder sb, int length, int cursorPosition)
                {
                    _writer = writer;
                    _sb = sb;
                    _length = length;
                    _cursorPosition = cursorPosition;
                }

                public bool IsEmpty => _sb.Length == _length;

                public bool HasValue => _sb.Length > _length;

                public int Length => _sb.Length - _length;

                public void Discard()
                {
                    if (_cursorPosition > -1)
                    {
                        _sb.Remove(_cursorPosition, Length);
                    }
                    else
                    {
                        _sb.Length = _length;
                    }

                    _writer.CursorPosition = _cursorPosition;
                }

                public void Dispose()
                {
                    if (_cursorPosition == -1)
                    {
                        _writer.CursorPosition = -1;
                    }
                    else
                    {
                        _writer.CursorPosition = _cursorPosition + Length;
                    }
                }

                public void Flyback() =>
                    _writer.CursorPosition = _cursorPosition > -1 ? _cursorPosition : _length;

                public override string ToString() =>
                    _sb.ToString(_cursorPosition > -1 ? _cursorPosition : _length, Length);
            }
        }

        private readonly IDbCorrectSettings _settings;
        private readonly SqlWriter _writer;

        private int takeSize = 0;
        private int skipSize = 0;

        private readonly Writer _main;
        private readonly Writer _rank;

        private SqlWriter()
        {
            _main = new Writer(this, 128);
            _rank = new Writer(this, 32);
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="settings">SQL矫正配置。</param>
        public SqlWriter(IDbCorrectSettings settings)
            : this()
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            Parameters = new Dictionary<string, object>();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="writer">T-SQL 写入流。</param>
        public SqlWriter(SqlWriter writer)
            : this()
        {
            _writer = writer;

            _settings = writer._settings;

            Parameters = writer.Parameters;
        }

        private int parameterIndex = 0;

        /// <summary>
        /// 参数索引。
        /// </summary>
        protected int ParameterIndex => _writer is null ? ++parameterIndex : _writer.ParameterIndex;

        /// <summary>
        /// 内容长度。
        /// </summary>
        public int Length => _main.Length + _rank.Length;

        /// <summary>
        /// 输入位置。
        /// </summary>
        public int CursorPosition => IsRankingAnalysis ? _rank.CursorPosition : _main.CursorPosition;

        /// <summary>
        /// 条件取反。
        /// </summary>
        public bool IsConditionReversal { get; private set; }

        /// <summary>
        /// 是否正在分析排序代码。
        /// </summary>
        public bool IsRankingAnalysis { get; private set; }

        /// <summary>
        /// 参数。
        /// </summary>
        public Dictionary<string, object> Parameters { get; private set; }

        /// <summary>
        /// 写入关键字。
        /// </summary>
        /// <param name="keyword">关键字。</param>
        public virtual void Keyword(SqlKeyword keyword)
        {
            if (IsConditionReversal)
            {
                IsConditionReversal = false;

                switch (keyword)
                {
                    case SqlKeyword.IN:
                    case SqlKeyword.LIKE:
                    case SqlKeyword.EXISTS:
                        Keyword(SqlKeyword.NOT);
                        break;
                    case SqlKeyword.AND:
                        keyword = SqlKeyword.OR;
                        break;
                    case SqlKeyword.OR:
                        keyword = SqlKeyword.AND;
                        break;
                }

                IsConditionReversal = true;
            }

            if (IsRankingAnalysis)
            {
                _rank.Keyword(keyword);
            }
            else
            {
                _main.Keyword(keyword);
            }

            if (IsConditionReversal)
            {
                IsConditionReversal = false;

                if (keyword == SqlKeyword.IS)
                {
                    Keyword(SqlKeyword.NOT);
                }

                IsConditionReversal = true;
            }
        }

        /// <summary>
        /// 写入运算符。
        /// </summary>
        /// <param name="operator"></param>
        public virtual void Operator(SqlOperator @operator)
        {
            if (IsConditionReversal)
            {
                switch (@operator)
                {
                    case SqlOperator.GreaterThan:
                        @operator = SqlOperator.LessThanOrEqual;
                        break;
                    case SqlOperator.GreaterThanOrEqual:
                        @operator = SqlOperator.LessThan;
                        break;
                    case SqlOperator.Equal:
                        @operator = SqlOperator.NotEqual;
                        break;
                    case SqlOperator.LessThanOrEqual:
                        @operator = SqlOperator.GreaterThan;
                        break;
                    case SqlOperator.LessThan:
                        @operator = SqlOperator.GreaterThanOrEqual;
                        break;
                    case SqlOperator.NotEqual:
                        @operator = SqlOperator.Equal;
                        break;
                    case SqlOperator.IsTrue:
                        @operator = SqlOperator.IsFalse;
                        break;
                    case SqlOperator.IsFalse:
                        @operator = SqlOperator.IsTrue;
                        break;
                }
            }

            switch (@operator)
            {
                case SqlOperator.And:
                    Write(" & ");
                    break;
                case SqlOperator.ExclusiveOr:
                    Write(" ^ ");
                    break;
                case SqlOperator.Or:
                    Write(" | ");
                    break;
                case SqlOperator.LeftShift:
                    Write(" << ");
                    break;
                case SqlOperator.RightShift:
                    Write(" >> ");
                    break;
                case SqlOperator.GreaterThan:
                    Write(" > ");
                    break;
                case SqlOperator.GreaterThanOrEqual:
                    Write(" >= ");
                    break;
                case SqlOperator.Equal:
                    Write(" = ");
                    break;
                case SqlOperator.LessThanOrEqual:
                    Write(" <= ");
                    break;
                case SqlOperator.LessThan:
                    Write(" < ");
                    break;
                case SqlOperator.NotEqual:
                    Write(" <> ");
                    break;
                case SqlOperator.Add:
                    Write(" + ");
                    break;
                case SqlOperator.Subtract:
                    Write(" - ");
                    break;
                case SqlOperator.Multiply:
                    Write(" * ");
                    break;
                case SqlOperator.Divide:
                    Write(" / ");
                    break;
                case SqlOperator.Modulo:
                    Write(" % ");
                    break;
                case SqlOperator.Decrement:
                    Write(" - 1");
                    break;
                case SqlOperator.Increment:
                    Write(" + 1");
                    break;
                case SqlOperator.Negate:
                    Write('-');
                    break;
                case SqlOperator.UnaryPlus:
                    Write('+');
                    break;
                case SqlOperator.IsTrue:
                    Write(" = ");
                    True();
                    break;
                case SqlOperator.IsFalse:
                    Write(" = ");
                    False();
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// 查询条目数。
        /// </summary>
        /// <param name="takeSize">查询条目数。</param>
        public void TakeSize(int takeSize)
        {
            if (takeSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(takeSize), "参数值必须大于零!");
            }

            if (this.takeSize > 0 && takeSize > this.takeSize)
            {
                throw new IndexOutOfRangeException();
            }

            if (skipSize > 0)
            {
                if (skipSize > takeSize)
                {
                    throw new IndexOutOfRangeException();
                }

                takeSize -= skipSize;
            }

            this.takeSize = takeSize;
        }

        /// <summary>
        /// 跳过条目数。
        /// </summary>
        /// <param name="skipSize">跳过条目数。</param>
        public void SkipSize(int skipSize)
        {
            if (skipSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(skipSize), "参数不能小于0。");
            }

            this.skipSize += skipSize;
        }

        /// <summary>
        /// 查询索引位数据。
        /// </summary>
        /// <param name="index">索引值。</param>
        public void ElementAt(int index)
        {
            if (index < 0)
            {
                throw new IndexOutOfRangeException();
            }

            if (takeSize > 0 && index > takeSize)
            {
                throw new IndexOutOfRangeException();
            }

            takeSize = 1;

            skipSize += index;
        }

        /// <summary>
        /// ''
        /// </summary>
        public void EmptyString() => Write("''");

        /// <summary>
        /// (
        /// </summary>
        public void OpenBrace() => Write('(');

        /// <summary>
        /// ,
        /// </summary>
        public void Delimiter() => Write(", ");

        /// <summary>
        /// )
        /// </summary>
        public void CloseBrace() => Write(')');

        /// <summary>
        /// “ ”
        /// </summary>
        public void WhiteSpace()
        {
            if (IsRankingAnalysis)
            {
                _rank.WhiteSpace();
            }
            else
            {
                _main.WhiteSpace();
            }
        }

        /// <summary>
        /// 架构。
        /// </summary>
        /// <param name="name">架构名称。</param>
        /// <example>“dbo.Users”中的“dbo”。</example>
        /// <example>“x.Name”中的“x”。</example>
        public void Schema(string name)
        {
            if (name?.Length > 0)
            {
                Name(name);

                Write('.');
            }
        }

        /// <summary>
        /// 名称。
        /// </summary>
        /// <param name="name">名称。</param>
        public void Name(string name) => Write(_settings.Name(name));

        /// <summary>
        /// 别名。
        /// </summary>
        /// <param name="name">别名。</param>
        /// <example>如“x.name AS Name”中的“Name”。</example>
        public virtual void AsName(string name)
        {
            if (name?.Length > 0)
            {
                Keyword(SqlKeyword.AS);

                Name(name);
            }
        }

        /// <summary>
        /// 常量。
        /// </summary>
        /// <param name="value">值。</param>
        public void Constant(object value)
        {
            switch (value)
            {
                case null:
                    Write("NULL");
                    break;
                case byte:
                case sbyte:
                case short:
                case ushort:
                case int:
                case uint:
                case long:
                case ulong:
                case float:
                case double:
                case decimal:
                    Write(value.ToString());

                    break;
                case char c:
                    Write('\'');
                    Write(c);
                    Write('\'');

                    break;
                case bool boolean:
                    if (boolean)
                    {
                        True();
                    }
                    else
                    {
                        False();
                    }
                    break;
                case string text:
                    if (text.Length == 0)
                    {
                        EmptyString();

                        break;
                    }

                    Variable(null, text);

                    break;
                case Version version:
                    Write('\'');
                    Write(version.ToString());
                    Write('\'');

                    break;
                case Enum @enum:

                    Write(@enum.ToString("D"));

                    break;
                case TimeSpan timeSpan:
                    Write(timeSpan.TotalMilliseconds.ToString());
                    break;
                default:
                    Variable(null, value);

                    break;
            }
        }

        /// <summary>
        /// 参数。
        /// </summary>
        /// <param name="varName">参数名称。</param>
        /// <param name="varValue">参数值。</param>
        public virtual void Variable(string varName, object varValue)
        {
            if (varValue is Version version)
            {
                varValue = version.ToString();
            }

            if (varName is null || varName.Length == 0)
            {
                varName = $"__var_{ParameterIndex}_val";
            }

            bool flag = true;

            string argName = varName;

            while (Parameters.TryGetValue(argName, out object argValue))
            {
                if (Equals(argValue, varValue))
                {
                    flag = false;

                    break;
                }

                argName = string.Concat(varName, "_", ParameterIndex.ToString());
            }

            Write(_settings.ParamterName(argName));

            if (flag)
            {
                Parameters.Add(argName, varValue);
            }
        }

        /// <summary>
        /// true。
        /// </summary>
        public virtual void True()
        {
            switch (_settings.Engine)
            {
                case DatabaseEngine.MySQL:
                case DatabaseEngine.PostgreSQL:
                    Write("TRUE");
                    break;
                case DatabaseEngine.SqlServer:
                    Write("CAST(1 AS BIT)");
                    break;
                case DatabaseEngine.Oracle:
                case DatabaseEngine.Sybase:
                case DatabaseEngine.Access:
                case DatabaseEngine.SQLite:
                case DatabaseEngine.DB2:
                default:
                    Variable("__var_true_val", true);
                    break;
            }
        }

        /// <summary>
        /// false。
        /// </summary>
        public virtual void False()
        {
            switch (_settings.Engine)
            {
                case DatabaseEngine.MySQL:
                case DatabaseEngine.PostgreSQL:
                    Write("FALSE");
                    break;
                case DatabaseEngine.SqlServer:
                    Write("CAST(0 AS BIT)");
                    break;
                case DatabaseEngine.Oracle:
                case DatabaseEngine.Sybase:
                case DatabaseEngine.Access:
                case DatabaseEngine.SQLite:
                case DatabaseEngine.DB2:
                default:
                    Variable("__var_false_val", false);
                    break;
            }
        }

        /// <summary>
        /// 条件始终为真。
        /// </summary>
        public virtual void AlwaysTrue()
        {
            if (IsConditionReversal)
            {
                True();

                Operator(SqlOperator.Equal);

                True();
            }
        }

        /// <summary>
        /// 条件始终为真。
        /// </summary>
        public virtual void AlwaysFalse()
        {
            if (!IsConditionReversal)
            {
                True();

                Operator(SqlOperator.Equal);

                False();
            }
        }

        /// <summary>
        /// 写入内容。
        /// </summary>
        /// <param name="value">内容。</param>
        public void Write(char value)
        {
            if (IsRankingAnalysis)
            {
                _rank.Write(value);
            }
            else
            {
                _main.Write(value);
            }
        }

        /// <summary>
        /// 写入内容。
        /// </summary>
        /// <param name="value">内容。</param>
        public void Write(string value)
        {
            if (value is null || value.Length == 0)
            {
                throw new ArgumentException($"“{nameof(value)}”不能为 null 或空。", nameof(value));
            }

            if (IsRankingAnalysis)
            {
                _rank.Write(value);
            }
            else
            {
                _main.Write(value);
            }
        }

        /// <summary>
        /// 指定位置插入数据。
        /// </summary>
        /// <param name="index">插入位置。</param>
        /// <param name="value">插入值。</param>
        public void Insert(int index, string value)
        {
            if (IsRankingAnalysis)
            {
                _rank.Insert(index, value);
            }
            else
            {
                _main.Insert(index, value);
            }
        }

        /// <summary>
        /// 写入换行。
        /// </summary>
        public void WriteLine() => Write(Environment.NewLine);

        /// <summary>
        /// 领域。
        /// </summary>
        /// <returns></returns>
        public ISqlDomain Domain()
        {
            return IsRankingAnalysis ? _rank.Domain() : _main.Domain();
        }

        /// <summary>
        /// 范围内的代码，条件反转。
        /// </summary>
        /// <returns>释放后，恢复条件反正。</returns>
        public IDisposable ConditionReversal() => new Reverse(this, IsConditionReversal ^= true);

        /// <summary>
        /// 排序分析。
        /// </summary>
        /// <returns>释放后结束排序。</returns>
        public IDisposable OrderByAnalysis()
        {
            try
            {
                return new RankingAnalysis(this, IsRankingAnalysis);
            }
            finally
            {
                IsRankingAnalysis = true;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (takeSize > 0)
            {
                return _settings.ToSQL(_main.ToString(), takeSize, skipSize, _rank.ToString());
            }

            return string.Concat(_main.ToString(), _rank.ToString());
        }

        /// <summary>
        ///释放内存。
        /// </summary>
        public void Dispose()
        {
            _main.Dispose();
            _rank.Dispose();

            Parameters = null;

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 条件反转。
        /// </summary>
        [DebuggerDisplay("{isConditionReversal}")]
        private sealed class Reverse : IDisposable
        {
            private readonly SqlWriter _writer;
            private readonly bool _isConditionReversal;

            public Reverse(SqlWriter writer, bool isConditionReversal)
            {
                _writer = writer;
                _isConditionReversal = isConditionReversal;
            }

            public void Dispose() => _writer.IsConditionReversal = !_isConditionReversal;
        }

        private sealed class RankingAnalysis : IDisposable
        {
            private readonly SqlWriter _writer;
            private readonly bool _isInSortingAnalysis;

            public RankingAnalysis(SqlWriter writer, bool isInSortingAnalysis)
            {
                _writer = writer;
                _isInSortingAnalysis = isInSortingAnalysis;
            }

            public void Dispose() => _writer.IsRankingAnalysis = _isInSortingAnalysis;
        }
    }
}
