using Inkslab.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static System.Linq.Expressions.Expression;

namespace Inkslab.Linq
{
    /// <summary>
    /// 数据库。
    /// </summary>
    public class Database : Database<IConnectionStrings>, IDatabase
    {
        /// <summary>
        /// 数据库。
        /// </summary>
        public Database(IDatabaseExecutor executor, IDatabaseStrings databaseStrings) : base(executor, databaseStrings)
        {
        }
    }

    /// <summary>
    /// 数据库。
    /// </summary>
    public class Database<TConnectionStrings> : IDatabase<TConnectionStrings> where TConnectionStrings : IConnectionStrings
    {
        private readonly IDatabaseExecutor _executor;
        private readonly IDatabaseStrings<TConnectionStrings> _databaseStrings;
        private static readonly Regex _literalTokens = new Regex(@"(?<![\p{L}\p{N}@_])\{=([\p{L}\p{N}_][\p{L}\p{N}@_]*)\}", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex _smellsLikeOleDb = new Regex(@"(?<![\p{L}\p{N}@_])[?@:]([\p{L}\p{N}_][\p{L}\p{N}@_]*)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex _inlistTokens = new Regex(@"[\x20\r\n\t\f]+IN[\x20\r\n\t\f]+(?<![\p{L}\p{N}@_])(\{=(?<name>[\p{L}\p{N}_][\p{L}\p{N}@_]*)\}|[?@:](?<name>[\p{L}\p{N}_][\p{L}\p{N}@_]*))", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        // 完整版本 - 支持Schema和转义标识符
        private static readonly Regex _validProcedureNameTokens = new Regex(
            @"^
    (?:
        # 数据库名(可选)
        (?:
            \[[^\]]+\]|                          # [Database Name]
            `[^`]+`|                             # `Database Name`
            ""[^""]+""|                          # ""Database Name""
            [a-zA-Z_@#][a-zA-Z0-9_@$#]*          # DatabaseName
        )
        \.
    )?
    (?:
        # Schema名(可选)
        (?:
            \[[^\]]+\]|                          # [Schema Name]
            `[^`]+`|                             # `Schema Name`
            ""[^""]+""|                          # ""Schema Name""
            [a-zA-Z_@#][a-zA-Z0-9_@$#]*          # SchemaName
        )
        \.
    )?
    # 存储过程名(必须)
    (?:
        \[[^\]]+\]|                              # [Procedure Name]
        `[^`]+`|                                 # `Procedure Name`
        ""[^""]+""|                              # ""Procedure Name""
        [a-zA-Z_@#][a-zA-Z0-9_@$#]*              # ProcedureName
    )
    $",
            RegexOptions.IgnorePatternWhitespace |
            RegexOptions.Compiled |
            RegexOptions.CultureInvariant
        );

        /// <summary>
        /// 数据库。
        /// </summary>
        public Database(IDatabaseExecutor executor, IDatabaseStrings<TConnectionStrings> databaseStrings)
        {
            _executor = executor;
            _databaseStrings = databaseStrings;
        }

        /// <inheritdoc/>
        public int Execute(string sql, object param = null, int? commandTimeout = null)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.Execute(_databaseStrings, commandSql);
        }

        /// <inheritdoc/>
        public Task<int> ExecuteAsync(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.ExecuteAsync(_databaseStrings, commandSql, cancellationToken);
        }

        /// <inheritdoc/>
        public List<T> Query<T>(string sql, object param, int? commandTimeout = null)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.Query<T>(_databaseStrings, commandSql);
        }

        /// <inheritdoc/>
        public async Task<List<T>> QueryAsync<T>(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            var asyncEnumerable = _executor.QueryAsync<T>(_databaseStrings, commandSql);

            var results = new List<T>();

            await using (var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken))
            {
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    results.Add(enumerator.Current);
                }
            }

            return results;
        }

        /// <inheritdoc/>
        public T First<T>(string sql, object param = null, int? commandTimeout = null)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.Read(_databaseStrings, new CommandSql<T>(commandSql, RowStyle.First));
        }

        /// <inheritdoc/>
        public Task<T> FirstAsync<T>(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.ReadAsync(_databaseStrings, new CommandSql<T>(commandSql, RowStyle.First), cancellationToken);
        }

        /// <inheritdoc/>
        public T FirstOrDefault<T>(string sql, object param = null, int? commandTimeout = null)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.Read(_databaseStrings, new CommandSql<T>(commandSql, RowStyle.FirstOrDefault));
        }

        /// <inheritdoc/>
        public Task<T> FirstOrDefaultAsync<T>(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.ReadAsync(_databaseStrings, new CommandSql<T>(commandSql, RowStyle.FirstOrDefault), cancellationToken);
        }

        /// <inheritdoc/>
        public IDbGridReader QueryMultiple(string sql, object param = null, int? commandTimeout = null)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.QueryMultiple(_databaseStrings, commandSql);
        }

        /// <inheritdoc/>
        public Task<IAsyncDbGridReader> QueryMultipleAsync(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.QueryMultipleAsync(_databaseStrings, commandSql);
        }

        /// <inheritdoc/>
        public T Single<T>(string sql, object param = null, int? commandTimeout = null)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.Read(_databaseStrings, new CommandSql<T>(commandSql, RowStyle.Single));
        }

        /// <inheritdoc/>
        public Task<T> SingleAsync<T>(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.ReadAsync(_databaseStrings, new CommandSql<T>(commandSql, RowStyle.Single), cancellationToken);
        }

        /// <inheritdoc/>
        public T SingleOrDefault<T>(string sql, object param = null, int? commandTimeout = null)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.Read(_databaseStrings, new CommandSql<T>(commandSql, RowStyle.SingleOrDefault));
        }

        /// <inheritdoc/>
        public Task<T> SingleOrDefaultAsync<T>(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.ReadAsync(_databaseStrings, new CommandSql<T>(commandSql, RowStyle.SingleOrDefault), cancellationToken);
        }

        /// <inheritdoc/>
        public int WriteToServer(DataTable dt, int? commandTimeout = null) => _executor.WriteToServer(_databaseStrings, dt, commandTimeout);

        /// <inheritdoc/>
        public int ExecuteMultiple(Action<IMultipleExecutor> multipleAction, int? commandTimeout = null) => _executor.ExecuteMultiple(_databaseStrings, multipleAction, commandTimeout);

        /// <inheritdoc/>
        public Task<int> WriteToServerAsync(DataTable dt, int? commandTimeout = null, CancellationToken cancellationToken = default) => _executor.WriteToServerAsync(_databaseStrings, dt, commandTimeout, cancellationToken);

        /// <inheritdoc/>
        public Task<int> ExecuteMultipleAsync(Func<IAsyncMultipleExecutor, Task> multipleAction, int? commandTimeout = null, CancellationToken cancellationToken = default) => _executor.ExecuteMultipleAsync(_databaseStrings, multipleAction, commandTimeout, cancellationToken);

        private static CommandSql MakeCommandSql(string sql, Dictionary<string, object> dictionaries, int? commandTimeout = null)
        {
            if (_validProcedureNameTokens.IsMatch(sql))
            {
                return new StoredProcedureCommandSql(sql, dictionaries, commandTimeout);
            }

            var inlist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var match = _inlistTokens.Match(sql);

            while (match.Success) //? 分析集合参数。
            {
                var name = match.Groups["name"].Value;

                if (dictionaries.TryGetValue(name, out var value))
                {
                    if (value is IDataParameter) //? 数据库参数不做处理。
                    {
                        continue;
                    }
                }

                inlist.Add(name);

                match = match.NextMatch();
            }

            var parameters = new Dictionary<string, object>(dictionaries.Count);

            var nonnullSql = _smellsLikeOleDb.Replace(sql, mt => //? 提取参数并把参数值为“null”的参数替换为“null”。
            {
                var name = mt.Groups[1].Value;

                if (inlist.Contains(name)) //? 集合参数先不分析。
                {
                    return mt.Value;
                }

                if (dictionaries.TryGetValue(name, out var value))
                {
                    if (value is null or DBNull) //? 空值处理为 null。
                    {
                        return "null";
                    }

                    parameters.TryAdd(name, value);
                }

                return mt.Value;
            });

            string nonliteralSql = _literalTokens.Replace(nonnullSql, mt => //? 处理 {=name} 参数。
            {
                var name = mt.Groups[1].Value;

                if (dictionaries.TryGetValue(name, out var value))
                {
                    if (inlist.Contains(name))
                    {
                        if (value is null or DBNull)
                        {
                            return "(SELECT null WHERE 1=0)";
                        }

                        return Format(value, false);
                    }

                    return Format(value, true);
                }

                throw new KeyNotFoundException(name);
            });

            string commandSql = nonliteralSql; //? 执行语句。

            if (inlist.Count > 0)
            {
                foreach (var name in inlist)
                {
                    int analysisCount = 0;
                    bool analysisFirst = true;

                    commandSql = Regex.Replace(commandSql, "([?@:$]" + Regex.Escape(name) + @")(?!\w)(\s+(?i)unknown(?-i))?", mt =>
                    {
                        if (analysisFirst)
                        {
                            if (!dictionaries.TryGetValue(name, out var value))
                            {
                                return mt.Value;
                            }

                            analysisFirst = false;

                            switch (value)
                            {
                                case null or DBNull:
                                    break;
                                case IEnumerable objects:
                                    {
                                        foreach (var item in objects)
                                        {
                                            if (item is null)
                                            {
                                                continue;
                                            }

                                            analysisCount++;

                                            parameters.Add($"{name}_{analysisCount}", item);
                                        }

                                        break;
                                    }

                                default:
                                    throw new ArgumentOutOfRangeException(name, value, "参数值不可迭代！");
                            }
                        }

                        if (analysisCount == 0)
                        {
                            return "(select null where 1=0)";
                        }

                        var variableName = mt.Groups[1].Value;

                        int capacity = (variableName.Length /* 名字位 */+ (analysisCount > 999 ? 4 : analysisCount > 99 ? 3 : analysisCount > 9 ? 2 : 1) /* 数字位*/ + analysisCount/* _ 符号位*/+ (analysisCount - 1)/* , 符号位 */ ) * analysisCount + 2 /*() 符号位*/;

                        if (mt.Groups[2].Success)
                        {
                            // looks like an optimize hint; expand it
                            var suffix = mt.Groups[2].Value;

                            var sb = new StringBuilder(capacity + suffix.Length * analysisCount);

                            sb.Append('(');

                            for (int i = 1; i <= analysisCount; i++)
                            {
                                if (i > 1)
                                {
                                    sb.Append(',');
                                }

                                sb.Append(variableName)
                                    .Append('_')
                                    .Append(i)
                                    .Append(suffix);
                            }

                            return sb.Append(')')
                                        .ToString();
                        }
                        else
                        {
                            var sb = new StringBuilder(capacity);

                            sb.Append('(');

                            for (int i = 1; i <= analysisCount; i++)
                            {
                                if (i > 1)
                                {
                                    sb.Append(',');
                                }

                                sb.Append(variableName)
                                    .Append('_')
                                    .Append(i);
                            }

                            return sb.Append(')')
                                        .ToString();
                        }
                    });
                }
            }

            return new CommandSql(commandSql, parameters, commandTimeout);
        }

        /// <summary>
        /// 格式化参数值为SQL字面量。
        /// </summary>
        public static string Format(object value, bool throwError)
        {
            switch (value)
            {
                case null or DBNull:
                    return "null";
                case bool @bool:
                    return @bool ? "1" : "0";
                case Enum @enum:
                    return @enum.ToString("D");
                case string text:
                    // 防止SQL注入：转义单引号
                    return string.Concat("'", text.Replace("'", "''"), "'");
                case DateTime dateTime:
                    // 日期时间格式化 - ISO 8601 格式，兼容所有主流数据库
                    // SQL Server, MySQL, PostgreSQL, SQLite, Oracle 等都支持此格式
                    return string.Concat("'", dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"), "'");
                case DateTimeOffset dateTimeOffset:
                    // 带时区的日期时间 - ISO 8601 格式
                    return string.Concat("'", dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss.fffzzz"), "'");
                case TimeSpan timeSpan:
                    // 时间间隔 - 格式化为 HH:mm:ss.fff
                    return string.Concat("'", timeSpan.ToString(@"hh\:mm\:ss\.fff"), "'");
                case Guid guid:
                    // GUID - 某些数据库(如PostgreSQL)需要明确的GUID格式
                    return string.Concat("'", guid.ToString(), "'");
                case byte[] bytes:
                    // 二进制数据 - 转换为十六进制字符串
                    // SQL Server: 0x..., PostgreSQL: \x..., Oracle: HEXTORAW('...')
                    return string.Concat("0x", BitConverter.ToString(bytes).Replace("-", ""));
                case decimal decimalValue:
                    // Decimal - 使用不变文化格式，避免本地化问题
                    return decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case float floatValue:
                    // Float - 使用不变文化格式
                    return floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case double doubleValue:
                    // Double - 使用不变文化格式
                    return doubleValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case DbParameter parameter:
                    return Format(parameter.Value, throwError);
                case JsonbPayload jsonbPayload:
                    return Format(jsonbPayload.ToString(), throwError);
                case JsonPayload jsonPayload:
                    return Format(jsonPayload.ToString(), throwError);
                case IEnumerable objects:
                    if (throwError)
                    {
                        throw new InvalidOperationException("不支持集合参数！");
                    }

                    var sb = new StringBuilder();

                    sb.Append('(');

                    bool first = true;

                    foreach (var item in objects)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            sb.Append(',');
                        }

                        sb.Append(Format(item, true));
                    }

                    if (first)
                    {
                        sb.Append("SELECT null WHERE 1=0");
                    }

                    sb.Append(')');

                    return sb.ToString();
                default:
                    var type = value.GetType();

                    if (type.IsMini())
                    {
                        return value.ToString();
                    }

                    if (type.FullName is "Newtonsoft.Json.Linq.JObject" or "Newtonsoft.Json.Linq.JArray")
                    {
                        return Format(value.ToString(), throwError);
                    }

                    return Format(value.ToString(), throwError);
            }
        }

        #region 参数处理。
        private static readonly Type _dictionaryType;
        private static readonly MethodInfo _dictionaryAdd;

        static Database()
        {
            _dictionaryType = typeof(Dictionary<string, object>);
            _dictionaryAdd = _dictionaryType.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { Types.String, Types.Object }, null);
        }

        private static readonly Lfu<Type, Func<object, Dictionary<string, object>>> _lru = new Lfu<Type, Func<object, Dictionary<string, object>>>(type =>
        {
            var parameterPar = Parameter(typeof(object), "arg");
            var targetVar = Variable(type, "value");
            var dictionariesVar = Variable(_dictionaryType, "dictionaries");

            var propertyInfos = type.GetProperties();

            var expressions = new List<Expression>
            {
                Assign(targetVar, Convert(parameterPar, type)),
                Assign(dictionariesVar, New(_dictionaryType.GetConstructor(new Type[]{ typeof(int)}), Constant(propertyInfos.Length)))
            };

            foreach (var propertyInfo in type.GetProperties())
            {
                if (!propertyInfo.CanRead)
                {
                    continue;
                }

                var propertyType = propertyInfo.PropertyType;

                if (propertyType.IsNullable())
                {
                    propertyType = Nullable.GetUnderlyingType(propertyType);
                }

                DbType dbType = propertyType.IsArray && propertyType != typeof(byte[]) || propertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(propertyType)
                    ? LookupDb.EnumerableDbType
                    : LookupDb.For(propertyType);

                expressions.Add(Call(dictionariesVar, _dictionaryAdd, Constant(propertyInfo.Name), Convert(Property(targetVar, propertyInfo), typeof(object))));
            }

            expressions.Add(dictionariesVar);

            var lambdaExp = Lambda<Func<object, Dictionary<string, object>>>(Block(new ParameterExpression[2] { targetVar, dictionariesVar }, expressions), new ParameterExpression[1] { parameterPar });

            return lambdaExp.Compile();
        });

        private static Dictionary<string, object> ToParameters(object param = null)
        {
            if (param is null)
            {
                return new Dictionary<string, object>(0);
            }

            if (param is string)
            {
                throw new InvalidOperationException();
            }

            if (param is IDictionary<string, object> dictionaries)
            {
                return dictionaries.ToDictionary(x => Clean(x.Key), x => x.Value);
            }

            if (param is IEnumerable<KeyValuePair<string, object>> collection)
            {
                return collection.ToDictionary(x => Clean(x.Key), x => x.Value);
            }

            if (param is IEnumerable objects)
            {
                return objects.Cast<IDataParameter>()
                    .ToDictionary(x => Clean(x.ParameterName), x => (object)x);
            }

            return _lru.Get(param.GetType())
                .Invoke(param);
        }

        private static string Clean(string name)
        {
            return name[0] switch
            {
                '@' or ':' or '?' => name[1..],
                _ => name,
            };
        }
        #endregion
    }
}
