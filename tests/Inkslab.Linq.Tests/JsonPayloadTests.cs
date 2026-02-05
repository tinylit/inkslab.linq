using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// JsonPayload 类单元测试
    /// </summary>
    public class JsonPayloadTests
    {
        #region 构造函数测试

        /// <summary>
        /// 测试：使用有效 JSON 字符串创建 JsonPayload
        /// </summary>
        [Fact]
        public void Constructor_WithValidJson_CreatesInstance()
        {
            // Arrange
            string json = "{\"name\":\"test\"}";

            // Act
            var payload = new JsonPayload(json);

            // Assert
            Assert.Equal(json, payload.ToString());
        }

        /// <summary>
        /// 测试：使用 null 创建 JsonPayload
        /// </summary>
        [Fact]
        public void Constructor_WithNull_CreatesInstance()
        {
            // Arrange & Act
            var payload = new JsonPayload(null);

            // Assert
            Assert.Null(payload.ToString());
        }

        /// <summary>
        /// 测试：使用空字符串创建 JsonPayload
        /// </summary>
        [Fact]
        public void Constructor_WithEmptyString_CreatesInstance()
        {
            // Arrange & Act
            var payload = new JsonPayload(string.Empty);

            // Assert
            Assert.Equal(string.Empty, payload.ToString());
        }

        #endregion

        #region ToString 测试

        /// <summary>
        /// 测试：ToString 返回原始 JSON 字符串
        /// </summary>
        [Fact]
        public void ToString_ReturnsOriginalJson()
        {
            // Arrange
            string json = "{\"id\":1,\"name\":\"test\",\"active\":true}";
            var payload = new JsonPayload(json);

            // Act
            var result = payload.ToString();

            // Assert
            Assert.Equal(json, result);
        }

        /// <summary>
        /// 测试：ToString 支持复杂 JSON
        /// </summary>
        [Fact]
        public void ToString_SupportsComplexJson()
        {
            // Arrange
            string json = "{\"users\":[{\"id\":1,\"name\":\"user1\"},{\"id\":2,\"name\":\"user2\"}],\"total\":2}";
            var payload = new JsonPayload(json);

            // Act
            var result = payload.ToString();

            // Assert
            Assert.Equal(json, result);
        }

        #endregion

        #region 隐式转换测试

        /// <summary>
        /// 测试：字符串隐式转换为 JsonPayload
        /// </summary>
        [Fact]
        public void ImplicitConversion_FromString_CreatesJsonPayload()
        {
            // Arrange
            string json = "{\"name\":\"test\"}";

            // Act
            JsonPayload payload = json;

            // Assert
            Assert.Equal(json, payload.ToString());
        }

        /// <summary>
        /// 测试：null 字符串隐式转换为 JsonPayload
        /// </summary>
        [Fact]
        public void ImplicitConversion_FromNullString_CreatesJsonPayload()
        {
            // Arrange
            string json = null;

            // Act
            JsonPayload payload = json;

            // Assert
            Assert.Null(payload.ToString());
        }

        #endregion
    }

    /// <summary>
    /// JsonbPayload 类单元测试
    /// </summary>
    public class JsonbPayloadTests
    {
        #region 构造函数测试

        /// <summary>
        /// 测试：使用有效 JSON 字符串创建 JsonbPayload
        /// </summary>
        [Fact]
        public void Constructor_WithValidJson_CreatesInstance()
        {
            // Arrange
            string json = "{\"name\":\"test\"}";

            // Act
            var payload = new JsonbPayload(json);

            // Assert
            Assert.Equal(json, payload.ToString());
        }

        /// <summary>
        /// 测试：使用 null 创建 JsonbPayload
        /// </summary>
        [Fact]
        public void Constructor_WithNull_CreatesInstance()
        {
            // Arrange & Act
            var payload = new JsonbPayload(null);

            // Assert
            Assert.Null(payload.ToString());
        }

        /// <summary>
        /// 测试：使用空字符串创建 JsonbPayload
        /// </summary>
        [Fact]
        public void Constructor_WithEmptyString_CreatesInstance()
        {
            // Arrange & Act
            var payload = new JsonbPayload(string.Empty);

            // Assert
            Assert.Equal(string.Empty, payload.ToString());
        }

        #endregion

        #region ToString 测试

        /// <summary>
        /// 测试：ToString 返回原始 JSON 字符串
        /// </summary>
        [Fact]
        public void ToString_ReturnsOriginalJson()
        {
            // Arrange
            string json = "{\"id\":1,\"name\":\"test\",\"active\":true}";
            var payload = new JsonbPayload(json);

            // Act
            var result = payload.ToString();

            // Assert
            Assert.Equal(json, result);
        }

        /// <summary>
        /// 测试：ToString 支持复杂 JSON
        /// </summary>
        [Fact]
        public void ToString_SupportsComplexJson()
        {
            // Arrange
            string json = "{\"users\":[{\"id\":1,\"name\":\"user1\"},{\"id\":2,\"name\":\"user2\"}],\"total\":2}";
            var payload = new JsonbPayload(json);

            // Act
            var result = payload.ToString();

            // Assert
            Assert.Equal(json, result);
        }

        #endregion

        #region 隐式转换测试

        /// <summary>
        /// 测试：字符串隐式转换为 JsonbPayload
        /// </summary>
        [Fact]
        public void ImplicitConversion_FromString_CreatesJsonbPayload()
        {
            // Arrange
            string json = "{\"name\":\"test\"}";

            // Act
            JsonbPayload payload = json;

            // Assert
            Assert.Equal(json, payload.ToString());
        }

        /// <summary>
        /// 测试：null 字符串隐式转换为 JsonbPayload
        /// </summary>
        [Fact]
        public void ImplicitConversion_FromNullString_CreatesJsonbPayload()
        {
            // Arrange
            string json = null;

            // Act
            JsonbPayload payload = json;

            // Assert
            Assert.Null(payload.ToString());
        }

        #endregion
    }
}
