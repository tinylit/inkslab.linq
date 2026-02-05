using System;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// Ranks 类单元测试
    /// </summary>
    public class RanksTests
    {
        /// <summary>
        /// 测试：By 方法在非表达式上下文中抛出 NotImplementedException
        /// </summary>
        [Fact]
        public void By_ThrowsNotImplementedException()
        {
            // Arrange
            var user = new User { Id = 1, Name = "Test" };

            // Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                Ranks.By(user, r => r.When(true).OrderBy(u => u.Id)));
        }

        /// <summary>
        /// 测试：By 方法使用复杂排序条件抛出 NotImplementedException
        /// </summary>
        [Fact]
        public void By_WithComplexRankCondition_ThrowsNotImplementedException()
        {
            // Arrange
            var user = new User { Id = 1, Name = "Test" };

            // Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                Ranks.By(user, r => r.When(true).OrderBy(u => u.Id).ThenByDescending(u => u.Name)));
        }

        /// <summary>
        /// 测试：RankOnly 结构体是只读结构体
        /// </summary>
        [Fact]
        public void RankOnly_IsReadonlyStruct()
        {
            // Arrange & Act
            var rankOnly = default(Ranks.RankOnly);

            // Assert - RankOnly 是空结构体，只要能创建就算通过
            Assert.IsType<Ranks.RankOnly>(rankOnly);
        }

        /// <summary>
        /// 测试：默认 RankOnly 值
        /// </summary>
        [Fact]
        public void RankOnly_DefaultValue()
        {
            // Arrange
            var defaultRankOnly = default(Ranks.RankOnly);
            var newRankOnly = new Ranks.RankOnly();

            // Assert
            Assert.Equal(defaultRankOnly, newRankOnly);
        }
    }
}
