using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Inkslab.Linq.Enums;
using static System.Linq.Expressions.Expression;

#if NET6_0_OR_GREATER
using System.Reflection;
using System.ComponentModel.DataAnnotations;
#endif

namespace Inkslab.Linq
{
    public partial class RepositoryRouter<TEntity>
        where TEntity : class, new()
    {
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

        /// <summary>
        /// 只读列表的窗口视图：以 <c>[offset, offset+count)</c> 区间映射到底层列表，零拷贝。
        /// 用于 INSERT 反写拆批，避免每批复制出新的实体列表。
        /// </summary>
        private sealed class ListSegment : IReadOnlyList<TEntity>
        {
            private readonly IReadOnlyList<TEntity> _source;
            private readonly int _offset;

            public ListSegment(IReadOnlyList<TEntity> source, int offset, int count)
            {
                if (source is null)
                {
                    throw new ArgumentNullException(nameof(source));
                }

                if (offset < 0 || count < 0 || offset + count > source.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }

                _source = source;
                _offset = offset;
                Count = count;
            }

            public int Count { get; }

            public TEntity this[int index] => _source[_offset + index];

            public IEnumerator<TEntity> GetEnumerator()
            {
                for (int i = 0; i < Count; i++)
                {
                    yield return _source[_offset + i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator() =>
                GetEnumerator();
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

            public IReadOnlyCollection<TEntity> Entities { get; }

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
                            if (_columnToProperty.TryGetValue(column, out var propertyName))
                            {
                                Fields.Remove(propertyName);
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
                        else if (_columnToProperty.TryGetValue(column, out var propertyName))
                        {
                            fields.Add(propertyName);
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
    }
}
