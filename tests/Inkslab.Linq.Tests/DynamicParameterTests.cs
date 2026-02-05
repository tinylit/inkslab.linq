using System.Data;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// DynamicParameter 类单元测试
    /// </summary>
    public class DynamicParameterTests
    {
        #region 属性测试

        /// <summary>
        /// 测试：Value 属性可以设置和获取
        /// </summary>
        [Fact]
        public void Value_CanBeSetAndGet()
        {
            // Arrange
            var param = new DynamicParameter
            {
                // Act
                Value = "TestValue"
            };

            // Assert
            Assert.Equal("TestValue", param.Value);
        }

        /// <summary>
        /// 测试：Value 属性支持不同类型
        /// </summary>
        [Theory]
        [InlineData(123)]
        [InlineData("string")]
        [InlineData(true)]
        [InlineData(3.14)]
        public void Value_SupportsVariousTypes(object value)
        {
            // Arrange
            var param = new DynamicParameter
            {
                // Act
                Value = value
            };

            // Assert
            Assert.Equal(value, param.Value);
        }

        /// <summary>
        /// 测试：Value 属性可以为 null
        /// </summary>
        [Fact]
        public void Value_CanBeNull()
        {
            // Arrange
            var param = new DynamicParameter
            {
                // Act
                Value = null
            };

            // Assert
            Assert.Null(param.Value);
        }

        /// <summary>
        /// 测试：Direction 属性默认值（枚举默认值为0）
        /// </summary>
        [Fact]
        public void Direction_DefaultValue()
        {
            // Arrange
            var param = new DynamicParameter();

            // Assert - 枚举默认值为0，对应 ParameterDirection 的第一个值
            Assert.Equal(default(ParameterDirection), param.Direction);
        }

        /// <summary>
        /// 测试：Direction 属性可以设置和获取
        /// </summary>
        [Theory]
        [InlineData(ParameterDirection.Input)]
        [InlineData(ParameterDirection.Output)]
        [InlineData(ParameterDirection.InputOutput)]
        [InlineData(ParameterDirection.ReturnValue)]
        public void Direction_CanBeSetAndGet(ParameterDirection direction)
        {
            // Arrange
            var param = new DynamicParameter
            {
                // Act
                Direction = direction
            };

            // Assert
            Assert.Equal(direction, param.Direction);
        }

        /// <summary>
        /// 测试：DbType 属性默认值为 null
        /// </summary>
        [Fact]
        public void DbType_DefaultValueIsNull()
        {
            // Arrange
            var param = new DynamicParameter();

            // Assert
            Assert.Null(param.DbType);
        }

        /// <summary>
        /// 测试：DbType 属性可以设置和获取
        /// </summary>
        [Theory]
        [InlineData(DbType.String)]
        [InlineData(DbType.Int32)]
        [InlineData(DbType.DateTime)]
        [InlineData(DbType.Boolean)]
        public void DbType_CanBeSetAndGet(DbType dbType)
        {
            // Arrange
            var param = new DynamicParameter
            {
                // Act
                DbType = dbType
            };

            // Assert
            Assert.Equal(dbType, param.DbType);
        }

        /// <summary>
        /// 测试：Size 属性默认值为 null
        /// </summary>
        [Fact]
        public void Size_DefaultValueIsNull()
        {
            // Arrange
            var param = new DynamicParameter();

            // Assert
            Assert.Null(param.Size);
        }

        /// <summary>
        /// 测试：Size 属性可以设置和获取
        /// </summary>
        [Theory]
        [InlineData(100)]
        [InlineData(255)]
        [InlineData(4000)]
        [InlineData(-1)] // -1 通常表示 MAX
        public void Size_CanBeSetAndGet(int size)
        {
            // Arrange
            var param = new DynamicParameter
            {
                // Act
                Size = size
            };

            // Assert
            Assert.Equal(size, param.Size);
        }

        /// <summary>
        /// 测试：Precision 属性默认值为 null
        /// </summary>
        [Fact]
        public void Precision_DefaultValueIsNull()
        {
            // Arrange
            var param = new DynamicParameter();

            // Assert
            Assert.Null(param.Precision);
        }

        /// <summary>
        /// 测试：Precision 属性可以设置和获取
        /// </summary>
        [Theory]
        [InlineData((byte)18)]
        [InlineData((byte)38)]
        [InlineData((byte)0)]
        public void Precision_CanBeSetAndGet(byte precision)
        {
            // Arrange
            var param = new DynamicParameter
            {
                // Act
                Precision = precision
            };

            // Assert
            Assert.Equal(precision, param.Precision);
        }

        /// <summary>
        /// 测试：Scale 属性默认值为 null
        /// </summary>
        [Fact]
        public void Scale_DefaultValueIsNull()
        {
            // Arrange
            var param = new DynamicParameter();

            // Assert
            Assert.Null(param.Scale);
        }

        /// <summary>
        /// 测试：Scale 属性可以设置和获取
        /// </summary>
        [Theory]
        [InlineData((byte)2)]
        [InlineData((byte)4)]
        [InlineData((byte)0)]
        [InlineData((byte)6)]
        public void Scale_CanBeSetAndGet(byte scale)
        {
            // Arrange
            var param = new DynamicParameter
            {
                // Act
                Scale = scale
            };

            // Assert
            Assert.Equal(scale, param.Scale);
        }

        #endregion

        #region 复合场景测试

        /// <summary>
        /// 测试：设置所有属性
        /// </summary>
        [Fact]
        public void AllProperties_CanBeSetTogether()
        {
            // Arrange
            var param = new DynamicParameter
            {
                // Act
                Value = "TestValue",
                Direction = ParameterDirection.InputOutput,
                DbType = DbType.String,
                Size = 100,
                Precision = 18,
                Scale = 2
            };

            // Assert
            Assert.Equal("TestValue", param.Value);
            Assert.Equal(ParameterDirection.InputOutput, param.Direction);
            Assert.Equal(DbType.String, param.DbType);
            Assert.Equal(100, param.Size);
            Assert.Equal((byte)18, param.Precision);
            Assert.Equal((byte)2, param.Scale);
        }

        /// <summary>
        /// 测试：输出参数的典型用法
        /// </summary>
        [Fact]
        public void OutputParameter_TypicalUsage()
        {
            // Arrange
            var param = new DynamicParameter
            {
                Direction = ParameterDirection.Output,
                DbType = DbType.Int32,
                // Act - 模拟存储过程返回值
                Value = 42
            };

            // Assert
            Assert.Equal(42, param.Value);
            Assert.Equal(ParameterDirection.Output, param.Direction);
        }

        /// <summary>
        /// 测试：Decimal 参数的典型用法
        /// </summary>
        [Fact]
        public void DecimalParameter_TypicalUsage()
        {
            // Arrange
            var param = new DynamicParameter
            {
                Value = 123.45m,
                DbType = DbType.Decimal,
                Precision = 18,
                Scale = 2
            };

            // Assert
            Assert.Equal(123.45m, param.Value);
            Assert.Equal(DbType.Decimal, param.DbType);
            Assert.Equal((byte)18, param.Precision);
            Assert.Equal((byte)2, param.Scale);
        }

        /// <summary>
        /// 测试：字符串参数的典型用法
        /// </summary>
        [Fact]
        public void StringParameter_TypicalUsage()
        {
            // Arrange
            var param = new DynamicParameter
            {
                Value = "Hello World",
                DbType = DbType.String,
                Size = 255
            };

            // Assert
            Assert.Equal("Hello World", param.Value);
            Assert.Equal(DbType.String, param.DbType);
            Assert.Equal(255, param.Size);
        }

        #endregion
    }
}
