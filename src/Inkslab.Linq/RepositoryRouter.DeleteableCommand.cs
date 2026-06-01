using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Inkslab.Linq.Enums;
using Microsoft.Extensions.Logging;

namespace Inkslab.Linq
{
    public partial class RepositoryRouter<TEntity>
        where TEntity : class, new()
    {
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
    }
}
