using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;

namespace Inkslab.Linq
{
    /// <summary>
    /// 查找数据类型。
    /// </summary>
    public static class LookupDb
    {
        /// <summary>
        /// 数组数据库类型。
        /// </summary>
        public const DbType EnumerableDbType = (DbType)(-1); // Custom value for Array type

        /// <summary>
        /// JSON 数据库类型。
        /// </summary>
        public const DbType JsonDbType = (DbType)(-512); // Custom value for JSON type

        /// <summary>
        /// JSONB 数据库类型。
        /// </summary>
        public const DbType JsonbDbType = (DbType)(-1024); // Custom value for JSONB type

        private static readonly Dictionary<Type, DbType> _typeMap;

        static LookupDb()
        {
            _typeMap = new Dictionary<Type, DbType>
            {
                [typeof(byte)] = DbType.Byte,
                [typeof(sbyte)] = DbType.SByte,
                [typeof(short)] = DbType.Int16,
                [typeof(ushort)] = DbType.UInt16,
                [typeof(int)] = DbType.Int32,
                [typeof(uint)] = DbType.UInt32,
                [typeof(long)] = DbType.Int64,
                [typeof(ulong)] = DbType.UInt64,
                [typeof(float)] = DbType.Single,
                [typeof(double)] = DbType.Double,
                [typeof(decimal)] = DbType.Decimal,
                [typeof(bool)] = DbType.Boolean,
                [typeof(string)] = DbType.String,
                [typeof(char)] = DbType.StringFixedLength,
                [typeof(Guid)] = DbType.Guid,
                [typeof(DateTime)] = DbType.DateTime,
                [typeof(DateTimeOffset)] = DbType.DateTimeOffset,
                [typeof(TimeSpan)] = DbType.Time,
                [typeof(JsonPayload)] = JsonDbType,
                [typeof(JsonbPayload)] = JsonbDbType,
                [typeof(byte[])] = DbType.Binary,
                [typeof(object)] = DbType.Object
            };
        }

        /// <summary>
        /// 转数据库类型。
        /// </summary>
        /// <param name="dataType">类型。</param>
        /// <returns></returns>
        public static DbType For(Type dataType)
        {
            if (dataType.IsNullable())
            {
                dataType = Nullable.GetUnderlyingType(dataType);
            }

            if (dataType.IsEnum)
            {
                dataType = Enum.GetUnderlyingType(dataType);
            }

            if (_typeMap.TryGetValue(dataType, out DbType dbType))
            {
                return dbType;
            }

            if (dataType.FullName == "System.Data.Linq.Binary")
            {
                return DbType.Binary;
            }

            return DbType.Object;
        }

