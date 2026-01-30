using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace Inkslab.Linq
{
    /// <summary>
    /// 命令SQL。
    /// </summary>
    public class CommandSql
    {
        /// <summary>
        /// 参数。
        /// </summary>
        private static readonly Regex _pattern = new Regex(@"(?<![\p{L}\p{N}@_])[?:@](?<name>[\p{L}\p{N}_][\p{L}\p{N}@_]*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="text">SQL。</param>
        /// <param name="parameters">参数。</param>
        /// <param name="timeout">超时时间。</param>
        public CommandSql(string text, IReadOnlyDictionary<string, object> parameters = null, int? timeout = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException($"“{nameof(text)}”不能是 Null 或为空。", nameof(text));
            }

            Text = text;
            Parameters = parameters ?? new Dictionary<string, object>(0);
            Timeout = timeout;
        }

        /// <summary>
        /// SQL。
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// 命令类型。
        /// </summary>
        public CommandType CommandType { get; protected set; } = CommandType.Text;

        /// <summary>
        /// 参数。
        /// </summary>
        public IReadOnlyDictionary<string, object> Parameters { get; }

        /// <summary>
        /// 超时时间。
        /// </summary>
        public int? Timeout { set; get; }

        /// <summary>
        /// 回调。
        /// </summary>
        /// <param name="command">命令。</param>
        public void Callback(IDbCommand command)
        {
            foreach (IDbDataParameter parameter in command.Parameters)
            {
                if (Parameters.TryGetValue(parameter.ParameterName, out var value))
                {
                    if (value is DynamicParameter dynamicParameter && (dynamicParameter.Direction == ParameterDirection.Output || dynamicParameter.Direction == ParameterDirection.InputOutput))
                    {
                        dynamicParameter.Value = parameter.Value;
                    }
                    else if (value is IDataParameter dbParameter && (dbParameter.Direction == ParameterDirection.Output || dbParameter.Direction == ParameterDirection.InputOutput))
                    {
                        dbParameter.Value = parameter.Value;
                    }
                }
            }
        }

        /// <summary>
        /// 转字符串。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Parameters.Count == 0)
            {
                return Text;
            }

            return _pattern.Replace(Text, m =>
            {
                var name = m.Groups["name"].Value;

                if (Parameters.TryGetValue(name, out var value)
                    || Parameters.TryGetValue(m.Value, out value)) //? 兼容Database操作。
                {
                    return Database.Format(value, throwError: false);
                }

                return m.Value;
            });
        }

        /// <summary>
        /// 隐式转换。
        /// </summary>
        /// <param name="sql">不需要参数的T-SQL执行脚本。</param>
        public static implicit operator CommandSql(string sql) => new CommandSql(sql);
    }

    /// <summary>
    /// 存储过程命令SQL。
    /// </summary>
    public class StoredProcedureCommandSql : CommandSql
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="text">SQL。</param>
        /// <param name="parameters">参数。</param>
        /// <param name="timeout">超时时间。</param>
        public StoredProcedureCommandSql(string text, IReadOnlyDictionary<string, object> parameters = null, int? timeout = null)
            : base(text, parameters, timeout)
        {
            CommandType = CommandType.StoredProcedure;
        }

        /// <summary>
        /// 转字符串。
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("EXEC ");

            sb.Append(Text);

            if (Parameters.Count > 0)
            {
                var first = true;

                foreach (var (key, value) in Parameters)
                {
                    if (value is DynamicParameter dynamicParameter && dynamicParameter.Direction == ParameterDirection.Output)
                    {
                        continue;
                    }

                    if (first)
                    {
                        first = false;

                        sb.Append(' ');
                    }
                    else
                    {
                        sb.Append(',');
                    }

                    sb.Append('@');
                    sb.Append(key);
                    sb.Append(" = ");
                    sb.Append(Database.Format(value, throwError: false));
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// 命令SQL。
    /// </summary>
    public class CommandSql<TElement> : CommandSql
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="commandSql">SQL命令。</param>
        /// <param name="hasDefaultValue">有指定默认值。</param>
        /// <param name="rowStyle">数据风格。</param>
        /// <param name="defaultValue">默认值。</param>
        /// <param name="customError">自定义异常。</param>
        /// <param name="noElementError">未找到数据异常消息。</param>
        public CommandSql(CommandSql commandSql, RowStyle rowStyle, bool hasDefaultValue = false, TElement defaultValue = default, bool customError = false, string noElementError = null) : base(commandSql.Text, commandSql.Parameters, commandSql.Timeout)
        {
            HasDefaultValue = hasDefaultValue;
            DefaultValue = defaultValue;
            CustomError = customError;
            NoElementError = noElementError;
            RowStyle = rowStyle;
        }

        /// <summary>
        /// 数据风格。
        /// </summary>
        public RowStyle RowStyle { get; }

        /// <summary>
        /// 是否包含默认值。
        /// </summary>
        public bool HasDefaultValue { set; get; }

        /// <summary>
        /// 默认值。
        /// </summary>
        public TElement DefaultValue { set; get; }

        /// <summary>
        /// 自定义错误。
        /// </summary>
        public bool CustomError { get; set; }

        /// <summary>
        /// 未找到数据异常消息。
        /// </summary>
        public string NoElementError { set; get; }
    }
}