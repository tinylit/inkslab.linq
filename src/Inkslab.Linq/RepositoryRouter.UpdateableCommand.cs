using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Inkslab.Linq.Enums;
using Microsoft.Extensions.Logging;

#if NET6_0_OR_GREATER
using System.ComponentModel.DataAnnotations;
#endif

namespace Inkslab.Linq
{
    public partial class RepositoryRouter<TEntity>
        where TEntity : class, new()
    {
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

#if NET6_0_OR_GREATER
                //! 反复构造 GetEntries(Fields) 时会包含 Version 列；预过滤一次而非循环内逐项判断。
                var condValidEntries = GetEntries(conditions);
                var updateValidEntries = BuildValidatableEntries(Fields);
#endif

                foreach (var entity in Entities)
                {
                    if (entity is null)
                    {
                        throw new InvalidOperationException("实体不能为空！");
                    }

#if NET6_0_OR_GREATER
                    var validationCtx = new ValidationContext(entity);
                    ValidateEntries(validationCtx, entity, condValidEntries);
                    ValidateEntries(validationCtx, entity, updateValidEntries);
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

                return (createSql, dt, updateSql, updateArgs, dropSql);
            }

#if NET6_0_OR_GREATER
            //! 排除 Version 列：版本字段由框架自动写值，业务无须校验。
            private static Entry[] BuildValidatableEntries(ICollection<string> fields)
            {
                if (_instance.Versions.Count == 0)
                {
                    return GetEntries(fields);
                }

                var filtered = new HashSet<string>(fields, StringComparer.OrdinalIgnoreCase);
                filtered.ExceptWith(_instance.Versions.Keys);
                return GetEntries(filtered);
            }

            //! 复用调用方提供的 ValidationContext，按需切换 MemberName；条件字段与更新字段
            //! 共享同一个上下文，避免每实体两次 ValidationContext 分配。
            private static void ValidateEntries(ValidationContext ctx, TEntity entity, Entry[] entries)
            {
                if (entries.Length == 0) return;

                foreach (var entry in entries)
                {
                    ctx.MemberName = entry.Name;
                    Validator.ValidateProperty(entry.GetValue(entity), ctx);
                }
            }
#endif

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

#if NET6_0_OR_GREATER
                //! 预先剔除 Version 列；版本由框架写值，不参与业务校验。
                var updateValidEntries = BuildValidatableEntries(Fields);
#endif

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

#if NET6_0_OR_GREATER
                    var validationCtx = new ValidationContext(entity);
                    ValidateEntries(validationCtx, entity, conditionEntries);
                    ValidateEntries(validationCtx, entity, updateValidEntries);
#endif

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
    }
}
