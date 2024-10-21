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

        /// <summary>
        /// 参数适配。
        /// </summary>
        /// <param name="command">命令。</param>
        /// <param name="name">参数名称。</param>
        /// <param name="value">参数值。</param>
        public static void AddParameterAuto(IDbCommand command, string name, object value)
        {
            var dbParameter = command.CreateParameter();

            dbParameter.Value = value is null ? DBNull.Value : value;
            dbParameter.ParameterName = name;
            dbParameter.Direction = ParameterDirection.Input;

            if (value is null)
            {
            }
            else if (value is Version version)
            {
                dbParameter.Value = version.ToString();
                dbParameter.DbType = DbType.String;
                dbParameter.Size = 4000;
            }
            else if (value is string text)
            {
                dbParameter.DbType = DbType.String;

                if (text.Length < 4000)
                {
                    dbParameter.Size = 4000;
                }
            }
            else
            {
                dbParameter.DbType = For(value.GetType());
            }

            command.Parameters.Add(dbParameter);
        }

        /// <summary>
        /// 参数适配。
        /// </summary>
        /// <param name="command">命令。</param>
        /// <param name="name">参数名称。</param>
        /// <param name="value">参数值。</param>
        /// <param name="dbType">指定类型。</param>
        /// <param name="direction">参数方向。</param>
        /// <param name="size">大小。</param>
        /// <param name="precision">精度。</param>
        /// <param name="scale">规模。</param>
        public static void AddParameterAuto(IDbCommand command, string name, object value, DbType? dbType = null, ParameterDirection? direction = null, int? size = null, byte? precision = null, byte? scale = null)
        {
            var dbParameter = command.CreateParameter();

            switch (value)
            {
                case IDbDataParameter dbDataParameter when dbParameter is IDbDataParameter parameter:

                    parameter.Value = dbDataParameter.Value;
                    parameter.ParameterName = name;
                    parameter.Direction = dbDataParameter.Direction;
                    parameter.DbType = dbType ?? dbDataParameter.DbType;
                    parameter.SourceColumn = dbDataParameter.SourceColumn;
                    parameter.SourceVersion = dbDataParameter.SourceVersion;

                    if (dbParameter is DbParameter myParameter)
                    {
                        myParameter.IsNullable = dbDataParameter.IsNullable;
                    }

                    parameter.Scale = dbDataParameter.Scale;
                    parameter.Size = dbDataParameter.Size;
                    parameter.Precision = dbDataParameter.Precision;
                    break;
                case IDataParameter dataParameter:
                    dbParameter.Value = dataParameter.Value;
                    dbParameter.ParameterName = name;
                    dbParameter.Direction = dataParameter.Direction;
                    dbParameter.DbType = dbType ?? dataParameter.DbType;
                    dbParameter.SourceColumn = dataParameter.SourceColumn;
                    dbParameter.SourceVersion = dataParameter.SourceVersion;
                    break;
                default:
                    dbParameter.Value = value ?? DBNull.Value;
                    dbParameter.ParameterName = name;
                    dbParameter.Direction = direction ?? ParameterDirection.Input;

                    if (dbType.HasValue)
                    {
                        dbParameter.DbType = dbType.Value;
                    }
                    else if (value is null)
                    {
                    }
                    else
                    {
                        dbParameter.DbType = For(value.GetType());
                    }

                    break;
            }

            if (size.HasValue)
            {
                dbParameter.Size = size.Value;
            }

            if (precision.HasValue)
            {
                dbParameter.Precision = precision.Value;
            }

            if (scale.HasValue)
            {
                dbParameter.Scale = scale.Value;
            }

            if (dbParameter.Value is Version version)
            {
                dbParameter.Value = version.ToString();
                dbParameter.DbType = DbType.String;
            }

            if (!size.HasValue
                && (dbParameter.Size <= 0 || dbParameter.Size > 4000)
                && dbParameter.DbType == DbType.String
                && dbParameter.Value is string text
                && text.Length < 4000)
            {
                dbParameter.Size = 4000;
            }

            command.Parameters.Add(dbParameter);
        }
    }
}