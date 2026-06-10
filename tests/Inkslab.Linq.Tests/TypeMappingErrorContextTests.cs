using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Reflection;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// 验证 DB 字段类型与实体属性类型不匹配时，异常消息包含字段名和属性名以便排查。
    /// </summary>
    public class TypeMappingErrorContextTests
    {
        private class OrderEntity
        {
            public int Amount { get; set; }
        }

        /// <summary>
        /// 模拟 SQL Server 行为：列"Amount"实际为 Decimal，
        /// 但代码尝试 GetInt32 时抛出 InvalidCastException。
        /// </summary>
        private sealed class DecimalColumnFakeReader : DbDataReader
        {
            private bool _advanced;

            public override int FieldCount => 1;
            public override bool HasRows => true;
            public override bool IsClosed => false;
            public override int RecordsAffected => -1;
            public override int Depth => 0;

            public override string GetName(int ordinal) => "Amount";
            public override Type GetFieldType(int ordinal) => typeof(decimal);
            public override string GetDataTypeName(int ordinal) => "decimal";
            public override int GetOrdinal(string name) => 0;
            public override bool IsDBNull(int ordinal) => false;
            public override object GetValue(int ordinal) => 123.45m;
            public override decimal GetDecimal(int ordinal) => 123.45m;

            // 模拟 SqlBuffer.get_Int32 在 Decimal 列上抛出 InvalidCastException
            public override int GetInt32(int ordinal) =>
                throw new InvalidCastException("Unable to cast object of type 'System.Decimal' to type 'System.Int32'.");

            public override int GetValues(object[] values) { values[0] = GetValue(0); return 1; }
            public override bool Read() { if (!_advanced) { _advanced = true; return true; } return false; }
            public override bool NextResult() => false;
            public override object this[int ordinal] => GetValue(ordinal);
            public override object this[string name] => GetValue(GetOrdinal(name));

            public override IEnumerator GetEnumerator() => throw new NotImplementedException();
            public override bool GetBoolean(int ordinal) => throw new NotImplementedException();
            public override byte GetByte(int ordinal) => throw new NotImplementedException();
            public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) => throw new NotImplementedException();
            public override char GetChar(int ordinal) => throw new NotImplementedException();
            public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) => throw new NotImplementedException();
            public override DateTime GetDateTime(int ordinal) => throw new NotImplementedException();
            public override double GetDouble(int ordinal) => throw new NotImplementedException();
            public override float GetFloat(int ordinal) => throw new NotImplementedException();
            public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
            public override short GetInt16(int ordinal) => throw new NotImplementedException();
            public override long GetInt64(int ordinal) => throw new NotImplementedException();
            public override string GetString(int ordinal) => throw new NotImplementedException();
        }

        private static (object adapter, Type adapterType) CreateMapAdapter()
        {
            var databaseExecutorType = typeof(DatabaseExecutor);
            var mapAdapterType = databaseExecutorType.GetNestedType("MapAdapter", BindingFlags.NonPublic);

            Assert.NotNull(mapAdapterType);

            var adapter = Activator.CreateInstance(mapAdapterType, typeof(DecimalColumnFakeReader), 100);
            return (adapter, mapAdapterType);
        }

        private static object CreateMapper<T>(object adapter, Type adapterType)
        {
            var createMapMethod = adapterType.GetMethod("CreateMap", BindingFlags.Public | BindingFlags.Instance)
                                            .MakeGenericMethod(typeof(T));
            return createMapMethod.Invoke(adapter, null);
        }

        /// <summary>
        /// 当 DB 字段类型（Decimal）与实体属性类型（Int32）不匹配时，
        /// 抛出的 InvalidCastException 消息应包含实体属性名和数据库字段名。
        /// </summary>
        [Fact]
        public void Map_TypeMismatch_ExceptionMessageContainsPropertyAndColumnName()
        {
            var (adapter, adapterType) = CreateMapAdapter();
            var mapper = CreateMapper<OrderEntity>(adapter, adapterType);

            var mapMethod = mapper.GetType().GetMethod("Map", BindingFlags.Public | BindingFlags.Instance);
            var reader = new DecimalColumnFakeReader();

            var ex = Assert.Throws<TargetInvocationException>(() =>
                mapMethod.Invoke(mapper, new object[] { reader })
            );

            var inner = ex.InnerException;
            Assert.IsType<InvalidCastException>(inner);

            // 消息应包含实体属性名
            Assert.Contains("Amount", inner.Message, StringComparison.OrdinalIgnoreCase);

            // 消息应包含数据库字段类型信息（Decimal）
            Assert.Contains("Decimal", inner.Message, StringComparison.OrdinalIgnoreCase);

            // 消息应包含目标属性类型（Int32）
            Assert.Contains("Int32", inner.Message, StringComparison.OrdinalIgnoreCase);

            // 原始异常应作为 InnerException 保留
            Assert.NotNull(inner.InnerException);
        }
    }
}
