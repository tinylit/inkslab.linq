using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

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
#if !NET_Traditional
                [typeof(JsonArray)] = JsonbDbType,
                [typeof(JsonObject)] = JsonbDbType,
                [typeof(JsonDocument)] = JsonbDbType,
#endif
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

            if (dataType.FullName is "Newtonsoft.Json.Linq.JObject" or "Newtonsoft.Json.Linq.JArray")
            {
                return JsonbDbType;
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
                                command.CommandText = Regex.Replace(command.CommandText, @"(?<![\p{L}\p{N}@_])[?@:](" + name + @")(?![\p{L}\p{N}_])", m => $"{m.Value}::json", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);
                            }
                            else if (dbType.IsJsonbType())
                            {
                                command.CommandText = Regex.Replace(command.CommandText, @"(?<![\p{L}\p{N}@_])[?@:](" + name + @")(?![\p{L}\p{N}_])", m => $"{m.Value}::jsonb", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);
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
                                command.CommandText = Regex.Replace(command.CommandText, @"(?<![\p{L}\p{N}@_])[?@:](" + name + @")(?![\p{L}\p{N}_])", m => $"{m.Value}::json", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);
                            }
                            else if (valueDbType.IsJsonbType())
                            {
                                command.CommandText = Regex.Replace(command.CommandText, @"(?<![\p{L}\p{N}@_])[?@:](" + name + @")(?![\p{L}\p{N}_])", m => $"{m.Value}::jsonb", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);
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

                    if (realValue is string text && text.Length < 4000)
                    {
                        dbParameter.Size = 4000;
                    }

                    break;
            }

            command.Parameters.Add(dbParameter);
        }

        private static object ChangeValue(DatabaseEngine databaseEngine, Type valueType, object value)
        {
            if (databaseEngine == DatabaseEngine.PostgreSQL && valueType.IsEnum)
            {
                return Convert.ChangeType(value, Enum.GetUnderlyingType(valueType)); // 解决 PostgreSQL 不支持枚举类型直接存储的问题
            }

            // 使用 switch pattern 匹配以提高分支预测与可读性
            switch (value)
            {
                case DateTime dt when dt.Kind != DateTimeKind.Utc && databaseEngine == DatabaseEngine.PostgreSQL: // PostgreSQL 要求 UTC 时间
                    return DateTime.SpecifyKind(dt, DateTimeKind.Utc);

                case Guid guid when databaseEngine == DatabaseEngine.MySQL:
                    // MySQL 通常使用 CHAR(36) 或 BINARY(16) 存储 GUID
                    // 返回字符串格式；如果需要二进制请改为 guid.ToByteArray()
                    return guid.ToString();

                case Guid guid when databaseEngine == DatabaseEngine.Oracle:
                    return guid.ToByteArray();

                case bool b when databaseEngine == DatabaseEngine.SQLite:
                    return b ? 1 : 0;

                case TimeSpan ts when databaseEngine == DatabaseEngine.SQLite:
                    return ts.Ticks; // 或使用 ts.ToString()

                case bool b when databaseEngine == DatabaseEngine.Oracle:
                    return b ? 1 : 0;

                case Version v:
                    return v.ToString();

                case JsonObject jo:
                    return jo.ToJsonString();

                case JsonArray ja:
                    return ja.ToJsonString();

                case JsonDocument jd:
                    return jd.RootElement.GetRawText();

                case JsonPayload jp:
                    return jp.ToString();

                case JsonbPayload jbp:
                    return jbp.ToString();
                    
                default:
                    // 对于 Newtonsoft 类型，使用 valueType.FullName 做后备判断以避免直接依赖第三方类型
                    if (valueType?.FullName is "Newtonsoft.Json.Linq.JObject" or "Newtonsoft.Json.Linq.JArray")
                    {
                        return value.ToString();
                    }

                    return value;
            }
        }
    }
}