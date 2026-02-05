using System;
using System.Collections.Generic;
using System.Data;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// CommandSql 类单元测试
    /// </summary>
    public class CommandSqlTests
    {
        #region 构造函数测试

        /// <summary>
        /// 测试：使用有效 SQL 创建 CommandSql 对象
        /// </summary>
        [Fact]
        public void Constructor_WithValidSql_CreatesInstance()
        {
            // Arrange
            string sql = "SELECT * FROM users";

            // Act
            var command = new CommandSql(sql);

            // Assert
            Assert.Equal(sql, command.Text);
            Assert.NotNull(command.Parameters);
            Assert.Empty(command.Parameters);
            Assert.Null(command.Timeout);
            Assert.Equal(CommandType.Text, command.CommandType);
        }

        /// <summary>
        /// 测试：使用 SQL 和参数创建 CommandSql 对象
        /// </summary>
        [Fact]
        public void Constructor_WithSqlAndParameters_CreatesInstance()
        {
            // Arrange
            string sql = "SELECT * FROM users WHERE id = @id";
            var parameters = new Dictionary<string, object> { ["id"] = 1 };

            // Act
            var command = new CommandSql(sql, parameters);

            // Assert
            Assert.Equal(sql, command.Text);
            Assert.Same(parameters, command.Parameters);
            Assert.Null(command.Timeout);
        }

        /// <summary>
        /// 测试：使用 SQL、参数和超时时间创建 CommandSql 对象
        /// </summary>
        [Fact]
        public void Constructor_WithSqlParametersAndTimeout_CreatesInstance()
        {
            // Arrange
            string sql = "SELECT * FROM users WHERE id = @id";
            var parameters = new Dictionary<string, object> { ["id"] = 1 };
            int timeout = 30;

            // Act
            var command = new CommandSql(sql, parameters, timeout);

            // Assert
            Assert.Equal(sql, command.Text);
            Assert.Same(parameters, command.Parameters);
            Assert.Equal(timeout, command.Timeout);
        }

        /// <summary>
        /// 测试：使用空 SQL 抛出异常
        /// </summary>
        [Fact]
        public void Constructor_WithEmptySql_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new CommandSql(string.Empty));
        }

        /// <summary>
        /// 测试：使用 null SQL 抛出异常
        /// </summary>
        [Fact]
        public void Constructor_WithNullSql_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new CommandSql(null));
        }

        #endregion

        #region ToString 测试

        /// <summary>
        /// 测试：无参数时 ToString 返回原始 SQL
        /// </summary>
        [Fact]
        public void ToString_WithoutParameters_ReturnsOriginalSql()
        {
            // Arrange
            string sql = "SELECT * FROM users";
            var command = new CommandSql(sql);

            // Act
            var result = command.ToString();

            // Assert
            Assert.Equal(sql, result);
        }

        /// <summary>
        /// 测试：ToString 替换参数值
        /// </summary>
        [Fact]
        public void ToString_WithParameters_ReplacesParameterValues()
        {
            // Arrange
            string sql = "SELECT * FROM users WHERE id = @id";
            var parameters = new Dictionary<string, object> { ["id"] = 1 };
            var command = new CommandSql(sql, parameters);

            // Act
            var result = command.ToString();

            // Assert
            Assert.Contains("1", result);
        }

        /// <summary>
        /// 测试：ToString 支持多个参数
        /// </summary>
        [Fact]
        public void ToString_WithMultipleParameters_ReplacesAllParameterValues()
        {
            // Arrange
            string sql = "SELECT * FROM users WHERE id = @id AND name = @name";
            var parameters = new Dictionary<string, object>
            {
                ["id"] = 1,
                ["name"] = "Test"
            };
            var command = new CommandSql(sql, parameters);

            // Act
            var result = command.ToString();

            // Assert
            Assert.Contains("1", result);
            Assert.Contains("Test", result);
        }

        #endregion

        #region 隐式转换测试

        /// <summary>
        /// 测试：字符串隐式转换为 CommandSql
        /// </summary>
        [Fact]
        public void ImplicitConversion_FromString_CreatesCommandSql()
        {
            // Arrange
            string sql = "SELECT 1";

            // Act
            CommandSql command = sql;

            // Assert
            Assert.Equal(sql, command.Text);
            Assert.Empty(command.Parameters);
        }

        #endregion

        #region Timeout 测试

        /// <summary>
        /// 测试：设置 Timeout 属性
        /// </summary>
        [Fact]
        public void Timeout_CanBeSetAndGet()
        {
            // Arrange
            var command = new CommandSql("SELECT 1")
            {
                // Act
                Timeout = 60
            };

            // Assert
            Assert.Equal(60, command.Timeout);
        }

        #endregion
    }

    /// <summary>
    /// StoredProcedureCommandSql 类单元测试
    /// </summary>
    public class StoredProcedureCommandSqlTests
    {
        /// <summary>
        /// 测试：创建存储过程命令
        /// </summary>
        [Fact]
        public void Constructor_CreatesStoredProcedureCommand()
        {
            // Arrange
            string procName = "sp_GetUsers";

            // Act
            var command = new StoredProcedureCommandSql(procName);

            // Assert
            Assert.Equal(procName, command.Text);
            Assert.Equal(CommandType.StoredProcedure, command.CommandType);
        }

        /// <summary>
        /// 测试：存储过程 ToString 格式
        /// </summary>
        [Fact]
        public void ToString_ReturnsExecFormat()
        {
            // Arrange
            string procName = "sp_GetUsers";
            var command = new StoredProcedureCommandSql(procName);

            // Act
            var result = command.ToString();

            // Assert
            Assert.StartsWith("EXEC ", result);
            Assert.Contains(procName, result);
        }

        /// <summary>
        /// 测试：存储过程带参数的 ToString 格式
        /// </summary>
        [Fact]
        public void ToString_WithParameters_ReturnsExecWithParams()
        {
            // Arrange
            string procName = "sp_GetUserById";
            var parameters = new Dictionary<string, object>
            {
                ["id"] = 1,
                ["name"] = "Test"
            };
            var command = new StoredProcedureCommandSql(procName, parameters);

            // Act
            var result = command.ToString();

            // Assert
            Assert.StartsWith("EXEC ", result);
            Assert.Contains(procName, result);
            Assert.Contains("@id", result);
            Assert.Contains("@name", result);
        }
    }

    /// <summary>
    /// CommandSql&lt;TElement&gt; 类单元测试
    /// </summary>
    public class CommandSqlGenericTests
    {
        /// <summary>
        /// 测试：创建泛型 CommandSql 对象
        /// </summary>
        [Fact]
        public void Constructor_CreatesGenericCommandSql()
        {
            // Arrange
            var baseSql = new CommandSql("SELECT * FROM users");

            // Act
            var command = new CommandSql<User>(baseSql, RowStyle.First);

            // Assert
            Assert.Equal("SELECT * FROM users", command.Text);
            Assert.Equal(RowStyle.First, command.RowStyle);
            Assert.False(command.HasDefaultValue);
            Assert.Null(command.DefaultValue);
            Assert.False(command.CustomError);
            Assert.Null(command.NoElementError);
        }

        /// <summary>
        /// 测试：创建带默认值的泛型 CommandSql
        /// </summary>
        [Fact]
        public void Constructor_WithDefaultValue_SetsDefaultValue()
        {
            // Arrange
            var baseSql = new CommandSql("SELECT * FROM users");
            var defaultUser = new User { Id = -1, Name = "Default" };

            // Act
            var command = new CommandSql<User>(baseSql, RowStyle.FirstOrDefault, hasDefaultValue: true, defaultValue: defaultUser);

            // Assert
            Assert.True(command.HasDefaultValue);
            Assert.Same(defaultUser, command.DefaultValue);
        }

        /// <summary>
        /// 测试：创建带自定义错误的泛型 CommandSql
        /// </summary>
        [Fact]
        public void Constructor_WithCustomError_SetsCustomError()
        {
            // Arrange
            var baseSql = new CommandSql("SELECT * FROM users");
            string errorMessage = "No user found";

            // Act
            var command = new CommandSql<User>(baseSql, RowStyle.Single, customError: true, noElementError: errorMessage);

            // Assert
            Assert.True(command.CustomError);
            Assert.Equal(errorMessage, command.NoElementError);
        }

        /// <summary>
        /// 测试：RowStyle 枚举值
        /// </summary>
        [Theory]
        [InlineData(RowStyle.First)]
        [InlineData(RowStyle.FirstOrDefault)]
        [InlineData(RowStyle.Single)]
        [InlineData(RowStyle.SingleOrDefault)]
        public void Constructor_WithRowStyle_SetsRowStyle(RowStyle rowStyle)
        {
            // Arrange
            var baseSql = new CommandSql("SELECT * FROM users");

            // Act
            var command = new CommandSql<User>(baseSql, rowStyle);

            // Assert
            Assert.Equal(rowStyle, command.RowStyle);
        }
    }
}
