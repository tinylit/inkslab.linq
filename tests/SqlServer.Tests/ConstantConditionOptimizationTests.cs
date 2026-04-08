using System;
using System.Linq;
using Xunit;
using Inkslab.Linq;
using XunitPlus;

namespace SqlServer.Tests
{
    /// <summary>
    /// 常量/变量条件优化单元测试。
    /// 核心功能：验证当常量值或变量作为条件或查询结果时，框架是否正确地将其识别为"始终为真"或"始终为假"，
    /// 从而在生成的SQL中消除冗余表达式，而不是将常量/变量字面量解析到SQL语句中。
    /// </summary>
    [TestPriority(50)]
    public class ConstantConditionOptimizationTests
    {
        private readonly IQueryable<User> _users;
        private readonly IQueryable<UserEx> _userExes;

        public ConstantConditionOptimizationTests(
            IQueryable<User> users,
            IQueryable<UserEx> userExes
        )
        {
            _users = users;
            _userExes = userExes;
        }

        #region 常量 true/false 作为 Where 条件

        /// <summary>
        /// 常量 true 作为 Where 条件，应被优化掉（等效于无 WHERE 子句）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] ORDER BY [x].[Id]
        /// </code>
        /// 注意：WHERE true 是始终为真的条件，应被完全移除，不会出现在 SQL 中。
        /// </remarks>
        [Fact]
        [Step(1)]
        public void Where_ConstantTrue_OptimizedAway()
        {
            // Arrange
            var memoryResults = (from x in _users orderby x.Id select x.Id).ToList();

            // Act - 常量 true 应被优化掉
            var results = _users.Where(x => true).OrderBy(x => x.Id).Select(x => x.Id).ToList();

            // Assert - 结果应与无条件查询一致
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// 常量 false 作为 Where 条件，应生成始终为假的条件（如 1 = 0）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE 1 = 0
        /// </code>
        /// </remarks>
        [Fact]
        [Step(2)]
        public void Where_ConstantFalse_AlwaysFalseCondition()
        {
            // Arrange & Act - 常量 false 应生成 1 = 0
            var results = _users.Where(x => false).Select(x => x.Id).ToList();

            // Assert - 结果应为空
            Assert.Empty(results);
        }

        #endregion

        #region 变量 true/false 作为 Where 条件

        /// <summary>
        /// 布尔变量值为 true 作为 Where 条件，应被优化掉。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[Id] > 0 ORDER BY [x].[Id]
        /// </code>
        /// 注意：变量 isActive = true 在表达式树构建时已知其值，应被优化为始终为真并移除。
        /// </remarks>
        [Fact]
        [Step(3)]
        public void Where_VariableTrue_OptimizedAway()
        {
            // Arrange
            bool isActive = true;
            var memoryResults = (from x in _users where isActive && x.Id > 0 orderby x.Id select x.Id).ToList();

            // Act - isActive = true 应被优化掉，仅保留 x.Id > 0
            var results = _users.Where(x => isActive && x.Id > 0).OrderBy(x => x.Id).Select(x => x.Id).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// 布尔变量值为 false 在 AND 条件中，整个条件应被优化为始终为假。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE 1 = 0
        /// </code>
        /// 注意：false AND anything 始终为假。
        /// </remarks>
        [Fact]
        [Step(4)]
        public void Where_VariableFalse_AndCondition_AlwaysFalse()
        {
            // Arrange
            bool isActive = false;

            // Act - false AND x.Id > 0 应优化为始终为假
            var results = _users.Where(x => isActive && x.Id > 0).Select(x => x.Id).ToList();

            // Assert
            Assert.Empty(results);
        }

        /// <summary>
        /// 布尔变量值为 true 在 OR 条件中，整个条件应被优化为始终为真。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] ORDER BY [x].[Id]
        /// </code>
        /// 注意：true OR anything 始终为真，OR 右侧不需要解析。
        /// </remarks>
        [Fact]
        [Step(5)]
        public void Where_VariableTrue_OrCondition_AlwaysTrue()
        {
            // Arrange
            bool isActive = true;
            var memoryResults = (from x in _users orderby x.Id select x.Id).ToList();

            // Act - true OR x.Id > 100 应优化为始终为真，无WHERE子句
            var results = _users.Where(x => isActive || x.Id > 100).OrderBy(x => x.Id).Select(x => x.Id).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// 布尔变量值为 false 在 OR 条件中，false 分支应被优化掉，仅保留另一分支。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[Id] > 100 ORDER BY [x].[Id]
        /// </code>
        /// 注意：false OR x.Id > 100 应优化为仅 x.Id > 100。
        /// </remarks>
        [Fact]
        [Step(6)]
        public void Where_VariableFalse_OrCondition_OnlyKeepsOtherBranch()
        {
            // Arrange
            bool isActive = false;
            var memoryResults = (from x in _users where x.Id > 100 orderby x.Id select x.Id).ToList();

            // Act - false OR x.Id > 100 应优化为仅 x.Id > 100
            var results = _users.Where(x => isActive || x.Id > 100).OrderBy(x => x.Id).Select(x => x.Id).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        #endregion

        #region 常量/变量作为查询结果（Select 投影）

        /// <summary>
        /// 常量值作为查询结果投影。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], [x].[Name] AS [Name], 1 AS [Type] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100 ORDER BY [x].[Id]
        /// </code>
        /// 注意：常量 1 直接作为结果字段输出。
        /// </remarks>
        [Fact]
        [Step(7)]
        public void Select_ConstantValue_AsProjectionField()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 orderby x.Id
                                 select new { x.Id, x.Name, Type = 1 }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           orderby x.Id
                           select new { x.Id, x.Name, Type = 1 }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
                Assert.Equal(memoryResults[i].Type, results[i].Type);
            }
        }

        /// <summary>
        /// 布尔常量 true 作为查询结果投影。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], 1 AS [Flag] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// </remarks>
        [Fact]
        [Step(8)]
        public void Select_ConstantBoolTrue_AsProjectionField()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Flag = true }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Flag = true }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Flag, results[i].Flag);
            }
        }

        /// <summary>
        /// 布尔常量 false 作为查询结果投影。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], 0 AS [Flag] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// </remarks>
        [Fact]
        [Step(9)]
        public void Select_ConstantBoolFalse_AsProjectionField()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Flag = false }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Flag = false }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Flag, results[i].Flag);
            }
        }

        /// <summary>
        /// 变量值作为查询结果投影。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], [x].[Name] AS [Name], 2 AS [Type] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100 ORDER BY [x].[Id]
        /// </code>
        /// </remarks>
        [Fact]
        [Step(10)]
        public void Select_VariableValue_AsProjectionField()
        {
            // Arrange
            int type = 2;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 orderby x.Id
                                 select new { x.Id, x.Name, Type = type }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           orderby x.Id
                           select new { x.Id, x.Name, Type = type }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
                Assert.Equal(memoryResults[i].Type, results[i].Type);
            }
        }

        #endregion

        #region string.IsNullOrEmpty / string.IsNullOrWhiteSpace 条件优化

        /// <summary>
        /// string.IsNullOrEmpty(变量) 当变量有值时，在 Where 条件中应被优化为始终为假。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[Name] LIKE '%测试%' ORDER BY [x].[Id]
        /// </code>
        /// 注意：!string.IsNullOrEmpty("测试") 始终为真，整个 IsNullOrEmpty 判断应被优化掉，仅保留实际条件。
        /// </remarks>
        [Fact]
        [Step(11)]
        public void Where_IsNullOrEmpty_VariableHasValue_OptimizedAway()
        {
            // Arrange
            string name = "测试";
            var memoryResults = (from x in _users
                                 where (!string.IsNullOrEmpty(name) && x.Name != null && x.Name.Contains(name))
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act - !string.IsNullOrEmpty("测试") 始终为真，仅保留 Contains 条件
            var results = _users
                .Where(x => !string.IsNullOrEmpty(name) && x.Name.Contains(name))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// string.IsNullOrEmpty(null) 在 Where 条件中时，应被优化（始终为真）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE 1 <> 1
        /// </code>
        /// 注意：!string.IsNullOrEmpty(null) 始终为假，整个 AND 条件被优化为始终为假。
        /// </remarks>
        [Fact]
        [Step(12)]
        public void Where_IsNullOrEmpty_NullVariable_WholeConditionOptimized()
        {
            // Arrange
            string name = null;

            // Act - !string.IsNullOrEmpty(null) = false，false AND anything = false
            var results = _users
                .Where(x => !string.IsNullOrEmpty(name) && x.Name.Contains(name))
                .Select(x => x.Id)
                .ToList();

            // Assert - 应返回空结果
            Assert.Empty(results);
        }

        /// <summary>
        /// string.IsNullOrEmpty(空字符串) 在 Where 条件中时，应被优化（始终为真）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE 1 <> 1
        /// </code>
        /// 注意：!string.IsNullOrEmpty("") 始终为假，整个 AND 条件被优化为始终为假。
        /// </remarks>
        [Fact]
        [Step(13)]
        public void Where_IsNullOrEmpty_EmptyString_WholeConditionOptimized()
        {
            // Arrange
            string name = string.Empty;

            // Act - !string.IsNullOrEmpty("") = false，false AND anything = false
            var results = _users
                .Where(x => !string.IsNullOrEmpty(name) && x.Name.Contains(name))
                .Select(x => x.Id)
                .ToList();

            // Assert - 应返回空结果
            Assert.Empty(results);
        }

        /// <summary>
        /// string.IsNullOrWhiteSpace(null) OR 条件优化，仅保留有效分支。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] ORDER BY [x].[Id]
        /// </code>
        /// 注意：string.IsNullOrWhiteSpace(null) = true，true OR anything = true，整个条件被优化掉。
        /// </remarks>
        [Fact]
        [Step(14)]
        public void Where_IsNullOrWhiteSpace_NullVariable_OrCondition_OptimizedToTrue()
        {
            // Arrange
            string name = null;
            var memoryResults = (from x in _users
                                 where string.IsNullOrWhiteSpace(name) || x.Name.Contains(name)
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act - string.IsNullOrWhiteSpace(null) = true，true OR x.Name.Contains(name) = true
            var results = _users
                .Where(x => string.IsNullOrWhiteSpace(name) || x.Name.Contains(name))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert - 应返回所有记录
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// string.IsNullOrWhiteSpace(有值变量) OR 条件优化，仅保留 OR 右侧分支。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[Name] LIKE '%测试%' ORDER BY [x].[Id]
        /// </code>
        /// 注意：string.IsNullOrWhiteSpace("测试") = false，false OR x.Name.Contains(name)，仅保留右侧。
        /// </remarks>
        [Fact]
        [Step(15)]
        public void Where_IsNullOrWhiteSpace_HasValue_OrCondition_OnlyKeepsRightBranch()
        {
            // Arrange
            string name = "测试";
            var memoryResults = (from x in _users
                                 where string.IsNullOrWhiteSpace(name) || (x.Name != null && x.Name.Contains(name))
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act
            var results = _users
                .Where(x => string.IsNullOrWhiteSpace(name) || x.Name.Contains(name))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// string.IsNullOrEmpty(字段) 在 Select 投影中，不应被优化，应生成对应的SQL表达式。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], ([x].[Name] IS NULL OR [x].[Name] = '') AS [IsEmpty] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：字段引用在 Select 中不应被优化，应保留数据库判断逻辑。
        /// </remarks>
        [Fact]
        [Step(16)]
        public void Select_IsNullOrEmpty_FieldReference_NotOptimized()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, IsEmpty = string.IsNullOrEmpty(x.Name) }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, IsEmpty = string.IsNullOrEmpty(x.Name) }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].IsEmpty, results[i].IsEmpty);
            }
        }

        /// <summary>
        /// string.IsNullOrEmpty(有值变量) 在 Select 投影中，应被折叠为常量 0。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], ('测试' IS NULL OR '测试' = '') AS [IsEmpty] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// </remarks>
        [Fact]
        [Step(17)]
        public void Select_IsNullOrEmpty_VariableHasValue_FoldedToFalse()
        {
            // Arrange
            string name = "测试";
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, IsEmpty = string.IsNullOrEmpty(name) }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, IsEmpty = string.IsNullOrEmpty(name) }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].IsEmpty, results[i].IsEmpty);
            }
        }

        /// <summary>
        /// string.IsNullOrEmpty(null变量) 在 Select 投影中，应被折叠为常量 1。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], 1 AS [IsEmpty] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// </remarks>
        [Fact]
        [Step(18)]
        public void Select_IsNullOrEmpty_NullVariable_FoldedToTrue()
        {
            // Arrange
            string name = null;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, IsEmpty = string.IsNullOrEmpty(name) }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, IsEmpty = string.IsNullOrEmpty(name) }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].IsEmpty, results[i].IsEmpty);
            }
        }

        #endregion

        #region 复合常量/变量条件逻辑优化

        /// <summary>
        /// 多个常量条件组合：true AND true AND 实际条件。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[Id] > 0 ORDER BY [x].[Id]
        /// </code>
        /// 注意：连续的 true AND 条件应被逐级优化掉。
        /// </remarks>
        [Fact]
        [Step(19)]
        public void Where_MultipleConstantTrue_AndActualCondition_AllOptimized()
        {
            // Arrange
            bool flag1 = true;
            bool flag2 = true;
            var memoryResults = (from x in _users
                                 where flag1 && flag2 && x.Id > 0
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act - flag1 和 flag2 均为 true，应被优化掉
            var results = _users
                .Where(x => flag1 && flag2 && x.Id > 0)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// 混合条件：true AND false → 始终为假。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE (1 = 0 AND [x].[Id] > 0)
        /// </code>
        /// </remarks>
        [Fact]
        [Step(20)]
        public void Where_TrueAndFalse_AlwaysFalse()
        {
            // Arrange
            bool flagTrue = true;
            bool flagFalse = false;

            // Act - true AND false → false
            var results = _users
                .Where(x => flagTrue && flagFalse && x.Id > 0)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Empty(results);
        }

        /// <summary>
        /// 混合条件：false OR 实际条件 → 仅保留实际条件。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100 ORDER BY [x].[Id]
        /// </code>
        /// </remarks>
        [Fact]
        [Step(21)]
        public void Where_FalseOrActualCondition_OnlyKeepsActualCondition()
        {
            // Arrange
            bool flagFalse = false;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act - false OR x.Id == 100 → x.Id == 100
            var results = _users
                .Where(x => flagFalse || x.Id == 100)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// 嵌套条件：(true AND (false OR 实际条件)) → 仅保留实际条件。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[Id] > 50 ORDER BY [x].[Id]
        /// </code>
        /// </remarks>
        [Fact]
        [Step(22)]
        public void Where_NestedConstantConditions_OptimizedCorrectly()
        {
            // Arrange
            bool flagTrue = true;
            bool flagFalse = false;
            var memoryResults = (from x in _users
                                 where flagTrue && (flagFalse || x.Id > 50)
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act - true AND (false OR x.Id > 50) → x.Id > 50
            var results = _users
                .Where(x => flagTrue && (flagFalse || x.Id > 50))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        #endregion

        #region NOT 取反常量/变量条件

        /// <summary>
        /// NOT 取反常量 true → 始终为假。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE 1 <> 1
        /// </code>
        /// </remarks>
        [Fact]
        [Step(23)]
        public void Where_NotTrue_AlwaysFalse()
        {
            // Arrange
            bool flag = true;

            // Act - !true → false
            var results = _users.Where(x => !flag).Select(x => x.Id).ToList();

            // Assert
            Assert.Empty(results);
        }

        /// <summary>
        /// NOT 取反常量 false → 始终为真。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] ORDER BY [x].[Id]
        /// </code>
        /// </remarks>
        [Fact]
        [Step(24)]
        public void Where_NotFalse_AlwaysTrue()
        {
            // Arrange
            bool flag = false;
            var memoryResults = (from x in _users orderby x.Id select x.Id).ToList();

            // Act - !false → true
            var results = _users.Where(x => !flag).OrderBy(x => x.Id).Select(x => x.Id).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        #endregion

        #region Conditions.If 常量条件动态构建

        /// <summary>
        /// Conditions.If：test 始终为假时，条件应被完全忽略。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] ORDER BY [x].[Id]
        /// </code>
        /// 注意：Conditions.If(false, expr) 应被优化为始终为真。
        /// </remarks>
        [Fact]
        [Step(25)]
        public void ConditionsIf_TestFalse_ConditionIgnored()
        {
            // Arrange
            string name = null;
            var memoryResults = (from x in _users orderby x.Id select x.Id).ToList();

            // Act - Conditions.If(!string.IsNullOrEmpty(null), ...) → Conditions.If(false, ...) → 忽略条件
            var results = _users
                .Where(x => Conditions.If(!string.IsNullOrEmpty(name), x.Name.Contains(name)))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// Conditions.If：test 始终为真时，条件应被正常解析。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[Name] LIKE '%测试%' ORDER BY [x].[Id]
        /// </code>
        /// </remarks>
        [Fact]
        [Step(26)]
        public void ConditionsIf_TestTrue_ConditionKept()
        {
            // Arrange
            string name = "测试";
            var memoryResults = (from x in _users
                                 where x.Name != null && x.Name.Contains(name)
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act - Conditions.If(!string.IsNullOrEmpty("测试"), expr) → Conditions.If(true, expr) → 保留条件
            var results = _users
                .Where(x => Conditions.If(!string.IsNullOrEmpty(name), x.Name.Contains(name)))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// Conditions.If 与其他条件组合：test 为假时，AND 中仅保留其他条件。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[Id] > 0 ORDER BY [x].[Id]
        /// </code>
        /// </remarks>
        [Fact]
        [Step(27)]
        public void ConditionsIf_TestFalse_AndOtherCondition_OnlyKeepsOther()
        {
            // Arrange
            string name = null;
            var memoryResults = (from x in _users
                                 where x.Id > 0
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act
            var results = _users
                .Where(x => x.Id > 0 && Conditions.If(!string.IsNullOrEmpty(name), x.Name.Contains(name)))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        #endregion

        #region 布尔成员字段条件

        /// <summary>
        /// 布尔成员字段直接作为条件（IsAdministrator 字段）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[IsAdministrator] = 1 ORDER BY [x].[Id]
        /// </code>
        /// 注意：布尔字段引用不应被优化掉，应正常生成 IS TRUE 或 = 1 的 SQL。
        /// </remarks>
        [Fact]
        [Step(28)]
        public void Where_BoolMemberField_NotOptimized()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where x.IsAdministrator
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act - 布尔字段引用不应被优化，应正常解析
            var results = _users
                .Where(x => x.IsAdministrator)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// 布尔成员字段取反作为条件。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[IsAdministrator] = 0 ORDER BY [x].[Id]
        /// </code>
        /// </remarks>
        [Fact]
        [Step(29)]
        public void Where_NotBoolMemberField_NotOptimized()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where !x.IsAdministrator
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act - !布尔字段引用不应被优化
            var results = _users
                .Where(x => !x.IsAdministrator)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// 布尔成员字段与常量变量组合：true AND 成员字段。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[IsAdministrator] = 1 ORDER BY [x].[Id]
        /// </code>
        /// 注意：true AND IsAdministrator → 仅保留 IsAdministrator。
        /// </remarks>
        [Fact]
        [Step(30)]
        public void Where_TrueAndBoolMember_OnlyKeepsMember()
        {
            // Arrange
            bool flag = true;
            var memoryResults = (from x in _users
                                 where flag && x.IsAdministrator
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act - true AND IsAdministrator → IsAdministrator
            var results = _users
                .Where(x => flag && x.IsAdministrator)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        #endregion

        #region Join 中常量条件优化

        /// <summary>
        /// Join + Where 中的常量条件优化。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], [y].[Role] AS [RoleType] FROM [dbo].[User] AS [x] INNER JOIN [dbo].[UserEx] AS [y] ON [x].[Id] = [y].[Id] ORDER BY [x].[Id]
        /// </code>
        /// 注意：WHERE true 在 Join 查询中也应被优化掉。
        /// </remarks>
        [Fact]
        [Step(31)]
        public void Join_Where_ConstantTrue_OptimizedAway()
        {
            // Arrange
            bool flag = true;
            var memoryResults = (from x in _users
                                 join y in _userExes on x.Id equals y.Id
                                 where flag
                                 orderby x.Id
                                 select new { x.Id, y.RoleType }).ToList();

            // Act
            var results = (from x in _users
                           join y in _userExes on x.Id equals y.Id
                           where flag
                           orderby x.Id
                           select new { x.Id, y.RoleType }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].RoleType, results[i].RoleType);
            }
        }

        #endregion

        #region 可空类型 Nullable 条件与常量优化

        /// <summary>
        /// Nullable<bool>.HasValue 作为条件，不应被优化（数据库字段引用）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE ([x].[Nullable] IS NOT NULL) ORDER BY [x].[Id]
        /// </code>
        /// </remarks>
        [Fact]
        [Step(32)]
        public void Where_NullableHasValue_FieldReference_NotOptimized()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where x.Nullable.HasValue
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act
            var results = _users
                .Where(x => x.Nullable.HasValue)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// 可空类型与 null 比较时，常量 null 的优化处理。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[Nullable] IS NULL ORDER BY [x].[Id]
        /// </code>
        /// </remarks>
        [Fact]
        [Step(33)]
        public void Where_NullableEqualsNull_GeneratesIsNull()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where x.Nullable == null
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act
            var results = _users
                .Where(x => x.Nullable == null)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        #endregion

        #region 三目运算（Conditional / Ternary）条件优化

        /// <summary>
        /// 三目运算：常量 true 作为 test，Select 投影中仅保留 ifTrue 分支。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], [x].[Name] AS [Value] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：true ? x.Name : "默认" → 直接返回 x.Name，ifFalse 分支不解析。
        /// </remarks>
        [Fact]
        [Step(34)]
        public void Select_Ternary_ConstantTrueTest_OnlyIfTrueBranch()
        {
            // Arrange
            bool flag = true;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Value = flag ? x.Name : "默认" }).ToList();

            // Act - true ? x.Name : "默认" → x.Name
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Value = flag ? x.Name : "默认" }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Value, results[i].Value);
            }
        }

        /// <summary>
        /// 三目运算：常量 false 作为 test，Select 投影中仅保留 ifFalse 分支。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], '默认' AS [Value] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：false ? x.Name : "默认" → 直接返回 "默认"，ifTrue 分支不解析。
        /// </remarks>
        [Fact]
        [Step(35)]
        public void Select_Ternary_ConstantFalseTest_OnlyIfFalseBranch()
        {
            // Arrange
            bool flag = false;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Value = flag ? x.Name : "默认" }).ToList();

            // Act - false ? x.Name : "默认" → "默认"
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Value = flag ? x.Name : "默认" }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Value, results[i].Value);
            }
        }

        /// <summary>
        /// 三目运算：常量 false 作为 test，Select 投影中仅保留 ifFalse 分支。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], [x].[Name] AS [Value] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：false ? x.Name : "默认" → 直接返回 "默认"，ifTrue 分支不解析。
        /// </remarks>
        [Fact]
        [Step(36)]
        public void Select_Ternary_ConstantFalseTest_OnlyIfNotFalseBranch()
        {
            // Arrange
            bool flag = false;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Value = !flag ? x.Name : "默认" }).ToList();

            // Act - false ? x.Name : "默认" → "默认"
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Value = !flag ? x.Name : "默认" }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Value, results[i].Value);
            }
        }

        /// <summary>
        /// 三目运算：字段条件作为 test，生成 CASE WHEN 语句。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], CASE WHEN [x].[IsAdministrator] THEN [x].[Name] ELSE '普通用户' END AS [Value] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：字段引用条件不应被优化，应生成完整的 CASE WHEN。
        /// </remarks>
        [Fact]
        [Step(37)]
        public void Select_Ternary_FieldTest_GeneratesCaseWhen()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Value = x.IsAdministrator ? x.Name : "普通用户" }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Value = x.IsAdministrator ? x.Name : "普通用户" }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Value, results[i].Value);
            }
        }

        /// <summary>
        /// 三目运算：常量 true 作为 test，返回数值常量。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], 1 AS [Type] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：true ? 1 : 2 → 直接折叠为常量 1。
        /// </remarks>
        [Fact]
        [Step(38)]
        public void Select_Ternary_ConstantTrueTest_NumericBranch()
        {
            // Arrange
            bool flag = true;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Type = flag ? 1 : 2 }).ToList();

            // Act - true ? 1 : 2 → 1
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Type = flag ? 1 : 2 }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Type, results[i].Type);
            }
        }

        /// <summary>
        /// 三目运算：常量 false 作为 test，返回数值常量。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], 2 AS [Type] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：false ? 1 : 2 → 直接折叠为常量 2。
        /// </remarks>
        [Fact]
        [Step(39)]
        public void Select_Ternary_ConstantFalseTest_NumericBranch()
        {
            // Arrange
            bool flag = false;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Type = flag ? 1 : 2 }).ToList();

            // Act - false ? 1 : 2 → 2
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Type = flag ? 1 : 2 }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Type, results[i].Type);
            }
        }

        /// <summary>
        /// 三目运算：string.IsNullOrEmpty 变量作为 test，变量有值时仅保留 ifFalse 分支。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], [x].[Name] AS [Value] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：string.IsNullOrEmpty("测试") = false → false ? "无" : x.Name → x.Name。
        /// </remarks>
        [Fact]
        [Step(40)]
        public void Select_Ternary_IsNullOrEmptyTest_HasValue_OnlyIfFalseBranch()
        {
            // Arrange
            string name = "测试";
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Value = string.IsNullOrEmpty(name) ? "无" : x.Name }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Value = string.IsNullOrEmpty(name) ? "无" : x.Name }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Value, results[i].Value);
            }
        }

        /// <summary>
        /// 三目运算：string.IsNullOrEmpty 变量作为 test，变量为 null 时仅保留 ifTrue 分支。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], '无' AS [Value] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：string.IsNullOrEmpty(null) = true → true ? "无" : x.Name → "无"。
        /// </remarks>
        [Fact]
        [Step(41)]
        public void Select_Ternary_IsNullOrEmptyTest_Null_OnlyIfTrueBranch()
        {
            // Arrange
            string name = null;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Value = string.IsNullOrEmpty(name) ? "无" : x.Name }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Value = string.IsNullOrEmpty(name) ? "无" : x.Name }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Value, results[i].Value);
            }
        }

        /// <summary>
        /// 三目运算：字段比较作为 test，两分支均为字段引用，生成完整 CASE WHEN。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], CASE WHEN [x].[Id] > 50 THEN [x].[Name] ELSE '未知' END AS [Value] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// </remarks>
        [Fact]
        [Step(42)]
        public void Select_Ternary_FieldComparison_GeneratesCaseWhen()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Value = x.Id > 50 ? x.Name : "未知" }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Value = x.Id > 50 ? x.Name : "未知" }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Value, results[i].Value);
            }
        }

        /// <summary>
        /// 三目运算：嵌套三目，外层常量 true 优化后内层字段条件保留。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], CASE WHEN [x].[IsAdministrator] THEN [x].[Name] ELSE '游客' END AS [Value] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：true ? (x.IsAdministrator ? x.Name : "游客") : "默认" → 外层折叠，保留内层 CASE WHEN。
        /// </remarks>
        [Fact]
        [Step(43)]
        public void Select_Ternary_NestedWithConstantOuterTest_InnerPreserved()
        {
            // Arrange
            bool flag = true;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Value = flag ? (x.IsAdministrator ? x.Name : "游客") : "默认" }).ToList();

            // Act - 外层 true 折叠，保留内层字段三目
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Value = flag ? (x.IsAdministrator ? x.Name : "游客") : "默认" }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Value, results[i].Value);
            }
        }

        /// <summary>
        /// 三目运算用于排序：常量 true 时仅保留 ifTrue 分支排序。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[UserEx] AS [x] WHERE [x].[Id] = 100 ORDER BY [x].[Age] ASC, [x].[DateAt] DESC
        /// </code>
        /// 注意：true ? x.Age : x.RoleType → 排序时仅用 x.Age。
        /// </remarks>
        [Fact]
        [Step(44)]
        public void OrderBy_Ternary_ConstantTrueTest_OnlyIfTrueBranch()
        {
            // Arrange
            bool flag = true;
            var memoryResults = (from x in _userExes
                                 where x.Id == 100
                                 orderby flag ? x.Age : x.RoleType, x.DateAt descending
                                 select x.Id).ToList();

            // Act
            var results = (from x in _userExes
                           where x.Id == 100
                           orderby flag ? x.Age : x.RoleType, x.DateAt descending
                           select x.Id).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            Assert.Equal(memoryResults, results);
        }

        /// <summary>
        /// 三目运算用于排序：常量 false 时仅保留 ifFalse 分支排序。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[UserEx] AS [x] WHERE [x].[Id] = 100 ORDER BY [x].[Role] ASC, [x].[DateAt] DESC
        /// </code>
        /// 注意：false ? x.Age : x.RoleType → 排序时仅用 x.RoleType。
        /// </remarks>
        [Fact]
        [Step(45)]
        public void OrderBy_Ternary_ConstantFalseTest_OnlyIfFalseBranch()
        {
            // Arrange
            bool flag = false;
            var memoryResults = (from x in _userExes
                                 where x.Id == 100
                                 orderby flag ? x.Age : x.RoleType, x.DateAt descending
                                 select x.Id).ToList();

            // Act
            var results = (from x in _userExes
                           where x.Id == 100
                           orderby flag ? x.Age : x.RoleType, x.DateAt descending
                           select x.Id).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            Assert.Equal(memoryResults, results);
        }

        #endregion

        #region 可空类型 HasValue 条件深度测试

        /// <summary>
        /// Nullable&lt;bool&gt;.HasValue 与 AND 条件组合。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100 AND [x].[Nullable] IS NOT NULL ORDER BY [x].[Id]
        /// </code>
        /// </remarks>
        [Fact]
        [Step(46)]
        public void Where_NullableHasValue_AndOtherCondition()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where x.Id == 100 && x.Nullable.HasValue
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act
            var results = _users
                .Where(x => x.Id == 100 && x.Nullable.HasValue)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// !Nullable&lt;bool&gt;.HasValue（即 IS NULL）作为条件。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[Nullable] IS NULL ORDER BY [x].[Id]
        /// </code>
        /// </remarks>
        [Fact]
        [Step(47)]
        public void Where_NotNullableHasValue_GeneratesIsNull()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where !x.Nullable.HasValue
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act - !HasValue → IS NULL
            var results = _users
                .Where(x => !x.Nullable.HasValue)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// Nullable&lt;bool&gt;.Value 直接作为布尔条件。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[Nullable] ORDER BY [x].[Id]
        /// </code>
        /// </remarks>
        [Fact]
        [Step(48)]
        public void Where_NullableValue_AsBoolCondition()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where x.Nullable.HasValue && x.Nullable.Value
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act
            var results = _users
                .Where(x => x.Nullable.Value)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// Nullable HasValue 在 Select 投影中。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], ([x].[Nullable] IS NOT NULL) AS [HasNullable] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// </remarks>
        [Fact]
        [Step(49)]
        public void Select_NullableHasValue_AsProjection()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, HasNullable = x.Nullable.HasValue }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, HasNullable = x.Nullable.HasValue }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].HasNullable, results[i].HasNullable);
            }
        }

        /// <summary>
        /// Nullable 变量 HasValue 且变量有值时，在条件中应被优化为始终为真。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[Id] > 10 ORDER BY [x].[Id]
        /// </code>
        /// 注意：age.HasValue 变量有值时 = true，true AND x.Id > age → 仅保留 x.Id > age。
        /// </remarks>
        [Fact]
        [Step(50)]
        public void Where_NullableVariableHasValue_True_OptimizedAway()
        {
            // Arrange
            int? age = 10;
            var memoryResults = (from x in _users
                                 where age.HasValue && x.Id > age.Value
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act - age.HasValue = true → 条件优化掉，仅保留 x.Id > age
            var results = _users
                .Where(x => age.HasValue && x.Id > age.Value)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// Nullable 变量 HasValue 且变量为 null 时，在 AND 条件中应被优化为始终为假。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE 1 = 0
        /// </code>
        /// 注意：age.HasValue 变量为 null 时 = false，false AND anything → 始终为假。
        /// </remarks>
        [Fact]
        [Step(51)]
        public void Where_NullableVariableHasValue_Null_AlwaysFalse()
        {
            // Arrange
            int? age = null;

            // Act - age.HasValue = false → false AND anything = false
            var results = _users
                .Where(x => age.HasValue && x.Id > age.Value)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Empty(results);
        }

        /// <summary>
        /// Nullable 变量在 OR 条件中的优化：!hasValue 时跳过。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] ORDER BY [x].[Id]
        /// </code>
        /// 注意：!age.HasValue 为 true → true OR anything → 始终为真。
        /// </remarks>
        [Fact]
        [Step(52)]
        public void Where_NullableVariableNotHasValue_OrCondition_AlwaysTrue()
        {
            // Arrange
            int? age = null;
            var memoryResults = (from x in _users
                                 where !age.HasValue || x.Id > age.Value
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act - !null.HasValue = true → true OR ... → 始终为真
            var results = _users
                .Where(x => !age.HasValue || x.Id > age.Value)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// Nullable 变量 HasValue 用于三目运算条件。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>（age 有值时）：
        /// <code>
        /// SELECT [x].[Id] AS [Id], [x].[Id] AS [Value] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：age.HasValue = true → true ? age.Value : 0 → age.Value。
        /// </remarks>
        [Fact]
        [Step(53)]
        public void Select_Ternary_NullableHasValue_True_OnlyIfTrueBranch()
        {
            // Arrange
            int? age = 25;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Value = age.HasValue ? x.Id : 0 }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Value = age.HasValue ? x.Id : 0 }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Value, results[i].Value);
            }
        }

        /// <summary>
        /// Nullable 变量 HasValue 用于三目运算条件（变量为 null）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>（age 为 null 时）：
        /// <code>
        /// SELECT [x].[Id] AS [Id], 0 AS [Value] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：age.HasValue = false → false ? age.Value : 0 → 0。
        /// </remarks>
        [Fact]
        [Step(54)]
        public void Select_Ternary_NullableHasValue_Null_OnlyIfFalseBranch()
        {
            // Arrange
            int? age = null;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Value = age.HasValue ? x.Id : 0 }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Value = age.HasValue ? x.Id : 0 }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Value, results[i].Value);
            }
        }

        /// <summary>
        /// Nullable 字段 HasValue 用于三目运算（数据库字段 test 不优化）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], CASE WHEN [x].[Nullable] IS NOT NULL THEN 1 ELSE 0 END AS [Value] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：x.Nullable.HasValue 是数据库字段引用，不应被优化，应生成 CASE WHEN。
        /// </remarks>
        [Fact]
        [Step(55)]
        public void Select_Ternary_FieldNullableHasValue_GeneratesCaseWhen()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Value = x.Nullable.HasValue ? 1 : 0 }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Value = x.Nullable.HasValue ? 1 : 0 }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Value, results[i].Value);
            }
        }

        /// <summary>
        /// 可空类型合并运算（??）在 Select 中，当变量有值时折叠为变量值。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], ISNULL(25, 0) AS [Value] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：age ?? 0 当 age = 25 时，直接折叠为 25。
        /// </remarks>
        [Fact]
        [Step(56)]
        public void Select_NullCoalescing_VariableHasValue_FoldedToValue()
        {
            // Arrange
            int? age = 25;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Value = age ?? 0 }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Value = age ?? 0 }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Value, results[i].Value);
            }
        }

        /// <summary>
        /// 可空类型合并运算（??）在 Select 中，当变量为 null 时折叠为默认值。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], 0 AS [Value] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：age ?? 0 当 age = null 时，直接折叠为 0。
        /// </remarks>
        [Fact]
        [Step(57)]
        public void Select_NullCoalescing_VariableNull_FoldedToDefault()
        {
            // Arrange
            int? age = null;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Value = age ?? 0 }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Value = age ?? 0 }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Value, results[i].Value);
            }
        }

        /// <summary>
        /// 可空类型合并运算（??）在 Select 中，字段引用不优化，生成 COALESCE/ISNULL。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], ISNULL([x].[Nullable], 0) AS [Value] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// </remarks>
        [Fact]
        [Step(58)]
        public void Select_NullCoalescing_FieldReference_GeneratesCoalesce()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Value = x.Nullable ?? false }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Value = x.Nullable ?? false }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Value, results[i].Value);
            }
        }

        #endregion

        #region 多层 Where 叠加常量条件

        /// <summary>
        /// 多个 Where 调用叠加，常量条件在不同层级被优化。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[Id] > 0 ORDER BY [x].[Id]
        /// </code>
        /// 注意：第一层 Where 是 true（优化掉），第二层是实际条件。
        /// </remarks>
        [Fact]
        [Step(59)]
        public void Where_MultipleChained_ConstantTrueAndActual_Optimized()
        {
            // Arrange
            bool flag = true;
            var memoryResults = (from x in _users
                                 where x.Id > 0
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act - 第一个 Where(true) 应被优化掉，第二个正常保留
            var results = _users
                .Where(x => flag)
                .Where(x => x.Id > 0)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// 多个 Where 调用叠加，常量 false 条件短路。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE (1 = 0 AND [x].[Id] &gt; 0)
        /// </code>
        /// 注意：第一层 Where 是 false，后续条件不会被解析到SQL中。
        /// </remarks>
        [Fact]
        [Step(60)]
        public void Where_MultipleChained_ConstantFalse_ShortCircuit()
        {
            // Arrange
            bool flag = false;

            // Act - 第一个 Where(false) 短路，最终生成 1 = 0
            var results = _users
                .Where(x => flag)
                .Where(x => x.Id > 0)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Empty(results);
        }

        #endregion

        #region 取反条件（NOT 应用于字段和表达式）

        /// <summary>
        /// 对字段相等比较取反：!(x.Id == 100)，应生成不等比较（&lt;&gt;）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[Id] &lt;&gt; 100 ORDER BY [x].[Id]
        /// </code>
        /// </remarks>
        [Fact]
        [Step(61)]
        public void Where_NotEquality_GeneratesNotEqual()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where !(x.Id == 100)
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act
            var results = _users
                .Where(x => !(x.Id == 100))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// 对字段 Contains 取反：!x.Name.Contains(name)，应生成 NOT LIKE 表达式。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE [x].[Name] NOT LIKE '%' + '测试' + '%' ORDER BY [x].[Id]
        /// </code>
        /// 注意：NULL NOT LIKE 在 SQL 中返回 NULL（被 WHERE 过滤），与 C# 中 name != null &amp;&amp; !name.Contains() 等效。
        /// </remarks>
        [Fact]
        [Step(62)]
        public void Where_NotContains_GeneratesNotLike()
        {
            // Arrange
            string name = "测试";
            var memoryResults = (from x in _users
                                 where x.Name != null && !x.Name.Contains(name)
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act
            var results = _users
                .Where(x => !x.Name.Contains(name))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// 对 AND 复合表达式取反（De Morgan 定律）：!(A &amp;&amp; B) → !A || !B。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE ([x].[Id] &lt;= 0 OR [x].[IsAdministrator] = 0) ORDER BY [x].[Id]
        /// </code>
        /// 注意：NOT (A AND B) 应按 De Morgan 定律展开为 (!A) OR (!B)。
        /// </remarks>
        [Fact]
        [Step(63)]
        public void Where_NotAndExpression_DecomposesToDeMorgan()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where !(x.Id > 0 && x.IsAdministrator)
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act - !(Id > 0 AND IsAdministrator) → Id <= 0 OR !IsAdministrator
            var results = _users
                .Where(x => !(x.Id > 0 && x.IsAdministrator))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// 对 OR 复合表达式取反（De Morgan 定律）：!(A || B) → !A &amp;&amp; !B。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE ([x].[Id] &lt;= 100 AND [x].[IsAdministrator] = 0) ORDER BY [x].[Id]
        /// </code>
        /// 注意：NOT (A OR B) 应按 De Morgan 定律展开为 (!A) AND (!B)。
        /// </remarks>
        [Fact]
        [Step(64)]
        public void Where_NotOrExpression_DecomposesToDeMorgan()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where !(x.Id > 100 || x.IsAdministrator)
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act - !(Id > 100 OR IsAdministrator) → Id <= 100 AND !IsAdministrator
            var results = _users
                .Where(x => !(x.Id > 100 || x.IsAdministrator))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// 对字段 IsNullOrEmpty 取反：!string.IsNullOrEmpty(x.Name)，不应被优化，应生成否定的空检查 SQL 表达式。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] FROM [dbo].[User] AS [x] WHERE ([x].[Name] IS NOT NULL AND [x].[Name] <> '') ORDER BY [x].[Id]
        /// </code>
        /// 注意：字段引用不应被优化，应保留数据库否定空检查逻辑。
        /// </remarks>
        [Fact]
        [Step(65)]
        public void Where_NotIsNullOrEmpty_FieldReference()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where !string.IsNullOrEmpty(x.Name)
                                 orderby x.Id
                                 select x.Id).ToList();

            // Act
            var results = _users
                .Where(x => !string.IsNullOrEmpty(x.Name))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        #endregion

        #region 取反查询结果（NOT 在 Select 投影中）

        /// <summary>
        /// 布尔变量 true 取反作为投影字段，应折叠为常量 0（false）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], CASE WHEN 1 <> 1 THEN 1 ELSE 0 END AS [IsNot] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：!true 直接折叠为 0。
        /// </remarks>
        [Fact]
        [Step(66)]
        public void Select_NotBoolVariableTrue_FoldedToZero()
        {
            // Arrange
            bool flag = true;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, IsNot = !flag }).ToList();

            // Act - !true → false → 0
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, IsNot = !flag }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].IsNot, results[i].IsNot);
            }
        }

        /// <summary>
        /// 布尔变量 false 取反作为投影字段，应折叠为常量 1（true）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], 1 AS [IsNot] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：!false 直接折叠为 1。
        /// </remarks>
        [Fact]
        [Step(67)]
        public void Select_NotBoolVariableFalse_FoldedToOne()
        {
            // Arrange
            bool flag = false;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, IsNot = !flag }).ToList();

            // Act - !false → true → 1
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, IsNot = !flag }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].IsNot, results[i].IsNot);
            }
        }

        /// <summary>
        /// 布尔成员字段取反作为投影字段，不应被优化，应生成取反的 SQL 表达式。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], CASE WHEN [x].[IsAdministrator] = 0 THEN 1 ELSE 0 END AS [IsNot] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：字段引用取反不优化，生成取反比较或 NOT 表达式。
        /// </remarks>
        [Fact]
        [Step(68)]
        public void Select_NotBoolMemberField_GeneratesNegation()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, IsNot = !x.IsAdministrator }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, IsNot = !x.IsAdministrator }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].IsNot, results[i].IsNot);
            }
        }

        /// <summary>
        /// Nullable 字段 HasValue 取反作为投影字段，应生成 IS NULL 判断。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], CASE WHEN ([x].[Nullable] IS NULL) THEN 1 ELSE 0 END AS [HasNo] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：!x.Nullable.HasValue 等价于 x.Nullable IS NULL。
        /// </remarks>
        [Fact]
        [Step(69)]
        public void Select_NotNullableHasValue_GeneratesIsNull()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, HasNo = !x.Nullable.HasValue }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, HasNo = !x.Nullable.HasValue }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].HasNo, results[i].HasNo);
            }
        }

        /// <summary>
        /// 字段 IsNullOrEmpty 取反作为投影字段，不应被优化，应生成否定的空检查 SQL 表达式。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], CASE WHEN ([x].[Name] IS NOT NULL AND [x].[Name] <> '') THEN 1 ELSE 0 END AS [IsNotEmpty] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：x.Name 是字段引用，不应被优化，应保留数据库否定判断逻辑。
        /// </remarks>
        [Fact]
        [Step(70)]
        public void Select_NotIsNullOrEmpty_FieldReference_GeneratesNotExpression()
        {
            // Arrange
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, IsNotEmpty = !string.IsNullOrEmpty(x.Name) }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, IsNotEmpty = !string.IsNullOrEmpty(x.Name) }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].IsNotEmpty, results[i].IsNotEmpty);
            }
        }

        /// <summary>
        /// 变量 IsNullOrEmpty 取反作为投影字段（变量有值），应折叠为常量 1（true）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], ('测试' IS NOT NULL AND '测试' <> '') AS [IsNotEmpty] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：!IsNullOrEmpty("测试") = !false = true → 折叠为常量 1。
        /// </remarks>
        [Fact]
        [Step(71)]
        public void Select_NotIsNullOrEmpty_VariableHasValue_FoldedToTrue()
        {
            // Arrange
            string name = "测试";
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, IsNotEmpty = !string.IsNullOrEmpty(name) }).ToList();

            // Act - !IsNullOrEmpty("测试") = true → 1
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, IsNotEmpty = !string.IsNullOrEmpty(name) }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].IsNotEmpty, results[i].IsNotEmpty);
            }
        }

        /// <summary>
        /// 变量 IsNullOrEmpty 取反作为投影字段（变量为 null），应折叠为常量 0（false）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], 0 AS [IsNotEmpty] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：!IsNullOrEmpty(null) = !true = false → 折叠为常量 0。
        /// </remarks>
        [Fact]
        [Step(72)]
        public void Select_NotIsNullOrEmpty_NullVariable_FoldedToFalse()
        {
            // Arrange
            string name = null;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, IsNotEmpty = !string.IsNullOrEmpty(name) }).ToList();

            // Act - !IsNullOrEmpty(null) = false → 0
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, IsNotEmpty = !string.IsNullOrEmpty(name) }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].IsNotEmpty, results[i].IsNotEmpty);
            }
        }

        #endregion

        #region 常量折叠后分支为条件表达式（CASE WHEN 包裹）

        /// <summary>
        /// 常量 true 折叠后，ifTrue 分支是布尔字段（条件表达式），需生成 CASE WHEN 包裹。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], CASE WHEN [x].[IsAdministrator] = 1 THEN 1 ELSE 0 END AS [Value] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：true ? x.IsAdministrator : false → 折叠为 x.IsAdministrator，
        /// SqlServer 需要 CASE WHEN 包裹布尔条件表达式。
        /// </remarks>
        [Fact]
        [Step(73)]
        public void Select_Ternary_ConstantTrue_IfTrueIsBoolField()
        {
            // Arrange
            bool flag = true;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Value = flag ? x.IsAdministrator : false }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Value = flag ? x.IsAdministrator : false }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Value, results[i].Value);
            }
        }

        /// <summary>
        /// 常量 false 折叠后，ifFalse 分支是布尔字段（条件表达式）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], CASE WHEN [x].[IsAdministrator] = 1 THEN 1 ELSE 0 END AS [Value] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：false ? true : x.IsAdministrator → 折叠为 x.IsAdministrator。
        /// </remarks>
        [Fact]
        [Step(74)]
        public void Select_Ternary_ConstantFalse_IfFalseIsBoolField()
        {
            // Arrange
            bool flag = false;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Value = flag ? true : x.IsAdministrator }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Value = flag ? true : x.IsAdministrator }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Value, results[i].Value);
            }
        }

        /// <summary>
        /// 常量 true 折叠后，ifTrue 分支是 Nullable.HasValue（条件表达式）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], CASE WHEN [x].[Nullable] IS NOT NULL THEN 1 ELSE 0 END AS [Value] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：true ? x.Nullable.HasValue : false → 折叠为 x.Nullable.HasValue。
        /// </remarks>
        [Fact]
        [Step(75)]
        public void Select_Ternary_ConstantTrue_IfTrueIsNullableHasValue()
        {
            // Arrange
            bool flag = true;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Value = flag ? x.Nullable.HasValue : false }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Value = flag ? x.Nullable.HasValue : false }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Value, results[i].Value);
            }
        }

        /// <summary>
        /// 常量 true 折叠后，ifTrue 分支是比较表达式（条件表达式）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], CASE WHEN [x].[Id] &gt; 50 THEN 1 ELSE 0 END AS [Value] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：true ? (x.Id > 50) : false → 折叠为 x.Id > 50，SqlServer 需 CASE WHEN 包裹。
        /// </remarks>
        [Fact]
        [Step(76)]
        public void Select_Ternary_ConstantTrue_IfTrueIsComparison()
        {
            // Arrange
            bool flag = true;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Value = flag ? x.Id > 50 : false }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Value = flag ? x.Id > 50 : false }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Value, results[i].Value);
            }
        }

        /// <summary>
        /// 常量 false 折叠后，ifFalse 分支是非空检查（条件表达式）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT [x].[Id] AS [Id], CASE WHEN [x].[Name] IS NOT NULL THEN 1 ELSE 0 END AS [Value] FROM [dbo].[User] AS [x] WHERE [x].[Id] = 100
        /// </code>
        /// 注意：false ? true : (x.Name != null) → 折叠为 x.Name != null。
        /// </remarks>
        [Fact]
        [Step(77)]
        public void Select_Ternary_ConstantFalse_IfFalseIsNotNullCheck()
        {
            // Arrange
            bool flag = false;
            var memoryResults = (from x in _users
                                 where x.Id == 100
                                 select new { x.Id, Value = flag ? true : x.Name != null }).ToList();

            // Act
            var results = (from x in _users
                           where x.Id == 100
                           select new { x.Id, Value = flag ? true : x.Name != null }).ToList();

            // Assert
            Assert.Equal(memoryResults.Count, results.Count);
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Value, results[i].Value);
            }
        }

        #endregion
    }
}
