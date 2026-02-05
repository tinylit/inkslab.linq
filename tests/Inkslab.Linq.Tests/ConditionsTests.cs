using System;
using System.Linq.Expressions;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// Conditions 类单元测试
    /// 测试表达式组合的各种场景
    /// </summary>
    public class ConditionsTests
    {
        #region True/False 测试

        /// <summary>
        /// 测试：True 方法返回始终为真的表达式
        /// </summary>
        [Fact]
        public void True_ReturnsAlwaysTrueExpression()
        {
            // Arrange & Act
            var expression = Conditions.True<int>();

            // Assert
            Assert.NotNull(expression);
            var compiled = expression.Compile();
            Assert.True(compiled(0));
            Assert.True(compiled(100));
            Assert.True(compiled(-1));
        }

        /// <summary>
        /// 测试：False 方法返回始终为假的表达式
        /// </summary>
        [Fact]
        public void False_ReturnsAlwaysFalseExpression()
        {
            // Arrange & Act
            var expression = Conditions.False<int>();

            // Assert
            Assert.NotNull(expression);
            var compiled = expression.Compile();
            Assert.False(compiled(0));
            Assert.False(compiled(100));
            Assert.False(compiled(-1));
        }

        /// <summary>
        /// 测试：True 方法支持复杂类型
        /// </summary>
        [Fact]
        public void True_SupportsComplexTypes()
        {
            // Arrange & Act
            var expression = Conditions.True<User>();

            // Assert
            var compiled = expression.Compile();
            Assert.True(compiled(new User { Id = 1, Name = "Test" }));
            Assert.True(compiled(null));
        }

        /// <summary>
        /// 测试：False 方法支持复杂类型
        /// </summary>
        [Fact]
        public void False_SupportsComplexTypes()
        {
            // Arrange & Act
            var expression = Conditions.False<User>();

            // Assert
            var compiled = expression.Compile();
            Assert.False(compiled(new User { Id = 1, Name = "Test" }));
            Assert.False(compiled(null));
        }

        #endregion

        #region Fragment 测试

        /// <summary>
        /// 测试：Fragment 方法返回输入的表达式
        /// </summary>
        [Fact]
        public void Fragment_ReturnsInputExpression()
        {
            // Arrange
            Expression<Func<int, bool>> predicate = x => x > 0;

            // Act
            var result = Conditions.Fragment(predicate);

            // Assert
            Assert.Same(predicate, result);
        }

        /// <summary>
        /// 测试：Fragment 方法在参数为 null 时抛出异常
        /// </summary>
        [Fact]
        public void Fragment_ThrowsArgumentNullException_WhenPredicateIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => Conditions.Fragment<int>(null));
        }

        /// <summary>
        /// 测试：Fragment 方法支持复杂表达式
        /// </summary>
        [Fact]
        public void Fragment_SupportsComplexExpressions()
        {
            // Arrange
            Expression<Func<User, bool>> predicate = x => x.Id > 0 && x.Name != null;

            // Act
            var result = Conditions.Fragment(predicate);

            // Assert
            Assert.Same(predicate, result);
            var compiled = result.Compile();
            Assert.True(compiled(new User { Id = 1, Name = "Test" }));
            Assert.False(compiled(new User { Id = 0, Name = "Test" }));
            Assert.False(compiled(new User { Id = 1, Name = null }));
        }

        #endregion

        #region And 测试

        /// <summary>
        /// 测试：And 方法在左节点为 null 时返回右节点
        /// </summary>
        [Fact]
        public void And_ReturnsRightNode_WhenLeftNodeIsNull()
        {
            // Arrange
            Expression<Func<int, bool>> rightNode = x => x > 0;

            // Act
            var result = Conditions.And(null, rightNode);

            // Assert
            Assert.Same(rightNode, result);
        }

        /// <summary>
        /// 测试：And 方法在右节点为 null 时返回左节点
        /// </summary>
        [Fact]
        public void And_ReturnsLeftNode_WhenRightNodeIsNull()
        {
            // Arrange
            Expression<Func<int, bool>> leftNode = x => x > 0;

            // Act
            var result = Conditions.And(leftNode, null);

            // Assert
            Assert.Same(leftNode, result);
        }

        /// <summary>
        /// 测试：And 方法合并两个条件
        /// </summary>
        [Fact]
        public void And_CombinesTwoConditions()
        {
            // Arrange
            Expression<Func<int, bool>> leftNode = x => x > 0;
            Expression<Func<int, bool>> rightNode = x => x < 100;

            // Act
            var result = Conditions.And(leftNode, rightNode);

            // Assert
            var compiled = result.Compile();
            Assert.True(compiled(50));
            Assert.False(compiled(0));
            Assert.False(compiled(100));
            Assert.False(compiled(-1));
        }

        /// <summary>
        /// 测试：And 方法在左节点为 True 常量时返回右节点
        /// </summary>
        [Fact]
        public void And_ReturnsRightNode_WhenLeftNodeIsTrue()
        {
            // Arrange
            Expression<Func<int, bool>> trueNode = Conditions.True<int>();
            Expression<Func<int, bool>> rightNode = x => x > 0;

            // Act
            var result = Conditions.And(trueNode, rightNode);

            // Assert
            Assert.Same(rightNode, result);
        }

        /// <summary>
        /// 测试：And 方法在左节点为 False 常量时返回左节点
        /// </summary>
        [Fact]
        public void And_ReturnsLeftNode_WhenLeftNodeIsFalse()
        {
            // Arrange
            Expression<Func<int, bool>> falseNode = Conditions.False<int>();
            Expression<Func<int, bool>> rightNode = x => x > 0;

            // Act
            var result = Conditions.And(falseNode, rightNode);

            // Assert
            // 结果应该仍然是 falseNode
            Assert.Same(falseNode, result);
        }

        /// <summary>
        /// 测试：And 方法支持多次链式调用
        /// </summary>
        [Fact]
        public void And_SupportsChaining()
        {
            // Arrange
            Expression<Func<int, bool>> cond1 = x => x > 0;
            Expression<Func<int, bool>> cond2 = x => x < 100;
            Expression<Func<int, bool>> cond3 = x => x % 2 == 0;

            // Act
            var result = cond1.And(cond2).And(cond3);

            // Assert
            var compiled = result.Compile();
            Assert.True(compiled(50)); // 50 > 0 && 50 < 100 && 50 % 2 == 0
            Assert.False(compiled(51)); // 51 % 2 != 0
            Assert.False(compiled(-2)); // -2 <= 0
            Assert.False(compiled(102)); // 102 >= 100
        }

        #endregion

        #region Or 测试

        /// <summary>
        /// 测试：Or 方法在左节点为 null 时返回右节点
        /// </summary>
        [Fact]
        public void Or_ReturnsRightNode_WhenLeftNodeIsNull()
        {
            // Arrange
            Expression<Func<int, bool>> rightNode = x => x > 0;

            // Act
            var result = Conditions.Or(null, rightNode);

            // Assert
            Assert.Same(rightNode, result);
        }

        /// <summary>
        /// 测试：Or 方法在右节点为 null 时返回左节点
        /// </summary>
        [Fact]
        public void Or_ReturnsLeftNode_WhenRightNodeIsNull()
        {
            // Arrange
            Expression<Func<int, bool>> leftNode = x => x > 0;

            // Act
            var result = Conditions.Or(leftNode, null);

            // Assert
            Assert.Same(leftNode, result);
        }

        /// <summary>
        /// 测试：Or 方法合并两个条件
        /// </summary>
        [Fact]
        public void Or_CombinesTwoConditions()
        {
            // Arrange
            Expression<Func<int, bool>> leftNode = x => x < 0;
            Expression<Func<int, bool>> rightNode = x => x > 100;

            // Act
            var result = Conditions.Or(leftNode, rightNode);

            // Assert
            var compiled = result.Compile();
            Assert.True(compiled(-1));
            Assert.True(compiled(101));
            Assert.False(compiled(50));
            Assert.False(compiled(0));
            Assert.False(compiled(100));
        }

        /// <summary>
        /// 测试：Or 方法在左节点为 False 常量时返回右节点
        /// </summary>
        [Fact]
        public void Or_ReturnsRightNode_WhenLeftNodeIsFalse()
        {
            // Arrange
            Expression<Func<int, bool>> falseNode = Conditions.False<int>();
            Expression<Func<int, bool>> rightNode = x => x > 0;

            // Act
            var result = Conditions.Or(falseNode, rightNode);

            // Assert
            Assert.Same(rightNode, result);
        }

        /// <summary>
        /// 测试：Or 方法在左节点为 True 常量时返回左节点
        /// </summary>
        [Fact]
        public void Or_ReturnsLeftNode_WhenLeftNodeIsTrue()
        {
            // Arrange
            // 创建一个 body 是 ConstantExpression 且 value 是 true 的表达式
            Expression<Func<int, bool>> trueNode = Conditions.True<int>();
            Expression<Func<int, bool>> rightNode = x => x > 0;

            // Act
            var result = Conditions.Or(trueNode, rightNode);

            // Assert
            // 结果应该仍然是 trueNode
            Assert.Same(trueNode, result);
        }

        /// <summary>
        /// 测试：Or 方法支持多次链式调用
        /// </summary>
        [Fact]
        public void Or_SupportsChaining()
        {
            // Arrange
            Expression<Func<int, bool>> cond1 = x => x < 0;
            Expression<Func<int, bool>> cond2 = x => x > 100;
            Expression<Func<int, bool>> cond3 = x => x == 50;

            // Act
            var result = cond1.Or(cond2).Or(cond3);

            // Assert
            var compiled = result.Compile();
            Assert.True(compiled(-1)); // x < 0
            Assert.True(compiled(101)); // x > 100
            Assert.True(compiled(50)); // x == 50
            Assert.False(compiled(25)); // none of above
        }

        #endregion

        #region And + Or 组合测试

        /// <summary>
        /// 测试：And 和 Or 方法组合使用
        /// </summary>
        [Fact]
        public void AndOr_CombinedUsage()
        {
            // Arrange
            // (x > 0 AND x < 100) OR x == -999
            Expression<Func<int, bool>> cond1 = x => x > 0;
            Expression<Func<int, bool>> cond2 = x => x < 100;
            Expression<Func<int, bool>> cond3 = x => x == -999;

            // Act
            var result = cond1.And(cond2).Or(cond3);

            // Assert
            var compiled = result.Compile();
            Assert.True(compiled(50)); // x > 0 && x < 100
            Assert.True(compiled(-999)); // x == -999
            Assert.False(compiled(-1)); // none
            Assert.False(compiled(100)); // none
        }

        #endregion

        #region IsTrue/If/Conditional 异常测试

        /// <summary>
        /// 测试：IsTrue 方法抛出 NotImplementedException
        /// </summary>
        [Fact]
        public void IsTrue_ThrowsNotImplementedException()
        {
            // Arrange & Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                Conditions.IsTrue(new User(), x => x.Id > 0));
        }

        /// <summary>
        /// 测试：If 方法（两参数）抛出 NotImplementedException
        /// </summary>
        [Fact]
        public void If_TwoParams_ThrowsNotImplementedException()
        {
            // Arrange & Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                Conditions.If(true, true));
        }

        /// <summary>
        /// 测试：If 方法（三参数）抛出 NotImplementedException
        /// </summary>
        [Fact]
        public void If_ThreeParams_ThrowsNotImplementedException()
        {
            // Arrange & Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                Conditions.If(new User(), true, x => x.Id > 0));
        }

        /// <summary>
        /// 测试：Conditional 方法（三参数）抛出 NotImplementedException
        /// </summary>
        [Fact]
        public void Conditional_ThreeParams_ThrowsNotImplementedException()
        {
            // Arrange & Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                Conditions.Conditional(true, true, false));
        }

        /// <summary>
        /// 测试：Conditional 方法（四参数）抛出 NotImplementedException
        /// </summary>
        [Fact]
        public void Conditional_FourParams_ThrowsNotImplementedException()
        {
            // Arrange & Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                Conditions.Conditional(new User(), true, x => x.Id > 0, x => x.Id < 100));
        }

        #endregion
    }
}
