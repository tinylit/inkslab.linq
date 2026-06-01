using System;
using System.Data;
using Inkslab.Linq.Annotations;
using Inkslab.Linq.Enums;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// 参数绑定与类型映射层边界测试。
    /// 覆盖 LookupDb.For、DynamicParameter、JsonPayload/JsonbPayload、
    /// Field struct、VersionAttribute 推断、Annotation 校验等边界场景。
    /// 所有测试均不需要真实数据库连接。
    /// </summary>
    public class TypeMappingBoundaryTests
    {
        // ─────────────────────────────────────────────────────────────────────
        // 1. LookupDb.For — 基础类型覆盖完整性
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 验证所有在 _typeMap 中直接注册的 .NET 基础类型均能返回正确的 DbType（非 DbType.Object fallback）。
        /// </summary>
        [Theory]
        [InlineData(typeof(byte),           DbType.Byte)]
        [InlineData(typeof(sbyte),          DbType.SByte)]
        [InlineData(typeof(short),          DbType.Int16)]
        [InlineData(typeof(ushort),         DbType.UInt16)]
        [InlineData(typeof(int),            DbType.Int32)]
        [InlineData(typeof(uint),           DbType.UInt32)]
        [InlineData(typeof(long),           DbType.Int64)]
        [InlineData(typeof(ulong),          DbType.UInt64)]
        [InlineData(typeof(float),          DbType.Single)]
        [InlineData(typeof(double),         DbType.Double)]
        [InlineData(typeof(decimal),        DbType.Decimal)]
        [InlineData(typeof(bool),           DbType.Boolean)]
        [InlineData(typeof(string),         DbType.String)]
        [InlineData(typeof(char),           DbType.StringFixedLength)]
        [InlineData(typeof(Guid),           DbType.Guid)]
        [InlineData(typeof(DateTime),       DbType.DateTime)]
        [InlineData(typeof(DateTimeOffset), DbType.DateTimeOffset)]
        [InlineData(typeof(TimeSpan),       DbType.Time)]
        [InlineData(typeof(byte[]),         DbType.Binary)]
        public void LookupDbFor_PrimitiveType_ReturnsExpectedDbType(Type clrType, DbType expectedDbType)
        {
            var result = LookupDb.For(clrType);

            Assert.Equal(expectedDbType, result);
        }

        /// <summary>
        /// LookupDb.For(typeof(JsonPayload)) 应返回自定义的 JsonDbType 常量（负值）。
        /// </summary>
        [Fact]
        public void LookupDbFor_JsonPayload_ReturnsJsonDbType()
        {
            var result = LookupDb.For(typeof(JsonPayload));

            Assert.Equal(LookupDb.JsonDbType, result);
        }

        /// <summary>
        /// LookupDb.For(typeof(JsonbPayload)) 应返回自定义的 JsonbDbType 常量（负值）。
        /// </summary>
        [Fact]
        public void LookupDbFor_JsonbPayload_ReturnsJsonbDbType()
        {
            var result = LookupDb.For(typeof(JsonbPayload));

            Assert.Equal(LookupDb.JsonbDbType, result);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 2. LookupDb.For — 可空（Nullable<T>）类型解包
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Nullable 包装的基础类型应解包后返回与非 Nullable 版本相同的 DbType。
        /// </summary>
        [Theory]
        [InlineData(typeof(int?),            DbType.Int32)]
        [InlineData(typeof(long?),           DbType.Int64)]
        [InlineData(typeof(bool?),           DbType.Boolean)]
        [InlineData(typeof(decimal?),        DbType.Decimal)]
        [InlineData(typeof(Guid?),           DbType.Guid)]
        [InlineData(typeof(DateTime?),       DbType.DateTime)]
        [InlineData(typeof(DateTimeOffset?), DbType.DateTimeOffset)]
        [InlineData(typeof(TimeSpan?),       DbType.Time)]
        [InlineData(typeof(double?),         DbType.Double)]
        [InlineData(typeof(float?),          DbType.Single)]
        [InlineData(typeof(byte?),           DbType.Byte)]
        [InlineData(typeof(char?),           DbType.StringFixedLength)]
        public void LookupDbFor_NullableType_ResolvesUnderlyingDbType(Type nullableType, DbType expectedDbType)
        {
            var result = LookupDb.For(nullableType);

            Assert.Equal(expectedDbType, result);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 3. LookupDb.For — 枚举类型映射
        // ─────────────────────────────────────────────────────────────────────

        private enum ByteBackedEnum : byte { A = 1 }
        private enum IntBackedEnum : int { A = 1 }
        private enum LongBackedEnum : long { A = 1 }

        /// <summary>
        /// byte 底层的枚举应映射为 DbType.Byte。
        /// </summary>
        [Fact]
        public void LookupDbFor_ByteEnum_ReturnsDbTypeByte()
        {
            var result = LookupDb.For(typeof(ByteBackedEnum));

            Assert.Equal(DbType.Byte, result);
        }

        /// <summary>
        /// int 底层的枚举应映射为 DbType.Int32。
        /// </summary>
        [Fact]
        public void LookupDbFor_IntEnum_ReturnsDbTypeInt32()
        {
            var result = LookupDb.For(typeof(IntBackedEnum));

            Assert.Equal(DbType.Int32, result);
        }

        /// <summary>
        /// long 底层的枚举应映射为 DbType.Int64。
        /// </summary>
        [Fact]
        public void LookupDbFor_LongEnum_ReturnsDbTypeInt64()
        {
            var result = LookupDb.For(typeof(LongBackedEnum));

            Assert.Equal(DbType.Int64, result);
        }

        /// <summary>
        /// Nullable 枚举应先解包再按底层类型映射。
        /// </summary>
        [Fact]
        public void LookupDbFor_NullableIntEnum_ReturnsDbTypeInt32()
        {
            var result = LookupDb.For(typeof(IntBackedEnum?));

            Assert.Equal(DbType.Int32, result);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 4. LookupDb.For — 未知类型的 fallback 行为
        // ─────────────────────────────────────────────────────────────────────

        private class SomeUnknownClass { }

        /// <summary>
        /// 未在 _typeMap 中注册的类类型应 fallback 到 DbType.Object，而不是抛出异常。
        /// </summary>
        [Fact]
        public void LookupDbFor_UnknownReferenceType_FallsBackToDbTypeObject()
        {
            var result = LookupDb.For(typeof(SomeUnknownClass));

            // 当前实现对未知类型直接 return DbType.Object；测试确认这一行为而非抛异常。
            Assert.Equal(DbType.Object, result);
        }

        /// <summary>
        /// System.Data.Linq.Binary 的 FullName 特殊分支——以类似 FullName 字符串通过反射验证
        /// 代码路径存在、且对不在 map 中的其它类型依然 fallback。
        /// </summary>
        [Fact]
        public void LookupDbFor_ObjectType_ReturnsDbTypeObject()
        {
            var result = LookupDb.For(typeof(object));

            // typeof(object) 在 _typeMap 中有直接注册，应返回 DbType.Object。
            Assert.Equal(DbType.Object, result);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 5. DbTypeExtensions — IsJsonType / IsJsonbType 扩展方法
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// JsonDbType 常量调用 IsJsonType() 应返回 true。
        /// </summary>
        [Fact]
        public void IsJsonType_JsonDbType_ReturnsTrue()
        {
            Assert.True(LookupDb.JsonDbType.IsJsonType());
        }

        /// <summary>
        /// JsonbDbType 调用 IsJsonType() 应返回 false（不是 JSON，是 JSONB）。
        /// </summary>
        [Fact]
        public void IsJsonType_JsonbDbType_ReturnsFalse()
        {
            Assert.False(LookupDb.JsonbDbType.IsJsonType());
        }

        /// <summary>
        /// JsonbDbType 常量调用 IsJsonbType() 应返回 true。
        /// </summary>
        [Fact]
        public void IsJsonbType_JsonbDbType_ReturnsTrue()
        {
            Assert.True(LookupDb.JsonbDbType.IsJsonbType());
        }

        /// <summary>
        /// JsonDbType 调用 IsJsonbType() 应返回 false。
        /// </summary>
        [Fact]
        public void IsJsonbType_JsonDbType_ReturnsFalse()
        {
            Assert.False(LookupDb.JsonDbType.IsJsonbType());
        }

        /// <summary>
        /// 标准 DbType.String 的 IsJsonType() 和 IsJsonbType() 均应为 false。
        /// </summary>
        [Fact]
        public void IsJsonType_StandardDbType_ReturnsFalse()
        {
            Assert.False(DbType.String.IsJsonType());
            Assert.False(DbType.String.IsJsonbType());
        }

        // ─────────────────────────────────────────────────────────────────────
        // 6. LookupDb 常量值语义
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 三个自定义 DbType 常量均为负数，与 System.Data.DbType 枚举的合法值不重叠。
        /// </summary>
        [Fact]
        public void LookupDbCustomConstants_AreNegative()
        {
            Assert.True((int)LookupDb.EnumerableDbType < 0,  "EnumerableDbType 应为负数");
            Assert.True((int)LookupDb.JsonDbType       < 0,  "JsonDbType 应为负数");
            Assert.True((int)LookupDb.JsonbDbType      < 0,  "JsonbDbType 应为负数");
        }

        /// <summary>
        /// 三个自定义 DbType 常量值互不相同，避免误判。
        /// </summary>
        [Fact]
        public void LookupDbCustomConstants_AreDistinct()
        {
            Assert.NotEqual(LookupDb.EnumerableDbType, LookupDb.JsonDbType);
            Assert.NotEqual(LookupDb.EnumerableDbType, LookupDb.JsonbDbType);
            Assert.NotEqual(LookupDb.JsonDbType,       LookupDb.JsonbDbType);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 7. DynamicParameter — null 值时的默认行为
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// DynamicParameter.Value 为 null 时，DbType 属性仍应可以保持 null（未强制推断），
        /// 调用方自行决定是否设置 DbType。这验证 DynamicParameter 本身不在构造时抛 NullReferenceException。
        /// </summary>
        [Fact]
        public void DynamicParameter_NullValue_DoesNotThrow_AndDbTypeRemainsNull()
        {
            var param = new DynamicParameter { Value = null };

            Assert.Null(param.Value);
            Assert.Null(param.DbType); // DbType? 默认 null，不会被 null Value 强制改写
        }

        /// <summary>
        /// DynamicParameter 所有属性均为 null/default 时，对象可以正常构造，不抛任何异常。
        /// </summary>
        [Fact]
        public void DynamicParameter_AllDefaultValues_NoException()
        {
            var exception = Record.Exception(() => new DynamicParameter());

            Assert.Null(exception);
        }

        /// <summary>
        /// DynamicParameter 设置负数 Size（-1 代表 MAX）时，Size 属性原样返回，不做截断。
        /// </summary>
        [Fact]
        public void DynamicParameter_NegativeSize_IsAccepted()
        {
            var param = new DynamicParameter { Value = "test", Size = -1 };

            Assert.Equal(-1, param.Size);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 8. JsonPayload — null / 空字符串 / JSON null 字符串
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// JsonPayload(null).ToString() 返回 null，而非字符串 "null"。
        /// 这区分了"字段缺失/SQL NULL"与"JSON null 值"。
        /// </summary>
        [Fact]
        public void JsonPayload_NullString_ToStringReturnsNull_NotJsonNullLiteral()
        {
            var payload = new JsonPayload(null);

            Assert.Null(payload.ToString());
            Assert.NotEqual("null", payload.ToString());
        }

        /// <summary>
        /// JsonPayload("null").ToString() 返回字符串 "null"（JSON null 字面量），
        /// 与 C# null 语义不同。
        /// </summary>
        [Fact]
        public void JsonPayload_JsonNullLiteral_ToStringReturnsNullString()
        {
            var payload = new JsonPayload("null");

            Assert.Equal("null", payload.ToString());
        }

        /// <summary>
        /// JsonPayload("[]").ToString() 返回 "[]"（空数组 JSON），不丢失内容。
        /// </summary>
        [Fact]
        public void JsonPayload_EmptyArray_ToStringReturnsEmptyArray()
        {
            var payload = new JsonPayload("[]");

            Assert.Equal("[]", payload.ToString());
        }

        /// <summary>
        /// JsonPayload("{}").ToString() 返回 "{}"（空对象 JSON），不丢失内容。
        /// </summary>
        [Fact]
        public void JsonPayload_EmptyObject_ToStringReturnsEmptyObject()
        {
            var payload = new JsonPayload("{}");

            Assert.Equal("{}", payload.ToString());
        }

        // ─────────────────────────────────────────────────────────────────────
        // 9. JsonbPayload — null / 空字符串 / JSON null 字符串（与 JsonPayload 对称）
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// JsonbPayload(null).ToString() 返回 null，而非字符串 "null"。
        /// </summary>
        [Fact]
        public void JsonbPayload_NullString_ToStringReturnsNull_NotJsonNullLiteral()
        {
            var payload = new JsonbPayload(null);

            Assert.Null(payload.ToString());
            Assert.NotEqual("null", payload.ToString());
        }

        /// <summary>
        /// JsonbPayload("null").ToString() 返回字符串 "null"（JSON null 字面量）。
        /// </summary>
        [Fact]
        public void JsonbPayload_JsonNullLiteral_ToStringReturnsNullString()
        {
            var payload = new JsonbPayload("null");

            Assert.Equal("null", payload.ToString());
        }

        /// <summary>
        /// JsonbPayload("[]").ToString() 返回 "[]"，空集合不丢失。
        /// </summary>
        [Fact]
        public void JsonbPayload_EmptyArray_ToStringReturnsEmptyArray()
        {
            var payload = new JsonbPayload("[]");

            Assert.Equal("[]", payload.ToString());
        }

        // ─────────────────────────────────────────────────────────────────────
        // 10. Field struct — 边界命名与不变性
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Field 允许接受空字符串名称（结构体本身无校验，调用方负责）。
        /// 这是"已知可接受行为"，不期望抛异常。
        /// </summary>
        [Fact]
        public void Field_EmptyName_IsPermittedByStruct()
        {
            var field = new Field(string.Empty, false, false);

            Assert.Equal(string.Empty, field.Name);
        }

        /// <summary>
        /// Field 允许接受 null 名称（结构体本身无校验）。
        /// </summary>
        [Fact]
        public void Field_NullName_IsPermittedByStruct()
        {
            var field = new Field(null, false, false);

            Assert.Null(field.Name);
        }

        /// <summary>
        /// Field 同时设置主键 + 只读 + 版本时，所有属性均独立持久化。
        /// </summary>
        [Fact]
        public void Field_AllFlagsSet_AreIndependentlyPersisted()
        {
            var field = new Field("ver_col", primaryKey: true, readOnly: true, version: VersionKind.Ticks);

            Assert.Equal("ver_col", field.Name);
            Assert.True(field.PrimaryKey);
            Assert.True(field.ReadOnly);
            Assert.Equal(VersionKind.Ticks, field.Version);
        }

        /// <summary>
        /// 验证全部 VersionKind 枚举成员均可作为 Field 的版本参数，枚举集合没有意外遗漏。
        /// </summary>
        [Theory]
        [InlineData(VersionKind.None)]
        [InlineData(VersionKind.Increment)]
        [InlineData(VersionKind.Ticks)]
        [InlineData(VersionKind.Timestamp)]
        [InlineData(VersionKind.Now)]
        public void Field_AllVersionKindValues_AreStoredCorrectly(VersionKind kind)
        {
            var field = new Field("col", false, false, kind);

            Assert.Equal(kind, field.Version);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 11. Annotation 校验 — FieldAttribute 边界
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// [Field("")] 应在构造时抛出 ArgumentException，空字符串不合法。
        /// </summary>
        [Fact]
        public void FieldAttribute_EmptyName_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => new FieldAttribute(string.Empty));

            Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// [Field(null)] 应在构造时抛出 ArgumentException，null 不合法。
        /// </summary>
        [Fact]
        public void FieldAttribute_NullName_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => new FieldAttribute(null));

            Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// [Field("  ")] 纯空白字符在构造时抛出 ArgumentException（实现使用 IsNullOrWhiteSpace）。
        /// </summary>
        [Fact]
        public void FieldAttribute_WhitespaceOnlyName_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => new FieldAttribute("   "));
            Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// [Field(" col")] 名称以空格开头应抛 ArgumentException，避免拼接 SQL 时
        /// 产生形如 `` ` col` `` 的非法标识符。
        /// </summary>
        [Theory]
        [InlineData(" col")]
        [InlineData("\tcol")]
        [InlineData("\ncol")]
        public void FieldAttribute_NameStartsWithWhitespace_ThrowsArgumentException(string name)
        {
            var ex = Assert.Throws<ArgumentException>(() => new FieldAttribute(name));
            Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// [Field("col ")] 名称以空格结尾应抛 ArgumentException，避免拼接 SQL 时
        /// 产生形如 `` `col ` `` 的非法标识符。
        /// </summary>
        [Theory]
        [InlineData("col ")]
        [InlineData("col\t")]
        [InlineData("col\n")]
        public void FieldAttribute_NameEndsWithWhitespace_ThrowsArgumentException(string name)
        {
            var ex = Assert.Throws<ArgumentException>(() => new FieldAttribute(name));
            Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// [Field("user_name")] 内部含下划线但首尾无空白，应正常构造。
        /// 这是控制用例，防止上面的校验误伤合法标识符。
        /// </summary>
        [Fact]
        public void FieldAttribute_ValidName_DoesNotThrow()
        {
            var attr = new FieldAttribute("user_name");
            Assert.Equal("user_name", attr.Name);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 12. Annotation 校验 — TableAttribute 边界
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// [Table("")] 应在构造时抛出 ArgumentException。
        /// </summary>
        [Fact]
        public void TableAttribute_EmptyName_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => new TableAttribute(string.Empty));

            Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// [Table(null)] 应在构造时抛出 ArgumentException。
        /// </summary>
        [Fact]
        public void TableAttribute_NullName_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => new TableAttribute(null));

            Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// [Table("  ")] 纯空白字符在构造时抛出 ArgumentException（实现使用 IsNullOrWhiteSpace）。
        /// </summary>
        [Fact]
        public void TableAttribute_WhitespaceOnlyName_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => new TableAttribute("   "));
            Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// [Table(" orders")] 名称以空格开头应抛 ArgumentException，避免拼接出
        /// 非法表名（如 `` ` orders` ``）或被错误解析为带架构名。
        /// </summary>
        [Theory]
        [InlineData(" orders")]
        [InlineData("\torders")]
        [InlineData("\norders")]
        public void TableAttribute_NameStartsWithWhitespace_ThrowsArgumentException(string name)
        {
            var ex = Assert.Throws<ArgumentException>(() => new TableAttribute(name));
            Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// [Table("orders ")] 名称以空格结尾应抛 ArgumentException。
        /// </summary>
        [Theory]
        [InlineData("orders ")]
        [InlineData("orders\t")]
        [InlineData("orders\n")]
        public void TableAttribute_NameEndsWithWhitespace_ThrowsArgumentException(string name)
        {
            var ex = Assert.Throws<ArgumentException>(() => new TableAttribute(name));
            Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// [Table("orders")] 正常名称应构造成功。
        /// </summary>
        [Fact]
        public void TableAttribute_ValidName_DoesNotThrow()
        {
            var attr = new TableAttribute("orders");
            Assert.Equal("orders", attr.Name);
        }

        /// <summary>
        /// TableAttribute.Schema 未设置时默认为 null（不应有非空默认值）。
        /// </summary>
        [Fact]
        public void TableAttribute_SchemaDefault_IsNull()
        {
            var attr = new TableAttribute("orders");

            Assert.Null(attr.Schema);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 13. VersionAttribute 支持类型推断（通过 DefaultTableAnalyzer）
        // ─────────────────────────────────────────────────────────────────────

        // 辅助实体：各合法版本类型
        [Table("tbl_ver_int")]
        private class EntityWithIntVersion
        {
            [System.ComponentModel.DataAnnotations.Key]
            public int Id { get; set; }
            [Version]
            [Field("rev")]
            public int Rev { get; set; }
        }

        [Table("tbl_ver_long")]
        private class EntityWithLongVersion
        {
            [System.ComponentModel.DataAnnotations.Key]
            public int Id { get; set; }
            [Version]
            [Field("rev")]
            public long Rev { get; set; }
        }

        [Table("tbl_ver_datetime")]
        private class EntityWithDateTimeVersion
        {
            [System.ComponentModel.DataAnnotations.Key]
            public int Id { get; set; }
            [Version]
            [Field("rev")]
            public DateTime Rev { get; set; }
        }

        [Table("tbl_ver_double")]
        private class EntityWithDoubleVersion
        {
            [System.ComponentModel.DataAnnotations.Key]
            public int Id { get; set; }
            [Version]
            [Field("rev")]
            public double Rev { get; set; }
        }

        // 辅助实体：不支持的版本类型
        [Table("tbl_ver_guid")]
        private class EntityWithGuidVersion
        {
            [System.ComponentModel.DataAnnotations.Key]
            public int Id { get; set; }
            [Version]
            [Field("rev")]
            public Guid Rev { get; set; }  // Guid 不在支持列表
        }

        [Table("tbl_ver_string")]
        private class EntityWithStringVersion
        {
            [System.ComponentModel.DataAnnotations.Key]
            public int Id { get; set; }
            [Version]
            [Field("rev")]
            public string Rev { get; set; }  // string 不在支持列表
        }

        [Table("tbl_ver_bool")]
        private class EntityWithBoolVersion
        {
            [System.ComponentModel.DataAnnotations.Key]
            public int Id { get; set; }
            [Version]
            [Field("rev")]
            public bool Rev { get; set; }  // bool 不在支持列表
        }

        /// <summary>
        /// [Version] 作用于 int 属性时，DefaultTableAnalyzer 应将 VersionKind 推断为 Increment。
        /// </summary>
        [Fact]
        public void DefaultTableAnalyzer_IntVersionProperty_InfersVersionKindIncrement()
        {
            var analyzer = new DefaultTableAnalyzer();
            var options = analyzer.Table(typeof(EntityWithIntVersion));

            Assert.Equal(VersionKind.Increment, options.Columns["Rev"].Version);
        }

        /// <summary>
        /// [Version] 作用于 long 属性时，DefaultTableAnalyzer 应将 VersionKind 推断为 Ticks。
        /// </summary>
        [Fact]
        public void DefaultTableAnalyzer_LongVersionProperty_InfersVersionKindTicks()
        {
            var analyzer = new DefaultTableAnalyzer();
            var options = analyzer.Table(typeof(EntityWithLongVersion));

            Assert.Equal(VersionKind.Ticks, options.Columns["Rev"].Version);
        }

        /// <summary>
        /// [Version] 作用于 DateTime 属性时，DefaultTableAnalyzer 应将 VersionKind 推断为 Now。
        /// </summary>
        [Fact]
        public void DefaultTableAnalyzer_DateTimeVersionProperty_InfersVersionKindNow()
        {
            var analyzer = new DefaultTableAnalyzer();
            var options = analyzer.Table(typeof(EntityWithDateTimeVersion));

            Assert.Equal(VersionKind.Now, options.Columns["Rev"].Version);
        }

        /// <summary>
        /// [Version] 作用于 double 属性时，DefaultTableAnalyzer 应将 VersionKind 推断为 Timestamp。
        /// </summary>
        [Fact]
        public void DefaultTableAnalyzer_DoubleVersionProperty_InfersVersionKindTimestamp()
        {
            var analyzer = new DefaultTableAnalyzer();
            var options = analyzer.Table(typeof(EntityWithDoubleVersion));

            Assert.Equal(VersionKind.Timestamp, options.Columns["Rev"].Version);
        }

        /// <summary>
        /// [Version] 作用于 Guid 属性（不在支持列表）时，DefaultTableAnalyzer 应抛出 NotSupportedException。
        /// 风险等级 High：若改成静默忽略，会导致乐观锁完全失效。
        /// </summary>
        [Fact]
        public void DefaultTableAnalyzer_GuidVersionProperty_ThrowsNotSupportedException()
        {
            var analyzer = new DefaultTableAnalyzer();

            Assert.Throws<NotSupportedException>(() => analyzer.Table(typeof(EntityWithGuidVersion)));
        }

        /// <summary>
        /// [Version] 作用于 string 属性（不在支持列表）时，DefaultTableAnalyzer 应抛出 NotSupportedException。
        /// </summary>
        [Fact]
        public void DefaultTableAnalyzer_StringVersionProperty_ThrowsNotSupportedException()
        {
            var analyzer = new DefaultTableAnalyzer();

            Assert.Throws<NotSupportedException>(() => analyzer.Table(typeof(EntityWithStringVersion)));
        }

        /// <summary>
        /// [Version] 作用于 bool 属性时，DefaultTableAnalyzer 应抛出 NotSupportedException。
        /// </summary>
        [Fact]
        public void DefaultTableAnalyzer_BoolVersionProperty_ThrowsNotSupportedException()
        {
            var analyzer = new DefaultTableAnalyzer();

            Assert.Throws<NotSupportedException>(() => analyzer.Table(typeof(EntityWithBoolVersion)));
        }

        // ─────────────────────────────────────────────────────────────────────
        // 14. DefaultTableAnalyzer — 无 [Table] Annotation 时用类名作为表名
        // ─────────────────────────────────────────────────────────────────────

        // 故意不标 [Table]
        private class EntityWithoutTableAttr
        {
            [System.ComponentModel.DataAnnotations.Key]
            public int Id { get; set; }
            public string Name { get; set; }
        }

        /// <summary>
        /// 未标注 [Table] 的实体类，DefaultTableAnalyzer 应用类名作为表名（约定优先）。
        /// </summary>
        [Fact]
        public void DefaultTableAnalyzer_NoTableAttribute_UsesClassNameAsTableName()
        {
            var analyzer = new DefaultTableAnalyzer();
            var options = analyzer.Table(typeof(EntityWithoutTableAttr));

            Assert.Equal("EntityWithoutTableAttr", options.Name);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 15. DefaultTableAnalyzer — [Field] 映射列名优先于属性名
        // ─────────────────────────────────────────────────────────────────────

        [Table("tbl_field_mapping")]
        private class EntityWithFieldAttr
        {
            [System.ComponentModel.DataAnnotations.Key]
            public int Id { get; set; }

            [Field("user_name")]
            public string UserName { get; set; }

            // 无 [Field]，应用属性名作列名
            public int Age { get; set; }
        }

        /// <summary>
        /// 标注了 [Field("user_name")] 的属性，分析器应将列名解析为 "user_name"，而非 "UserName"。
        /// </summary>
        [Fact]
        public void DefaultTableAnalyzer_FieldAttribute_OverridesPropertyName()
        {
            var analyzer = new DefaultTableAnalyzer();
            var options = analyzer.Table(typeof(EntityWithFieldAttr));

            Assert.Equal("user_name", options.Columns["UserName"].Name);
        }

        /// <summary>
        /// 未标注 [Field] 的属性，分析器应直接用属性名作为列名。
        /// </summary>
        [Fact]
        public void DefaultTableAnalyzer_NoFieldAttribute_UsesPropertyNameAsColumnName()
        {
            var analyzer = new DefaultTableAnalyzer();
            var options = analyzer.Table(typeof(EntityWithFieldAttr));

            Assert.Equal("Age", options.Columns["Age"].Name);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 16. DefaultTableAnalyzer — [DatabaseGenerated] 标记 ReadOnly
        // ─────────────────────────────────────────────────────────────────────

        [Table("tbl_db_generated")]
        private class EntityWithDbGenerated
        {
            [System.ComponentModel.DataAnnotations.Key]
            [DatabaseGenerated]
            public int Id { get; set; }

            public string Name { get; set; }
        }

        /// <summary>
        /// 标注了 [DatabaseGenerated] 的属性，Column.ReadOnly 应为 true。
        /// </summary>
        [Fact]
        public void DefaultTableAnalyzer_DatabaseGeneratedAttribute_SetsColumnReadOnly()
        {
            var analyzer = new DefaultTableAnalyzer();
            var options = analyzer.Table(typeof(EntityWithDbGenerated));

            Assert.True(options.Columns["Id"].ReadOnly);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 17. DefaultTableAnalyzer — [Key] 标记主键
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 标注了 [Key] 的属性，Column.Key 应为 true；未标注的属性 Column.Key 应为 false。
        /// </summary>
        [Fact]
        public void DefaultTableAnalyzer_KeyAttribute_SetsColumnKey()
        {
            var analyzer = new DefaultTableAnalyzer();
            var options = analyzer.Table(typeof(EntityWithFieldAttr));

            Assert.True(options.Columns["Id"].Key);
            Assert.False(options.Columns["UserName"].Key);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 18. TableAnalyzer.Table — null 参数抛异常
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// TableAnalyzer.Table(null) 应抛出 ArgumentNullException，而非 NullReferenceException。
        /// </summary>
        [Fact]
        public void TableAnalyzer_NullType_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => TableAnalyzer.Table(null));
        }

        // ─────────────────────────────────────────────────────────────────────
        // 19. VersionAttribute / DatabaseGeneratedAttribute 是密封类
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// VersionAttribute 应为 sealed，防止派生后改变语义。
        /// </summary>
        [Fact]
        public void VersionAttribute_IsSealed()
        {
            Assert.True(typeof(VersionAttribute).IsSealed);
        }

        /// <summary>
        /// DatabaseGeneratedAttribute 应为 sealed。
        /// </summary>
        [Fact]
        public void DatabaseGeneratedAttribute_IsSealed()
        {
            Assert.True(typeof(DatabaseGeneratedAttribute).IsSealed);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 20. LookupDb.For — Nullable<JsonPayload> 不在 _typeMap，
        //     但 JsonPayload 是引用类型，Nullable.GetUnderlyingType 返回 null，
        //     验证代码路径不会抛 NullReferenceException
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// LookupDb.For 内部先调用 IsNullable()，对引用类型（JsonPayload）IsNullable() 应返回 false，
        /// 因此 dataType 不被解包，直接走 _typeMap 命中 JsonDbType。
        /// 本测试间接验证引用类型的 IsNullable 路径不会 NullReferenceException。
        /// </summary>
        [Fact]
        public void LookupDbFor_JsonPayload_IsNotTreatedAsNullable_ReturnsJsonDbType()
        {
            // JsonPayload 是 class，不是 Nullable<T>，IsNullable() 应返回 false
            var result = LookupDb.For(typeof(JsonPayload));

            Assert.Equal(LookupDb.JsonDbType, result);
        }
    }
}
