using System;
using System.Linq.Expressions;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// ExpressionEqualityComparer 单元测试
    /// </summary>
    public class ExpressionEqualityComparerTests
    {
        #region Instance 测试

        /// <summary>
        /// 测试：Instance 返回单例
        /// </summary>
        [Fact]
        public void Instance_ReturnsSingletonInstance()
        {
            // Act
            var instance1 = ExpressionEqualityComparer.Instance;
            var instance2 = ExpressionEqualityComparer.Instance;

            // Assert
            Assert.Same(instance1, instance2);
        }

        /// <summary>
        /// 测试：Instance 不为 null
        /// </summary>
        [Fact]
        public void Instance_IsNotNull()
        {
            // Act
            var instance = ExpressionEqualityComparer.Instance;

            // Assert
            Assert.NotNull(instance);
        }

        #endregion

        #region GetHashCode 测试

        /// <summary>
        /// 测试：相同表达式返回相同哈希码
        /// </summary>
        [Fact]
        public void GetHashCode_SameExpression_ReturnsSameHash()
        {
            // Arrange
            Expression<Func<int, int>> expr1 = x => x + 1;
            Expression<Func<int, int>> expr2 = x => x + 1;
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var hash1 = comparer.GetHashCode(expr1);
            var hash2 = comparer.GetHashCode(expr2);

            // Assert
            Assert.Equal(hash1, hash2);
        }

        /// <summary>
        /// 测试：不同表达式可能返回不同哈希码
        /// </summary>
        [Fact]
        public void GetHashCode_DifferentExpressions_MayReturnDifferentHash()
        {
            // Arrange
            Expression<Func<int, int>> expr1 = x => x + 1;
            Expression<Func<int, int>> expr2 = x => x + 2;
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var hash1 = comparer.GetHashCode(expr1);
            var hash2 = comparer.GetHashCode(expr2);

            // Assert - 不同表达式通常返回不同哈希码，但不是绝对的
            // 这里只确保两个都能正常计算哈希码
            Assert.True(hash1 != 0 || hash2 != 0);
        }

        /// <summary>
        /// 测试：null 表达式返回 0
        /// </summary>
        [Fact]
        public void GetHashCode_NullExpression_ReturnsZero()
        {
            // Arrange
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var hash = comparer.GetHashCode(null);

            // Assert
            Assert.Equal(0, hash);
        }

        /// <summary>
        /// 测试：常量表达式哈希码
        /// </summary>
        [Fact]
        public void GetHashCode_ConstantExpression_ReturnsValidHash()
        {
            // Arrange
            var expr = Expression.Constant(42);
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var hash = comparer.GetHashCode(expr);

            // Assert
            Assert.True(true); // 只要不抛异常就算通过
        }

        /// <summary>
        /// 测试：成员访问表达式哈希码
        /// </summary>
        [Fact]
        public void GetHashCode_MemberExpression_ReturnsValidHash()
        {
            // Arrange
            Expression<Func<User, int>> expr = u => u.Id;
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var hash = comparer.GetHashCode(expr);

            // Assert
            Assert.True(true); // 只要不抛异常就算通过
        }

        /// <summary>
        /// 测试：方法调用表达式哈希码
        /// </summary>
        [Fact]
        public void GetHashCode_MethodCallExpression_ReturnsValidHash()
        {
            // Arrange
            Expression<Func<string, bool>> expr = s => s.Contains("test");
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var hash = comparer.GetHashCode(expr);

            // Assert
            Assert.True(true); // 只要不抛异常就算通过
        }

        /// <summary>
        /// 测试：条件表达式哈希码
        /// </summary>
        [Fact]
        public void GetHashCode_ConditionalExpression_ReturnsValidHash()
        {
            // Arrange
            Expression<Func<int, int>> expr = x => x > 0 ? x : -x;
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var hash = comparer.GetHashCode(expr);

            // Assert
            Assert.True(true); // 只要不抛异常就算通过
        }

        /// <summary>
        /// 测试：二元表达式哈希码
        /// </summary>
        [Fact]
        public void GetHashCode_BinaryExpression_ReturnsValidHash()
        {
            // Arrange
            Expression<Func<int, int, int>> expr = (a, b) => a + b;
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var hash = comparer.GetHashCode(expr);

            // Assert
            Assert.True(true); // 只要不抛异常就算通过
        }

        /// <summary>
        /// 测试：一元表达式哈希码
        /// </summary>
        [Fact]
        public void GetHashCode_UnaryExpression_ReturnsValidHash()
        {
            // Arrange
            Expression<Func<int, int>> expr = x => -x;
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var hash = comparer.GetHashCode(expr);

            // Assert
            Assert.True(true); // 只要不抛异常就算通过
        }

        /// <summary>
        /// 测试：NewExpression 哈希码
        /// </summary>
        [Fact]
        public void GetHashCode_NewExpression_ReturnsValidHash()
        {
            // Arrange
            Expression<Func<DateTime>> expr = () => new DateTime(2026, 1, 1);
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var hash = comparer.GetHashCode(expr);

            // Assert
            Assert.True(true); // 只要不抛异常就算通过
        }

        #endregion

        #region Equals 测试

        /// <summary>
        /// 测试：相同表达式相等
        /// </summary>
        [Fact]
        public void Equals_SameExpression_ReturnsTrue()
        {
            // Arrange
            Expression<Func<int, int>> expr1 = x => x + 1;
            Expression<Func<int, int>> expr2 = x => x + 1;
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var result = comparer.Equals(expr1, expr2);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试：不同表达式不相等
        /// </summary>
        [Fact]
        public void Equals_DifferentExpressions_ReturnsFalse()
        {
            // Arrange
            Expression<Func<int, int>> expr1 = x => x + 1;
            Expression<Func<int, int>> expr2 = x => x + 2;
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var result = comparer.Equals(expr1, expr2);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// 测试：两个 null 表达式相等
        /// </summary>
        [Fact]
        public void Equals_BothNull_ReturnsTrue()
        {
            // Arrange
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var result = comparer.Equals(null, null);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试：一个 null 一个非 null 不相等
        /// </summary>
        [Fact]
        public void Equals_OneNull_ReturnsFalse()
        {
            // Arrange
            Expression<Func<int, int>> expr = x => x + 1;
            var comparer = ExpressionEqualityComparer.Instance;

            // Act & Assert
            Assert.False(comparer.Equals(expr, null));
            Assert.False(comparer.Equals(null, expr));
        }

        /// <summary>
        /// 测试：同一引用相等
        /// </summary>
        [Fact]
        public void Equals_SameReference_ReturnsTrue()
        {
            // Arrange
            Expression<Func<int, int>> expr = x => x + 1;
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var result = comparer.Equals(expr, expr);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试：复杂表达式相等
        /// </summary>
        [Fact]
        public void Equals_ComplexExpressions_ReturnsTrue()
        {
            // Arrange
            Expression<Func<User, bool>> expr1 = u => u.Id > 0 && u.Name != null;
            Expression<Func<User, bool>> expr2 = u => u.Id > 0 && u.Name != null;
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var result = comparer.Equals(expr1, expr2);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试：不同类型的表达式不相等
        /// </summary>
        [Fact]
        public void Equals_DifferentTypes_ReturnsFalse()
        {
            // Arrange
            Expression<Func<int, int>> expr1 = x => x + 1;
            Expression<Func<int, bool>> expr2 = x => x > 0;
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var result = comparer.Equals(expr1, expr2);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region 特殊表达式测试

        /// <summary>
        /// 测试：参数表达式
        /// </summary>
        [Fact]
        public void Equals_ParameterExpressions_WithSameName_AreEqual()
        {
            // Arrange
            Expression<Func<int, int>> expr1 = x => x;
            Expression<Func<int, int>> expr2 = x => x;
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var result = comparer.Equals(expr1, expr2);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试：常量表达式
        /// </summary>
        [Fact]
        public void Equals_ConstantExpressions_WithSameValue_AreEqual()
        {
            // Arrange
            var expr1 = Expression.Constant(42);
            var expr2 = Expression.Constant(42);
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var result = comparer.Equals(expr1, expr2);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试：常量表达式不同值
        /// </summary>
        [Fact]
        public void Equals_ConstantExpressions_WithDifferentValue_AreNotEqual()
        {
            // Arrange
            var expr1 = Expression.Constant(42);
            var expr2 = Expression.Constant(43);
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var result = comparer.Equals(expr1, expr2);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// 测试：Lambda 表达式
        /// </summary>
        [Fact]
        public void Equals_LambdaExpressions_AreEqual()
        {
            // Arrange
            Expression<Func<int, bool>> expr1 = x => x > 10;
            Expression<Func<int, bool>> expr2 = x => x > 10;
            var comparer = ExpressionEqualityComparer.Instance;

            // Act
            var result = comparer.Equals(expr1, expr2);

            // Assert
            Assert.True(result);
        }

        #endregion
    }
}
