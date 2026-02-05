using System;
using System.Data;
using Inkslab.Linq.Exceptions;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// DSyntaxErrorException 单元测试
    /// </summary>
    public class DSyntaxErrorExceptionTests
    {
        /// <summary>
        /// 测试：使用默认构造函数创建异常
        /// </summary>
        [Fact]
        public void Constructor_Default_CreatesException()
        {
            // Arrange & Act
            var exception = new DSyntaxErrorException();

            // Assert
            Assert.NotNull(exception);
            Assert.IsAssignableFrom<SyntaxErrorException>(exception);
        }

        /// <summary>
        /// 测试：使用消息创建异常
        /// </summary>
        [Fact]
        public void Constructor_WithMessage_CreatesException()
        {
            // Arrange
            string message = "Syntax error in SQL";

            // Act
            var exception = new DSyntaxErrorException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }

        /// <summary>
        /// 测试：使用消息和内部异常创建异常
        /// </summary>
        [Fact]
        public void Constructor_WithMessageAndInnerException_CreatesException()
        {
            // Arrange
            string message = "Syntax error in SQL";
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new DSyntaxErrorException(message, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Same(innerException, exception.InnerException);
        }

        /// <summary>
        /// 测试：DSyntaxErrorException 继承自 SyntaxErrorException
        /// </summary>
        [Fact]
        public void DSyntaxErrorException_InheritsFromSyntaxErrorException()
        {
            // Arrange
            var exception = new DSyntaxErrorException("Test");

            // Assert
            Assert.IsType<DSyntaxErrorException>(exception);
            Assert.IsAssignableFrom<SyntaxErrorException>(exception);
            Assert.IsAssignableFrom<DataException>(exception);
            Assert.IsAssignableFrom<SystemException>(exception);
            Assert.IsAssignableFrom<Exception>(exception);
        }

        /// <summary>
        /// 测试：异常可以被抛出和捕获
        /// </summary>
        [Fact]
        public void DSyntaxErrorException_CanBeThrownAndCaught()
        {
            // Arrange
            string message = "Invalid SQL syntax";

            // Act & Assert
            void act() => throw new DSyntaxErrorException(message);
            var exception = Assert.Throws<DSyntaxErrorException>(act);

            Assert.Equal(message, exception.Message);
        }

        /// <summary>
        /// 测试：可以被基类捕获
        /// </summary>
        [Fact]
        public void DSyntaxErrorException_CanBeCaughtAsSyntaxErrorException()
        {
            // Arrange
            string message = "Invalid SQL syntax";

            // Act
            Exception caughtException = null;
            try
            {
                throw new DSyntaxErrorException(message);
            }
            catch (SyntaxErrorException ex)
            {
                caughtException = ex;
            }

            // Assert - 可以被基类 SyntaxErrorException 捕获
            Assert.NotNull(caughtException);
            Assert.IsType<DSyntaxErrorException>(caughtException);
        }
    }

    /// <summary>
    /// NoElementException 单元测试
    /// </summary>
    public class NoElementExceptionTests
    {
        /// <summary>
        /// 测试：使用消息创建异常
        /// </summary>
        [Fact]
        public void Constructor_WithMessage_CreatesException()
        {
            // Arrange
            string message = "No element found";

            // Act
            var exception = new NoElementException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(1, exception.ErrorCode); // 默认错误码
        }

        /// <summary>
        /// 测试：使用消息和错误码创建异常
        /// </summary>
        [Fact]
        public void Constructor_WithMessageAndErrorCode_CreatesException()
        {
            // Arrange
            string message = "No element found";
            int errorCode = 404;

            // Act
            var exception = new NoElementException(message, errorCode);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(errorCode, exception.ErrorCode);
        }

        /// <summary>
        /// 测试：使用不同错误码
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(404)]
        [InlineData(-1)]
        public void Constructor_WithVariousErrorCodes_CreatesException(int errorCode)
        {
            // Arrange
            string message = "Test message";

            // Act
            var exception = new NoElementException(message, errorCode);

            // Assert
            Assert.Equal(errorCode, exception.ErrorCode);
        }

        /// <summary>
        /// 测试：异常可以被抛出和捕获
        /// </summary>
        [Fact]
        public void NoElementException_CanBeThrownAndCaught()
        {
            // Arrange
            string message = "Record not found";

            // Act & Assert
            void act() => throw new NoElementException(message);
            var exception = Assert.Throws<NoElementException>(act);

            Assert.Equal(message, exception.Message);
        }

        /// <summary>
        /// 测试：使用各种消息
        /// </summary>
        [Theory]
        [InlineData("No user found")]
        [InlineData("Record does not exist")]
        [InlineData("")]
        [InlineData("用户不存在")]
        public void Constructor_WithVariousMessages_CreatesException(string message)
        {
            // Arrange & Act
            var exception = new NoElementException(message);

            // Assert
            Assert.Equal(message, exception.Message);
        }
    }
}
