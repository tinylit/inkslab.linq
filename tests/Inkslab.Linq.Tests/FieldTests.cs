using Inkslab.Linq.Enums;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// Field 结构体单元测试
    /// </summary>
    public class FieldTests
    {
        #region 构造函数测试

        /// <summary>
        /// 测试：使用最小参数创建 Field
        /// </summary>
        [Fact]
        public void Constructor_WithMinimalParams_CreatesField()
        {
            // Arrange & Act
            var field = new Field("name", false, false);

            // Assert
            Assert.Equal("name", field.Name);
            Assert.False(field.PrimaryKey);
            Assert.False(field.ReadOnly);
            Assert.Equal(VersionKind.None, field.Version);
        }

        /// <summary>
        /// 测试：使用主键参数创建 Field
        /// </summary>
        [Fact]
        public void Constructor_WithPrimaryKey_CreatesField()
        {
            // Arrange & Act
            var field = new Field("id", true, false);

            // Assert
            Assert.Equal("id", field.Name);
            Assert.True(field.PrimaryKey);
            Assert.False(field.ReadOnly);
        }

        /// <summary>
        /// 测试：使用只读参数创建 Field
        /// </summary>
        [Fact]
        public void Constructor_WithReadOnly_CreatesField()
        {
            // Arrange & Act
            var field = new Field("created_at", false, true);

            // Assert
            Assert.Equal("created_at", field.Name);
            Assert.False(field.PrimaryKey);
            Assert.True(field.ReadOnly);
        }

        /// <summary>
        /// 测试：使用版本参数创建 Field
        /// </summary>
        [Theory]
        [InlineData(VersionKind.None)]
        [InlineData(VersionKind.Increment)]
        [InlineData(VersionKind.Ticks)]
        [InlineData(VersionKind.Timestamp)]
        [InlineData(VersionKind.Now)]
        public void Constructor_WithVersion_CreatesField(VersionKind versionKind)
        {
            // Arrange & Act
            var field = new Field("version", false, false, versionKind);

            // Assert
            Assert.Equal("version", field.Name);
            Assert.Equal(versionKind, field.Version);
        }

        /// <summary>
        /// 测试：创建主键且只读的 Field
        /// </summary>
        [Fact]
        public void Constructor_WithPrimaryKeyAndReadOnly_CreatesField()
        {
            // Arrange & Act
            var field = new Field("id", true, true);

            // Assert
            Assert.Equal("id", field.Name);
            Assert.True(field.PrimaryKey);
            Assert.True(field.ReadOnly);
        }

        /// <summary>
        /// 测试：创建所有属性都设置的 Field
        /// </summary>
        [Fact]
        public void Constructor_WithAllParams_CreatesField()
        {
            // Arrange & Act
            var field = new Field("version_col", false, true, VersionKind.Increment);

            // Assert
            Assert.Equal("version_col", field.Name);
            Assert.False(field.PrimaryKey);
            Assert.True(field.ReadOnly);
            Assert.Equal(VersionKind.Increment, field.Version);
        }

        #endregion

        #region 边界测试

        /// <summary>
        /// 测试：使用空字符串作为名称
        /// </summary>
        [Fact]
        public void Constructor_WithEmptyName_CreatesField()
        {
            // Arrange & Act
            var field = new Field(string.Empty, false, false);

            // Assert
            Assert.Equal(string.Empty, field.Name);
        }

        /// <summary>
        /// 测试：使用 null 作为名称
        /// </summary>
        [Fact]
        public void Constructor_WithNullName_CreatesField()
        {
            // Arrange & Act
            var field = new Field(null, false, false);

            // Assert
            Assert.Null(field.Name);
        }

        /// <summary>
        /// 测试：使用特殊字符作为名称
        /// </summary>
        [Theory]
        [InlineData("user_name")]
        [InlineData("userName")]
        [InlineData("column-name")]
        [InlineData("column.name")]
        [InlineData("`column`")]
        [InlineData("[column]")]
        public void Constructor_WithSpecialCharacterName_CreatesField(string name)
        {
            // Arrange & Act
            var field = new Field(name, false, false);

            // Assert
            Assert.Equal(name, field.Name);
        }

        #endregion

        #region 不可变性测试

        /// <summary>
        /// 测试：Field 是只读结构体（属性值不可变）
        /// </summary>
        [Fact]
        public void Field_IsImmutable()
        {
            // Arrange
            var field = new Field("name", true, true, VersionKind.Increment);

            // Assert - 所有属性只有 getter，没有 setter
            Assert.Equal("name", field.Name);
            Assert.True(field.PrimaryKey);
            Assert.True(field.ReadOnly);
            Assert.Equal(VersionKind.Increment, field.Version);

            // 结构体是值类型，复制后独立
            var field2 = field;
            Assert.Equal(field.Name, field2.Name);
            Assert.Equal(field.PrimaryKey, field2.PrimaryKey);
            Assert.Equal(field.ReadOnly, field2.ReadOnly);
            Assert.Equal(field.Version, field2.Version);
        }

        /// <summary>
        /// 测试：默认 Field 值
        /// </summary>
        [Fact]
        public void DefaultField_HasDefaultValues()
        {
            // Arrange & Act
            var field = default(Field);

            // Assert
            Assert.Null(field.Name);
            Assert.False(field.PrimaryKey);
            Assert.False(field.ReadOnly);
            Assert.Equal(VersionKind.None, field.Version);
        }

        #endregion
    }
}
