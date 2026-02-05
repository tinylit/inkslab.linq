using Inkslab.Linq.Enums;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// 枚举类型单元测试
    /// </summary>
    public class EnumTests
    {
        #region SqlKeyword 测试

        /// <summary>
        /// 测试：SqlKeyword 包含必要的关键字
        /// </summary>
        [Theory]
        [InlineData(SqlKeyword.SELECT)]
        [InlineData(SqlKeyword.FROM)]
        [InlineData(SqlKeyword.WHERE)]
        [InlineData(SqlKeyword.INSERT)]
        [InlineData(SqlKeyword.UPDATE)]
        [InlineData(SqlKeyword.DELETE)]
        [InlineData(SqlKeyword.AND)]
        [InlineData(SqlKeyword.OR)]
        [InlineData(SqlKeyword.NOT)]
        [InlineData(SqlKeyword.NULL)]
        [InlineData(SqlKeyword.IS)]
        [InlineData(SqlKeyword.IN)]
        [InlineData(SqlKeyword.LIKE)]
        [InlineData(SqlKeyword.JOIN)]
        [InlineData(SqlKeyword.ON)]
        [InlineData(SqlKeyword.AS)]
        public void SqlKeyword_ContainsRequiredKeywords(SqlKeyword keyword)
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(SqlKeyword), keyword));
        }

        /// <summary>
        /// 测试：SqlKeyword 排序关键字
        /// </summary>
        [Theory]
        [InlineData(SqlKeyword.ORDER)]
        [InlineData(SqlKeyword.BY)]
        [InlineData(SqlKeyword.ASC)]
        [InlineData(SqlKeyword.DESC)]
        [InlineData(SqlKeyword.GROUP)]
        [InlineData(SqlKeyword.HAVING)]
        public void SqlKeyword_ContainsOrderingKeywords(SqlKeyword keyword)
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(SqlKeyword), keyword));
        }

        /// <summary>
        /// 测试：SqlKeyword JOIN 相关关键字
        /// </summary>
        [Theory]
        [InlineData(SqlKeyword.INNER)]
        [InlineData(SqlKeyword.LEFT)]
        [InlineData(SqlKeyword.RIGHT)]
        [InlineData(SqlKeyword.OUTER)]
        [InlineData(SqlKeyword.CROSS)]
        public void SqlKeyword_ContainsJoinKeywords(SqlKeyword keyword)
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(SqlKeyword), keyword));
        }

        /// <summary>
        /// 测试：SqlKeyword CASE WHEN 相关关键字
        /// </summary>
        [Theory]
        [InlineData(SqlKeyword.CASE)]
        [InlineData(SqlKeyword.WHEN)]
        [InlineData(SqlKeyword.THEN)]
        [InlineData(SqlKeyword.ELSE)]
        [InlineData(SqlKeyword.END)]
        public void SqlKeyword_ContainsCaseWhenKeywords(SqlKeyword keyword)
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(SqlKeyword), keyword));
        }

        /// <summary>
        /// 测试：SqlKeyword 集合操作关键字
        /// </summary>
        [Theory]
        [InlineData(SqlKeyword.UNION)]
        [InlineData(SqlKeyword.INTERSECT)]
        [InlineData(SqlKeyword.EXCEPT)]
        [InlineData(SqlKeyword.ALL)]
        public void SqlKeyword_ContainsSetOperationKeywords(SqlKeyword keyword)
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(SqlKeyword), keyword));
        }

        /// <summary>
        /// 测试：SqlKeyword 可以转换为字符串
        /// </summary>
        [Theory]
        [InlineData(SqlKeyword.SELECT, "SELECT")]
        [InlineData(SqlKeyword.FROM, "FROM")]
        [InlineData(SqlKeyword.WHERE, "WHERE")]
        [InlineData(SqlKeyword.INSERT, "INSERT")]
        [InlineData(SqlKeyword.UPDATE, "UPDATE")]
        [InlineData(SqlKeyword.DELETE, "DELETE")]
        public void SqlKeyword_ToStringReturnsKeyword(SqlKeyword keyword, string expected)
        {
            // Act
            var result = keyword.ToString();

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region VersionKind 测试

        /// <summary>
        /// 测试：VersionKind.None 是默认值
        /// </summary>
        [Fact]
        public void VersionKind_None_IsDefaultValue()
        {
            // Arrange
            var defaultValue = default(VersionKind);

            // Assert
            Assert.Equal(VersionKind.None, defaultValue);
        }

        /// <summary>
        /// 测试：VersionKind 包含所有版本类型
        /// </summary>
        [Theory]
        [InlineData(VersionKind.None)]
        [InlineData(VersionKind.Increment)]
        [InlineData(VersionKind.Ticks)]
        [InlineData(VersionKind.Timestamp)]
        [InlineData(VersionKind.Now)]
        public void VersionKind_ContainsAllVersionTypes(VersionKind kind)
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(VersionKind), kind));
        }

        /// <summary>
        /// 测试：VersionKind 可以转换为字符串
        /// </summary>
        [Theory]
        [InlineData(VersionKind.None, "None")]
        [InlineData(VersionKind.Increment, "Increment")]
        [InlineData(VersionKind.Ticks, "Ticks")]
        [InlineData(VersionKind.Timestamp, "Timestamp")]
        [InlineData(VersionKind.Now, "Now")]
        public void VersionKind_ToStringReturnsName(VersionKind kind, string expected)
        {
            // Act
            var result = kind.ToString();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试：VersionKind 可以从整数转换
        /// </summary>
        [Fact]
        public void VersionKind_CanBeConvertedFromInt()
        {
            // Assert
            Assert.Equal(VersionKind.None, (VersionKind)0);
            Assert.Equal(VersionKind.Increment, (VersionKind)1);
            Assert.Equal(VersionKind.Ticks, (VersionKind)2);
            Assert.Equal(VersionKind.Timestamp, (VersionKind)3);
            Assert.Equal(VersionKind.Now, (VersionKind)4);
        }

        #endregion

        #region RowStyle 测试

        /// <summary>
        /// 测试：RowStyle.First 是默认值
        /// </summary>
        [Fact]
        public void RowStyle_First_IsDefaultValue()
        {
            // Arrange
            var defaultValue = default(RowStyle);

            // Assert
            Assert.Equal(RowStyle.First, defaultValue);
        }

        /// <summary>
        /// 测试：RowStyle 包含所有行样式
        /// </summary>
        [Theory]
        [InlineData(RowStyle.First)]
        [InlineData(RowStyle.FirstOrDefault)]
        [InlineData(RowStyle.Single)]
        [InlineData(RowStyle.SingleOrDefault)]
        public void RowStyle_ContainsAllRowStyles(RowStyle style)
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(RowStyle), style));
        }

        /// <summary>
        /// 测试：RowStyle 可以转换为字符串
        /// </summary>
        [Theory]
        [InlineData(RowStyle.First, "First")]
        [InlineData(RowStyle.FirstOrDefault, "FirstOrDefault")]
        [InlineData(RowStyle.Single, "Single")]
        [InlineData(RowStyle.SingleOrDefault, "SingleOrDefault")]
        public void RowStyle_ToStringReturnsName(RowStyle style, string expected)
        {
            // Act
            var result = style.ToString();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试：RowStyle 可以从整数转换
        /// </summary>
        [Fact]
        public void RowStyle_CanBeConvertedFromInt()
        {
            // Assert
            Assert.Equal(RowStyle.First, (RowStyle)0);
            Assert.Equal(RowStyle.FirstOrDefault, (RowStyle)1);
            Assert.Equal(RowStyle.Single, (RowStyle)2);
            Assert.Equal(RowStyle.SingleOrDefault, (RowStyle)3);
        }

        /// <summary>
        /// 测试：RowStyle 数值对应关系
        /// </summary>
        [Fact]
        public void RowStyle_HasCorrectNumericValues()
        {
            // Assert
            Assert.Equal(0, (int)RowStyle.First);
            Assert.Equal(1, (int)RowStyle.FirstOrDefault);
            Assert.Equal(2, (int)RowStyle.Single);
            Assert.Equal(3, (int)RowStyle.SingleOrDefault);
        }

        #endregion
    }
}
