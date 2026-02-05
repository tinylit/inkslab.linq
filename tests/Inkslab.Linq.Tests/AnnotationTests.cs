using System;
using Inkslab.Linq.Annotations;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// FieldAttribute 单元测试
    /// </summary>
    public class FieldAttributeTests
    {
        /// <summary>
        /// 测试：使用有效名称创建 FieldAttribute
        /// </summary>
        [Fact]
        public void Constructor_WithValidName_CreatesAttribute()
        {
            // Arrange & Act
            var attribute = new FieldAttribute("column_name");

            // Assert
            Assert.Equal("column_name", attribute.Name);
        }

        /// <summary>
        /// 测试：使用各种有效名称创建 FieldAttribute
        /// </summary>
        [Theory]
        [InlineData("id")]
        [InlineData("user_name")]
        [InlineData("userId")]
        [InlineData("create_time")]
        [InlineData("COLUMN_NAME")]
        public void Constructor_WithVariousValidNames_CreatesAttribute(string name)
        {
            // Arrange & Act
            var attribute = new FieldAttribute(name);

            // Assert
            Assert.Equal(name, attribute.Name);
        }

        /// <summary>
        /// 测试：使用 null 名称抛出异常
        /// </summary>
        [Fact]
        public void Constructor_WithNullName_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new FieldAttribute(null));
        }

        /// <summary>
        /// 测试：使用空字符串名称抛出异常
        /// </summary>
        [Fact]
        public void Constructor_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new FieldAttribute(string.Empty));
        }

        /// <summary>
        /// 测试：FieldAttribute 只能应用于属性
        /// </summary>
        [Fact]
        public void FieldAttribute_HasPropertyTarget()
        {
            // Arrange
            var attributeUsage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
                typeof(FieldAttribute),
                typeof(AttributeUsageAttribute));

            // Assert
            Assert.NotNull(attributeUsage);
            Assert.Equal(AttributeTargets.Property, attributeUsage.ValidOn);
        }
    }

    /// <summary>
    /// TableAttribute 单元测试
    /// </summary>
    public class TableAttributeTests
    {
        /// <summary>
        /// 测试：使用有效名称创建 TableAttribute
        /// </summary>
        [Fact]
        public void Constructor_WithValidName_CreatesAttribute()
        {
            // Arrange & Act
            var attribute = new TableAttribute("users");

            // Assert
            Assert.Equal("users", attribute.Name);
            Assert.Null(attribute.Schema);
        }

        /// <summary>
        /// 测试：设置 Schema 属性
        /// </summary>
        [Fact]
        public void Schema_CanBeSetAndGet()
        {
            // Arrange
            var attribute = new TableAttribute("users")
            {
                // Act
                Schema = "dbo"
            };

            // Assert
            Assert.Equal("users", attribute.Name);
            Assert.Equal("dbo", attribute.Schema);
        }

        /// <summary>
        /// 测试：使用各种有效名称创建 TableAttribute
        /// </summary>
        [Theory]
        [InlineData("user")]
        [InlineData("user_info")]
        [InlineData("UserInfo")]
        [InlineData("tbl_users")]
        [InlineData("user_[sharding]")]
        public void Constructor_WithVariousValidNames_CreatesAttribute(string name)
        {
            // Arrange & Act
            var attribute = new TableAttribute(name);

            // Assert
            Assert.Equal(name, attribute.Name);
        }

        /// <summary>
        /// 测试：使用 null 名称抛出异常
        /// </summary>
        [Fact]
        public void Constructor_WithNullName_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new TableAttribute(null));
        }

        /// <summary>
        /// 测试：使用空字符串名称抛出异常
        /// </summary>
        [Fact]
        public void Constructor_WithEmptyName_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new TableAttribute(string.Empty));
        }

        /// <summary>
        /// 测试：TableAttribute 只能应用于类
        /// </summary>
        [Fact]
        public void TableAttribute_HasClassTarget()
        {
            // Arrange
            var attributeUsage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
                typeof(TableAttribute),
                typeof(AttributeUsageAttribute));

            // Assert
            Assert.NotNull(attributeUsage);
            Assert.Equal(AttributeTargets.Class, attributeUsage.ValidOn);
            Assert.False(attributeUsage.Inherited);
        }
    }

    /// <summary>
    /// VersionAttribute 单元测试
    /// </summary>
    public class VersionAttributeTests
    {
        /// <summary>
        /// 测试：创建 VersionAttribute
        /// </summary>
        [Fact]
        public void Constructor_CreatesAttribute()
        {
            // Arrange & Act
            var attribute = new VersionAttribute();

            // Assert
            Assert.NotNull(attribute);
        }

        /// <summary>
        /// 测试：VersionAttribute 只能应用于属性
        /// </summary>
        [Fact]
        public void VersionAttribute_HasPropertyTarget()
        {
            // Arrange
            var attributeUsage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
                typeof(VersionAttribute),
                typeof(AttributeUsageAttribute));

            // Assert
            Assert.NotNull(attributeUsage);
            Assert.Equal(AttributeTargets.Property, attributeUsage.ValidOn);
        }

        /// <summary>
        /// 测试：VersionAttribute 是密封类
        /// </summary>
        [Fact]
        public void VersionAttribute_IsSealed()
        {
            // Assert
            Assert.True(typeof(VersionAttribute).IsSealed);
        }
    }

    /// <summary>
    /// DatabaseGeneratedAttribute 单元测试
    /// </summary>
    public class DatabaseGeneratedAttributeTests
    {
        /// <summary>
        /// 测试：创建 DatabaseGeneratedAttribute
        /// </summary>
        [Fact]
        public void Constructor_CreatesAttribute()
        {
            // Arrange & Act
            var attribute = new DatabaseGeneratedAttribute();

            // Assert
            Assert.NotNull(attribute);
        }

        /// <summary>
        /// 测试：DatabaseGeneratedAttribute 只能应用于属性
        /// </summary>
        [Fact]
        public void DatabaseGeneratedAttribute_HasPropertyTarget()
        {
            // Arrange
            var attributeUsage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
                typeof(DatabaseGeneratedAttribute),
                typeof(AttributeUsageAttribute));

            // Assert
            Assert.NotNull(attributeUsage);
            Assert.Equal(AttributeTargets.Property, attributeUsage.ValidOn);
        }

        /// <summary>
        /// 测试：DatabaseGeneratedAttribute 是密封类
        /// </summary>
        [Fact]
        public void DatabaseGeneratedAttribute_IsSealed()
        {
            // Assert
            Assert.True(typeof(DatabaseGeneratedAttribute).IsSealed);
        }
    }
}