        /// <summary>
        /// 参数适配。
        /// </summary>
        /// <param name="command">命令。</param>
        /// <param name="databaseEngine">数据库引擎。</param>
        /// <param name="name">参数名称。</param>
        /// <param name="value">参数值。</param>
        public static void AddParameterAuto(IDbCommand command, DatabaseEngine databaseEngine, string name, object value)
        {
            var dbParameter = command.CreateParameter();

            switch (value)
            {
                case null or DBNull:
                    dbParameter.ParameterName = name;
                    dbParameter.Value = DBNull.Value;

                    if (dbParameter is DbParameter myParameter)
                    {
                        myParameter.IsNullable = true;
                    }
                    break;
                case DynamicParameter dynamicParameter:

                    var dynamicParameterValueType = dynamicParameter.Value?.GetType() ?? typeof(object);

                    dbParameter.Value = dynamicParameter.Value is null ? DBNull.Value : ChangeValue(databaseEngine, dynamicParameterValueType, dynamicParameter.Value);

                    dbParameter.ParameterName = name;

                    dbParameter.Direction = dynamicParameter.Direction;

                    var dbType = dynamicParameter.DbType ?? (dbParameter.Value is null ? DbType.Object : For(dynamicParameterValueType));

                    if (dbType < 0)
                    {
                        if (databaseEngine == DatabaseEngine.PostgreSQL)
                        {
                            if (dbType.IsJsonType())
                            {
                                command.CommandText = AppendJsonCast(command.CommandText, name, "::json");
                            }
                            else if (dbType.IsJsonbType())
                            {
                                command.CommandText = AppendJsonCast(command.CommandText, name, "::jsonb");
                            }
                        }

                        dbParameter.DbType = DbType.String;
                    }
                    else
                    {
                        dbParameter.DbType = dbType;
                    }

                    if (dynamicParameter.Size.HasValue)
                    {
                        dbParameter.Size = dynamicParameter.Size.Value;
                    }
                    if (dynamicParameter.Precision.HasValue || dynamicParameter.Scale.HasValue)
                    {
                        if (dbParameter is DbParameter dbParameter1)
                        {
                            if (dynamicParameter.Precision.HasValue)
                            {
                                dbParameter1.Precision = dynamicParameter.Precision.Value;
                            }
                            if (dynamicParameter.Scale.HasValue)
                            {
                                dbParameter1.Scale = dynamicParameter.Scale.Value;
                            }
                        }
                    }
                    break;
                default:
                    var valueType = value.GetType();

                    var valueDbType = For(valueType);

                    if (valueDbType < 0)
                    {
                        if (databaseEngine == DatabaseEngine.PostgreSQL)
                        {
                            if (valueDbType.IsJsonType())
                            {
                                command.CommandText = AppendJsonCast(command.CommandText, name, "::json");
                            }
                            else if (valueDbType.IsJsonbType())
                            {
                                command.CommandText = AppendJsonCast(command.CommandText, name, "::jsonb");
                            }
                        }

                        dbParameter.DbType = DbType.String;
                    }
                    else
                    {
                        dbParameter.DbType = valueDbType;
                    }

                    var realValue = ChangeValue(databaseEngine, valueType, value);

                    dbParameter.Value = realValue;
                    dbParameter.ParameterName = name;
                    dbParameter.Direction = ParameterDirection.Input;

                    if (realValue is string text)
                    {
                        // 区分 varchar/nvarchar 的引擎（SqlServer、Oracle、DB2、Sybase）：
                        // 使用 AnsiString (varchar) 替代 String (nvarchar)，
                        // 避免 nvarchar/varchar 隐式转换导致索引失效。
                        if (IsAnsiPreferredEngine(databaseEngine) && IsAscii(text))
                        {
                            dbParameter.DbType = DbType.AnsiString;

                            // varchar 非 MAX 上限为 8000，统一 Size 使执行计划缓存可复用。
                            if (text.Length < 8000)
                            {
                                dbParameter.Size = 8000;
                            }
                        }
                        else if (text.Length < 4000)
                        {
                            // nvarchar 非 MAX 上限为 4000，统一 Size 使执行计划缓存可复用。
                            dbParameter.Size = 4000;
                        }
                    }

                    break;
            }

            command.Parameters.Add(dbParameter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAnsiPreferredEngine(DatabaseEngine engine)
        {
            return engine is DatabaseEngine.SqlServer
                or DatabaseEngine.Oracle
                or DatabaseEngine.DB2
                or DatabaseEngine.Sybase;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAscii(string text)
        {
            foreach (var c in text)
            {
                if (c > 127)
                {
                    return false;
                }
            }

            return true;
        }

        private static object ChangeValue(DatabaseEngine databaseEngine, Type valueType, object value)
        {
            if (databaseEngine == DatabaseEngine.PostgreSQL && valueType.IsEnum)
            {
                return Convert.ChangeType(value, Enum.GetUnderlyingType(valueType)); // 解决 PostgreSQL 不支持枚举类型直接存储的问题
            }

            // 使用 switch pattern 匹配以提高分支预测与可读性
            return value switch
            {
                // PostgreSQL 要求 UTC 时间
                DateTime dt when dt.Kind != DateTimeKind.Utc && databaseEngine == DatabaseEngine.PostgreSQL => dt.ToUniversalTime(),
                DateTimeOffset dto when databaseEngine == DatabaseEngine.PostgreSQL => dto.ToUniversalTime(),
                Guid guid when databaseEngine == DatabaseEngine.MySQL => guid.ToString(),
                Guid guid when databaseEngine == DatabaseEngine.Oracle => guid.ToByteArray(),
                bool b when databaseEngine is DatabaseEngine.SQLite or DatabaseEngine.Oracle => b ? 1 : 0,
                TimeSpan ts when databaseEngine == DatabaseEngine.SQLite => ts.Ticks,
                Version v => v.ToString(),
                JsonPayload jp => jp.ToString(),
                JsonbPayload jbp => jbp.ToString(),
                _ => value,
            };
        }

        /// <summary>
        /// 在 SQL 中形如 <c>?name</c>/<c>@name</c>/<c>:name</c> 的参数占位后追加 PostgreSQL 的类型转换后缀，如 <c>::json</c>/<c>::jsonb</c>。
        /// </summary>
        /// <remarks>
        /// 高性能实现（牺牲可读性换性能）：
        /// 1. <see cref="MemoryExtensions.IndexOfAny{T}(ReadOnlySpan{T}, T, T, T)"/> 跳跃式搜索占位符前缀（SIMD 向量化）。
        /// 2. 标识符边界判定采用 ASCII 位运算，替代 <c>char.IsLetterOrDigit</c> 的 Unicode 分类表查询。
        /// 3. 参数名采用 ASCII 大小写折叠循环，避免 <c>CompareInfo</c> 查询路径。
        /// 4. 采用两趟算法：首趟仅收集命中尾索引（<c>stackalloc int[16]</c> + <see cref="ArrayPool{T}"/> 兜底），
        ///    二趟通过 <see cref="string.Create{TState}(int, TState, SpanAction{char, TState})"/> 单次精确分配结果字符串，
        ///    回调内以 <see cref="ReadOnlySpan{T}.CopyTo(Span{T})"/>（memmove / SIMD）拼接，省掉 <see cref="StringBuilder"/> 及其 <c>ToString</c> 拷贝。
        /// 前置约束：占位符前字符不是字母/数字/@/_；后置约束：参数名后字符不是字母/数字/_。
        /// </remarks>
        internal static string AppendJsonCast(string sql, string name, string cast)
        {
            if (string.IsNullOrEmpty(sql) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(cast))
            {
                return sql;
            }

            int sqlLength = sql.Length;
            int nameLength = name.Length;
            int maxPrefixIndex = sqlLength - nameLength - 1; //? 前缀位置 i 的最大合法值：i+1+nameLength <= sqlLength。

            if (maxPrefixIndex < 0)
            {
                return sql;
            }

            ReadOnlySpan<char> sqlSpan = sql.AsSpan();
            ReadOnlySpan<char> nameSpan = name.AsSpan();

            //? 首趟：命中尾索引收集。典型 SQL 中 JSON 参数极少，栈上 16 槽够用；溢出则迁移到 ArrayPool。
            Span<int> stackHits = stackalloc int[16];
            Span<int> hits = stackHits;
            int[] rentedHits = null;
            int hitCount = 0;

            int i = 0;
            while (i <= maxPrefixIndex)
            {
                int rel = sqlSpan.Slice(i, maxPrefixIndex - i + 1).IndexOfAny('?', '@', ':');
                if (rel < 0)
                {
                    break;
                }

                i += rel;

                //? 前置边界。
                if (i > 0)
                {
                    char prev = sqlSpan[i - 1];
                    if (IsIdentifierChar(prev) || prev == '@')
                    {
                        i++;
                        continue;
                    }
                }

                //? 参数名比较。
                if (!EqualsIgnoreCaseAscii(sqlSpan.Slice(i + 1, nameLength), nameSpan))
                {
                    i++;
                    continue;
                }

                int tailIndex = i + 1 + nameLength;

                //? 后置边界。
                if (tailIndex < sqlLength && IsIdentifierChar(sqlSpan[tailIndex]))
                {
                    i++;
                    continue;
                }

                //? 命中收集，必要时迁移到堆。
                if (hitCount == hits.Length)
                {
                    int newCap = hits.Length * 2;
                    int[] next = ArrayPool<int>.Shared.Rent(newCap);
                    hits.CopyTo(next);
                    if (rentedHits != null)
                    {
                        ArrayPool<int>.Shared.Return(rentedHits);
                    }
                    rentedHits = next;
                    hits = rentedHits;
                }

                hits[hitCount++] = tailIndex;
                i = tailIndex;
            }

            if (hitCount == 0)
            {
                if (rentedHits != null)
                {
                    ArrayPool<int>.Shared.Return(rentedHits);
                }
                return sql;
            }

            int castLength = cast.Length;
            int finalLength = sqlLength + castLength * hitCount;

            //? 二趟：把命中数组固化为数组（SpanAction 的 TState 不能是 ref struct，不能直接传 Span<int>）。
            int[] hitsArray;
            bool returnHitsArray;
            if (rentedHits != null)
            {
                hitsArray = rentedHits;
                returnHitsArray = true;
            }
            else
            {
                hitsArray = ArrayPool<int>.Shared.Rent(hitCount);
                stackHits.Slice(0, hitCount).CopyTo(hitsArray);
                returnHitsArray = true;
            }

            var state = new AppendJsonCastState(sql, cast, hitsArray, hitCount);

            string result = string.Create(finalLength, state, static (dest, st) =>
            {
                ReadOnlySpan<char> src = st.Sql.AsSpan();
                ReadOnlySpan<char> castSpan = st.Cast.AsSpan();
                int[] localHits = st.Hits;
                int count = st.HitCount;
                int castLen = castSpan.Length;

                int copyFrom = 0;
                int destIdx = 0;

                for (int k = 0; k < count; k++)
                {
                    int ti = localHits[k];
                    int segLen = ti - copyFrom;
                    if (segLen > 0)
                    {
                        src.Slice(copyFrom, segLen).CopyTo(dest.Slice(destIdx));
                        destIdx += segLen;
                    }
                    castSpan.CopyTo(dest.Slice(destIdx));
                    destIdx += castLen;
                    copyFrom = ti;
                }

                if (copyFrom < src.Length)
                {
                    src.Slice(copyFrom).CopyTo(dest.Slice(destIdx));
                }
            });

            if (returnHitsArray)
            {
                ArrayPool<int>.Shared.Return(hitsArray);
            }

            return result;
        }

        private readonly struct AppendJsonCastState
        {
            public readonly string Sql;
            public readonly string Cast;
            public readonly int[] Hits;
            public readonly int HitCount;

            public AppendJsonCastState(string sql, string cast, int[] hits, int hitCount)
            {
                Sql = sql;
                Cast = cast;
                Hits = hits;
                HitCount = hitCount;
            }
        }

        /// <summary>
        /// ASCII 标识符字符判定：<c>[A-Za-z0-9_]</c>。位运算分支消除。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsIdentifierChar(char c)
        {
            //? 0x20 = 32（大小写字母 ASCII 差）；97 = 'a'；48 = '0'；95 = '_'。
            //? (c|0x20) 小写化，再减 97；若落在 [0,25] 则是字母。uint 强转使负数变大正数，一次比较完成。
            uint letter = (uint)((c | 0x20) - 97);
            uint digit = (uint)(c - 48);
            //? 25u = 'z' - 'a'；9u = '9' - '0'。常量硬编码以避免 JIT 在每次比较前做算术抗衰减。
            return letter <= 25u || digit <= 9u || c == 95;
        }

        /// <summary>
        /// ASCII 大小写不敏感比较。内循环对 <see cref="ReadOnlySpan{T}"/> 的索引访问，JIT 通常可在简单步长循环中消除边界检查。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool EqualsIgnoreCaseAscii(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
        {
            //? 调用方保证长度一致，省略长度判断。
            int len = a.Length;
            for (int k = 0; k < len; k++)
            {
                char ca = a[k];
                char cb = b[k];

                if (ca == cb)
                {
                    continue;
                }

                int folded = ca | 0x20;
                if (folded != (cb | 0x20))
                {
                    return false;
                }

                //? 折叠后若不在 [a,z] 区间，说明原字符是非字母的特殊符号差异，仍视为不等。
                //? 97 = 'a'；25u = 'z' - 'a'。
                if ((uint)(folded - 97) > 25u)
                {
                    return false;
                }
            }
            return true;
        }
    }
}