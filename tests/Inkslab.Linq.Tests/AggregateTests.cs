using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using XunitPlus;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// 聚合方法单元测试
    /// 核心功能：测试 Count、LongCount、Any、Contains 以及 GroupBy 聚合方法
    /// 注意：Max/Min/Sum/Average 仅在 GroupBy 后支持，直接查询不支持
    /// </summary>
    [TestPriority(40)]
    public class AggregateTests
    {
        private readonly IQueryable<User> _users;

        public AggregateTests(IQueryable<User> users)
        {
            _users = users;
        }

        #region Count 测试

        /// <summary>
        /// 测试：Count 获取记录总数
        /// SQL预览: SELECT COUNT(1) FROM `user`
        /// </summary>
        [Fact]
        [Step(1)]
        public void Count_ReturnsRecordCount()
        {
            // Arrange
            var allUsers = _users.ToList();

            // Act
            var count = _users.Count();

            // Assert
            Assert.Equal(allUsers.Count, count);
        }

        /// <summary>
        /// 测试：Count 使用谓词条件
        /// SQL预览: SELECT COUNT(1) FROM `user` WHERE `is_administrator` = 1
        /// </summary>
        [Fact]
        [Step(2)]
        public void Count_WithPredicate_ReturnsFilteredCount()
        {
            // Arrange
            var allUsers = _users.ToList();
            var expectedCount = allUsers.Count(x => x.IsAdministrator);

            // Act
            var count = _users.Count(x => x.IsAdministrator);

            // Assert
            Assert.Equal(expectedCount, count);
        }

        /// <summary>
        /// 测试：Count 与 Where 条件组合
        /// SQL预览: SELECT COUNT(1) FROM `user` WHERE `id` > 0
        /// </summary>
        [Fact]
        [Step(3)]
        public void Count_WithWhere_ReturnsFilteredCount()
        {
            // Arrange
            var allUsers = _users.ToList();
            var expectedCount = allUsers.Count(x => x.Id > 0);

            // Act
            var count = _users.Where(x => x.Id > 0).Count();

            // Assert
            Assert.Equal(expectedCount, count);
        }

        /// <summary>
        /// 测试：CountAsync 异步获取记录总数
        /// </summary>
        [Fact]
        [Step(4)]
        public async Task CountAsync_ReturnsRecordCountAsync()
        {
            // Arrange
            var allUsers = _users.ToList();

            // Act
            var count = await _users.CountAsync();

            // Assert
            Assert.Equal(allUsers.Count, count);
        }

        /// <summary>
        /// 测试：CountAsync 使用谓词条件
        /// </summary>
        [Fact]
        [Step(5)]
        public async Task CountAsync_WithPredicate_ReturnsFilteredCountAsync()
        {
            // Arrange
            var allUsers = _users.ToList();
            var expectedCount = allUsers.Count(x => x.IsAdministrator);

            // Act
            var count = await _users.CountAsync(x => x.IsAdministrator);

            // Assert
            Assert.Equal(expectedCount, count);
        }

        /// <summary>
        /// 测试：CountAsync 与 Where 条件组合
        /// </summary>
        [Fact]
        [Step(6)]
        public async Task CountAsync_WithWhere_ReturnsFilteredCountAsync()
        {
            // Arrange
            var allUsers = _users.ToList();
            var expectedCount = allUsers.Count(x => x.Id > 0);

            // Act
            var count = await _users.Where(x => x.Id > 0).CountAsync();

            // Assert
            Assert.Equal(expectedCount, count);
        }

        #endregion

        #region LongCount 测试

        /// <summary>
        /// 测试：LongCount 获取长整型记录总数
        /// SQL预览: SELECT COUNT(1) FROM `user`
        /// </summary>
        [Fact]
        [Step(7)]
        public void LongCount_ReturnsRecordCount()
        {
            // Arrange
            var allUsers = _users.ToList();

            // Act
            var count = _users.LongCount();

            // Assert
            Assert.Equal(allUsers.Count, count);
        }

        /// <summary>
        /// 测试：LongCount 使用谓词条件
        /// </summary>
        [Fact]
        [Step(8)]
        public void LongCount_WithPredicate_ReturnsFilteredCount()
        {
            // Arrange
            var allUsers = _users.ToList();
            var expectedCount = allUsers.LongCount(x => x.IsAdministrator);

            // Act
            var count = _users.LongCount(x => x.IsAdministrator);

            // Assert
            Assert.Equal(expectedCount, count);
        }

        /// <summary>
        /// 测试：LongCount 与 Where 条件组合
        /// </summary>
        [Fact]
        [Step(9)]
        public void LongCount_WithWhere_ReturnsFilteredCount()
        {
            // Arrange
            var allUsers = _users.ToList();
            var expectedCount = allUsers.LongCount(x => x.Id > 0);

            // Act
            var count = _users.Where(x => x.Id > 0).LongCount();

            // Assert
            Assert.Equal(expectedCount, count);
        }

        /// <summary>
        /// 测试：LongCountAsync 异步获取长整型记录总数
        /// </summary>
        [Fact]
        [Step(10)]
        public async Task LongCountAsync_ReturnsRecordCountAsync()
        {
            // Arrange
            var allUsers = _users.ToList();

            // Act
            var count = await _users.LongCountAsync();

            // Assert
            Assert.Equal(allUsers.Count, count);
        }

        /// <summary>
        /// 测试：LongCountAsync 使用谓词条件
        /// </summary>
        [Fact]
        [Step(11)]
        public async Task LongCountAsync_WithPredicate_ReturnsFilteredCountAsync()
        {
            // Arrange
            var allUsers = _users.ToList();
            var expectedCount = allUsers.LongCount(x => x.IsAdministrator);

            // Act
            var count = await _users.LongCountAsync(x => x.IsAdministrator);

            // Assert
            Assert.Equal(expectedCount, count);
        }

        /// <summary>
        /// 测试：LongCountAsync 与 Where 条件组合
        /// </summary>
        [Fact]
        [Step(12)]
        public async Task LongCountAsync_WithWhere_ReturnsFilteredCountAsync()
        {
            // Arrange
            var allUsers = _users.ToList();
            var expectedCount = allUsers.LongCount(x => x.Id > 0);

            // Act
            var count = await _users.Where(x => x.Id > 0).LongCountAsync();

            // Assert
            Assert.Equal(expectedCount, count);
        }

        #endregion

        #region Any 测试

        /// <summary>
        /// 测试：Any 检查是否存在任何记录
        /// SQL预览: SELECT EXISTS(SELECT 1 FROM `user`)
        /// </summary>
        [Fact]
        [Step(13)]
        public void Any_ReturnsExistence()
        {
            // Arrange
            var allUsers = _users.ToList();
            var expected = allUsers.Any();

            // Act
            var result = _users.Any();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试：Any 使用谓词检查特定条件
        /// SQL预览: SELECT EXISTS(SELECT 1 FROM `user` WHERE `is_administrator` = 1)
        /// </summary>
        [Fact]
        [Step(14)]
        public void Any_WithPredicate_ReturnsTrueIfMatches()
        {
            // Arrange
            var allUsers = _users.ToList();
            var expected = allUsers.Any(x => x.IsAdministrator);

            // Act
            var result = _users.Any(x => x.IsAdministrator);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试：Any 使用 Id 条件
        /// </summary>
        [Fact]
        [Step(15)]
        public void Any_WithIdPredicate_ReturnsCorrectResult()
        {
            // Arrange
            var allUsers = _users.ToList();

            if (allUsers.Count == 0)
            {
                Assert.False(_users.Any(x => x.Id > 0));
                return;
            }

            var existingId = allUsers.First().Id;
            var nonExistingId = allUsers.Max(x => x.Id) + 9999;

            // Act & Assert - 存在的 Id
            Assert.True(_users.Any(x => x.Id == existingId));
            
            // Act & Assert - 不存在的 Id
            Assert.False(_users.Any(x => x.Id == nonExistingId));
        }

        /// <summary>
        /// 测试：Any 使用复合条件
        /// </summary>
        [Fact]
        [Step(16)]
        public void Any_WithCompoundPredicate_ReturnsCorrectResult()
        {
            // Arrange
            var allUsers = _users.ToList();
            var expected = allUsers.Any(x => x.Id > 0 && x.Name != null);

            // Act
            var result = _users.Any(x => x.Id > 0 && x.Name != null);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试：Any 与 Where 条件组合
        /// </summary>
        [Fact]
        [Step(17)]
        public void Any_WithWhere_ReturnsCorrectResult()
        {
            // Arrange
            var allUsers = _users.ToList();
            var expected = allUsers.Where(x => x.IsAdministrator).Any();

            // Act
            var result = _users.Where(x => x.IsAdministrator).Any();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试：AnyAsync 异步检查是否存在任何记录
        /// </summary>
        [Fact]
        [Step(18)]
        public async Task AnyAsync_ReturnsExistenceAsync()
        {
            // Arrange
            var allUsers = _users.ToList();
            var expected = allUsers.Any();

            // Act
            var result = await _users.AnyAsync();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试：AnyAsync 使用谓词检查特定条件
        /// </summary>
        [Fact]
        [Step(19)]
        public async Task AnyAsync_WithPredicate_ReturnsTrueIfMatchesAsync()
        {
            // Arrange
            var allUsers = _users.ToList();
            var expected = allUsers.Any(x => x.IsAdministrator);

            // Act
            var result = await _users.AnyAsync(x => x.IsAdministrator);

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试：AnyAsync 与 Where 条件组合
        /// </summary>
        [Fact]
        [Step(20)]
        public async Task AnyAsync_WithWhere_ReturnsCorrectResultAsync()
        {
            // Arrange
            var allUsers = _users.ToList();
            var expected = allUsers.Where(x => x.Id > 0).Any();

            // Act
            var result = await _users.Where(x => x.Id > 0).AnyAsync();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试：Any 检查空结果集
        /// </summary>
        [Fact]
        [Step(21)]
        public void Any_WithNoMatchingRecords_ReturnsFalse()
        {
            // Arrange - 使用肯定不存在的条件
            var allUsers = _users.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var maxId = allUsers.Max(x => x.Id);

            // Act
            var result = _users.Any(x => x.Id > maxId + 9999);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// 测试：AnyAsync 检查空结果集
        /// </summary>
        [Fact]
        [Step(22)]
        public async Task AnyAsync_WithNoMatchingRecords_ReturnsFalseAsync()
        {
            // Arrange
            var allUsers = _users.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var maxId = allUsers.Max(x => x.Id);

            // Act
            var result = await _users.AnyAsync(x => x.Id > maxId + 9999);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Contains (IN 查询) 测试

        /// <summary>
        /// 测试：使用数组进行 IN 查询
        /// SQL预览: SELECT * FROM `user` WHERE `id` IN (1, 2, 3)
        /// </summary>
        [Fact]
        [Step(23)]
        public void Contains_WithArray_ReturnsMatchingRecords()
        {
            // Arrange
            var allUsers = _users.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var targetIds = allUsers.Take(Math.Min(3, allUsers.Count)).Select(x => x.Id).ToArray();
            var expected = allUsers.Where(x => targetIds.Contains(x.Id)).ToList();

            // Act
            var result = _users.Where(x => targetIds.Contains(x.Id)).ToList();

            // Assert
            Assert.Equal(expected.Count, result.Count);
            foreach (var id in targetIds)
            {
                Assert.Contains(result, x => x.Id == id);
            }
        }

        /// <summary>
        /// 测试：使用 List 进行 IN 查询
        /// SQL预览: SELECT * FROM `user` WHERE `id` IN (1, 2, 3)
        /// </summary>
        [Fact]
        [Step(24)]
        public void Contains_WithList_ReturnsMatchingRecords()
        {
            // Arrange
            var allUsers = _users.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var targetIds = allUsers.Take(Math.Min(3, allUsers.Count)).Select(x => x.Id).ToList();
            var expected = allUsers.Where(x => targetIds.Contains(x.Id)).ToList();

            // Act
            var result = _users.Where(x => targetIds.Contains(x.Id)).ToList();

            // Assert
            Assert.Equal(expected.Count, result.Count);
        }

        /// <summary>
        /// 测试：使用 HashSet 进行 IN 查询
        /// </summary>
        [Fact]
        [Step(25)]
        public void Contains_WithHashSet_ReturnsMatchingRecords()
        {
            // Arrange
            var allUsers = _users.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var targetIds = new HashSet<int>(allUsers.Take(Math.Min(3, allUsers.Count)).Select(x => x.Id));
            var expected = allUsers.Where(x => targetIds.Contains(x.Id)).ToList();

            // Act
            var result = _users.Where(x => targetIds.Contains(x.Id)).ToList();

            // Assert
            Assert.Equal(expected.Count, result.Count);
        }

        /// <summary>
        /// 测试：使用字符串数组进行 IN 查询
        /// SQL预览: SELECT * FROM `user` WHERE `name` IN ('name1', 'name2')
        /// </summary>
        [Fact]
        [Step(26)]
        public void Contains_WithStringArray_ReturnsMatchingRecords()
        {
            // Arrange
            var allUsers = _users.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var targetNames = allUsers
                .Where(x => x.Name != null)
                .Take(Math.Min(2, allUsers.Count))
                .Select(x => x.Name)
                .ToArray();

            if (targetNames.Length == 0)
            {
                return;
            }

            var expected = allUsers.Where(x => targetNames.Contains(x.Name)).ToList();

            // Act
            var result = _users.Where(x => targetNames.Contains(x.Name)).ToList();

            // Assert
            Assert.Equal(expected.Count, result.Count);
        }

        /// <summary>
        /// 测试：使用空集合进行 IN 查询
        /// SQL预览: SELECT * FROM `user` WHERE 1=0 (或类似的空结果)
        /// </summary>
        [Fact]
        [Step(27)]
        public void Contains_WithEmptyCollection_ReturnsEmptyResult()
        {
            // Arrange
            var emptyIds = new int[] { };

            // Act
            var result = _users.Where(x => emptyIds.Contains(x.Id)).ToList();

            // Assert
            Assert.Empty(result);
        }

        /// <summary>
        /// 测试：IN 查询与其他条件组合
        /// SQL预览: SELECT * FROM `user` WHERE `id` IN (1, 2, 3) AND `is_administrator` = 1
        /// </summary>
        [Fact]
        [Step(28)]
        public void Contains_WithOtherConditions_ReturnsFilteredRecords()
        {
            // Arrange
            var allUsers = _users.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var targetIds = allUsers.Take(Math.Min(5, allUsers.Count)).Select(x => x.Id).ToArray();
            var expected = allUsers.Where(x => targetIds.Contains(x.Id) && x.IsAdministrator).ToList();

            // Act
            var result = _users.Where(x => targetIds.Contains(x.Id) && x.IsAdministrator).ToList();

            // Assert
            Assert.Equal(expected.Count, result.Count);
        }

        /// <summary>
        /// 测试：IN 查询异步版本
        /// </summary>
        [Fact]
        [Step(29)]
        public async Task Contains_AsyncToList_ReturnsMatchingRecordsAsync()
        {
            // Arrange
            var allUsers = _users.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var targetIds = allUsers.Take(Math.Min(3, allUsers.Count)).Select(x => x.Id).ToArray();
            var expected = allUsers.Where(x => targetIds.Contains(x.Id)).ToList();

            // Act
            var result = await _users.Where(x => targetIds.Contains(x.Id)).ToListAsync();

            // Assert
            Assert.Equal(expected.Count, result.Count);
        }

        /// <summary>
        /// 测试：NOT IN 查询（使用 !Contains）
        /// SQL预览: SELECT * FROM `user` WHERE `id` NOT IN (1, 2, 3)
        /// </summary>
        [Fact]
        [Step(30)]
        public void NotContains_ReturnsNonMatchingRecords()
        {
            // Arrange
            var allUsers = _users.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var excludeIds = allUsers.Take(Math.Min(2, allUsers.Count)).Select(x => x.Id).ToArray();
            var expected = allUsers.Where(x => !excludeIds.Contains(x.Id)).ToList();

            // Act
            var result = _users.Where(x => !excludeIds.Contains(x.Id)).ToList();

            // Assert
            Assert.Equal(expected.Count, result.Count);
        }

        /// <summary>
        /// 测试：IN 查询与计数组合
        /// </summary>
        [Fact]
        [Step(31)]
        public void Contains_WithCount_ReturnsCorrectCount()
        {
            // Arrange
            var allUsers = _users.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var targetIds = allUsers.Take(Math.Min(3, allUsers.Count)).Select(x => x.Id).ToArray();
            var expected = allUsers.Count(x => targetIds.Contains(x.Id));

            // Act
            var result = _users.Where(x => targetIds.Contains(x.Id)).Count();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试：IN 查询与 Any 组合
        /// </summary>
        [Fact]
        [Step(32)]
        public void Contains_WithAny_ReturnsCorrectResult()
        {
            // Arrange
            var allUsers = _users.ToList();

            if (allUsers.Count == 0)
            {
                Assert.False(_users.Where(x => new int[] { 1, 2, 3 }.Contains(x.Id)).Any());
                return;
            }

            var targetIds = allUsers.Take(Math.Min(3, allUsers.Count)).Select(x => x.Id).ToArray();
            var expected = allUsers.Any(x => targetIds.Contains(x.Id));

            // Act
            var result = _users.Where(x => targetIds.Contains(x.Id)).Any();

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region GroupBy 聚合测试

        /// <summary>
        /// 测试：GroupBy 后使用 Count
        /// SQL预览: SELECT `is_administrator`, COUNT(1) AS `Total` FROM `user` GROUP BY `is_administrator`
        /// </summary>
        [Fact]
        [Step(33)]
        public void GroupBy_Count_ReturnsGroupedCounts()
        {
            // Arrange
            var allUsers = _users.ToList();
            var expectedGroups = allUsers
                .GroupBy(x => x.IsAdministrator)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToList();

            // Act
            var result = (from x in _users
                          group x by x.IsAdministrator into g
                          select new { Key = g.Key, Count = g.Count() })
                         .ToList();

            // Assert
            Assert.Equal(expectedGroups.Count, result.Count);
            foreach (var expected in expectedGroups)
            {
                var actual = result.FirstOrDefault(x => x.Key == expected.Key);
                Assert.NotNull(actual);
                Assert.Equal(expected.Count, actual.Count);
            }
        }

        /// <summary>
        /// 测试：GroupBy 后使用 Max
        /// SQL预览: SELECT `is_administrator`, MAX(`id`) AS `MaxId` FROM `user` GROUP BY `is_administrator`
        /// </summary>
        [Fact]
        [Step(34)]
        public void GroupBy_Max_ReturnsMaxPerGroup()
        {
            // Arrange
            var allUsers = _users.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var expectedGroups = allUsers
                .GroupBy(x => x.IsAdministrator)
                .Select(g => new { Key = g.Key, MaxId = g.Max(x => x.Id) })
                .ToList();

            // Act
            var result = (from x in _users
                          group x by x.IsAdministrator into g
                          select new { Key = g.Key, MaxId = g.Max(x => x.Id) })
                         .ToList();

            // Assert
            Assert.Equal(expectedGroups.Count, result.Count);
            foreach (var expected in expectedGroups)
            {
                var actual = result.FirstOrDefault(x => x.Key == expected.Key);
                Assert.NotNull(actual);
                Assert.Equal(expected.MaxId, actual.MaxId);
            }
        }

        /// <summary>
        /// 测试：GroupBy 后使用 Min
        /// SQL预览: SELECT `is_administrator`, MIN(`id`) AS `MinId` FROM `user` GROUP BY `is_administrator`
        /// </summary>
        [Fact]
        [Step(35)]
        public void GroupBy_Min_ReturnsMinPerGroup()
        {
            // Arrange
            var allUsers = _users.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var expectedGroups = allUsers
                .GroupBy(x => x.IsAdministrator)
                .Select(g => new { Key = g.Key, MinId = g.Min(x => x.Id) })
                .ToList();

            // Act
            var result = (from x in _users
                          group x by x.IsAdministrator into g
                          select new { Key = g.Key, MinId = g.Min(x => x.Id) })
                         .ToList();

            // Assert
            Assert.Equal(expectedGroups.Count, result.Count);
            foreach (var expected in expectedGroups)
            {
                var actual = result.FirstOrDefault(x => x.Key == expected.Key);
                Assert.NotNull(actual);
                Assert.Equal(expected.MinId, actual.MinId);
            }
        }

        /// <summary>
        /// 测试：GroupBy 后使用 Sum
        /// SQL预览: SELECT `is_administrator`, SUM(`id`) AS `SumId` FROM `user` GROUP BY `is_administrator`
        /// </summary>
        [Fact]
        [Step(36)]
        public void GroupBy_Sum_ReturnsSumPerGroup()
        {
            // Arrange
            var allUsers = _users.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var expectedGroups = allUsers
                .GroupBy(x => x.IsAdministrator)
                .Select(g => new { Key = g.Key, SumId = g.Sum(x => x.Id) })
                .ToList();

            // Act
            var result = (from x in _users
                          group x by x.IsAdministrator into g
                          select new { Key = g.Key, SumId = g.Sum(x => x.Id) })
                         .ToList();

            // Assert
            Assert.Equal(expectedGroups.Count, result.Count);
            foreach (var expected in expectedGroups)
            {
                var actual = result.FirstOrDefault(x => x.Key == expected.Key);
                Assert.NotNull(actual);
                Assert.Equal(expected.SumId, actual.SumId);
            }
        }

        /// <summary>
        /// 测试：GroupBy 后使用 Average
        /// SQL预览: SELECT `is_administrator`, AVG(`id`) AS `AvgId` FROM `user` GROUP BY `is_administrator`
        /// </summary>
        [Fact]
        [Step(37)]
        public void GroupBy_Average_ReturnsAveragePerGroup()
        {
            // Arrange
            var allUsers = _users.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var expectedGroups = allUsers
                .GroupBy(x => x.IsAdministrator)
                .Select(g => new { Key = g.Key, AvgId = g.Average(x => x.Id) })
                .ToList();

            // Act
            var result = (from x in _users
                          group x by x.IsAdministrator into g
                          select new { Key = g.Key, AvgId = g.Average(x => x.Id) })
                         .ToList();

            // Assert
            Assert.Equal(expectedGroups.Count, result.Count);
            foreach (var expected in expectedGroups)
            {
                var actual = result.FirstOrDefault(x => x.Key == expected.Key);
                Assert.NotNull(actual);
                Assert.Equal(expected.AvgId, actual.AvgId, 2);
            }
        }

        /// <summary>
        /// 测试：GroupBy 后使用多个聚合函数
        /// SQL预览: SELECT `is_administrator`, COUNT(1) AS `Count`, MAX(`id`) AS `MaxId`, MIN(`id`) AS `MinId` FROM `user` GROUP BY `is_administrator`
        /// </summary>
        [Fact]
        [Step(38)]
        public void GroupBy_MultipleAggregates_ReturnsAllAggregates()
        {
            // Arrange
            var allUsers = _users.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var expectedGroups = allUsers
                .GroupBy(x => x.IsAdministrator)
                .Select(g => new
                {
                    Key = g.Key,
                    Count = g.Count(),
                    MaxId = g.Max(x => x.Id),
                    MinId = g.Min(x => x.Id)
                })
                .ToList();

            // Act
            var result = (from x in _users
                          group x by x.IsAdministrator into g
                          select new
                          {
                              Key = g.Key,
                              Count = g.Count(),
                              MaxId = g.Max(x => x.Id),
                              MinId = g.Min(x => x.Id)
                          })
                         .ToList();

            // Assert
            Assert.Equal(expectedGroups.Count, result.Count);
            foreach (var expected in expectedGroups)
            {
                var actual = result.FirstOrDefault(x => x.Key == expected.Key);
                Assert.NotNull(actual);
                Assert.Equal(expected.Count, actual.Count);
                Assert.Equal(expected.MaxId, actual.MaxId);
                Assert.Equal(expected.MinId, actual.MinId);
            }
        }

        /// <summary>
        /// 测试：GroupBy 按 Id 分组并使用聚合
        /// </summary>
        [Fact]
        [Step(39)]
        public void GroupBy_ById_ReturnsGroupedWithAggregates()
        {
            // Arrange
            var allUsers = _users.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var expectedGroups = allUsers
                .GroupBy(x => x.Id)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToList();

            // Act
            var result = (from x in _users
                          group x by x.Id into g
                          select new { Key = g.Key, Count = g.Count() })
                         .ToList();

            // Assert
            Assert.Equal(expectedGroups.Count, result.Count);
        }

        /// <summary>
        /// 测试：GroupBy 后带 Having 条件
        /// SQL预览: SELECT `is_administrator`, COUNT(1) AS `Count` FROM `user` GROUP BY `is_administrator` HAVING COUNT(1) > 1
        /// </summary>
        [Fact]
        [Step(40)]
        public void GroupBy_Having_ReturnsFilteredGroups()
        {
            // Arrange
            var allUsers = _users.ToList();

            var expectedGroups = allUsers
                .GroupBy(x => x.IsAdministrator)
                .Where(g => g.Count() > 1)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToList();

            // Act
            var result = (from x in _users
                          group x by x.IsAdministrator into g
                          where g.Count() > 1
                          select new { Key = g.Key, Count = g.Count() })
                         .ToList();

            // Assert
            Assert.Equal(expectedGroups.Count, result.Count);
        }

        #endregion

        #region 综合测试

        /// <summary>
        /// 测试：Count 和 Any 一致性
        /// </summary>
        [Fact]
        [Step(41)]
        public void CountAndAny_ConsistentResults()
        {
            // Arrange & Act
            var count = _users.Count();
            var any = _users.Any();

            // Assert
            Assert.Equal(count > 0, any);
        }

        /// <summary>
        /// 测试：异步 Count 和 Any 一致性
        /// </summary>
        [Fact]
        [Step(42)]
        public async Task CountAndAnyAsync_ConsistentResultsAsync()
        {
            // Arrange & Act
            var count = await _users.CountAsync();
            var any = await _users.AnyAsync();

            // Assert
            Assert.Equal(count > 0, any);
        }

        /// <summary>
        /// 测试：带条件的 Count 和 Any 一致性
        /// </summary>
        [Fact]
        [Step(43)]
        public void CountAndAny_WithPredicate_ConsistentResults()
        {
            // Arrange & Act
            var count = _users.Count(x => x.IsAdministrator);
            var any = _users.Any(x => x.IsAdministrator);

            // Assert
            Assert.Equal(count > 0, any);
        }

        /// <summary>
        /// 测试：Count 和 LongCount 返回相同值
        /// </summary>
        [Fact]
        [Step(44)]
        public void Count_And_LongCount_ReturnSameValue()
        {
            // Act
            var count = _users.Count();
            var longCount = _users.LongCount();

            // Assert
            Assert.Equal(count, longCount);
        }

        /// <summary>
        /// 测试：异步 Count 和 LongCount 返回相同值
        /// </summary>
        [Fact]
        [Step(45)]
        public async Task CountAsync_And_LongCountAsync_ReturnSameValueAsync()
        {
            // Act
            var count = await _users.CountAsync();
            var longCount = await _users.LongCountAsync();

            // Assert
            Assert.Equal(count, longCount);
        }

        /// <summary>
        /// 测试：Where 后的 Count 与直接 Count 带谓词结果一致
        /// </summary>
        [Fact]
        [Step(46)]
        public void WhereCount_And_CountWithPredicate_ReturnSameValue()
        {
            // Act
            var whereCount = _users.Where(x => x.IsAdministrator).Count();
            var predicateCount = _users.Count(x => x.IsAdministrator);

            // Assert
            Assert.Equal(whereCount, predicateCount);
        }

        /// <summary>
        /// 测试：Where 后的 Any 与直接 Any 带谓词结果一致
        /// </summary>
        [Fact]
        [Step(47)]
        public void WhereAny_And_AnyWithPredicate_ReturnSameValue()
        {
            // Act
            var whereAny = _users.Where(x => x.IsAdministrator).Any();
            var predicateAny = _users.Any(x => x.IsAdministrator);

            // Assert
            Assert.Equal(whereAny, predicateAny);
        }

        /// <summary>
        /// 测试：多条件 Where 后的聚合
        /// </summary>
        [Fact]
        [Step(48)]
        public void MultipleWhere_WithAggregates_ReturnsCorrectResults()
        {
            // Arrange
            var allUsers = _users.ToList();
            var expected = allUsers
                .Where(x => x.Id > 0)
                .Where(x => x.Name != null)
                .Count();

            // Act
            var result = _users
                .Where(x => x.Id > 0)
                .Where(x => x.Name != null)
                .Count();

            // Assert
            Assert.Equal(expected, result);
        }

        /// <summary>
        /// 测试：Take 后的聚合（需要排序）
        /// </summary>
        [Fact]
        [Step(49)]
        public void Take_WithCount_ReturnsLimitedCount()
        {
            // Arrange
            var allUsers = _users.ToList();
            var takeCount = Math.Min(5, allUsers.Count);
            var expected = allUsers.OrderBy(x => x.Id).Take(takeCount).Count();

            // Act - 框架不支持 Take().Count() 组合，使用 ToList() 验证
            var result = _users.OrderBy(x => x.Id).Take(takeCount).ToList();

            // Assert
            Assert.Equal(expected, result.Count);
        }

        /// <summary>
        /// 测试：Skip 和 Take 后的聚合（需要排序）
        /// </summary>
        [Fact]
        [Step(50)]
        public void SkipTake_WithCount_ReturnsPagedCount()
        {
            // Arrange
            var allUsers = _users.ToList();
            var skipCount = Math.Min(2, allUsers.Count);
            var takeCount = Math.Min(3, Math.Max(0, allUsers.Count - skipCount));
            var expected = allUsers.OrderBy(x => x.Id).Skip(skipCount).Take(takeCount).Count();

            // Act - 框架不支持 Skip().Take().Count() 组合，使用 ToList() 验证
            var result = _users.OrderBy(x => x.Id).Skip(skipCount).Take(takeCount).ToList();

            // Assert
            Assert.Equal(expected, result.Count);
        }

        #endregion
    }
}
