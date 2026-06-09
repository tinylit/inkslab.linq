using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Inkslab.Linq.Enums;
using static System.Linq.Expressions.Expression;

using System.ComponentModel.DataAnnotations;

namespace Inkslab.Linq
{
    public partial class RepositoryRouter<TEntity>
        where TEntity : class, new()
    {
        private class InsertCommand : Command
        {
            //! 自增主键回填元数据：仅当 [Key]+[DatabaseGenerated] 标记的属性可写时才有值。
            private static readonly string _identityColumnName;
            private static readonly Action<TEntity, object> _identitySetter;

            static InsertCommand()
            {
                (_identityColumnName, _identitySetter) = BuildIdentitySetter();
            }

            //! 收紧判定：当且仅当主键唯一且该主键标记为只读（[DatabaseGenerated]）时，
            //! 视作可回填的自增主键。复合主键、非只读主键统统不进回填路径，
            //! 避免「Key+ReadOnly」推断在复合键、默认值主键等场景下的误判。
            private static (string ColumnName, Action<TEntity, object> Setter) BuildIdentitySetter()
            {
                if (_instance.Keys.Count != 1)
                {
                    return (null, null);
                }

                string key = _instance.Keys.First();

                if (!_instance.ReadOnlys.Contains(key))
                {
                    return (null, null);
                }

                if (!_instance.Fields.TryGetValue(key, out var colName))
                {
                    return (null, null);
                }

                var prop = _elementType.GetProperty(key);

                if (prop is null || !prop.CanWrite)
                {
                    return (null, null);
                }

                //! value 始终来自 RETURNING / OUTPUT INSERTED / LAST_INSERT_ID()，装箱 long。
                //! 常见整型直接 (T)(long)value，避开 Convert.ChangeType 的 IConvertible 调度。
                var entityParam = Parameter(_elementType, "entity");
                var valueParam = Parameter(typeof(object), "value");
                var propType = prop.PropertyType;

                //! 可空主键（long? / int? ...）先解包到底层类型再做整型转换，
                //! 否则会落入 Convert.ChangeType(value, typeof(T?)) 运行时抛 InvalidCastException。
                var underlyingType = Nullable.GetUnderlyingType(propType);
                bool isNullable = underlyingType is not null;
                var targetType = underlyingType ?? propType;

                Expression assigned;

                if (targetType == typeof(long))
                {
                    assigned = Convert(valueParam, typeof(long));
                }
                else if (targetType == typeof(int)
                    || targetType == typeof(short)
                    || targetType == typeof(byte)
                    || targetType == typeof(sbyte)
                    || targetType == typeof(uint)
                    || targetType == typeof(ushort)
                    || targetType == typeof(ulong)
                    || targetType == typeof(decimal))
                {
                    assigned = Convert(Convert(valueParam, typeof(long)), targetType);
                }
                else
                {
                    var changeType = typeof(System.Convert).GetMethod(
                        nameof(System.Convert.ChangeType),
                        new[] { typeof(object), typeof(Type) }
                    )!;

                    assigned = Convert(Call(changeType, valueParam, Constant(targetType)), targetType);
                }

                //! 解包过的可空主键：用 Nullable<T>(value) 构造函数装回，匹配赋值目标类型。
                if (isNullable)
                {
                    assigned = New(propType.GetConstructor(new[] { targetType })!, assigned);
                }

                var setter = Lambda<Action<TEntity, object>>(
                    Assign(Property(entityParam, prop), assigned),
                    entityParam,
                    valueParam
                ).Compile();

                return (colName, setter);
            }

            //! 保留构造上下文，供 Slice 派生承载实体子集的同构子命令（Bulk 拆批 / Ignore 单行降级）。
            //! 实体集合在入口即为 IReadOnlyList，命令内部按索引操作，无需再做集合转换。
            private readonly DbStrictAdapter _adapter;
            private readonly string _shardingKey;
            private readonly IReadOnlyList<TEntity> _entities;

            public InsertCommand(
                IReadOnlyList<TEntity> entities,
                DbStrictAdapter adapter,
                bool ignore,
                string shardingKey,
                int? commandTimeout
            )
                : base(entities, adapter, shardingKey, commandTimeout)
            {
                _adapter = adapter;
                _shardingKey = shardingKey;
                _entities = entities;

                Ignore = ignore;

                Fields = _instance
                    .Fields.SkipWhile(x => _instance.ReadOnlys.Contains(x.Key))
                    .Select(x => x.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            /// <summary>
            /// 派生一个仅承载 <c>[offset, offset+count)</c> 区间实体的同构子命令：复制当前字段集合与反写开关，
            /// 复用适配器、分区键与超时上下文，确保子命令的 <see cref="GetCommandSql"/> 行为与父命令一致。
            /// 用于 Bulk 拆批与 Ignore 单行降级路径——子命令以自身实体集合（原集合的只读窗口视图）工作，
            /// 不复制实体、不向外暴露实体参数。
            /// </summary>
            public InsertCommand Slice(int offset, int count)
            {
                var command = new InsertCommand(
                    new ListSegment(_entities, offset, count),
                    _adapter,
                    Ignore,
                    _shardingKey,
                    CommandTimeout
                )
                {
                    Fields = new HashSet<string>(Fields, StringComparer.OrdinalIgnoreCase)
                };

                if (PopulateIdentityEnabled)
                {
                    command.PopulateIdentityEnabled = true;
                }

                return command;
            }

            public bool Ignore { get; }

            /// <summary>是否可回填自增主键（存在 [Key]+[DatabaseGenerated] 标记的可写属性）。</summary>
            public bool CanPopulateIdentity => _identitySetter != null;

            /// <summary>调用方是否显式开启了自增主键回填（通过 <c>IInsertable.PopulateIdentity()</c>）。</summary>
            public bool PopulateIdentityEnabled { get; private set; }

            /// <summary>显式开启自增主键回填。fail-fast 守门：
            /// 1) 无可回填自增主键 → <see cref="InvalidOperationException"/>；
            /// 2) 当前数据库引擎不支持回填或 Ignore + 回填组合不被支持 → <see cref="NotSupportedException"/>。
            /// 所有可静态判定的不兼容情况都在此处暴露，避免拖到 Execute 阶段。</summary>
            public void EnablePopulateIdentity()
            {
                if (!CanPopulateIdentity)
                {
                    throw new InvalidOperationException(
                        $"实体“{_elementType.Name}”不存在「单主键 + [DatabaseGenerated]」配置，无法启用自增主键回填。"
                    );
                }

                //! 引擎可支持矩阵（能净则净，余者 fail-fast）：
                //! • 非 Ignore：PostgreSQL / SqlServer / MySQL / SQLite / DB2 / Sybase
                //!   （Oracle 无法用可被 Query<long> 读取的多行 identity 返回，不支持批量自增主键回填）；
                //! • Ignore：仅 PostgreSQL / MySQL / SQLite（具备原生 INSERT 忽略且能配合 id 返回）。
                //!   SqlServer / Oracle / DB2 / Sybase 无干净的原生忽略语义。
                if (Ignore)
                {
                    if (Engine is not (DatabaseEngine.PostgreSQL
                        or DatabaseEngine.MySQL
                        or DatabaseEngine.SQLite))
                    {
                        throw new NotSupportedException(
                            $"启用自增主键回填时，数据库引擎“{Engine}”不支持 Ignore 模式（仅支持 PostgreSQL / MySQL / SQLite）。"
                        );
                    }
                }
                else if (Engine is not (DatabaseEngine.PostgreSQL
                    or DatabaseEngine.SqlServer
                    or DatabaseEngine.MySQL
                    or DatabaseEngine.SQLite
                    or DatabaseEngine.DB2
                    or DatabaseEngine.Sybase))
                {
                    throw new NotSupportedException(
                        $"启用自增主键回填时，数据库引擎“{Engine}”不支持（仅支持 PostgreSQL / SqlServer / MySQL / SQLite / DB2 / Sybase；Oracle 不支持批量自增主键回填）。"
                    );
                }

                PopulateIdentityEnabled = true;
            }

            /// <summary>
            /// 按位置 zip 回填一批自增主键 ID 到当前命令自身的实体集合。
            /// 严格校验 <paramref name="ids"/> 与实体数长度相等，否则抛 <see cref="InvalidOperationException"/>。
            /// 每个实体回填的都是数据库为该行真实生成的 ID——
            /// RETURNING 族（PostgreSQL/SqlServer/SQLite/DB2）逐行返回，标量族（MySQL/Sybase）逐行单条执行后返回。
            /// 不做任何「ID 连续」假设。
            /// </summary>
            public void PopulateIdentities(IList<long> ids)
            {
                if (ids.Count != _entities.Count)
                {
                    throw new InvalidOperationException(
                        $"自增主键回填：实体数（{_entities.Count}）与返回的 ID 数（{ids.Count}）不一致。"
                    );
                }

                for (int i = 0; i < _entities.Count; i++)
                {
                    _identitySetter(_entities[i], ids[i]);
                }
            }

            /// <summary>
            /// 回填第 <paramref name="index"/> 个实体的精确自增主键 ID。
            /// 用于标量族（MySQL <c>LAST_INSERT_ID()</c> / Sybase <c>@@IDENTITY</c>）多语句分批后，
            /// 按行回填各自单行 INSERT 取回的精确生成 ID——无任何连续/倒推假设。
            /// </summary>
            public void PopulateIdentityAt(int index, long id) => _identitySetter(_entities[index], id);

            /// <summary>每行非内联（非 Mini、需参数）字段数，用于估算多语句分批的参数预算上限。</summary>
            public int CountParameterPerRow()
            {
                var entries = GetEntries();
                int n = 0;
                for (int k = 0; k < entries.Length; k++)
                {
                    if (!entries[k].Mini)
                    {
                        n++;
                    }
                }
                return n;
            }

            /// <summary>
            /// 生成标量族（MySQL/Sybase）的多语句反写命令：为本命令（一个分块）的每个实体，按行追加
            /// 「单行 INSERT[ IGNORE]; [SELECT ROW_COUNT();] SELECT LAST_INSERT_ID()/@@IDENTITY;」。
            /// 结果集按语句执行顺序返回，每个标量紧跟自己那条单行 INSERT，逐行精确——不依赖 ID 连续，
            /// 也不依赖结果集行序（每个 SELECT 只回一行标量）。仅 MySQL 的 Ignore 追加 <c>ROW_COUNT()</c> 以可靠判定是否被跳过。
            /// </summary>
            public CommandSql GetScalarPopulateIdentityCommandSql()
            {
                var entries = GetEntries();

                int parameterPerRow = 0;
                for (int k = 0; k < entries.Length; k++)
                {
                    if (!entries[k].Mini)
                    {
                        parameterPerRow++;
                    }
                }

                var sb = new StringBuilder(64 + Entities.Count * (entries.Length * 24 + 48));
                var parameters = new Dictionary<string, object>(parameterPerRow * Entities.Count);

                int i = 0;

                foreach (var entity in Entities)
                {
                    if (entity is null)
                    {
                        throw new InvalidOperationException("实体不能为空！");
                    }

                    Validator.ValidateObject(entity, new ValidationContext(entity), true);

                    sb.Append("INSERT ");

                    if (Ignore) //! 标量族里仅 MySQL 允许 Ignore（Sybase 已在 EnablePopulateIdentity 拦截）。
                    {
                        sb.Append("IGNORE ");
                    }

                    sb.Append("INTO ");

                    if (Schema.Length > 0)
                    {
                        sb.Append(Settings.Name(Schema)).Append('.');
                    }

                    sb.Append(Settings.Name(Name)).Append('(');

                    for (int k = 0; k < entries.Length; k++)
                    {
                        if (k > 0) sb.Append(',');
                        sb.Append(Settings.Name(entries[k].ColumnName));
                    }

                    sb.Append(')').Append("VALUES(");

                    for (int j = 0; j < entries.Length; j++)
                    {
                        if (j > 0) sb.Append(',');

                        var entry = entries[j];
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

                        var name = i == 0
                            ? entry.ColumnName
                            : string.Concat(entry.ColumnName, "_", i.ToString());

                        parameters.Add(name, value);

                        sb.Append(Settings.ParamterName(name));
                    }

                    sb.Append(')').Append(';');

                    if (Ignore)
                    {
                        sb.Append("\nSELECT ROW_COUNT();");
                    }

                    sb.Append(Engine == DatabaseEngine.Sybase
                        ? "\nSELECT @@IDENTITY;"
                        : "\nSELECT LAST_INSERT_ID();");

                    i++;
                }

                return new CommandSql(sb.ToString(), parameters, CommandTimeout);
            }

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

                    Validator.ValidateObject(entity, new ValidationContext(entity), true);
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

                    Validator.ValidateObject(entity, new ValidationContext(entity), true);

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

            /// <summary>
            /// 生成 INSERT 语句，以当前命令自身的实体集合为数据源。
            /// 是否追加自增主键回填子句由调用方是否通过 <see cref="EnablePopulateIdentity"/> 开启回填决定：
            /// PostgreSQL 使用 <c>RETURNING</c>，SQL Server 使用 <c>OUTPUT INSERTED</c>，MySQL 追加 <c>SELECT LAST_INSERT_ID()</c>。
            /// </summary>
            //! 单内核同时承载普通批量、Bulk 拆批、Ignore 单行降级三条路径。
            //! Ignore 与反写可共存：PostgreSQL 走 ON CONFLICT DO NOTHING RETURNING，
            //! MySQL 用 INSERT IGNORE + LAST_INSERT_ID()（被忽略时返回 0，由调用方据此判定）。
            //! SQL Server 不存在原生 IGNORE 语义，调用方应避免该组合。
            public override CommandSql GetCommandSql()
            {
                var entities = Entities;
                bool withReturnedIds = PopulateIdentityEnabled;

                var entries = GetEntries();

                var sb = new StringBuilder(128 + entities.Count * Fields.Count * 20);

                sb.Append("INSERT ");

                if (Ignore)
                {
                    switch (Engine)
                    {
                        case DatabaseEngine.SQLite:
                            sb.Append("OR IGNORE ");
                            break;
                        case DatabaseEngine.PostgreSQL:
                            //! PostgreSQL 由结尾 ON CONFLICT DO NOTHING 处理。
                            break;
                        default:
                            sb.Append("IGNORE ");
                            break;
                    }
                }

                sb.Append("INTO ");

                if (Schema.Length > 0)
                {
                    sb.Append(Settings.Name(Schema)).Append('.');
                }

                sb.Append(Settings.Name(Name)).Append('(');

                for (int k = 0; k < entries.Length; k++)
                {
                    if (k > 0) sb.Append(',');
                    sb.Append(Settings.Name(entries[k].ColumnName));
                }

                sb.Append(')');

                if (withReturnedIds && Engine == DatabaseEngine.SqlServer)
                {
                    sb.Append(" OUTPUT INSERTED.").Append(Settings.Name(_identityColumnName));
                }

                int parameterPerRow = 0;
                for (int k = 0; k < entries.Length; k++)
                {
                    if (!entries[k].Mini) parameterPerRow++;
                }

                var parameters = new Dictionary<string, object>(parameterPerRow * entities.Count);

                int i = 0;

                foreach (var entity in entities)
                {
                    if (entity is null)
                    {
                        throw new InvalidOperationException("实体不能为空！");
                    }

                    Validator.ValidateObject(entity, new ValidationContext(entity), true);

                    sb.Append(i == 0 ? "VALUES(" : ",(");

                    for (int j = 0; j < entries.Length; j++)
                    {
                        if (j > 0) sb.Append(',');

                        var entry = entries[j];
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

                        var name = i == 0
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
                    sb.AppendLine().Append("ON CONFLICT DO NOTHING");
                }

                if (withReturnedIds)
                {
                    switch (Engine)
                    {
                        case DatabaseEngine.PostgreSQL:
                        case DatabaseEngine.SQLite:
                            //! SQLite 与 PostgreSQL 同构，尾部 RETURNING（需要 SQLite 3.35+）。
                            sb.AppendLine()
                              .Append("RETURNING ")
                              .Append(Settings.Name(_identityColumnName));
                            break;
                        case DatabaseEngine.MySQL:
                            //! 标量族逐行单条执行：单行 INSERT 的 LAST_INSERT_ID() 即该行精确 id。
                            sb.Append(";\nSELECT LAST_INSERT_ID()");
                            break;
                        case DatabaseEngine.Sybase:
                            //! 标量族逐行单条执行：单行 INSERT 的 @@IDENTITY 即该行精确 id。
                            sb.Append(";\nSELECT @@IDENTITY");
                            break;
                        case DatabaseEngine.DB2:
                            //! DB2：SELECT col FROM FINAL TABLE (INSERT ...)，包裹整条插入语句。
                            sb.Insert(
                                    0,
                                    string.Concat(
                                        "SELECT ",
                                        Settings.Name(_identityColumnName),
                                        " FROM FINAL TABLE ("
                                    )
                                )
                              .Append(')');
                            break;
                    }
                }

                return new CommandSql(sb.ToString(), parameters, CommandTimeout);
            }
        }
    }
}
