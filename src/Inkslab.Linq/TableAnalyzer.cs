using Inkslab.Linq.Enums;
using Inkslab.Linq.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Linq
{
    /// <summary>
    /// 表分析器。
    /// </summary>
    public static class TableAnalyzer
    {
        private static readonly ITableAnalyzer _analyzer;
        private static readonly ConcurrentDictionary<Type, ITableInfo> _tables = new ConcurrentDictionary<Type, ITableInfo>();

        static TableAnalyzer() => _analyzer = SingletonPools.Singleton<ITableAnalyzer, DefaultTableAnalyzer>();

        private class TableInfo : ITableInfo
        {
            public TableInfo(string schema, string name, HashSet<string> keys, HashSet<string> readOnlys, Dictionary<string, VersionKind> versions, Dictionary<string, string> fields)
            {
                Schema = schema;
                Name = name;
                Keys = keys;
                ReadOnlys = readOnlys;
                Versions = versions;
                Fields = fields;
            }

            public string Schema { get; }

            public virtual string Name { get; }

            public IReadOnlyCollection<string> Keys { get; }

            public IReadOnlyCollection<string> ReadOnlys { get; }

            public IReadOnlyDictionary<string, VersionKind> Versions { get; }

            public IReadOnlyDictionary<string, string> Fields { get; }
        }

        private class Config : TabelOptions, IConfig, IConfigTable
        {
            public bool IsValid { get; set; }

            private readonly Type _tableType;

            public Config(Type tableType)
            {
                _tableType = tableType;
            }

            public IConfigTable Field(PropertyInfo propertyInfo, string name, Action<IConfigCol> configCol = null)
            {
                if (propertyInfo is null)
                {
                    throw new ArgumentNullException(nameof(propertyInfo));
                }

                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException($"“{nameof(name)}”不能为 null 或空。", nameof(name));
                }

                if (!propertyInfo.DeclaringType.IsAssignableFrom(_tableType))
                {
                    throw new ArgumentException($"“{_tableType}”类型中，不包含该属性！");
                }

                var key = propertyInfo.Name;

                var field = new Column(name);

                configCol?.Invoke(new ConfigCol(field));

                Columns[key] = field;

                return this;
            }

            IConfigTable IConfig.Table(string name, string schema)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException($"“{nameof(name)}”不能为 null 或空。", nameof(name));
                }

                IsValid = true;

                Name = name;

                Schema = schema;

                return this;
            }
        }

        private class Config<Table> : TabelOptions, IConfig<Table>, IConfigTable<Table>
        {
            public bool IsValid { get; set; }

            public IConfigTable<Table> Field<TCol>(Expression<Func<Table, TCol>> memberCol, string name, Action<IConfigCol> configCol = null)
            {
                if (memberCol is null)
                {
                    throw new ArgumentNullException(nameof(memberCol));
                }

                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException($"“{nameof(name)}”不能为 null 或空。", nameof(name));
                }

                var key = Field(memberCol.Body);

                var field = new Column(name);

                configCol?.Invoke(new ConfigCol(field));

                Columns[key] = field;

                return this;
            }

            private static string Field(Expression node)
            {
                return node switch
                {
                    MemberExpression member => member.Member.Name,
                    BlockExpression block when block.Variables.Count == 0 && block.Expressions.Count == 1 => Field(block.Expressions[0]),
                    GotoExpression @goto when @goto.Kind == GotoExpressionKind.Return => Field(@goto.Value),
                    _ => throw new NotSupportedException(),
                };
            }

            IConfigTable<Table> IConfig<Table>.Table(string name, string schema)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException($"“{nameof(name)}”不能为 null 或空。", nameof(name));
                }

                IsValid = true;

                Name = name;

                Schema = schema;

                return this;
            }
        }

        private class ConfigCol : IConfigCol
        {
            private readonly Column _field;

            public ConfigCol(Column field)
            {
                _field = field;
            }

            public void Ignore()
            {
                _field.Ignore = true;
            }

            public IConfigCol IsPrimaryKey()
            {
                _field.Key = true;

                return this;
            }

            public IConfigCol IsReadOnly()
            {
                _field.ReadOnly = true;

                return this;
            }

            public IConfigCol Version(VersionKind version)
            {
                _field.Version = version;

                return this;
            }
        }

        /// <summary>
        /// 表消息。
        /// </summary>
        /// <param name="tableType">表类型。</param>
        /// <returns>表信息。</returns>
        /// <exception cref="ArgumentNullException">参数“<paramref name="tableType"/>”为“null”。</exception>
        public static ITableInfo Table(Type tableType)
        {
            if (tableType is null)
            {
                throw new ArgumentNullException(nameof(tableType));
            }

            return _tables.GetOrAdd(tableType, type =>
            {
                var options = _analyzer.Table(type) ?? throw new NotSupportedException();

                if (options.Name.IsEmpty())
                {
                    throw new NotSupportedException("请指定表名称！");
                }

                if (options.Columns.Count == 0)
                {
                    throw new NotSupportedException("请声明字段！");
                }

                HashSet<string> keys = new HashSet<string>(1);
                HashSet<string> readOnlys = new HashSet<string>(1);
                Dictionary<string, VersionKind> versions = new Dictionary<string, VersionKind>(1);
                Dictionary<string, string> fields = new Dictionary<string, string>(options.Columns.Count);

                foreach (var (key, col) in options.Columns)
                {
                    if (col.Key)
                    {
                        keys.Add(key);
                    }

                    if (col.ReadOnly)
                    {
                        readOnlys.Add(key);
                    }

                    if (col.Version > VersionKind.None)
                    {
                        versions.Add(key, col.Version);
                    }

                    fields.Add(key, col.Name);
                }

                return new TableInfo(options.Schema ?? string.Empty, options.Name, keys, readOnlys, versions, fields);
            });
        }

        private static bool Register(Type tableType, TabelOptions options)
        {
            if (_tables.ContainsKey(tableType))
            {
                return false;
            }

            var optionsGlobal = _analyzer.Table(tableType);

            if (options is null)
            {
                throw new NotSupportedException();
            }

            if (options.Name.IsEmpty())
            {
                throw new NotSupportedException("请指定表名称！");
            }

            if (options.Columns.Count == 0)
            {
                throw new NotSupportedException("请声明字段！");
            }

            HashSet<string> keys = new HashSet<string>(1);
            HashSet<string> readOnlys = new HashSet<string>(1);
            Dictionary<string, VersionKind> versions = new Dictionary<string, VersionKind>(1);

            Dictionary<string, string> cloumns = new Dictionary<string, string>(optionsGlobal.Columns.Count);

            foreach (var (key, col) in options.Columns.Union(optionsGlobal.Columns, Singleton<EqualityComparer>.Instance))
            {
                if (col.Ignore)
                {
                    continue;
                }

                if (col.Key)
                {
                    keys.Add(key);
                }

                if (col.ReadOnly)
                {
                    readOnlys.Add(key);
                }

                if (col.Version > VersionKind.None)
                {
                    versions.Add(key, col.Version);
                }

                cloumns.Add(key, col.Name);
            }

            return _tables.TryAdd(tableType, new TableInfo(options.Schema ?? string.Empty, options.Name, keys, readOnlys, versions, cloumns));
        }

        private class EqualityComparer : IEqualityComparer<KeyValuePair<string, Column>>
        {
            public bool Equals(KeyValuePair<string, Column> x, KeyValuePair<string, Column> y) => string.Equals(x.Key, y.Key, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode(KeyValuePair<string, Column> obj) => obj.Key.GetHashCode();
        }

        /// <summary>
        /// 注册表。
        /// </summary>
        /// <typeparam name="Table">表类型。</typeparam>
        /// <param name="config">配置器（没有配置的属性，将使用默认配置）。</param>
        /// <returns>是否注册成功。</returns>
        /// <exception cref="ArgumentNullException">参数“<paramref name="config"/>”为“null”。</exception>
        public static bool Register<Table>(Action<IConfig<Table>> config) where Table : class
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var tableType = typeof(Table);

            var configTable = new Config<Table>();

            config(configTable);

            return configTable.IsValid && Register(tableType, configTable);
        }

        /// <summary>
        /// 注册表。
        /// </summary>
        /// <param name="tableType">表类型。</param>
        /// <param name="config">配置器（没有配置的属性，将使用默认配置）。</param>
        /// <returns>是否注册成功。</returns>
        /// <exception cref="ArgumentNullException">参数“<paramref name="tableType"/>”或“<paramref name="config"/>”为“null”。</exception>
        public static bool Register(Type tableType, Action<IConfig> config)
        {
            if (tableType is null)
            {
                throw new ArgumentNullException(nameof(tableType));
            }

            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var configTable = new Config(tableType);

            config(configTable);

            return configTable.IsValid && Register(tableType, configTable);
        }
    }
}
