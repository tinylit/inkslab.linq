using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Inkslab.Linq.Abilities;
using Inkslab.Linq.Enums;
using Microsoft.Extensions.Logging;

namespace Inkslab.Linq
{
    /// <summary>
    /// 仓库路由。
    /// </summary>
    /// <typeparam name="TEntity">实体类型。</typeparam>
    public partial class RepositoryRouter<TEntity> : IRepositoryRouter<TEntity>
        where TEntity : class, new()
    {
        private static readonly Type _elementType;
        private static readonly ITableInfo _instance;
        private static readonly IReadOnlyDictionary<string, string> _columnToProperty;
        private readonly IDatabaseExecutor _databaseExecutor;
        private readonly DbStrictAdapter _adapter;
        private readonly IDatabaseStrings _databaseStrings;
        private readonly ILogger _logger;

        static RepositoryRouter()
        {
            _elementType = typeof(TEntity);

            _instance = TableAnalyzer.Table(_elementType);

            var reverse = new Dictionary<string, string>(_instance.Fields.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var kv in _instance.Fields)
            {
                reverse[kv.Value] = kv.Key;
            }

            _columnToProperty = reverse;
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
            IReadOnlyList<TEntity> entries,
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

        #endregion
    }
}
