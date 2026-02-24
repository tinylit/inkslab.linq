using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Linq.Abilities;
using Inkslab.Linq.Enums;
using Microsoft.Extensions.Logging;
using static System.Linq.Expressions.Expression;

#if NET6_0_OR_GREATER
using System.Reflection;
using System.ComponentModel.DataAnnotations;
#endif

namespace Inkslab.Linq
{
    /// <summary>
    /// 仓库路由。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public class RepositoryRouter<TEntity> : IRepositoryRouter<TEntity>
        where TEntity : class, new()
    {
        private static readonly Type _elementType;
        private static readonly ITableInfo _instance;
        private readonly IDatabaseExecutor _databaseExecutor;
        private readonly DbStrictAdapter _adapter;
        private readonly IDatabaseStrings _databaseStrings;
        private readonly ILogger _logger;

        static RepositoryRouter()
        {
            _elementType = typeof(TEntity);

            _instance = TableAnalyzer.Table(_elementType);
        }

        /// <summary>
        /// 仓库路由。
        /// </summary>
        /// <param name="databaseExecutor">执行器。</param>
        /// <param name="adapter">严格适配器。</param>
        /// <param name="databaseStrings">数据库链接。</param>
        /// <param name="logger">日志。</param>
        public RepositoryRouter(
            IDatabaseExecutor databaseExecutor,
            DbStrictAdapter adapter,
            IDatabaseStrings databaseStrings,
            ILogger<RepositoryRouter<TEntity>> logger
        )
        {
            _databaseExecutor = databaseExecutor;
            _adapter = adapter;
            _databaseStrings = databaseStrings;
            _logger = logger;
        }

        #region 能力。

        private static string[] AnalysisFields<TColumn>(Expression<Func<TEntity, TColumn>> columns)
        {
            if (columns is null)
            {
                throw new ArgumentNullException(nameof(columns));
            }

            var body = columns.Body;

            return body.NodeType switch
            {
                ExpressionType.Constant when body is ConstantExpression constant
                    => constant.Value switch
                    {
                        string text => text.Split(',', ' '),
                        string[] arr => arr,
                        _ => throw new NotImplementedException(),
                    },
                ExpressionType.MemberAccess when body is MemberExpression member
                    => new[] { member.Member.Name },
                ExpressionType.MemberInit when body is MemberInitExpression memberInit
                    => memberInit.Bindings.Select(x => x.Member.Name).ToArray(),
                ExpressionType.New when body is NewExpression newExpression
                    => newExpression.Members?.Select(x => x.Name).ToArray()
                        ?? Array.Empty<string>(),
                ExpressionType.Parameter
                    => _instance
                        .Fields.SkipWhile(x => _instance.ReadOnlys.Contains(x.Key))
                        .Select(x => x.Key)
                        .ToArray(),
                _ => throw new NotSupportedException($"不支持表达式({columns})!"),
            };
        }

        /// <inheritdoc/>
        public IInsertable<TEntity> AsInsertable(
            IReadOnlyCollection<TEntity> entries,
            bool ignore = false,
            string shardingKey = null,
            int? commandTimeout = null
        )
        {
            if (entries is null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (_instance.DataSharding ^ shardingKey?.Length > 0)
            {
                if (_instance.DataSharding)
                {
                    throw new InvalidOperationException($"分区表“{_instance.Name}”的操作，必须指定分区键！");
                }

                throw new InvalidOperationException($"普通表“{_instance.Name}”不支持分区操作！");
            }

            return new Insertable(
                _databaseExecutor,
                _databaseStrings,
                _adapter,
                entries,
                commandTimeout,
                shardingKey,
                ignore
            );
        }

        /// <inheritdoc/>
        public IUpdateable<TEntity> AsUpdateable(
            IReadOnlyCollection<TEntity> entries,
            string shardingKey = null,
            int? commandTimeout = null
        )
        {
            if (entries is null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (_instance.Keys.Count == 0)
            {
                throw new NotSupportedException("不支持无主键表的更新操作！");
            }

            if (_instance.DataSharding ^ shardingKey?.Length > 0)
            {
                if (_instance.DataSharding)
                {
                    throw new InvalidOperationException($"分区表“{_instance.Name}”的操作，必须指定分区键！");
                }

                throw new InvalidOperationException($"普通表“{_instance.Name}”不支持分区操作！");
            }

            return new Updateable(
                _databaseExecutor,
                _databaseStrings,
                _adapter,
                entries,
                commandTimeout,
                shardingKey,
                _logger
            );
        }

        /// <inheritdoc/>
        public IDeleteable<TEntity> AsDeleteable(
            IReadOnlyCollection<TEntity> entries,
            string shardingKey = null,
            int? commandTimeout = null
        )
        {
            if (entries is null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (_instance.Keys.Count == 0)
            {
                throw new NotSupportedException("不支持无主键表的删除操作！");
            }

            if (_instance.DataSharding ^ shardingKey?.Length > 0)
            {
                if (_instance.DataSharding)
                {
                    throw new InvalidOperationException($"分区表“{_instance.Name}”的操作，必须指定分区键！");
                }

                throw new InvalidOperationException($"普通表“{_instance.Name}”不支持分区操作！");
            }

            return new Deleteable(
                _databaseExecutor,
                _databaseStrings,
                _adapter,
                entries,
                commandTimeout,
                shardingKey,
                _logger
            );
        }

        /// <summary>
        /// 获取数据库类型名称（支持8大主流数据库：MySQL、SQL Server、PostgreSQL、SQLite、Oracle、DB2、Access、Sybase）。
        /// </summary>
        /// <param name="entry">列信息</param>
        /// <param name="engine">数据库引擎</param>
        /// <returns>数据库类型名称</returns>
        private static string GetDbTypeName(Entry entry, DatabaseEngine engine)
        {
            var typeCode = Type.GetTypeCode(entry.ColumnType);
            var length = entry.Length;

            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return engine switch
                    {
                        DatabaseEngine.PostgreSQL => "boolean",
                        DatabaseEngine.Oracle => "number(1)",
                        DatabaseEngine.SQLite => "integer",
                        _ => "bit" // MySQL, SqlServer, DB2, Access, Sybase
                    };

                case TypeCode.Char:
                    return engine switch
                    {
                        DatabaseEngine.MySQL or DatabaseEngine.PostgreSQL or DatabaseEngine.SQLite
                            or DatabaseEngine.Oracle or DatabaseEngine.DB2 => "char(1)",
                        _ => "nchar(1)" // SqlServer, Sybase
                    };

                case TypeCode.Byte:
                    return engine switch
                    {
                        DatabaseEngine.PostgreSQL or DatabaseEngine.DB2 => "smallint",
                        DatabaseEngine.Oracle => "number(3)",
                        DatabaseEngine.SQLite => "integer",
                        _ => "tinyint" // MySQL, SqlServer, Sybase
                    };

                case TypeCode.SByte:
                    return engine switch
                    {
                        DatabaseEngine.MySQL => "tinyint unsigned",
                        DatabaseEngine.PostgreSQL or DatabaseEngine.DB2 => "smallint",
                        DatabaseEngine.Oracle => "number(3)",
                        DatabaseEngine.SQLite => "integer",
                        _ => "smallint" // SqlServer, Sybase, Access
                    };

                case TypeCode.Int16:
                    return engine switch
                    {
                        DatabaseEngine.Oracle => "number(5)",
                        DatabaseEngine.SQLite => "integer",
                        _ => "smallint" // MySQL, SqlServer, PostgreSQL, DB2, Sybase, Access
                    };

                case TypeCode.UInt16:
                    return engine switch
                    {
                        DatabaseEngine.MySQL => "smallint unsigned",
                        DatabaseEngine.PostgreSQL or DatabaseEngine.SQLite or DatabaseEngine.DB2 => "integer",
                        DatabaseEngine.Oracle => "number(5)",
                        _ => "int" // SqlServer, Sybase, Access
                    };

                case TypeCode.Int32:
                    return engine switch
                    {
                        DatabaseEngine.PostgreSQL or DatabaseEngine.SQLite or DatabaseEngine.DB2 => "integer",
                        DatabaseEngine.Oracle => "number(10)",
                        _ => "int" // MySQL, SqlServer, Sybase
                    };

                case TypeCode.UInt32:
                    return engine switch
                    {
                        DatabaseEngine.MySQL => "int unsigned",
                        DatabaseEngine.PostgreSQL or DatabaseEngine.DB2 => "bigint",
                        DatabaseEngine.Oracle => "number(10)",
                        DatabaseEngine.SQLite => "integer",
                        _ => "bigint" // SqlServer, Sybase, Access
                    };

                case TypeCode.Int64:
                    return engine switch
                    {
                        DatabaseEngine.Oracle => "number(19)",
                        DatabaseEngine.SQLite => "integer",
                        _ => "bigint" // MySQL, SqlServer, PostgreSQL, DB2, Sybase
                    };

                case TypeCode.UInt64:
                    return engine switch
                    {
                        DatabaseEngine.MySQL => "bigint unsigned",
                        DatabaseEngine.PostgreSQL => "numeric(20)",
                        DatabaseEngine.Oracle => "number(20)",
                        DatabaseEngine.SQLite => "integer",
                        DatabaseEngine.DB2 => "decimal(20)",
                        _ => "bigint" // SqlServer, Sybase, Access
                    };

                case TypeCode.Single:
                    return engine switch
                    {
                        DatabaseEngine.PostgreSQL or DatabaseEngine.SQLite or DatabaseEngine.DB2 => "real",
                        DatabaseEngine.Oracle => "binary_float",
                        _ => "float" // MySQL, SqlServer, Sybase
                    };

                case TypeCode.Double:
                    return engine switch
                    {
                        DatabaseEngine.PostgreSQL => "double precision",
                        DatabaseEngine.Oracle => "binary_double",
                        DatabaseEngine.SQLite => "real",
                        DatabaseEngine.DB2 or DatabaseEngine.MySQL => "double",
                        _ => "float" // SqlServer, Sybase
                    };

                case TypeCode.Decimal:
                    return engine switch
                    {
                        DatabaseEngine.PostgreSQL or DatabaseEngine.SQLite => "numeric",
                        DatabaseEngine.Oracle => "number",
                        _ => "decimal" // MySQL, SqlServer, DB2, Access, Sybase
                    };

                case TypeCode.DateTime:
                    return engine switch
                    {
                        DatabaseEngine.PostgreSQL or DatabaseEngine.DB2 => "timestamp",
                        DatabaseEngine.Oracle => "date",
                        DatabaseEngine.SQLite or DatabaseEngine.MySQL => "datetime",
                        _ => "datetime" // SqlServer, Sybase
                    };

                case TypeCode.String:
                    if (length == -1)
                    {
                        return engine switch
                        {
                            DatabaseEngine.SqlServer => "ntext",
                            DatabaseEngine.MySQL or DatabaseEngine.PostgreSQL or DatabaseEngine.SQLite or DatabaseEngine.Sybase => "text",
                            DatabaseEngine.Oracle or DatabaseEngine.DB2 => "clob",
                            _ => "text"
                        };
                    }

                    return engine switch
                    {
                        DatabaseEngine.SqlServer when length > 8000 => "nvarchar(max)",
                        DatabaseEngine.SqlServer => $"nvarchar({length})",
                        DatabaseEngine.MySQL when length <= 65535 => $"varchar({length})",
                        DatabaseEngine.MySQL => "text",
                        DatabaseEngine.PostgreSQL when length <= 10485760 => $"varchar({length})",
                        DatabaseEngine.PostgreSQL => "text",
                        DatabaseEngine.Oracle when length <= 4000 => $"varchar2({length})",
                        DatabaseEngine.Oracle => "clob",
                        DatabaseEngine.SQLite when length <= 1000000000 => $"varchar({length})",
                        DatabaseEngine.SQLite => "text",
                        DatabaseEngine.DB2 when length <= 32672 => $"varchar({length})",
                        DatabaseEngine.DB2 => "clob",
                        DatabaseEngine.Sybase when length <= 8000 => $"varchar({length})",
                        DatabaseEngine.Sybase => "text",
                        _ => "text"
                    };

                default:
                    throw new NotSupportedException(
                        $"列\"{entry.ColumnName}\"的类型\"{entry.ColumnType}\"不支持批处理！"
                    );
            }
        }

        #region 命令。
        private class Entry
        {
            private readonly Func<TEntity, object> _factory;

            public Entry(Func<TEntity, object> factory)
            {
                _factory = factory;
            }

            public string Name { get; set; }

            public string ColumnName { get; set; }

            public int Length { get; set; }

            public bool Mini { get; set; }

            public bool Nullable { get; set; }

            public Type ColumnType { get; set; }

            public object GetValue(TEntity entry) => _factory.Invoke(entry);
        }

        private abstract class Command
        {
            /// <summary>
            /// 最大参数长度。
            /// </summary>
            public const int MAX_PARAMETERS_COUNT = 1000;

            /// <summary>
            /// 最大IN SQL 参数长度（取自 Oracle 9I）。
            /// </summary>
            public const int MAX_IN_SQL_PARAMETERS_COUNT = 256;

            private static readonly ConcurrentDictionary<
                Type,
                Dictionary<string, Entry>
            > _cachings = new ConcurrentDictionary<Type, Dictionary<string, Entry>>();

            public Command(
                IReadOnlyCollection<TEntity> entities,
                DbStrictAdapter adapter,
                string shardingKey,
                int? commandTimeout
            )
            {
                Name = _instance.DataSharding
                    ? _instance.Fragment(shardingKey)
                    : _instance.Name;

                Schema = _instance.Schema ?? string.Empty;

                if (Schema.Length == 0)
                {
                    if (adapter.Engine == DatabaseEngine.SqlServer)
                    {
                        Schema = "dbo";
                    }
                    else if (adapter.Engine == DatabaseEngine.PostgreSQL)
                    {
                        Schema = "public";
                    }
                }

                Entities = entities;
                Engine = adapter.Engine;
                Settings = adapter.Settings;

                CommandTimeout = commandTimeout;
            }

            public bool IsEmpty => Entities.Count == 0;

            public bool RequiredBulk => Entities.Count > 100;

            protected IReadOnlyCollection<TEntity> Entities { get; }

            public DatabaseEngine Engine { get; }

            public IDbCorrectSettings Settings { get; }

            public string Schema { get; }

            public string Name { get; }

            public HashSet<string> Fields { protected set; get; }

            public int? CommandTimeout { get; }

            public void Except(string[] columns)
            {
                if (columns is null)
                {
                    throw new ArgumentNullException(nameof(columns));
                }

                Array.ForEach(
                    columns,
                    column =>
                    {
                        if (!Fields.Remove(column))
                        {
                            if (_instance.Fields.TryGetValue(column, out var value))
                            {
                                Fields.Remove(value);
                            }
                        }
                    }
                );
            }

            public void Limit(string[] columns)
            {
                if (columns is null)
                {
                    throw new ArgumentNullException(nameof(columns));
                }

                var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                Array.ForEach(
                    columns,
                    column =>
                    {
                        if (_instance.Fields.ContainsKey(column))
                        {
                            fields.Add(column);
                        }
                        else
                        {
                            foreach (var (key, value) in _instance.Fields)
                            {
                                if (
                                    string.Equals(value, column, StringComparison.OrdinalIgnoreCase)
                                )
                                {
                                    fields.Add(key);

                                    break;
                                }
                            }
                        }
                    }
                );

                if (fields.Count == 0)
                {
                    throw new InvalidOperationException("未指定操作字段！");
                }

                Fields = fields;
            }

            protected Entry[] GetEntries() => GetEntries(Fields);

            protected static Entry[] GetEntries(ICollection<string> fields)
            {
                var dictionary = _cachings.GetOrAdd(
                    _elementType,
                    type =>
                    {
                        var fields = new Dictionary<string, Entry>(
                            _instance.Fields.Count,
                            StringComparer.OrdinalIgnoreCase
                        );

                        var dbnull = Constant(null, Types.Object);

                        foreach (var property in type.GetProperties())
                        {
                            if (!_instance.Fields.TryGetValue(property.Name, out string value))
                            {
                                continue;
                            }

                            var destinationType = property.PropertyType;

                            bool nullable = destinationType.IsNullable();

                            if (nullable)
                            {
                                destinationType = Nullable.GetUnderlyingType(destinationType);
                            }

                            bool isEnum = destinationType.IsEnum;

                            if (isEnum)
                            {
                                destinationType = Enum.GetUnderlyingType(destinationType);
                            }

                            var entryArg = Parameter(type);
                            var valueVar = Variable(property.PropertyType);

                            var expressions = new List<Expression>
                            {
                                Assign(valueVar, Property(entryArg, property))
                            };

                            if (nullable)
                            {
                                if (isEnum)
                                {
                                    expressions.Add(
                                        Condition(
                                            Property(valueVar, "HasValue"),
                                            Convert(
                                                Convert(
                                                    Property(valueVar, "Value"),
                                                    destinationType
                                                ),
                                                Types.Object
                                            ),
                                            dbnull
                                        )
                                    );
                                }
                                else
                                {
                                    expressions.Add(
                                        Condition(
                                            Property(valueVar, "HasValue"),
                                            Convert(Property(valueVar, "Value"), Types.Object),
                                            dbnull
                                        )
                                    );
                                }
                            }
                            else if (isEnum)
                            {
                                expressions.Add(
                                    Convert(Convert(valueVar, destinationType), Types.Object)
                                );
                            }
                            else
                            {
                                expressions.Add(Convert(valueVar, Types.Object));
                            }

                            int length = -1;

#if NET6_0_OR_GREATER
                            if (destinationType == Types.String)
                            {
                                var maxLengthAttr = property.GetCustomAttribute<MaxLengthAttribute>(true);

                                if (maxLengthAttr is null)
                                {
                                    var stringLengthAttr = property.GetCustomAttribute<StringLengthAttribute>(true);

                                    if (stringLengthAttr is null)
                                    {

                                    }
                                    else
                                    {
                                        length = stringLengthAttr.MaximumLength;
                                    }
                                }
                                else
                                {
                                    length = maxLengthAttr.Length;
                                }
                            }
#endif

                            var lambdaEx = Lambda<Func<TEntity, object>>(
                                Block(new[] { valueVar }, expressions),
                                entryArg
                            );

                            fields.Add(
                                property.Name,
                                new Entry(lambdaEx.Compile())
                                {
                                    Name = property.Name,
                                    ColumnName = value,
                                    Length = length,
                                    Mini = destinationType.IsMini(),
                                    Nullable = nullable || !destinationType.IsValueType,
                                    ColumnType = destinationType
                                }
                            );
                        }

                        return fields;
                    }
                );

                var entries = new List<Entry>(fields.Count);

                foreach (var key in fields)
                {
                    if (dictionary.TryGetValue(key, out var entry))
                    {
                        entries.Add(entry);
                    }
                }

                return entries.ToArray();
            }

            public abstract void CheckValid();

            public abstract CommandSql GetCommandSql();
        }

        private class InsertCommand : Command
        {
            public InsertCommand(
                IReadOnlyCollection<TEntity> entities,
                DbStrictAdapter adapter,
                bool ignore,
                string shardingKey,
                int? commandTimeout
            )
                : base(entities, adapter, shardingKey, commandTimeout)
            {
                Ignore = ignore;

                Fields = _instance
                    .Fields.SkipWhile(x => _instance.ReadOnlys.Contains(x.Key))
                    .Select(x => x.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            public bool Ignore { get; }

            public DataTable Combination()
            {
                var entries = GetEntries();

                var dt = new DataTable(Name);

                Array.ForEach(
                    entries,
                    entry =>
                    {
                        var column = dt.Columns.Add(entry.ColumnName, entry.ColumnType);

                        column.AllowDBNull = entry.Nullable;
                    }
                );

                foreach (var entity in Entities)
                {
                    if (entity == null)
                    {
                        throw new InvalidOperationException("实体不能为空！");
                    }

#if NET6_0_OR_GREATER
                    Validator.ValidateObject(entity, new ValidationContext(entity), true);
#endif
                    var dr = dt.NewRow();

                    Array.ForEach(
                        entries,
                        entry =>
                        {
                            dr[entry.ColumnName] = entry.GetValue(entity) ?? DBNull.Value;
                        }
                    );

                    dt.Rows.Add(dr);
                }

                return dt;
            }

            public (
                string,
                DataTable,
                string,
                Dictionary<string, object>,
                string
            ) IgnoreCombination()
            {
                var entries = GetEntries();

                var sb = new StringBuilder(128);
                var insertArgs = new Dictionary<string, object>();

                var temporaryName = $"bulk_{Guid.NewGuid():N}_i";

                #region 构造插入语句。
                sb.Append("INSERT ");

                if (Ignore)
                {
                    if (Engine == DatabaseEngine.SQLite)
                    {
                        sb.Append("OR ");
                    }

                    if (Engine == DatabaseEngine.PostgreSQL)
                    {

                    }
                    else
                    {
                        sb.Append("IGNORE ");
                    }
                }

                sb.Append("INTO ");

                if (Schema.Length > 0)
                {
                    sb.Append(Settings.Name(Schema)).Append('.');
                }

                sb.Append(Settings.Name(Name))
                    .Append('(')
                    .Append(string.Join(',', entries.Select(x => Settings.Name(x.ColumnName))))
                    .Append(')')
                    .AppendLine()
                    .Append("SELECT ")
                    .Append(string.Join(',', entries.Select(x => Settings.Name(x.ColumnName))))
                    .Append(" FROM ")
                    .Append(Settings.Name(temporaryName));

                if (Ignore && Engine == DatabaseEngine.PostgreSQL)
                {
                    sb.AppendLine()
                        .Append("ON CONFLICT DO NOTHING");
                }

                #endregion

                string insertSql = sb.ToString();

                //! 清除更新语句内容，复用容量。
                sb.Clear();

                #region 创建表

                sb.Append("CREATE ");

                if (Engine == DatabaseEngine.MySQL)
                {
                    sb.Append("TEMPORARY ");
                }
                else if (Engine == DatabaseEngine.PostgreSQL)
                {
                    sb.Append("TEMP ");
                }

                sb.Append("TABLE ")
                    .Append(Settings.Name(temporaryName))
                    .Append('(');

                for (int i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];

                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append(Settings.Name(entry.ColumnName))
                        .Append(' ')
                        .Append(GetDbTypeName(entry, Engine));

                    if (!entry.Nullable)
                    {
                        sb.Append(" NOT");
                    }

                    sb.Append(" NULL");
                }

                sb.Append(')');

                #endregion

                string createSql = sb.ToString();

                //! 清除更新语句内容，复用容量。
                sb.Clear();

                #region 删除表。
                sb.Append("DROP ");

                if (Engine == DatabaseEngine.MySQL)
                {
                    sb.Append("TEMPORARY ");
                }

                sb.Append("TABLE ")
                    .Append(Settings.Name(temporaryName))
                    .Append(';');

                #endregion

                string dropSql = sb.ToString();

                var dt = new DataTable(temporaryName);

                #region 组装数据。

                Array.ForEach(
                    entries,
                    entry =>
                    {
                        dt.Columns.Add(entry.ColumnName, entry.ColumnType).AllowDBNull =
                            entry.Nullable;
                    }
                );

                foreach (var entity in Entities)
                {
                    if (entity is null)
                    {
                        throw new InvalidOperationException("实体不能为空！");
                    }

#if NET6_0_OR_GREATER
                    Validator.ValidateObject(entity, new ValidationContext(entity), true);
#endif

                    var dr = dt.NewRow();

                    Array.ForEach(
                        entries,
                        entry =>
                        {
                            dr[entry.ColumnName] = entry.GetValue(entity) ?? DBNull.Value;
                        }
                    );

                    dt.Rows.Add(dr);
                }
                #endregion

                return (createSql, dt, insertSql, insertArgs, dropSql);
            }

            public override void CheckValid()
            {
                if (Fields.Count == 0)
                {
                    throw new InvalidOperationException("未指定插入字段！");
                }
            }

            public override CommandSql GetCommandSql()
            {
                var sb = new StringBuilder(128 + Entities.Count * Fields.Count * 20);

                sb.Append("INSERT ");

                if (Ignore)
                {
                    if (Engine == DatabaseEngine.SQLite)
                    {
                        sb.Append("OR ");
                    }

                    if (Engine == DatabaseEngine.PostgreSQL)
                    {

                    }
                    else
                    {
                        sb.Append("IGNORE ");
                    }
                }

                sb.Append("INTO ");

                if (Schema.Length > 0)
                {
                    sb.Append(Settings.Name(Schema)).Append('.');
                }

                var entries = GetEntries();

                sb.Append(Settings.Name(Name))
                    .Append('(')
                    .Append(string.Join(',', entries.Select(x => Settings.Name(x.ColumnName))))
                    .Append(')');

                var parameterSingle = entries.Count(x => !x.Mini);

                var parameters = new Dictionary<string, object>(parameterSingle * Entities.Count);

                int i = 0;

                foreach (var entity in Entities)
                {
                    if (entity is null)
                    {
                        throw new InvalidOperationException("实体不能为空！");
                    }

#if NET6_0_OR_GREATER
                    Validator.ValidateObject(entity, new ValidationContext(entity), true);
#endif

                    if (i == 0)
                    {
                        sb.Append("VALUES(");
                    }
                    else
                    {
                        sb.Append(',').Append('(');
                    }

                    for (int j = 0; j < entries.Length; j++)
                    {
                        var entry = entries[j];

                        if (j > 0)
                        {
                            sb.Append(',');
                        }

                        var value = entry.GetValue(entity);

                        if (value is null)
                        {
                            sb.Append("null");

                            continue;
                        }

                        if (entry.Mini)
                        {
                            sb.Append(value);

                            continue;
                        }

                        var name =
                            i == 0
                                ? entry.ColumnName
                                : string.Concat(entry.ColumnName, "_", i.ToString());

                        parameters.Add(name, value);

                        sb.Append(Settings.ParamterName(name));
                    }

                    sb.Append(')');

                    i++;
                }

                if (Ignore && Engine == DatabaseEngine.PostgreSQL)
                {
                    sb.AppendLine()
                        .Append("ON CONFLICT DO NOTHING");
                }

                return new CommandSql(sb.ToString(), parameters, CommandTimeout);
            }
        }

        private class UpdateableCommand : Command
        {
            private static readonly HashSet<string> _allFields;

            static UpdateableCommand()
            {
                _allFields = _instance
                    .Fields.Select(x => x.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            private readonly ILogger _logger;

            public UpdateableCommand(
                IReadOnlyCollection<TEntity> entities,
                DbStrictAdapter adapter,
                string shardingKey,
                int? commandTimeout,
                ILogger logger
            )
                : base(entities, adapter, shardingKey, commandTimeout)
            {
                _logger = logger;

                Fields = _instance
                    .Fields.SkipWhile(x =>
                        _instance.Keys.Contains(x.Key) || _instance.ReadOnlys.Contains(x.Key)
                    )
                    .Select(x => x.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            public bool SkipIdempotentValid { get; set; }

            public (string, DataTable, string, Dictionary<string, object>, string) Combination()
            {
                var conditions = new HashSet<string>(
                    _instance.Keys,
                    StringComparer.OrdinalIgnoreCase
                );

                foreach (var version in _instance.Versions)
                {
                    if (!_instance.ReadOnlys.Contains(version.Key))
                    {
                        Fields.Add(version.Key);
                    }

                    if (!SkipIdempotentValid)
                    {
                        conditions.Add(version.Key);
                    }
                }

                var entries = GetEntries(_allFields);

                var sb = new StringBuilder(128);
                var updateArgs = new Dictionary<string, object>();

                var temporaryName = $"bulk_{Guid.NewGuid():N}_u";

                #region 构造更新语句。

                bool commaFlag = false;
                bool moreoverFlag = false;

                var t1 = Settings.Name("t1");
                var t2 = Settings.Name("t2");

                switch (Engine)
                {
                    case DatabaseEngine.MySQL:

                        sb.Append("UPDATE ");

                        if (Schema.Length > 0)
                        {
                            sb.Append(Settings.Name(Schema)).Append('.');
                        }

                        sb.Append(Settings.Name(Name))
                            .Append(" AS ")
                            .Append(t1)
                            .AppendLine()
                            .Append("INNER JOIN ")
                            .Append(Settings.Name(temporaryName))
                            .Append(" AS ")
                            .Append(t2)
                            .Append(" ON ");

                        Condition();

                        sb.AppendLine().Append("SET ");

                        Set();
                        break;
                    case DatabaseEngine.SqlServer:
                        sb.Append("UPDATE ").Append(t1).AppendLine().Append("SET ");

                        Set();

                        sb.AppendLine().Append("FROM ");

                        if (Schema.Length > 0)
                        {
                            sb.Append(Settings.Name(Schema)).Append('.');
                        }

                        sb.Append(Settings.Name(Name))
                            .Append(" AS ")
                            .Append(t1)
                            .Append("INNER JOIN ")
                            .Append(Settings.Name(temporaryName))
                            .Append(" AS ")
                            .Append(t2)
                            .Append(" ON ");

                        Condition();

                        break;
                    case DatabaseEngine.DB2:
                    case DatabaseEngine.PostgreSQL:
                        sb.Append("UPDATE ");

                        if (Schema.Length > 0)
                        {
                            sb.Append(Settings.Name(Schema)).Append('.');
                        }

                        sb.Append(Settings.Name(Name))
                            .Append(" AS ")
                            .Append(t1)
                            .AppendLine()
                            .Append("SET ");

                        Set();

                        sb.AppendLine()
                            .Append("FROM ")
                            .Append(Settings.Name(temporaryName))
                            .Append(" AS ")
                            .Append(t2)
                            .AppendLine()
                            .Append("WHERE ");

                        Condition();

                        break;
                    case DatabaseEngine.SQLite:
                    case DatabaseEngine.Oracle:
                    case DatabaseEngine.Sybase:
                    default:
                        throw new NotSupportedException($"数据库引擎“{Engine}”不支持批量更新！");
                }

                //? 更新条件。
                void Condition()
                {
                    foreach (var key in conditions)
                    {
                        if (!_instance.Fields.TryGetValue(key, out var value))
                        {
                            continue;
                        }

                        if (moreoverFlag)
                        {
                            sb.Append(" AND ");
                        }
                        else
                        {
                            moreoverFlag = true;
                        }

                        sb.Append(t1)
                            .Append('.')
                            .Append(Settings.Name(value))
                            .Append('=')
                            .Append(t2)
                            .Append('.')
                            .Append(Settings.Name(value));
                    }
                }

                //? 更新字段。
                void Set()
                {
                    foreach (var key in Fields)
                    {
                        if (!_instance.Fields.TryGetValue(key, out var value))
                        {
                            continue;
                        }

                        if (commaFlag)
                        {
                            sb.Append(',');
                        }
                        else
                        {
                            commaFlag = true;
                        }

                        sb.Append(t1).Append('.').Append(Settings.Name(value)).Append('=');

                        if (!_instance.Versions.TryGetValue(key, out var versionKind))
                        {
                            sb.Append(t2).Append('.').Append(Settings.Name(value));

                            continue;
                        }

                        switch (versionKind)
                        {
                            case VersionKind.Increment:

                                sb.Append(Settings.Name(value)).Append('+').Append('1');

                                break;
                            case VersionKind.Ticks:
                                sb.Append(DateTime.Now.Ticks);

                                break;
                            case VersionKind.Now:
                                switch (Engine)
                                {
                                    case DatabaseEngine.MySQL:
                                    case DatabaseEngine.PostgreSQL:
                                        sb.Append("NOW()");

                                        break;
                                    case DatabaseEngine.SqlServer:
                                    case DatabaseEngine.Sybase:
                                        sb.Append("GETDATE()");

                                        break;
                                    default:
                                        {
                                            string name = value;

                                            updateArgs.Add(name, DateTime.Now);

                                            sb.Append(Settings.ParamterName(value));

                                            break;
                                        }
                                }

                                break;
                            case VersionKind.Timestamp:
                                sb.Append((DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds);
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                    }
                }

                #endregion

                string updateSql = sb.ToString();

                //! 清除更新语句内容，复用容量。
                sb.Clear();

                #region 创建表

                sb.Append("CREATE ");

                if (Engine == DatabaseEngine.MySQL)
                {
                    sb.Append("TEMPORARY ");
                }

                sb.Append("TABLE ").Append(Settings.Name(temporaryName)).Append('(');

                for (int i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];

                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append(Settings.Name(entry.ColumnName))
                        .Append(' ')
                        .Append(GetDbTypeName(entry, Engine));

                    if (!entry.Nullable)
                    {
                        sb.Append(" NOT");
                    }

                    sb.Append(" NULL");
                }

                sb.Append(')');

                #endregion

                string createSql = sb.ToString();

                //! 清除更新语句内容，复用容量。
                sb.Clear();

                #region 删除表。
                sb.Append("DROP ");

                if (Engine == DatabaseEngine.MySQL)
                {
                    sb.Append("TEMPORARY ");
                }

                sb.Append("TABLE ").Append(Settings.Name(temporaryName)).Append(';');

                #endregion

                string dropSql = sb.ToString();

                var dt = new DataTable(temporaryName);

                #region 组装数据。

                Array.ForEach(
                    entries,
                    entry =>
                    {
                        dt.Columns.Add(entry.ColumnName, entry.ColumnType).AllowDBNull =
                            entry.Nullable;
                    }
                );

                foreach (var entity in Entities)
                {
                    if (entity is null)
                    {
                        throw new InvalidOperationException("实体不能为空！");
                    }

                    var dr = dt.NewRow();

                    Array.ForEach(
                        entries,
                        entry =>
                        {
                            dr[entry.ColumnName] = entry.GetValue(entity) ?? DBNull.Value;
                        }
                    );

                    dt.Rows.Add(dr);
                }
                #endregion

                return (createSql, dt, updateSql, updateArgs, dropSql);
            }

            public override void CheckValid()
            {
                if (Fields.Count == 0)
                {
                    throw new InvalidOperationException("未指定更新字段！");
                }
            }

            public override CommandSql GetCommandSql()
            {
                var conditions = new HashSet<string>(
                    _instance.Keys,
                    StringComparer.OrdinalIgnoreCase
                );

                foreach (var version in _instance.Versions)
                {
                    Fields.Add(version.Key);

                    if (SkipIdempotentValid)
                    {
                        continue;
                    }

                    conditions.Add(version.Key);
                }

                var updateEntries = GetEntries();
                var conditionEntries = GetEntries(conditions);

                var parameters = new Dictionary<string, object>();

                var sb = new StringBuilder(
                    (128 + Fields.Count + conditions.Count) * Entities.Count
                );

                int i = 0;

                foreach (var entity in Entities)
                {
                    if (entity is null)
                    {
                        throw new InvalidOperationException("实体不能为空！");
                    }

                    sb.Append("UPDATE ");

                    if (Schema.Length > 0)
                    {
                        sb.Append(Settings.Name(Schema)).Append('.');
                    }

                    sb.Append(Settings.Name(Name));

                    sb.Append(" SET ");

                    for (var j = 0; j < updateEntries.Length; j++)
                    {
                        if (j > 0)
                        {
                            sb.Append(',');
                        }

                        var entry = updateEntries[j];

                        sb.Append(Settings.Name(entry.ColumnName)).Append('=');

                        if (_instance.Versions.TryGetValue(entry.Name, out var versionKind))
                        {
                            switch (versionKind)
                            {
                                case VersionKind.Increment:

                                    sb.Append(Settings.Name(entry.ColumnName))
                                        .Append('+')
                                        .Append('1');

                                    break;
                                case VersionKind.Ticks:
                                    sb.Append(DateTime.Now.Ticks);

                                    break;
                                case VersionKind.Now:
                                    switch (Engine)
                                    {
                                        case DatabaseEngine.MySQL:
                                        case DatabaseEngine.PostgreSQL:
                                            sb.Append("NOW()");
                                            break;
                                        case DatabaseEngine.SqlServer:
                                        case DatabaseEngine.Sybase:
                                            sb.Append("GETDATE()");

                                            break;
                                        default:
                                            {
                                                string nameNow =
                                                    i == 0
                                                        ? string.Concat(entry.ColumnName, "_token")
                                                        : string.Concat(
                                                            entry.ColumnName,
                                                            "_token_",
                                                            i.ToString()
                                                        );

                                                parameters.Add(nameNow, DateTime.Now);

                                                sb.Append(Settings.ParamterName(nameNow));

                                                break;
                                            }
                                    }

                                    break;
                                case VersionKind.Timestamp:
                                    sb.Append(
                                        (DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds
                                    );
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }

                            continue;
                        }

                        var value = entry.GetValue(entity);

                        if (value is null)
                        {
                            sb.Append("null");

                            continue;
                        }

                        if (entry.Mini)
                        {
                            sb.Append(value);

                            continue;
                        }

                        string name =
                            i == 0
                                ? entry.ColumnName
                                : string.Concat(entry.ColumnName, "_", i.ToString());

                        parameters.Add(name, value);

                        sb.Append(Settings.ParamterName(name));
                    }

                    for (int j = 0; j < conditionEntries.Length; j++)
                    {
                        if (j == 0)
                        {
                            sb.Append(" WHERE ");
                        }
                        else
                        {
                            sb.Append(" AND ");
                        }

                        var entry = conditionEntries[j];

                        sb.Append(Settings.Name(entry.ColumnName)).Append('=');

                        var value = entry.GetValue(entity);

                        if (value is null)
                        {
                            _logger.LogWarning("字段“{name}”作为条件时，值不应该为“null”！", entry.ColumnName);

                            sb.Append("null");

                            continue;
                        }

                        if (entry.Mini)
                        {
                            sb.Append(value);

                            continue;
                        }

                        string name =
                            i == 0
                                ? entry.ColumnName
                                : string.Concat(entry.ColumnName, "_", i.ToString());

                        parameters.TryAdd(name, value);

                        sb.Append(Settings.ParamterName(name));
                    }

                    sb.Append(';');

                    i++;
                }

                return new CommandSql(sb.ToString(), parameters, CommandTimeout);
            }
        }

        private class DeleteableCommand : Command
        {
            private readonly ILogger _logger;

            public DeleteableCommand(
                IReadOnlyCollection<TEntity> entities,
                DbStrictAdapter adapter,
                string shardingKey,
                int? commandTimeout,
                ILogger logger
            )
                : base(entities, adapter, shardingKey, commandTimeout)
            {
                _logger = logger;

                Fields = new HashSet<string>(_instance.Keys, StringComparer.OrdinalIgnoreCase);
            }

            public bool SkipIdempotentValid { get; set; }

            public override CommandSql GetCommandSql()
            {
                if (!SkipIdempotentValid)
                {
                    foreach (var version in _instance.Versions)
                    {
                        Fields.Add(version.Key);
                    }
                }

                var sb = new StringBuilder();
                var parameters = new Dictionary<string, object>();

                var entries = GetEntries();

                if (entries.Length == 1)
                {
                    var entry = entries[0];

                    sb.Append("DELETE FROM ");

                    if (Schema.Length > 0)
                    {
                        sb.Append(Settings.Name(Schema)).Append('.');
                    }

                    sb.Append(Settings.Name(Name))
                        .Append(" WHERE ")
                        .Append(Settings.Name(entry.ColumnName));

                    bool hasValue = false;
                    bool hasKeyNull = false;

                    int i = 0;

                    foreach (var entity in Entities)
                    {
                        if (entity is null)
                        {
                            throw new InvalidOperationException("实体不能为空！");
                        }

                        var value = entry.GetValue(entity);

                        if (value is null)
                        {
                            hasKeyNull = true;

                            _logger.LogWarning("字段“{name}”作为条件时，值不应该为“null”！", entry.ColumnName);

                            continue;
                        }

                        if (hasValue)
                        {
                            sb.Append(',');
                        }
                        else
                        {
                            hasValue = true;

                            sb.Append(" IN(");
                        }

                        if (entry.Mini)
                        {
                            sb.Append(value);

                            continue;
                        }

                        var name =
                            i == 0
                                ? entry.ColumnName
                                : string.Concat(entry.ColumnName, "_", i.ToString());

                        sb.Append(Settings.ParamterName(name));

                        parameters.Add(name, value);

                        i++;
                    }

                    if (hasValue)
                    {
                        sb.Append(')').Append(';');

                        if (hasKeyNull)
                        {
                            sb.Append("DELETE FROM ");

                            if (Schema.Length > 0)
                            {
                                sb.Append(Settings.Name(Schema)).Append('.');
                            }

                            sb.Append(Settings.Name(Name))
                                .Append(" WHERE ")
                                .Append(Settings.Name(entry.ColumnName))
                                .Append('=')
                                .Append("null")
                                .Append(';');
                        }
                    }
                    else
                    {
                        sb.Append('=').Append("null").Append(';');
                    }
                }
                else
                {
                    int i = 0;

                    foreach (var entity in Entities)
                    {
                        if (entity is null)
                        {
                            throw new InvalidOperationException("实体不能为空！");
                        }

                        sb.Append("DELETE FROM ");

                        if (Schema.Length > 0)
                        {
                            sb.Append(Settings.Name(Schema)).Append('.');
                        }

                        sb.Append(Settings.Name(Name));

                        for (int j = 0; j < entries.Length; j++)
                        {
                            if (j == 0)
                            {
                                sb.Append(" WHERE ");
                            }
                            else
                            {
                                sb.Append(" AND ");
                            }

                            var entry = entries[j];

                            sb.Append(Settings.Name(entry.ColumnName)).Append('=');

                            var value = entry.GetValue(entity);

                            if (value is null)
                            {
                                _logger.LogWarning(
                                    "字段“{name}”作为条件时，值不应该为“null”！",
                                    entry.ColumnName
                                );

                                sb.Append("null");

                                continue;
                            }

                            if (entry.Mini)
                            {
                                sb.Append(value);

                                continue;
                            }

                            string name =
                                i == 0
                                    ? entry.ColumnName
                                    : string.Concat(entry.ColumnName, "_", i.ToString());

                            parameters.TryAdd(name, value);

                            sb.Append(Settings.ParamterName(name));
                        }

                        sb.Append(';');

                        i++;
                    }
                }

                return new CommandSql(sb.ToString(), parameters, CommandTimeout);
            }

            public override void CheckValid() { }

            public (string, DataTable, string, Dictionary<string, object>, string) Combination()
            {
                if (!SkipIdempotentValid)
                {
                    foreach (var version in _instance.Versions)
                    {
                        Fields.Add(version.Key);
                    }
                }

                var entries = GetEntries();

                var sb = new StringBuilder(128);
                var updateArgs = new Dictionary<string, object>();

                var temporaryName = $"bulk_{Guid.NewGuid():N}_u";

                #region 构造删除语句。

                bool moreoverFlag = false;

                var t1 = Settings.Name("t1");
                var t2 = Settings.Name("t2");

                switch (Engine)
                {
                    case DatabaseEngine.SQLite:
                    case DatabaseEngine.MySQL:
                    case DatabaseEngine.SqlServer:

                        sb.Append("DELETE ").Append(t1).AppendLine().Append("FROM ");

                        if (Schema.Length > 0)
                        {
                            sb.Append(Settings.Name(Schema)).Append('.');
                        }

                        sb.Append(Settings.Name(Name))
                            .Append(" AS ")
                            .Append(t1)
                            .AppendLine()
                            .Append("INNER JOIN ")
                            .Append(Settings.Name(temporaryName))
                            .Append(" AS ")
                            .Append(t2)
                            .Append(" ON ");

                        Condition();

                        break;
                    case DatabaseEngine.DB2:
                    case DatabaseEngine.PostgreSQL:
                        sb.Append("DELETE FROM ");

                        if (Schema.Length > 0)
                        {
                            sb.Append(Settings.Name(Schema)).Append('.');
                        }

                        sb.Append(Settings.Name(Name))
                            .Append(" AS ")
                            .Append(t1)
                            .AppendLine()
                            .Append("USING ")
                            .Append(Settings.Name(temporaryName))
                            .Append(" AS ")
                            .Append(t2)
                            .AppendLine()
                            .Append("WHERE ");

                        Condition();

                        break;
                    case DatabaseEngine.Oracle:
                    case DatabaseEngine.Sybase:
                    default:
                        throw new NotSupportedException($"数据库引擎“{Engine}”不支持批量更新！");
                }

                //? 更新条件。
                void Condition()
                {
                    foreach (var key in Fields)
                    {
                        if (!_instance.Fields.TryGetValue(key, out var value))
                        {
                            continue;
                        }

                        if (moreoverFlag)
                        {
                            sb.Append(" AND ");
                        }
                        else
                        {
                            moreoverFlag = true;
                        }

                        sb.Append(t1)
                            .Append('.')
                            .Append(Settings.Name(value))
                            .Append('=')
                            .Append(t2)
                            .Append('.')
                            .Append(Settings.Name(value));
                    }
                }

                #endregion

                string updateSql = sb.ToString();

                //! 清除更新语句内容，复用容量。
                sb.Clear();

                #region 创建表

                sb.Append("CREATE ");

                if (Engine == DatabaseEngine.MySQL)
                {
                    sb.Append("TEMPORARY ");
                }

                sb.Append("TABLE ").Append(Settings.Name(temporaryName)).Append('(');

                for (int i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];

                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append(Settings.Name(entry.ColumnName))
                        .Append(' ')
                        .Append(GetDbTypeName(entry, Engine));

                    if (!entry.Nullable)
                    {
                        sb.Append(" NOT");
                    }

                    sb.Append(" NULL");
                }

                sb.Append(')');

                #endregion

                string createSql = sb.ToString();

                //! 清除更新语句内容，复用容量。
                sb.Clear();

                #region 删除表。
                sb.Append("DROP ");

                if (Engine == DatabaseEngine.MySQL)
                {
                    sb.Append("TEMPORARY ");
                }

                sb.Append("TABLE ").Append(Settings.Name(temporaryName)).Append(';');

                #endregion

                string deleteSql = sb.ToString();

                var dt = new DataTable(temporaryName);

                #region 组装数据。

                Array.ForEach(
                    entries,
                    entry =>
                    {
                        dt.Columns.Add(entry.ColumnName, entry.ColumnType).AllowDBNull =
                            entry.Nullable;
                    }
                );

                foreach (var entity in Entities)
                {
                    if (entity is null)
                    {
                        throw new InvalidOperationException("实体不能为空！");
                    }

                    var dr = dt.NewRow();

                    Array.ForEach(
                        entries,
                        entry =>
                        {
                            dr[entry.ColumnName] = entry.GetValue(entity) ?? DBNull.Value;
                        }
                    );

                    dt.Rows.Add(dr);
                }
                #endregion

                return (createSql, dt, updateSql, updateArgs, deleteSql);
            }
        }
        #endregion

        #region Insertable
        private class Insertable : IInsertable<TEntity>
        {
            private readonly IDatabaseExecutor _executor;
            private readonly IDatabaseStrings _databaseStrings;

            private readonly InsertCommand _command;

            public Insertable(
                IDatabaseExecutor executor,
                IDatabaseStrings databaseStrings,
                DbStrictAdapter adapter,
                IReadOnlyCollection<TEntity> entities,
                int? commandTimeout,
                string shardingKey,
                bool ignore
            )
            {
                _executor = executor;
                _databaseStrings = databaseStrings;

                _command = new InsertCommand(
                    entities,
                    adapter,
                    ignore,
                    shardingKey,
                    commandTimeout
                );
            }

            public ICommandExecutor Except(string[] columns)
            {
                _command.Except(columns);

                return this;
            }

            public ICommandExecutor Except<TColumn>(Expression<Func<TEntity, TColumn>> columns)
            {
                if (columns is null)
                {
                    throw new ArgumentNullException(nameof(columns));
                }

                return Except(AnalysisFields(columns));
            }

            public int Execute()
            {
                _command.CheckValid();

                if (_command.IsEmpty)
                {
                    return 0;
                }

                if (!_command.RequiredBulk)
                {
                    var commandSql = _command.GetCommandSql();

                    return _executor.Execute(_databaseStrings, commandSql);
                }

                if (_command.Ignore)
                {
                    int influenceSkipRows = 0;

                    var (createSql, dt, insertSql, insertArgs, dropSql) =
                        _command.IgnoreCombination();

                    return _executor.ExecuteMultiple(
                            _databaseStrings,
                            executor =>
                            {
                                influenceSkipRows += executor.Execute(createSql);

                                try
                                {
                                    influenceSkipRows += executor.WriteToServer(dt);

                                    executor.Execute(new CommandSql(insertSql, insertArgs));
                                }
                                finally
                                {
                                    influenceSkipRows += executor.Execute(dropSql);
                                }
                            },
                            _command.CommandTimeout
                        ) - influenceSkipRows;
                }
                else
                {
                    var dt = _command.Combination();

                    return _executor.WriteToServer(
                        _databaseStrings,
                        dt,
                        _command.CommandTimeout
                    );
                }
            }

            public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
            {
                _command.CheckValid();

                if (_command.IsEmpty)
                {
                    return 0;
                }

                if (!_command.RequiredBulk)
                {
                    var commandSql = _command.GetCommandSql();

                    return await _executor.ExecuteAsync(
                        _databaseStrings,
                        commandSql,
                        cancellationToken
                    );
                }

                if (_command.Ignore)
                {
                    int influenceSkipRows = 0;

                    var (createSql, dt, insertSql, insertArgs, dropSql) =
                        _command.IgnoreCombination();

                    return await _executor.ExecuteMultipleAsync(
                            _databaseStrings,
                            async executor =>
                            {
                                influenceSkipRows += await executor.ExecuteAsync(createSql);

                                try
                                {
                                    influenceSkipRows += await executor.WriteToServerAsync(dt);

                                    await executor.ExecuteAsync(
                                        new CommandSql(insertSql, insertArgs)
                                    );
                                }
                                finally
                                {
                                    influenceSkipRows += await executor.ExecuteAsync(dropSql);
                                }
                            },
                            _command.CommandTimeout,
                            cancellationToken
                        ) - influenceSkipRows;
                }
                else
                {
                    var dt = _command.Combination();

                    return await _executor.WriteToServerAsync(
                        _databaseStrings,
                        dt,
                        _command.CommandTimeout,
                        cancellationToken
                    );
                }
            }

            public ICommandExecutor Limit(string[] columns)
            {
                _command.Limit(columns);

                return this;
            }

            public ICommandExecutor Limit<TColumn>(Expression<Func<TEntity, TColumn>> columns)
            {
                if (columns is null)
                {
                    throw new ArgumentNullException(nameof(columns));
                }

                return Limit(AnalysisFields(columns));
            }
        }
        #endregion

        #region Updateable
        private class Updateable : IUpdateable<TEntity>
        {
            private readonly IDatabaseExecutor _executor;
            private readonly IDatabaseStrings _databaseStrings;

            private readonly UpdateableCommand _command;

            public Updateable(
                IDatabaseExecutor executor,
                IDatabaseStrings databaseStrings,
                DbStrictAdapter adapter,
                IReadOnlyCollection<TEntity> entities,
                int? commandTimeout,
                string shardingKey,
                ILogger logger
            )
            {
                _executor = executor;
                _databaseStrings = databaseStrings;

                _command = new UpdateableCommand(
                    entities,
                    adapter,
                    shardingKey,
                    commandTimeout,
                    logger
                );
            }

            public int Execute()
            {
                _command.CheckValid();

                if (_command.IsEmpty)
                {
                    return 0;
                }

                if (!_command.RequiredBulk)
                {
                    var commandSql = _command.GetCommandSql();

                    return _executor.Execute(_databaseStrings, commandSql);
                }

                int influenceSkipRows = 0;

                var (createSql, dt, updateSql, updateArgs, dropSql) = _command.Combination();

                return _executor.ExecuteMultiple(
                        _databaseStrings,
                        executor =>
                        {
                            influenceSkipRows += executor.Execute(createSql);

                            try
                            {
                                influenceSkipRows += executor.WriteToServer(dt);

                                executor.Execute(new CommandSql(updateSql, updateArgs));
                            }
                            finally
                            {
                                influenceSkipRows += executor.Execute(dropSql);
                            }
                        },
                        _command.CommandTimeout
                    ) - influenceSkipRows;
            }

            public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
            {
                _command.CheckValid();

                if (_command.IsEmpty)
                {
                    return 0;
                }

                if (!_command.RequiredBulk)
                {
                    var commandSql = _command.GetCommandSql();

                    return await _executor.ExecuteAsync(
                        _databaseStrings,
                        commandSql,
                        cancellationToken
                    );
                }

                int influenceSkipRows = 0;

                var (createSql, dt, updateSql, updateArgs, dropSql) = _command.Combination();

                return await _executor.ExecuteMultipleAsync(
                        _databaseStrings,
                        async executor =>
                        {
                            influenceSkipRows += await executor.ExecuteAsync(createSql);

                            try
                            {
                                influenceSkipRows += await executor.WriteToServerAsync(dt);

                                await executor.ExecuteAsync(new CommandSql(updateSql, updateArgs));
                            }
                            finally
                            {
                                influenceSkipRows += await executor.ExecuteAsync(dropSql);
                            }
                        },
                        _command.CommandTimeout,
                        cancellationToken
                    ) - influenceSkipRows;
            }

            public IUpdateableOfSet<TEntity> Set(string[] columns)
            {
                _command.Limit(columns);

                return this;
            }

            public IUpdateableOfSet<TEntity> Set<TColumn>(
                Expression<Func<TEntity, TColumn>> columns
            )
            {
                if (columns is null)
                {
                    throw new ArgumentNullException(nameof(columns));
                }

                _command.Limit(AnalysisFields(columns));

                return this;
            }

            public IUpdateableOfSet<TEntity> SetExcept(string[] columns)
            {
                _command.Except(columns);

                return this;
            }

            public IUpdateableOfSet<TEntity> SetExcept<TColumn>(
                Expression<Func<TEntity, TColumn>> columns
            )
            {
                if (columns is null)
                {
                    throw new ArgumentNullException(nameof(columns));
                }

                _command.Except(AnalysisFields(columns));

                return this;
            }

            public ICommandExecutor SkipIdempotentValid()
            {
                _command.SkipIdempotentValid = true;

                return this;
            }
        }
        #endregion

        #region Deleteable
        private class Deleteable : IDeleteable<TEntity>
        {
            private readonly IDatabaseExecutor _executor;
            private readonly IDatabaseStrings _databaseStrings;

            private readonly DeleteableCommand _command;

            public Deleteable(
                IDatabaseExecutor executor,
                IDatabaseStrings databaseStrings,
                DbStrictAdapter adapter,
                IReadOnlyCollection<TEntity> entities,
                int? commandTimeout,
                string shardingKey,
                ILogger logger
            )
            {
                _executor = executor;
                _databaseStrings = databaseStrings;

                _command = new DeleteableCommand(
                    entities,
                    adapter,
                    shardingKey,
                    commandTimeout,
                    logger
                );
            }

            public int Execute()
            {
                _command.CheckValid();

                if (_command.IsEmpty)
                {
                    return 0;
                }

                if (!_command.RequiredBulk)
                {
                    var commandSql = _command.GetCommandSql();

                    return _executor.Execute(_databaseStrings, commandSql);
                }

                int influenceSkipRows = 0;

                var (createSql, dt, updateSql, updateArgs, dropSql) = _command.Combination();

                return _executor.ExecuteMultiple(
                        _databaseStrings,
                        executor =>
                        {
                            influenceSkipRows += executor.Execute(createSql);

                            try
                            {
                                influenceSkipRows += executor.WriteToServer(dt);

                                executor.Execute(new CommandSql(updateSql, updateArgs));
                            }
                            finally
                            {
                                influenceSkipRows += executor.Execute(dropSql);
                            }
                        },
                        _command.CommandTimeout
                    ) - influenceSkipRows;
            }

            public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
            {
                _command.CheckValid();

                if (_command.IsEmpty)
                {
                    return 0;
                }

                if (!_command.RequiredBulk)
                {
                    var commandSql = _command.GetCommandSql();

                    return await _executor.ExecuteAsync(
                        _databaseStrings,
                        commandSql,
                        cancellationToken
                    );
                }

                int influenceSkipRows = 0;

                var (createSql, dt, updateSql, updateArgs, dropSql) = _command.Combination();

                return await _executor.ExecuteMultipleAsync(
                        _databaseStrings,
                        async executor =>
                        {
                            influenceSkipRows += await executor.ExecuteAsync(createSql);

                            try
                            {
                                influenceSkipRows += await executor.WriteToServerAsync(dt);

                                await executor.ExecuteAsync(new CommandSql(updateSql, updateArgs));
                            }
                            finally
                            {
                                influenceSkipRows += await executor.ExecuteAsync(dropSql);
                            }
                        },
                        _command.CommandTimeout,
                        cancellationToken
                    ) - influenceSkipRows;
            }

            public ICommandExecutor SkipIdempotentValid()
            {
                _command.SkipIdempotentValid = true;

                return this;
            }
        }
        #endregion

        #endregion
    }
}
