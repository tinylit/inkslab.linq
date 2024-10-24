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
        public Database(IDatabaseExecutor executor, IConnectionStrings connectionStrings, IDbCorrectSettings settings) : base(executor, connectionStrings, settings)
        {
        }
    }

    /// <summary>
    /// 数据库。
    /// </summary>
    public class Database<TConnectionStrings> : IDatabase<TConnectionStrings> where TConnectionStrings : IConnectionStrings
    {
        private readonly IDatabaseExecutor _executor;
        private readonly TConnectionStrings _connectionStrings;
        private readonly IDbCorrectSettings _settings;
        private static readonly Regex _literalTokens = new Regex(@"(?<![\p{L}\p{N}@_])\{=([\p{L}\p{N}_][\p{L}\p{N}@_]*)\}", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex _smellsLikeOleDb = new Regex(@"(?<![\p{L}\p{N}@_])[?@:]([\p{L}\p{N}_][\p{L}\p{N}@_]*)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex _inlistTokens = new Regex(@"[\x20\r\n\t\f]+IN[\x20\r\n\t\f]+(?<![\p{L}\p{N}@_])(\{=(?<name>[\p{L}\p{N}_][\p{L}\p{N}@_]*)\}|[?@:](?<name>[\p{L}\p{N}_][\p{L}\p{N}@_]*))", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex _libraryTokens = new Regex("(`(?<name>[^`]+?)`)|(\\[(?<name>[^\\]]+?)\\])|(\\\"(?<name>[^\"]+?)\\\")", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 数据库。
        /// </summary>
        public Database(IDatabaseExecutor executor, TConnectionStrings connectionStrings, IDbCorrectSettings settings)
        {
            _executor = executor;
            _connectionStrings = connectionStrings;
            _settings = settings;
        }

        /// <inheritdoc/>
        public int Execute(string sql, object param = null, int? commandTimeout = null)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.Execute(_connectionStrings.Strings, commandSql);
        }

        /// <inheritdoc/>
        public Task<int> ExecuteAsync(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.ExecuteAsync(_connectionStrings.Strings, commandSql, cancellationToken);
        }

        /// <inheritdoc/>
        public List<T> Query<T>(string sql, object param, int? commandTimeout = null)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.Query<T>(_connectionStrings.Strings, commandSql);
        }

        /// <inheritdoc/>
        public async Task<List<T>> QueryAsync<T>(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            var asyncEnumerable = _executor.QueryAsync<T>(_connectionStrings.Strings, commandSql);

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

            return _executor.Read(_connectionStrings.Strings, new CommandSql<T>(commandSql, RowStyle.First));
        }

        /// <inheritdoc/>
        public Task<T> FirstAsync<T>(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.ReadAsync(_connectionStrings.Strings, new CommandSql<T>(commandSql, RowStyle.First), cancellationToken);
        }

        /// <inheritdoc/>
        public T FirstOrDefault<T>(string sql, object param = null, int? commandTimeout = null)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.Read(_connectionStrings.Strings, new CommandSql<T>(commandSql, RowStyle.FirstOrDefault));
        }

        /// <inheritdoc/>
        public Task<T> FirstOrDefaultAsync<T>(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.ReadAsync(_connectionStrings.Strings, new CommandSql<T>(commandSql, RowStyle.FirstOrDefault), cancellationToken);
        }

        /// <inheritdoc/>
        public IDbGridReader QueryMultiple(string sql, object param = null, int? commandTimeout = null)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.QueryMultiple(_connectionStrings.Strings, commandSql);
        }

        /// <inheritdoc/>
        public Task<IAsyncDbGridReader> QueryMultipleAsync(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.QueryMultipleAsync(_connectionStrings.Strings, commandSql);
        }

        /// <inheritdoc/>
        public T Single<T>(string sql, object param = null, int? commandTimeout = null)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.Read(_connectionStrings.Strings, new CommandSql<T>(commandSql, RowStyle.Single));
        }

        /// <inheritdoc/>
        public Task<T> SingleAsync<T>(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.ReadAsync(_connectionStrings.Strings, new CommandSql<T>(commandSql, RowStyle.Single), cancellationToken);
        }

        /// <inheritdoc/>
        public T SingleOrDefault<T>(string sql, object param = null, int? commandTimeout = null)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.Read(_connectionStrings.Strings, new CommandSql<T>(commandSql, RowStyle.SingleOrDefault));
        }

        /// <inheritdoc/>
        public Task<T> SingleOrDefaultAsync<T>(string sql, object param = null, int? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            var dictionaries = ToParameters(param);

            var commandSql = MakeCommandSql(sql, dictionaries, commandTimeout);

            return _executor.ReadAsync(_connectionStrings.Strings, new CommandSql<T>(commandSql, RowStyle.SingleOrDefault), cancellationToken);
        }

        /// <inheritdoc/>
        public int WriteToServer(DataTable dt, int? commandTimeout = null) => _executor.WriteToServer(_connectionStrings.Strings, dt, commandTimeout);

        /// <inheritdoc/>
        public Task<int> WriteToServerAsync(DataTable dt, int? commandTimeout = null, CancellationToken cancellationToken = default) => _executor.WriteToServerAsync(_connectionStrings.Strings, dt, commandTimeout, cancellationToken);

        private CommandSql MakeCommandSql(string sql, Dictionary<string, object> dictionaries, int? commandTimeout = null)
        {
            var inlist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var match = _inlistTokens.Match(sql);

            while (match.Success) //? 分析集合参数。
            {
                var name = match.Groups["name"].Value;

                if (dictionaries.TryGetValue(name, out var value))
                {
                    if (value is DbParameter) //? 数据库参数不做处理。
                    {
                        continue;
                    }
                }

                inlist.Add(name);

                match = match.NextMatch();
            }

            var parameters = new Dictionary<string, object>(dictionaries.Count);

            //? 字段名称或表名称处理。
            var librarySql = _libraryTokens.Replace(sql, mt =>
            {
                return _settings.Name(mt.Groups["name"].Value);
            });

            var nonnullSql = _smellsLikeOleDb.Replace(librarySql, mt => //? 提取参数并把参数值为“null”的参数替换为“null”。
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
                else
                {
                    throw new KeyNotFoundException(name);
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

            foreach (var formatter in _settings.Formatters)
            {
                commandSql = formatter.Format(commandSql);
            }

            return new CommandSql(commandSql, parameters, commandTimeout);
        }

        private static string Format(object value, bool throwError)
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
                    return string.Concat("'", text, "'");
                case DbParameter parameter:
                    return Format(parameter.Value, throwError);
                case IEnumerable objects:
                    {
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
                    }
                default:
                    var type = value.GetType();

                    if (type.IsMini())
                    {
                        return value.ToString();
                    }

                    return string.Concat("'", value.ToString(), "'");
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
                    ? (DbType)(-1)
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

            if (param is Dictionary<string, object> dictionaries)
            {
                return dictionaries;
            }

            if (param is IEnumerable<KeyValuePair<string, object>> collection)
            {
                return new Dictionary<string, object>(collection);
            }

            if (param is IEnumerable objects)
            {
                return objects.Cast<DbParameter>()
                    .ToDictionary(x => x.ParameterName, x => (object)x);
            }

            return _lru.Get(param.GetType())
                .Invoke(param);
        }
        #endregion
    }
}
