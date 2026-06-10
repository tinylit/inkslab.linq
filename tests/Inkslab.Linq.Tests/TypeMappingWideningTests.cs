using System;
using System.Collections;
using System.Data.Common;
using System.Reflection;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// ORM 读取映射（DB 字段 → 实体属性）的“无损小转大”与“任意类型转 string”行为测试。
    ///
    /// 核心约定：
    /// 1. 任意数据库字段类型 → string 属性，均通过 ToString() 支持；
    /// 2. 浮点型（float/double）→ decimal 支持；
    /// 3. 在不丢失精度的前提下，支持小类型 → 大类型（如 int→long、uint→ulong、uint→long、int→decimal、float→double）。
    ///
    /// 测试不需要真实数据库连接：通过反射构造内部 MapAdapter，并以可配置的伪 DbDataReader 驱动单列标量映射。
    /// </summary>
    public class TypeMappingWideningTests
    {
        /// <summary>
        /// 单列标量伪读取器：GetFieldType 返回配置的字段类型，各 GetXxx 按各自类型对装箱值拆箱
        /// （与真实 ADO.NET 行为一致——类型不匹配的 getter 会抛 InvalidCastException）。
        /// </summary>
        private sealed class ScalarReader : DbDataReader
        {
            private readonly Type _fieldType;
            private readonly object _value;
            private bool _advanced;

            public ScalarReader(Type fieldType, object value)
            {
                _fieldType = fieldType;
                _value = value;
            }

            public override int FieldCount => 1;
            public override bool HasRows => true;
            public override bool IsClosed => false;
            public override int RecordsAffected => -1;
            public override int Depth => 0;

            public override string GetName(int ordinal) => "col";
            public override int GetOrdinal(string name) => 0;
            public override Type GetFieldType(int ordinal) => _fieldType;
            public override string GetDataTypeName(int ordinal) => _fieldType.Name;
            public override bool IsDBNull(int ordinal) => _value is null;
            public override object GetValue(int ordinal) => _value;
            public override int GetValues(object[] values) { values[0] = _value; return 1; }

            public override bool Read() { if (!_advanced) { _advanced = true; return true; } return false; }
            public override bool NextResult() => false;
            public override object this[int ordinal] => _value;
            public override object this[string name] => _value;
            public override IEnumerator GetEnumerator() => throw new NotImplementedException();

            // —— 标准 DbDataReader getter（按精确类型拆箱）——
            public override bool GetBoolean(int ordinal) => (bool)_value;
            public override byte GetByte(int ordinal) => (byte)_value;
            public override char GetChar(int ordinal) => (char)_value;
            public override DateTime GetDateTime(int ordinal) => (DateTime)_value;
            public override decimal GetDecimal(int ordinal) => (decimal)_value;
            public override double GetDouble(int ordinal) => (double)_value;
            public override float GetFloat(int ordinal) => (float)_value;
            public override Guid GetGuid(int ordinal) => (Guid)_value;
            public override short GetInt16(int ordinal) => (short)_value;
            public override int GetInt32(int ordinal) => (int)_value;
            public override long GetInt64(int ordinal) => (long)_value;
            public override string GetString(int ordinal) => (string)_value;

            // —— 提供商扩展 getter（无符号/有符号补充），用于被 _typeMap 反射登记 ——
            public sbyte GetSByte(int ordinal) => (sbyte)_value;
            public ushort GetUInt16(int ordinal) => (ushort)_value;
            public uint GetUInt32(int ordinal) => (uint)_value;
            public ulong GetUInt64(int ordinal) => (ulong)_value;

            public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) => throw new NotImplementedException();
            public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) => throw new NotImplementedException();
        }

        private static T MapScalar<T>(Type fieldType, object value)
        {
            var databaseExecutorType = typeof(DatabaseExecutor);
            var mapAdapterType = databaseExecutorType.GetNestedType("MapAdapter", BindingFlags.NonPublic);

            Assert.NotNull(mapAdapterType);

            var adapter = Activator.CreateInstance(mapAdapterType, typeof(ScalarReader), 100);

            var createMapMethod = mapAdapterType
                .GetMethod("CreateMap", BindingFlags.Public | BindingFlags.Instance)
                .MakeGenericMethod(typeof(T));

            var mapper = createMapMethod.Invoke(adapter, null);

            var mapMethod = mapper.GetType().GetMethod("Map", BindingFlags.Public | BindingFlags.Instance);

            var reader = new ScalarReader(fieldType, value);

            return (T)mapMethod.Invoke(mapper, new object[] { reader });
        }

        // ─────────────────────────────────────────────────────────────────────
        // 1. 无损整数小转大
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Int32Field_ToLongProperty_Widens()
        {
            Assert.Equal(42L, MapScalar<long>(typeof(int), 42));
        }

        [Fact]
        public void UInt32Field_ToULongProperty_Widens()
        {
            Assert.Equal(42UL, MapScalar<ulong>(typeof(uint), 42u));
        }

        [Fact]
        public void UInt32Field_ToLongProperty_Widens()
        {
            Assert.Equal(42L, MapScalar<long>(typeof(uint), 42u));
        }

        [Fact]
        public void Int16Field_ToIntProperty_Widens()
        {
            Assert.Equal(7, MapScalar<int>(typeof(short), (short)7));
        }

        // ─────────────────────────────────────────────────────────────────────
        // 2. 浮点型 → decimal / 浮点小转大
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void SingleField_ToDecimalProperty_Widens()
        {
            Assert.Equal(1.5m, MapScalar<decimal>(typeof(float), 1.5f));
        }

        [Fact]
        public void DoubleField_ToDecimalProperty_Widens()
        {
            Assert.Equal(2.5m, MapScalar<decimal>(typeof(double), 2.5d));
        }

        [Fact]
        public void SingleField_ToDoubleProperty_Widens()
        {
            Assert.Equal(1.5d, MapScalar<double>(typeof(float), 1.5f));
        }

        [Fact]
        public void Int32Field_ToDecimalProperty_Widens()
        {
            Assert.Equal(100m, MapScalar<decimal>(typeof(int), 100));
        }

        // decimal 的取值范围被 double/float 完整覆盖，应作为安全小转大直接转换，
        // 不得进入会计算 (decimal)double.MaxValue 的区间校验（否则 OverflowException）。
        [Fact]
        public void DecimalField_ToDoubleProperty_Widens()
        {
            Assert.Equal(100.5d, MapScalar<double>(typeof(decimal), 100.5m));
        }

        [Fact]
        public void DecimalField_ToFloatProperty_Widens()
        {
            Assert.Equal(100.5f, MapScalar<float>(typeof(decimal), 100.5m));
        }

        // ─────────────────────────────────────────────────────────────────────
        // 3. 任意类型 → string
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void Int32Field_ToStringProperty_UsesToString()
        {
            Assert.Equal("42", MapScalar<string>(typeof(int), 42));
        }

        [Fact]
        public void Int64Field_ToStringProperty_UsesToString()
        {
            Assert.Equal("9999999999", MapScalar<string>(typeof(long), 9999999999L));
        }

        [Fact]
        public void DecimalField_ToStringProperty_UsesToString()
        {
            Assert.Equal(123.45m.ToString(), MapScalar<string>(typeof(decimal), 123.45m));
        }

        [Fact]
        public void BoolField_ToStringProperty_UsesToString()
        {
            Assert.Equal(true.ToString(), MapScalar<string>(typeof(bool), true));
        }

        [Fact]
        public void GuidField_ToStringProperty_UsesToString()
        {
            var guid = Guid.NewGuid();

            Assert.Equal(guid.ToString(), MapScalar<string>(typeof(Guid), guid));
        }

        [Fact]
        public void DateTimeField_ToStringProperty_UsesToString()
        {
            var now = new DateTime(2026, 6, 5, 12, 30, 0);

            Assert.Equal(now.ToString(), MapScalar<string>(typeof(DateTime), now));
        }

        // ─────────────────────────────────────────────────────────────────────
        // 4. 回归：精确匹配仍直接读取
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void LongField_ToLongProperty_ExactMatch()
        {
            Assert.Equal(123L, MapScalar<long>(typeof(long), 123L));
        }

        [Fact]
        public void StringField_ToStringProperty_ExactMatch()
        {
            Assert.Equal("hello", MapScalar<string>(typeof(string), "hello"));
        }

        // ─────────────────────────────────────────────────────────────────────
        // 5. 枚举：支持“数字”与“字符串（名称/数字文本）”双向来源
        // ─────────────────────────────────────────────────────────────────────

        private enum Color
        {
            Red = 1,
            Green = 2,
            Blue = 3
        }

        private enum LongColor : long
        {
            Cyan = 10,
            Magenta = 20
        }

        [Fact]
        public void Int32Field_ToEnumProperty_ByNumber()
        {
            Assert.Equal(Color.Green, MapScalar<Color>(typeof(int), 2));
        }

        [Fact]
        public void ByteField_ToEnumProperty_WidensUnderlying()
        {
            // 数据库字段为 byte，枚举底层为 int：底层无损小转大后再转枚举。
            Assert.Equal(Color.Blue, MapScalar<Color>(typeof(byte), (byte)3));
        }

        [Fact]
        public void Int64Field_ToLongEnumProperty_ByNumber()
        {
            Assert.Equal(LongColor.Magenta, MapScalar<LongColor>(typeof(long), 20L));
        }

        [Fact]
        public void StringField_Name_ToEnumProperty()
        {
            Assert.Equal(Color.Green, MapScalar<Color>(typeof(string), "Green"));
        }

        [Fact]
        public void StringField_Name_CaseInsensitive_ToEnumProperty()
        {
            Assert.Equal(Color.Red, MapScalar<Color>(typeof(string), "red"));
        }

        [Fact]
        public void StringField_NumericText_ToEnumProperty()
        {
            Assert.Equal(Color.Blue, MapScalar<Color>(typeof(string), "3"));
        }

        [Fact]
        public void StringField_Name_ToLongEnumProperty()
        {
            Assert.Equal(LongColor.Cyan, MapScalar<LongColor>(typeof(string), "Cyan"));
        }

        [Fact]
        public void NullableEnum_FromNumber()
        {
            Assert.Equal(Color.Red, MapScalar<Color?>(typeof(int), 1));
        }

        [Fact]
        public void NullableEnum_FromString()
        {
            Assert.Equal(Color.Blue, MapScalar<Color?>(typeof(string), "Blue"));
        }

        // ─────────────────────────────────────────────────────────────────────
        // 6. 守卫：有损/不支持映射仍抛异常
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void StringField_ToIntProperty_Throws()
        {
            var ex = Assert.Throws<TargetInvocationException>(() => MapScalar<int>(typeof(string), "abc"));

            Assert.NotNull(ex.InnerException);
        }

        /// <summary>
        /// 「大转小」超界时，ToSolveByTransform 抛出的异常消息应与 SwitchCaseAssign 列映射格式一致：
        /// “映射失败，列 {列名}({字段类型}) → 属性({属性类型}) …”。
        /// </summary>
        [Fact]
        public void OutOfRangeMapping_UsesUnifiedErrorMessageFormat()
        {
            // long 值 5_000_000_000 超出 int 范围 → 区间校验失败。
            var ex = Assert.Throws<TargetInvocationException>(() => MapScalar<int>(typeof(long), 5_000_000_000L));

            var inner = ex.InnerException;

            Assert.NotNull(inner);
            Assert.StartsWith("映射失败，列 ", inner.Message);
            Assert.Contains("col(Int64)", inner.Message);
            Assert.Contains("属性(Int32)", inner.Message);
        }
    }
}
