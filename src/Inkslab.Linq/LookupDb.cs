using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Inkslab.Linq
{
    /// <summary>
    /// 查找数据类型。
    /// </summary>
    public static class LookupDb
    {
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

        private static string Clean(string name)
        {
            return name[0] switch
            {
                '@' or ':' or '?' => name[1..],
                _ => name,
            };
        }

        /// <summary>
        /// 参数适配。
        /// </summary>
        /// <param name="command">命令。</param>
        /// <param name="name">参数名称。</param>
        /// <param name="value">参数值。</param>
        public static void AddParameterAuto(IDbCommand command, string name, object value)
        {
            var dbParameter = command.CreateParameter();

            switch (value)
            {
                case null or DBNull:
                    dbParameter.ParameterName = Clean(name);
                    dbParameter.Value = DBNull.Value;

                    if (dbParameter is DbParameter myParameter)
                    {
                        myParameter.IsNullable = true;
                    }
                    break;
                case DynamicParameter dynamicParameter:

                    dbParameter.Value = dynamicParameter.Value is null ? DBNull.Value : ChangeValue(command, dynamicParameter.Value);

                    dbParameter.ParameterName = Clean(name);

                    dbParameter.Direction = dynamicParameter.Direction;

                    if (dynamicParameter.DbType.HasValue)
                    {
                        dbParameter.DbType = dynamicParameter.DbType.Value;
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
                    dbParameter.Value = ChangeValue(command, value);
                    dbParameter.ParameterName = Clean(name);
                    dbParameter.Direction = ParameterDirection.Input;

                    if (value is string text)
                    {
                        dbParameter.DbType = DbType.String;

                        if (text.Length < 4000)
                        {
                            dbParameter.Size = 4000;
                        }
                    }
                    else
                    {
                        dbParameter.DbType = For(dbParameter.Value.GetType());
                    }

                    break;
            }

            command.Parameters.Add(dbParameter);
        }

        private static object ChangeValue(IDbCommand command, object value)
        {
            var commandType = command.GetType();
            var commandTypeName = commandType.FullName ?? string.Empty;
            var assemblyName = commandType.Assembly.FullName ?? string.Empty;

            // PostgreSQL: DateTime 需要指定 UTC Kind
            if (value is DateTime dateTime && dateTime.Kind != DateTimeKind.Utc)
            {
                if (commandTypeName.StartsWith("Npgsql.") && assemblyName.StartsWith("Npgsql,"))
                {
                    return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                }
            }

            // MySQL: GUID 需要转换为字符串或字节数组
            if (value is Guid guid)
            {
                if (commandTypeName.StartsWith("MySql.") && (assemblyName.StartsWith("MySql.Data,") || assemblyName.StartsWith("MySqlConnector,")))
                {
                    // MySQL 通常使用 CHAR(36) 或 BINARY(16) 存储 GUID
                    // 这里转换为字符串格式,如需二进制格式可改为 guid.ToByteArray()
                    return guid.ToString();
                }
            }

            // Oracle: GUID 需要转换为字节数组
            if (value is Guid oracleGuid)
            {
                if (commandTypeName.StartsWith("Oracle.") && (assemblyName.StartsWith("Oracle.DataAccess,") || assemblyName.StartsWith("Oracle.ManagedDataAccess,")))
                {
                    return oracleGuid.ToByteArray();
                }
            }

            // SQLite: Boolean 需要转换为整数
            if (value is bool boolValue)
            {
                if (commandTypeName.StartsWith("System.Data.SQLite.") || commandTypeName.StartsWith("Microsoft.Data.Sqlite."))
                {
                    return boolValue ? 1 : 0;
                }
            }

            // SQLite: TimeSpan 需要转换为字符串或 Ticks
            if (value is TimeSpan timeSpan)
            {
                if (commandTypeName.StartsWith("System.Data.SQLite.") || commandTypeName.StartsWith("Microsoft.Data.Sqlite."))
                {
                    return timeSpan.Ticks; // 或使用 timeSpan.ToString()
                }
            }

            // Oracle: Boolean 需要转换为数字
            if (value is bool oracleBool)
            {
                if (commandTypeName.StartsWith("Oracle.") && (assemblyName.StartsWith("Oracle.DataAccess,") || assemblyName.StartsWith("Oracle.ManagedDataAccess,")))
                {
                    return oracleBool ? 1 : 0;
                }
            }

            if (value is Version version)
            {
                return version.ToString();
            }

            return value;
        }
    }
}