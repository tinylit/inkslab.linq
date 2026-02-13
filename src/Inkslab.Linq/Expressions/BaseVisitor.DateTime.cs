using System;
using System.Linq.Expressions;
using Inkslab.Linq.Enums;

namespace Inkslab.Linq.Expressions
{
    public abstract partial class BaseVisitor
    {
        #region DatabaseEngine DateTime Members

        /// <summary>
        /// MySQL
        /// </summary>
        private void MySql(string name, Expression node)
        {
            switch (name)
            {
                case nameof(DateTime.Date):
                    Writer.Write("DATE");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Year):
                    Writer.Write("YEAR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Month):
                    Writer.Write("MONTH");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Day):
                    Writer.Write("DAY");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Hour):
                    Writer.Write("HOUR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Minute):
                    Writer.Write("MINUTE");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Second):
                    Writer.Write("SECOND");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Millisecond):
                    Writer.Write("FLOOR");
                    Writer.OpenBrace();
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("MICROSECOND");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Divide);
                    Writer.Constant(1000);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.DayOfWeek):
                    Writer.Write("DAYOFWEEK");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.DayOfYear):
                    Writer.Write("DAYOFYEAR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Ticks):
                    Writer.Write("TIMESTAMPDIFF");
                    Writer.OpenBrace();
                    Writer.Write("MICROSECOND");
                    Writer.Delimiter();
                    Writer.Write("'0001-01-01'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10);
                    break;
                case nameof(DateTime.TimeOfDay):
                    Writer.Write("TIME");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                default:
                    throw new NotSupportedException($"不支持“{name}”日期片段计算!");
            }
        }

        /// <summary>
        /// PostgreSQL
        /// </summary>
        private void PostgreSql(string name, Expression node)
        {
            switch (name)
            {
                case nameof(DateTime.Date):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("DATE_TRUNC");
                    Writer.OpenBrace();
                    Writer.Write("'day'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS DATE");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Year):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("YEAR");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Month):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("MONTH");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Day):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("DAY");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Hour):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("HOUR");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Minute):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("MINUTE");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Second):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("SECOND");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Millisecond):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("MILLISECONDS");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write("::INTEGER % 1000");
                    break;
                case nameof(DateTime.DayOfWeek):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("DOW");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.DayOfYear):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("DOY");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Ticks):
                    // PostgreSQL: ticks = 621355968000000000 + (EXTRACT(EPOCH FROM (node AT TIME ZONE 'UTC')) * 10000000) + (EXTRACT(MICROSECONDS FROM node) * 10)
                    Writer.OpenBrace();
                    Writer.Constant(621355968000000000L);
                    Writer.Operator(SqlOperator.Add);

                    Writer.OpenBrace();
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("EPOCH");
                    Writer.Keyword(SqlKeyword.FROM);
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.Write(" AT TIME ZONE 'UTC'");
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10000000L);
                    Writer.CloseBrace();

                    Writer.Operator(SqlOperator.Add);

                    Writer.OpenBrace();
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("MICROSECONDS");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10L);
                    Writer.CloseBrace();

                    Writer.CloseBrace();
                    Writer.Write("::BIGINT");
                    break;
                case nameof(DateTime.TimeOfDay):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.Write(" AS TIME");
                    Writer.CloseBrace();
                    break;
                default:
                    throw new NotSupportedException($"不支持“{name}”日期片段计算!");
            }
        }

        /// <summary>
        /// SqlServer
        /// </summary>
        private void SqlServer(string name, Expression node)
        {
            switch (name)
            {
                case nameof(DateTime.Date):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.Write(" AS DATE");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Year):
                    Writer.Write("YEAR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Month):
                    Writer.Write("MONTH");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Day):
                    Writer.Write("DAY");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Hour):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("hour");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Minute):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("minute");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Second):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("second");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Millisecond):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("millisecond");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.DayOfWeek):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("weekday");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.DayOfYear):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("dayofyear");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Ticks):
                    Writer.Write("DATEDIFF_BIG");
                    Writer.OpenBrace();
                    Writer.Write("MICROSECOND");
                    Writer.Delimiter();
                    Writer.Constant("'0001-01-01'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10);
                    break;
                case nameof(DateTime.TimeOfDay):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.Write(" AS time");
                    Writer.CloseBrace();
                    break;
                default:
                    throw new NotSupportedException($"不支持“{name}”日期片段计算!");
            }
        }

        /// <summary>
        /// SQLite
        /// </summary>
        private void SQLite(string name, Expression node)
        {
            switch (name)
            {
                case nameof(DateTime.Date):
                    Writer.Write("DATE");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Year):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%Y'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS INTEGER");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Month):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%m'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS INTEGER");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Day):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%d'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS INTEGER");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Hour):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%H'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS INTEGER");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Minute):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%M'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS INTEGER");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Second):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%S'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS INTEGER");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Millisecond):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%f'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(1000);
                    Writer.CloseBrace();
                    Writer.Write(" % 1000 AS INTEGER");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.DayOfWeek):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%w'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS INTEGER");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.DayOfYear):
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%j'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS INTEGER");
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Ticks):
                    // SQLite: 使用 unixepoch 和毫秒组合计算，避免 JULIANDAY 浮点精度损失
                    // Ticks = (Unix秒 + 62135596800) * 10000000 + 毫秒部分 * 10000
                    Writer.OpenBrace();
                    Writer.OpenBrace();
                    Writer.OpenBrace();
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%s'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Write(" AS INTEGER");
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Add);
                    Writer.Constant(62135596800L);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10000000L);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Add);
                    Writer.OpenBrace();
                    Writer.Write("CAST");
                    Writer.OpenBrace();
                    Writer.OpenBrace();
                    Writer.Write("STRFTIME");
                    Writer.OpenBrace();
                    Writer.Write("'%f'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(1000);
                    Writer.CloseBrace();
                    Writer.Write(" % 1000 AS INTEGER");
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10000L);
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.TimeOfDay):
                    Writer.Write("TIME");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                default:
                    throw new NotSupportedException($"不支持“{name}”日期片段计算!");
            }
        }

        /// <summary>
        /// Oracle
        /// </summary>
        private void Oracle(string name, Expression node)
        {
            switch (name)
            {
                case nameof(DateTime.Date):
                    Writer.Write("TRUNC");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Year):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("YEAR");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Month):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("MONTH");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Day):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("DAY");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Hour):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("HOUR");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Minute):
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("MINUTE");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Second):
                    Writer.Write("FLOOR");
                    Writer.OpenBrace();
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("SECOND");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Millisecond):
                    Writer.Write("FLOOR");
                    Writer.OpenBrace();
                    Writer.Write("MOD");
                    Writer.OpenBrace();
                    Writer.Write("EXTRACT");
                    Writer.OpenBrace();
                    Writer.Write("SECOND");
                    Writer.Keyword(SqlKeyword.FROM);
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(1000);
                    Writer.Delimiter();
                    Writer.Constant(1000);
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.DayOfWeek):
                    Writer.Write("TO_NUMBER");
                    Writer.OpenBrace();
                    Writer.Write("TO_CHAR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.Delimiter();
                    Writer.Write("'D'");
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Subtract);
                    Writer.Constant(1);
                    break;
                case nameof(DateTime.DayOfYear):
                    Writer.Write("TO_NUMBER");
                    Writer.OpenBrace();
                    Writer.Write("TO_CHAR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.Delimiter();
                    Writer.Write("'DDD'");
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Ticks):
                    // Oracle: 计算完整天数 * 每天 ticks + 当天秒数 * 每秒 ticks + 小数秒 * ticks/秒
                    Writer.OpenBrace();
                    Writer.OpenBrace();
                    Writer.Write("TRUNC");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Subtract);
                    Writer.Write("TO_DATE");
                    Writer.OpenBrace();
                    Writer.Write("'0001-01-01'");
                    Writer.Delimiter();
                    Writer.Write("'YYYY-MM-DD'");
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(864000000000L);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Add);
                    Writer.OpenBrace();
                    Writer.Write("FLOOR");
                    Writer.OpenBrace();
                    Writer.Write("TO_NUMBER");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.Operator(SqlOperator.Subtract);
                    Writer.Write("TRUNC");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(86400);
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10000000);
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.TimeOfDay):
                    Writer.Write("TO_CHAR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.Delimiter();
                    Writer.Write("'HH24:MI:SS'");
                    Writer.CloseBrace();
                    break;
                default:
                    throw new NotSupportedException($"不支持“{name}”日期片段计算!");
            }
        }

        /// <summary>
        /// DB2
        /// </summary>
        private void DB2(string name, Expression node)
        {
            switch (name)
            {
                case nameof(DateTime.Date):
                    Writer.Write("DATE");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Year):
                    Writer.Write("YEAR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Month):
                    Writer.Write("MONTH");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Day):
                    Writer.Write("DAY");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Hour):
                    Writer.Write("HOUR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Minute):
                    Writer.Write("MINUTE");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Second):
                    Writer.Write("SECOND");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Millisecond):
                    Writer.Write("MICROSECOND");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Divide);
                    Writer.Constant(1000);
                    break;
                case nameof(DateTime.DayOfWeek):
                    Writer.Write("DAYOFWEEK");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Subtract);
                    Writer.Constant(1);
                    break;
                case nameof(DateTime.DayOfYear):
                    Writer.Write("DAYOFYEAR");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Ticks):
                    // DB2: 天数 * 每天ticks + 秒数 * 每秒ticks + 微秒 * 10
                    Writer.OpenBrace();
                    Writer.OpenBrace();
                    Writer.OpenBrace();
                    Writer.Write("DAYS");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Subtract);
                    Writer.Write("DAYS");
                    Writer.OpenBrace();
                    Writer.Write("'0001-01-01'");
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(86400);
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10000000);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Add);
                    Writer.OpenBrace();
                    Writer.Write("MIDNIGHT_SECONDS");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10000000);
                    Writer.CloseBrace();
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Add);
                    Writer.OpenBrace();
                    Writer.Write("MICROSECOND");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.TimeOfDay):
                    Writer.Write("TIME");
                    Writer.OpenBrace();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                default:
                    throw new NotSupportedException($"不支持“{name}”日期片段计算!");
            }
        }

        /// <summary>
        /// Sybase
        /// </summary>
        private void Sybase(string name, Expression node)
        {
            switch (name)
            {
                case nameof(DateTime.Date):
                    Writer.Write("CONVERT");
                    Writer.OpenBrace();
                    Writer.Write("DATE");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Year):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("year");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Month):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("month");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Day):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("day");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Hour):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("hour");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Minute):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("minute");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Second):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("second");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Millisecond):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("millisecond");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.DayOfWeek):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("weekday");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Subtract);
                    Writer.Constant(1);
                    break;
                case nameof(DateTime.DayOfYear):
                    Writer.Write("DATEPART");
                    Writer.OpenBrace();
                    Writer.Write("dayofyear");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    break;
                case nameof(DateTime.Ticks):
                    Writer.Write("DATEDIFF");
                    Writer.OpenBrace();
                    Writer.Write("us");
                    Writer.Delimiter();
                    Writer.Write("'0001-01-01'");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.CloseBrace();
                    Writer.Operator(SqlOperator.Multiply);
                    Writer.Constant(10);
                    break;
                case nameof(DateTime.TimeOfDay):
                    Writer.Write("CONVERT");
                    Writer.OpenBrace();
                    Writer.Write("VARCHAR(8)");
                    Writer.Delimiter();
                    Visit(node);
                    Writer.Delimiter();
                    Writer.Constant(108);
                    Writer.CloseBrace();
                    break;
                default:
                    throw new NotSupportedException($"不支持“{name}”日期片段计算!");
            }
        }

        #endregion
    }
}