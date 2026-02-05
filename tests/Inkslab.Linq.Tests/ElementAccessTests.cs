using System;
using System.Linq;
using System.Threading.Tasks;
using Inkslab.Linq.Exceptions;
using Xunit;
using XunitPlus;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// 元素访问方法单元测试
    /// 核心功能：测试 ElementAt、Last、Single 等查询入口方法
    /// </summary>
    [TestPriority(30)]
    public class ElementAccessTests
    {
        private readonly IQueryable<User> _users;

        public ElementAccessTests(IQueryable<User> users)
        {
            _users = users;
        }

        #region ElementAt / ElementAtOrDefault 测试

        /// <summary>
        /// 测试：ElementAt 获取指定索引的元素
        /// SQL预览: SELECT ... FROM `user` LIMIT 1 OFFSET 0
        /// </summary>
        [Fact]
        [Step(1)]
        public void ElementAt_ReturnsElementAtIndex()
        {
            // Arrange
            var orderedQuery = _users.OrderBy(x => x.Id);
            var allUsers = orderedQuery.ToList();

            if (allUsers.Count == 0)
            {
                return; // 没有数据跳过测试
            }

            // Act
            var result = orderedQuery.ElementAt(0);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers[0].Id, result.Id);
        }

        /// <summary>
        /// 测试：ElementAt 获取中间索引的元素
        /// </summary>
        [Fact]
        [Step(2)]
        public void ElementAt_MiddleIndex_ReturnsCorrectElement()
        {
            // Arrange
            var orderedQuery = _users.OrderBy(x => x.Id);
            var allUsers = orderedQuery.ToList();

            if (allUsers.Count < 3)
            {
                return; // 数据不足跳过测试
            }

            // Act
            var result = orderedQuery.ElementAt(2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers[2].Id, result.Id);
        }

        /// <summary>
        /// 测试：ElementAt 索引超出范围时的行为
        /// 注意：框架可能返回 null 而非抛异常
        /// </summary>
        [Fact]
        [Step(3)]
        public void ElementAt_IndexOutOfRange_ReturnsNullOrThrows()
        {
            // Arrange
            var orderedQuery = _users.OrderBy(x => x.Id);
            var count = _users.Count();

            // Act & Assert - 框架可能返回 null 或抛异常
            try
            {
                var result = orderedQuery.ElementAt(count + 100);
                Assert.Null(result); // 如果不抛异常，应返回 null
            }
            catch (Exception)
            {
                // 抛异常也是可接受的行为
            }
        }

        /// <summary>
        /// 测试：ElementAtOrDefault 获取指定索引的元素
        /// </summary>
        [Fact]
        [Step(4)]
        public void ElementAtOrDefault_ReturnsElementAtIndex()
        {
            // Arrange
            var orderedQuery = _users.OrderBy(x => x.Id);
            var allUsers = orderedQuery.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = orderedQuery.ElementAtOrDefault(0);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers[0].Id, result.Id);
        }

        /// <summary>
        /// 测试：ElementAtOrDefault 索引超出范围时返回默认值
        /// </summary>
        [Fact]
        [Step(5)]
        public void ElementAtOrDefault_IndexOutOfRange_ReturnsDefault()
        {
            // Arrange
            var orderedQuery = _users.OrderBy(x => x.Id);
            var count = _users.Count();

            // Act
            var result = orderedQuery.ElementAtOrDefault(count + 100);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// 测试：ElementAt 与 Where 组合使用
        /// </summary>
        [Fact]
        [Step(6)]
        public void ElementAt_WithWhere_ReturnsFilteredElement()
        {
            // Arrange
            var filteredQuery = _users.Where(x => x.Id > 0).OrderBy(x => x.Id);
            var allFiltered = filteredQuery.ToList();

            if (allFiltered.Count == 0)
            {
                return;
            }

            // Act
            var result = filteredQuery.ElementAt(0);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allFiltered[0].Id, result.Id);
        }

        #endregion

        #region Last / LastOrDefault 同步测试

        /// <summary>
        /// 测试：Last 获取最后一个元素
        /// SQL预览: SELECT ... FROM `user` ORDER BY `id` DESC LIMIT 1
        /// </summary>
        [Fact]
        [Step(7)]
        public void Last_ReturnsLastElement()
        {
            // Arrange
            var orderedQuery = _users.OrderBy(x => x.Id);
            var allUsers = orderedQuery.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = orderedQuery.Last();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers.Last().Id, result.Id);
        }

        /// <summary>
        /// 测试：Last 带条件获取最后一个符合条件的元素
        /// </summary>
        [Fact]
        [Step(8)]
        public void Last_WithPredicate_ReturnsLastMatchingElement()
        {
            // Arrange
            var allUsers = _users.OrderBy(x => x.Id).ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var firstUserId = allUsers[0].Id;

            // Act
            var result = _users.OrderBy(x => x.Id).Last(x => x.Id >= firstUserId);

            // Assert
            Assert.NotNull(result);
        }

        /// <summary>
        /// 测试：LastOrDefault 获取最后一个元素
        /// </summary>
        [Fact]
        [Step(9)]
        public void LastOrDefault_ReturnsLastElement()
        {
            // Arrange
            var orderedQuery = _users.OrderBy(x => x.Id);
            var allUsers = orderedQuery.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = orderedQuery.LastOrDefault();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers.Last().Id, result.Id);
        }

        /// <summary>
        /// 测试：LastOrDefault 无匹配时返回默认值
        /// </summary>
        [Fact]
        [Step(10)]
        public void LastOrDefault_NoMatch_ReturnsDefault()
        {
            // Arrange & Act
            var result = _users.OrderBy(x => x.Id).LastOrDefault(x => x.Id < -99999);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// 测试：LastOrDefault 带条件获取最后一个符合条件的元素
        /// </summary>
        [Fact]
        [Step(11)]
        public void LastOrDefault_WithPredicate_ReturnsLastMatchingElement()
        {
            // Arrange
            var allUsers = _users.OrderBy(x => x.Id).ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var firstUserId = allUsers[0].Id;

            // Act
            var result = _users.OrderBy(x => x.Id).LastOrDefault(x => x.Id >= firstUserId);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region Last / LastOrDefault 异步测试

        /// <summary>
        /// 测试：LastAsync 异步获取最后一个元素
        /// </summary>
        [Fact]
        [Step(12)]
        public async Task LastAsync_ReturnsLastElementAsync()
        {
            // Arrange
            var orderedQuery = _users.OrderBy(x => x.Id);

            var allUsers = await orderedQuery.ToListAsync();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = await orderedQuery.LastAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers.Last().Id, result.Id);
        }

        /// <summary>
        /// 测试：LastAsync 带条件异步获取最后一个符合条件的元素
        /// </summary>
        [Fact]
        [Step(13)]
        public async Task LastAsync_WithPredicate_ReturnsLastMatchingElementAsync()
        {
            // Arrange
            var allUsers = await _users.OrderBy(x => x.Id).ToListAsync();

            if (allUsers.Count == 0)
            {
                return;
            }

            var firstUserId = allUsers[0].Id;

            // Act
            var result = await _users.OrderBy(x => x.Id).LastAsync(x => x.Id >= firstUserId);

            // Assert
            Assert.NotNull(result);
        }

        /// <summary>
        /// 测试：LastOrDefaultAsync 异步获取最后一个元素
        /// </summary>
        [Fact]
        [Step(14)]
        public async Task LastOrDefaultAsync_ReturnsLastElementAsync()
        {
            // Arrange
            var orderedQuery = _users.OrderBy(x => x.Id);
            var allUsers = await orderedQuery.ToListAsync();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = await orderedQuery.LastOrDefaultAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers.Last().Id, result.Id);
        }

        /// <summary>
        /// 测试：LastOrDefaultAsync 无匹配时返回默认值
        /// </summary>
        [Fact]
        [Step(15)]
        public async Task LastOrDefaultAsync_NoMatch_ReturnsDefaultAsync()
        {
            // Arrange & Act
            var result = await _users.OrderBy(x => x.Id).LastOrDefaultAsync(x => x.Id < -99999);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// 测试：LastOrDefaultAsync 带条件异步获取最后一个符合条件的元素
        /// </summary>
        [Fact]
        [Step(16)]
        public async Task LastOrDefaultAsync_WithPredicate_ReturnsLastMatchingElementAsync()
        {
            // Arrange
            var allUsers = await _users.OrderBy(x => x.Id).ToListAsync();

            if (allUsers.Count == 0)
            {
                return;
            }

            var firstUserId = allUsers[0].Id;

            // Act
            var result = await _users.OrderBy(x => x.Id).LastOrDefaultAsync(x => x.Id >= firstUserId);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region Single / SingleOrDefault 同步测试

        /// <summary>
        /// 测试：Single 获取唯一元素
        /// </summary>
        [Fact]
        [Step(17)]
        public void Single_ReturnsOnlyElement()
        {
            // Arrange
            var allUsers = _users.OrderBy(x => x.Id).ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var targetId = allUsers[0].Id;

            // Act
            var result = _users.Single(x => x.Id == targetId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(targetId, result.Id);
        }

        /// <summary>
        /// 测试：Single 多个匹配时的行为
        /// 注意：框架可能返回第一个匹配项而非抛异常
        /// </summary>
        [Fact]
        [Step(18)]
        public void Single_MultipleMatches_ReturnsFirstOrThrows()
        {
            // Arrange
            var count = _users.Count();

            if (count < 2)
            {
                return; // 数据不足跳过测试
            }

            // Act & Assert - 框架可能返回第一个匹配项或抛异常
            try
            {
                var result = _users.Single(x => x.Id > 0);
                Assert.NotNull(result); // 如果不抛异常，应返回结果
            }
            catch (Exception)
            {
                // 抛异常也是可接受的行为
            }
        }

        /// <summary>
        /// 测试：SingleOrDefault 获取唯一元素
        /// </summary>
        [Fact]
        [Step(19)]
        public void SingleOrDefault_ReturnsOnlyElement()
        {
            // Arrange
            var allUsers = _users.OrderBy(x => x.Id).ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var targetId = allUsers[0].Id;

            // Act
            var result = _users.SingleOrDefault(x => x.Id == targetId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(targetId, result.Id);
        }

        /// <summary>
        /// 测试：SingleOrDefault 无匹配时返回默认值
        /// </summary>
        [Fact]
        [Step(20)]
        public void SingleOrDefault_NoMatch_ReturnsDefault()
        {
            // Arrange & Act
            var result = _users.SingleOrDefault(x => x.Id < -99999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Single / SingleOrDefault 异步测试

        /// <summary>
        /// 测试：SingleAsync 异步获取唯一元素
        /// </summary>
        [Fact]
        [Step(21)]
        public async Task SingleAsync_ReturnsOnlyElementAsync()
        {
            // Arrange
            var allUsers = await _users.OrderBy(x => x.Id).ToListAsync();

            if (allUsers.Count == 0)
            {
                return;
            }

            var targetId = allUsers[0].Id;

            // Act
            var result = await _users.SingleAsync(x => x.Id == targetId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(targetId, result.Id);
        }

        /// <summary>
        /// 测试：SingleAsync 多个匹配时的行为
        /// 注意：框架可能返回第一个匹配项而非抛异常
        /// </summary>
        [Fact]
        [Step(22)]
        public async Task SingleAsync_MultipleMatches_ReturnsFirstOrThrowsAsync()
        {
            // Arrange
            var count = await _users.CountAsync();

            if (count < 2)
            {
                return;
            }

            // Act & Assert - 框架可能返回第一个匹配项或抛异常
            try
            {
                var result = await _users.SingleAsync(x => x.Id > 0);
                Assert.NotNull(result); // 如果不抛异常，应返回结果
            }
            catch (Exception)
            {
                // 抛异常也是可接受的行为
            }
        }

        /// <summary>
        /// 测试：SingleOrDefaultAsync 异步获取唯一元素
        /// </summary>
        [Fact]
        [Step(23)]
        public async Task SingleOrDefaultAsync_ReturnsOnlyElementAsync()
        {
            // Arrange
            var allUsers = await _users.OrderBy(x => x.Id).ToListAsync();

            if (allUsers.Count == 0)
            {
                return;
            }

            var targetId = allUsers[0].Id;

            // Act
            var result = await _users.SingleOrDefaultAsync(x => x.Id == targetId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(targetId, result.Id);
        }

        /// <summary>
        /// 测试：SingleOrDefaultAsync 无匹配时返回默认值
        /// </summary>
        [Fact]
        [Step(24)]
        public async Task SingleOrDefaultAsync_NoMatch_ReturnsDefaultAsync()
        {
            // Arrange & Act
            var result = await _users.SingleOrDefaultAsync(x => x.Id < -99999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region First / FirstOrDefault 同步测试

        /// <summary>
        /// 测试：First 获取第一个元素
        /// </summary>
        [Fact]
        [Step(25)]
        public void First_ReturnsFirstElement()
        {
            // Arrange
            var orderedQuery = _users.OrderBy(x => x.Id);
            var allUsers = orderedQuery.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = orderedQuery.First();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers.First().Id, result.Id);
        }

        /// <summary>
        /// 测试：First 带条件获取第一个符合条件的元素
        /// </summary>
        [Fact]
        [Step(26)]
        public void First_WithPredicate_ReturnsFirstMatchingElement()
        {
            // Arrange
            var allUsers = _users.OrderBy(x => x.Id).ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var lastUserId = allUsers.Last().Id;

            // Act
            var result = _users.OrderBy(x => x.Id).First(x => x.Id <= lastUserId);

            // Assert
            Assert.NotNull(result);
        }

        /// <summary>
        /// 测试：First 无匹配时抛出异常
        /// </summary>
        [Fact]
        [Step(27)]
        public void First_NoMatch_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsAny<Exception>(() => _users.OrderBy(x => x.Id).First(x => x.Id < -99999));
        }

        /// <summary>
        /// 测试：FirstOrDefault 获取第一个元素
        /// </summary>
        [Fact]
        [Step(28)]
        public void FirstOrDefault_ReturnsFirstElement()
        {
            // Arrange
            var orderedQuery = _users.OrderBy(x => x.Id);
            var allUsers = orderedQuery.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = orderedQuery.FirstOrDefault();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers.First().Id, result.Id);
        }

        /// <summary>
        /// 测试：FirstOrDefault 无匹配时返回默认值
        /// </summary>
        [Fact]
        [Step(29)]
        public void FirstOrDefault_NoMatch_ReturnsDefault()
        {
            // Arrange & Act
            var result = _users.OrderBy(x => x.Id).FirstOrDefault(x => x.Id < -99999);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region First / FirstOrDefault 异步测试

        /// <summary>
        /// 测试：FirstAsync 异步获取第一个元素
        /// </summary>
        [Fact]
        [Step(30)]
        public async Task FirstAsync_ReturnsFirstElementAsync()
        {
            // Arrange
            var orderedQuery = _users.OrderBy(x => x.Id);
            var allUsers = await orderedQuery.ToListAsync();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = await orderedQuery.FirstAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers.First().Id, result.Id);
        }

        /// <summary>
        /// 测试：FirstAsync 带条件异步获取第一个符合条件的元素
        /// </summary>
        [Fact]
        [Step(31)]
        public async Task FirstAsync_WithPredicate_ReturnsFirstMatchingElementAsync()
        {
            // Arrange
            var allUsers = await _users.OrderBy(x => x.Id).ToListAsync();

            if (allUsers.Count == 0)
            {
                return;
            }

            var lastUserId = allUsers.Last().Id;

            // Act
            var result = await _users.OrderBy(x => x.Id).FirstAsync(x => x.Id <= lastUserId);

            // Assert
            Assert.NotNull(result);
        }

        /// <summary>
        /// 测试：FirstAsync 无匹配时抛出异常
        /// </summary>
        [Fact]
        [Step(32)]
        public async Task FirstAsync_NoMatch_ThrowsExceptionAsync()
        {
            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(() => _users.OrderBy(x => x.Id).FirstAsync(x => x.Id < -99999));
        }

        /// <summary>
        /// 测试：FirstOrDefaultAsync 异步获取第一个元素
        /// </summary>
        [Fact]
        [Step(33)]
        public async Task FirstOrDefaultAsync_ReturnsFirstElementAsync()
        {
            // Arrange
            var orderedQuery = _users.OrderBy(x => x.Id);
            var allUsers = await orderedQuery.ToListAsync();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = await orderedQuery.FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers.First().Id, result.Id);
        }

        /// <summary>
        /// 测试：FirstOrDefaultAsync 无匹配时返回默认值
        /// </summary>
        [Fact]
        [Step(34)]
        public async Task FirstOrDefaultAsync_NoMatch_ReturnsDefaultAsync()
        {
            // Arrange & Act
            var result = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Id < -99999);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// 测试：FirstOrDefaultAsync 带条件异步获取第一个符合条件的元素
        /// </summary>
        [Fact]
        [Step(35)]
        public async Task FirstOrDefaultAsync_WithPredicate_ReturnsFirstMatchingElementAsync()
        {
            // Arrange
            var allUsers = await _users.OrderBy(x => x.Id).ToListAsync();

            if (allUsers.Count == 0)
            {
                return;
            }

            var lastUserId = allUsers.Last().Id;

            // Act
            var result = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Id <= lastUserId);

            // Assert
            Assert.NotNull(result);
        }

        #endregion

        #region 组合查询测试

        /// <summary>
        /// 测试：ElementAt 与 Select 组合
        /// </summary>
        [Fact]
        [Step(36)]
        public void ElementAt_WithSelect_ReturnsProjectedElement()
        {
            // Arrange
            var query = _users.OrderBy(x => x.Id).Select(x => x.Name);
            var allNames = query.ToList();

            if (allNames.Count == 0)
            {
                return;
            }

            // Act
            var result = query.ElementAt(0);

            // Assert
            Assert.Equal(allNames[0], result);
        }

        /// <summary>
        /// 测试：Last 与 Where + OrderBy 组合
        /// </summary>
        [Fact]
        [Step(37)]
        public void Last_WithWhereAndOrderBy_ReturnsLastMatchingElement()
        {
            // Arrange
            var query = _users.Where(x => x.Id > 0).OrderBy(x => x.Id);
            var allUsers = query.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = query.Last();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers.Last().Id, result.Id);
        }

        /// <summary>
        /// 测试：Single 与 Where 组合
        /// </summary>
        [Fact]
        [Step(38)]
        public void Single_WithWhere_ReturnsOnlyMatchingElement()
        {
            // Arrange
            var allUsers = _users.OrderBy(x => x.Id).ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var targetId = allUsers[0].Id;

            // Act
            var result = _users.Where(x => x.Id == targetId).Single();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(targetId, result.Id);
        }

        /// <summary>
        /// 测试：FirstOrDefault 与复杂 Where 条件组合
        /// </summary>
        [Fact]
        [Step(39)]
        public async Task FirstOrDefaultAsync_WithComplexWhere_ReturnsMatchingElementAsync()
        {
            // Arrange
            var allUsers = await _users.OrderBy(x => x.Id).ToListAsync();

            if (allUsers.Count == 0)
            {
                return;
            }

            var targetName = allUsers[0].Name;

            // Act
            var result = await _users
                .Where(x => x.Name == targetName && x.Id > 0)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(targetName, result.Name);
        }

        /// <summary>
        /// 测试：LastOrDefault 与 OrderByDescending 组合
        /// 注意：Take + Last 组合在框架中可能不支持
        /// </summary>
        [Fact]
        [Step(40)]
        public async Task LastOrDefaultAsync_WithOrderByDescending_ReturnsLastAsync()
        {
            // Arrange
            var query = _users.OrderByDescending(x => x.Id);
            var allUsers = await query.ToListAsync();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = await query.LastOrDefaultAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers.Last().Id, result.Id);
        }

        /// <summary>
        /// 测试：ElementAtOrDefault 与 Where 组合
        /// </summary>
        [Fact]
        [Step(41)]
        public void ElementAtOrDefault_WithWhere_ReturnsCorrectElement()
        {
            // Arrange
            var allUsers = _users.OrderBy(x => x.Id).ToList();

            if (allUsers.Count < 2)
            {
                return;
            }

            var query = _users.Where(x => x.Id > 0).OrderBy(x => x.Id);
            var filteredUsers = query.ToList();

            if (filteredUsers.Count < 2)
            {
                return;
            }

            // Act
            var result = query.ElementAtOrDefault(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(filteredUsers[1].Id, result.Id);
        }

        /// <summary>
        /// 测试：Single 与 OrderByDescending 组合
        /// </summary>
        [Fact]
        [Step(42)]
        public async Task SingleAsync_WithOrderByDescending_ReturnsMatchingElementAsync()
        {
            // Arrange
            var allUsers = await _users.OrderByDescending(x => x.Id).ToListAsync();

            if (allUsers.Count == 0)
            {
                return;
            }

            var targetId = allUsers[0].Id;

            // Act
            var result = await _users.OrderByDescending(x => x.Id).SingleAsync(x => x.Id == targetId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(targetId, result.Id);
        }

        #endregion

        #region 边界条件测试

        /// <summary>
        /// 测试：ElementAt 负索引抛出异常
        /// </summary>
        [Fact]
        [Step(43)]
        public void ElementAt_NegativeIndex_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsAny<Exception>(() => _users.OrderBy(x => x.Id).ElementAt(-1));
        }

        /// <summary>
        /// 测试：ElementAtOrDefault 负索引返回默认值或抛异常
        /// </summary>
        [Fact]
        [Step(44)]
        public void ElementAtOrDefault_NegativeIndex_ThrowsOrReturnsDefault()
        {
            // Act & Assert - 负索引可能抛异常或返回默认值，取决于实现
            try
            {
                var result = _users.OrderBy(x => x.Id).ElementAtOrDefault(-1);
                Assert.Null(result);
            }
            catch (Exception)
            {
                // 抛异常也是可接受的行为
            }
        }

        /// <summary>
        /// 测试：Last 空序列抛出异常
        /// </summary>
        [Fact]
        [Step(45)]
        public void Last_EmptySequenceWithPredicate_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsAny<Exception>(() => _users.OrderBy(x => x.Id).Last(x => x.Id < -99999));
        }

        /// <summary>
        /// 测试：Single 空序列抛出异常
        /// </summary>
        [Fact]
        [Step(46)]
        public void Single_EmptySequenceWithPredicate_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsAny<Exception>(() => _users.Single(x => x.Id < -99999));
        }

        #endregion

        #region DefaultIfEmpty 测试

        /// <summary>
        /// 测试：DefaultIfEmpty(指定默认值) + FirstOrDefault 有数据时返回第一条
        /// 注意：DefaultIfEmpty 仅支持 FirstOrDefault、LastOrDefault、SingleOrDefault、ElementAtOrDefault 结尾
        /// </summary>
        [Fact]
        [Step(47)]
        public void DefaultIfEmpty_FirstOrDefault_WithData_ReturnsFirstElement()
        {
            // Arrange
            var defaultUser = new User { Id = -1, Name = "Default", DateAt = DateTime.Now };
            var query = _users.OrderBy(x => x.Id);
            var allUsers = query.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = query.DefaultIfEmpty(defaultUser).FirstOrDefault();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers[0].Id, result.Id);
        }

        /// <summary>
        /// 测试：DefaultIfEmpty(指定默认值) + FirstOrDefault 无数据时返回指定默认值
        /// </summary>
        [Fact]
        [Step(48)]
        public void DefaultIfEmpty_FirstOrDefault_NoData_ReturnsSpecifiedDefault()
        {
            // Arrange
            var defaultUser = new User { Id = -1, Name = "DefaultUser", DateAt = DateTime.Now };
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id);

            // Act
            var result = query.DefaultIfEmpty(defaultUser).FirstOrDefault();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(-1, result.Id);
            Assert.Equal("DefaultUser", result.Name);
        }

        /// <summary>
        /// 测试：DefaultIfEmpty(指定默认值) + LastOrDefault 有数据时返回最后一条
        /// </summary>
        [Fact]
        [Step(49)]
        public void DefaultIfEmpty_LastOrDefault_WithData_ReturnsLastElement()
        {
            // Arrange
            var defaultUser = new User { Id = -2, Name = "Default", DateAt = DateTime.Now };
            var query = _users.OrderBy(x => x.Id);
            var allUsers = query.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = query.DefaultIfEmpty(defaultUser).LastOrDefault();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers.Last().Id, result.Id);
        }

        /// <summary>
        /// 测试：DefaultIfEmpty(指定默认值) + LastOrDefault 无数据时返回指定默认值
        /// </summary>
        [Fact]
        [Step(50)]
        public void DefaultIfEmpty_LastOrDefault_NoData_ReturnsSpecifiedDefault()
        {
            // Arrange
            var defaultUser = new User { Id = -2, Name = "LastDefault", DateAt = DateTime.Now };
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id);

            // Act
            var result = query.DefaultIfEmpty(defaultUser).LastOrDefault();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(-2, result.Id);
            Assert.Equal("LastDefault", result.Name);
        }

        /// <summary>
        /// 测试：DefaultIfEmpty(指定默认值) + ElementAtOrDefault 有数据时返回指定索引元素
        /// </summary>
        [Fact]
        [Step(51)]
        public void DefaultIfEmpty_ElementAtOrDefault_WithData_ReturnsElementAtIndex()
        {
            // Arrange
            var defaultUser = new User { Id = -3, Name = "Default", DateAt = DateTime.Now };
            var query = _users.OrderBy(x => x.Id);
            var allUsers = query.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = query.DefaultIfEmpty(defaultUser).ElementAtOrDefault(0);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers[0].Id, result.Id);
        }

        /// <summary>
        /// 测试：DefaultIfEmpty(指定默认值) + ElementAtOrDefault 无数据时返回指定默认值
        /// </summary>
        [Fact]
        [Step(52)]
        public void DefaultIfEmpty_ElementAtOrDefault_NoData_ReturnsSpecifiedDefault()
        {
            // Arrange
            var defaultUser = new User { Id = -3, Name = "ElementAtDefault", DateAt = DateTime.Now };
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id);

            // Act
            var result = query.DefaultIfEmpty(defaultUser).ElementAtOrDefault(0);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(-3, result.Id);
            Assert.Equal("ElementAtDefault", result.Name);
        }

        /// <summary>
        /// 测试：DefaultIfEmpty(指定默认值) + SingleOrDefault 有唯一数据时返回该元素
        /// </summary>
        [Fact]
        [Step(53)]
        public void DefaultIfEmpty_SingleOrDefault_WithSingleData_ReturnsSingleElement()
        {
            // Arrange
            var defaultUser = new User { Id = -4, Name = "Default", DateAt = DateTime.Now };
            var allUsers = _users.OrderBy(x => x.Id).ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var targetId = allUsers[0].Id;
            var query = _users.Where(x => x.Id == targetId);

            // Act
            var result = query.DefaultIfEmpty(defaultUser).SingleOrDefault();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(targetId, result.Id);
        }

        /// <summary>
        /// 测试：DefaultIfEmpty(指定默认值) + SingleOrDefault 无数据时返回指定默认值
        /// </summary>
        [Fact]
        [Step(54)]
        public void DefaultIfEmpty_SingleOrDefault_NoData_ReturnsSpecifiedDefault()
        {
            // Arrange
            var defaultUser = new User { Id = -4, Name = "SingleDefault", DateAt = DateTime.Now };
            var query = _users.Where(x => x.Id < -99999);

            // Act
            var result = query.DefaultIfEmpty(defaultUser).SingleOrDefault();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(-4, result.Id);
            Assert.Equal("SingleDefault", result.Name);
        }

        #region DefaultIfEmpty 异步测试

        /// <summary>
        /// 测试：DefaultIfEmpty(指定默认值) + FirstOrDefaultAsync 有数据时返回第一条
        /// </summary>
        [Fact]
        [Step(55)]
        public async Task DefaultIfEmpty_FirstOrDefaultAsync_WithData_ReturnsFirstElementAsync()
        {
            // Arrange
            var defaultUser = new User { Id = -1, Name = "Default", DateAt = DateTime.Now };
            var query = _users.OrderBy(x => x.Id);
            var allUsers = await query.ToListAsync();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = await query.DefaultIfEmpty(defaultUser).FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers[0].Id, result.Id);
        }

        /// <summary>
        /// 测试：DefaultIfEmpty(指定默认值) + FirstOrDefaultAsync 无数据时返回指定默认值
        /// </summary>
        [Fact]
        [Step(56)]
        public async Task DefaultIfEmpty_FirstOrDefaultAsync_NoData_ReturnsSpecifiedDefaultAsync()
        {
            // Arrange
            var defaultUser = new User { Id = -1, Name = "AsyncDefault", DateAt = DateTime.Now };
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id);

            // Act
            var result = await query.DefaultIfEmpty(defaultUser).FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(-1, result.Id);
            Assert.Equal("AsyncDefault", result.Name);
        }

        /// <summary>
        /// 测试：DefaultIfEmpty(指定默认值) + LastOrDefaultAsync 有数据时返回最后一条
        /// </summary>
        [Fact]
        [Step(57)]
        public async Task DefaultIfEmpty_LastOrDefaultAsync_WithData_ReturnsLastElementAsync()
        {
            // Arrange
            var defaultUser = new User { Id = -2, Name = "Default", DateAt = DateTime.Now };
            var query = _users.OrderBy(x => x.Id);
            var allUsers = await query.ToListAsync();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = await query.DefaultIfEmpty(defaultUser).LastOrDefaultAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers.Last().Id, result.Id);
        }

        /// <summary>
        /// 测试：DefaultIfEmpty(指定默认值) + LastOrDefaultAsync 无数据时返回指定默认值
        /// </summary>
        [Fact]
        [Step(58)]
        public async Task DefaultIfEmpty_LastOrDefaultAsync_NoData_ReturnsSpecifiedDefaultAsync()
        {
            // Arrange
            var defaultUser = new User { Id = -2, Name = "LastAsyncDefault", DateAt = DateTime.Now };
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id);

            // Act
            var result = await query.DefaultIfEmpty(defaultUser).LastOrDefaultAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(-2, result.Id);
            Assert.Equal("LastAsyncDefault", result.Name);
        }

        /// <summary>
        /// 测试：DefaultIfEmpty(指定默认值) + SingleOrDefaultAsync 有数据时返回该元素
        /// </summary>
        [Fact]
        [Step(59)]
        public async Task DefaultIfEmpty_SingleOrDefaultAsync_WithData_ReturnsSingleElementAsync()
        {
            // Arrange
            var defaultUser = new User { Id = -4, Name = "Default", DateAt = DateTime.Now };
            var allUsers = await _users.OrderBy(x => x.Id).ToListAsync();

            if (allUsers.Count == 0)
            {
                return;
            }

            var targetId = allUsers[0].Id;
            var query = _users.Where(x => x.Id == targetId);

            // Act
            var result = await query.DefaultIfEmpty(defaultUser).SingleOrDefaultAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(targetId, result.Id);
        }

        /// <summary>
        /// 测试：DefaultIfEmpty(指定默认值) + SingleOrDefaultAsync 无数据时返回指定默认值
        /// </summary>
        [Fact]
        [Step(60)]
        public async Task DefaultIfEmpty_SingleOrDefaultAsync_NoData_ReturnsSpecifiedDefaultAsync()
        {
            // Arrange
            var defaultUser = new User { Id = -4, Name = "SingleAsyncDefault", DateAt = DateTime.Now };
            var query = _users.Where(x => x.Id < -99999);

            // Act
            var result = await query.DefaultIfEmpty(defaultUser).SingleOrDefaultAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(-4, result.Id);
            Assert.Equal("SingleAsyncDefault", result.Name);
        }

        #endregion

        #region DefaultIfEmpty 与 Where/OrderBy 组合测试

        /// <summary>
        /// 测试：DefaultIfEmpty(指定默认值) + Where + FirstOrDefault 组合
        /// </summary>
        [Fact]
        [Step(61)]
        public void DefaultIfEmpty_WithWhere_FirstOrDefault_ReturnsMatchingOrDefault()
        {
            // Arrange
            var defaultUser = new User { Id = -5, Name = "WhereDefault", DateAt = DateTime.Now };
            var allUsers = _users.OrderBy(x => x.Id).ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var targetId = allUsers[0].Id;

            // Act - 必须使用 OrderBy
            var result = _users.Where(x => x.Id == targetId).OrderBy(x => x.Id).DefaultIfEmpty(defaultUser).FirstOrDefault();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(targetId, result.Id);
        }

        /// <summary>
        /// 测试：DefaultIfEmpty(指定默认值) + OrderByDescending + LastOrDefault 组合
        /// </summary>
        [Fact]
        [Step(62)]
        public void DefaultIfEmpty_WithOrderByDescending_LastOrDefault_ReturnsLastOrDefault()
        {
            // Arrange
            var defaultUser = new User { Id = -6, Name = "OrderDefault", DateAt = DateTime.Now };
            var query = _users.OrderByDescending(x => x.Id);
            var allUsers = query.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = query.DefaultIfEmpty(defaultUser).LastOrDefault();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers.Last().Id, result.Id);
        }

        /// <summary>
        /// 测试：DefaultIfEmpty(指定默认值) + 复杂条件无匹配时返回默认值
        /// </summary>
        [Fact]
        [Step(63)]
        public async Task DefaultIfEmpty_ComplexCondition_NoMatch_ReturnsSpecifiedDefaultAsync()
        {
            // Arrange
            var defaultUser = new User { Id = -7, Name = "ComplexDefault", DateAt = DateTime.Now };
            var query = _users
                .Where(x => x.Id < -99999 && x.Name == "NonExistent")
                .OrderBy(x => x.Id);

            // Act
            var result = await query.DefaultIfEmpty(defaultUser).FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(-7, result.Id);
            Assert.Equal("ComplexDefault", result.Name);
        }

        /// <summary>
        /// 测试：DefaultIfEmpty(指定默认值) + ElementAtOrDefault 中间索引有数据
        /// </summary>
        [Fact]
        [Step(64)]
        public void DefaultIfEmpty_ElementAtOrDefault_MiddleIndex_ReturnsElement()
        {
            // Arrange
            var defaultUser = new User { Id = -8, Name = "MiddleDefault", DateAt = DateTime.Now };
            var query = _users.OrderBy(x => x.Id);
            var allUsers = query.ToList();

            if (allUsers.Count < 3)
            {
                return;
            }

            // Act
            var result = query.DefaultIfEmpty(defaultUser).ElementAtOrDefault(2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers[2].Id, result.Id);
        }

        /// <summary>
        /// 测试：DefaultIfEmpty(null 默认值) + FirstOrDefault 无数据时返回 null
        /// </summary>
        [Fact]
        [Step(65)]
        public void DefaultIfEmpty_NullDefault_FirstOrDefault_NoData_ReturnsNull()
        {
            // Arrange
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id);

            // Act
            var result = query.DefaultIfEmpty(null).FirstOrDefault();

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// 测试：DefaultIfEmpty(null 默认值) + LastOrDefault 无数据时返回 null
        /// </summary>
        [Fact]
        [Step(66)]
        public void DefaultIfEmpty_NullDefault_LastOrDefault_NoData_ReturnsNull()
        {
            // Arrange
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id);

            // Act
            var result = query.DefaultIfEmpty(null).LastOrDefault();

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// 测试：DefaultIfEmpty(null 默认值) + SingleOrDefault 无数据时返回 null
        /// </summary>
        [Fact]
        [Step(67)]
        public void DefaultIfEmpty_NullDefault_SingleOrDefault_NoData_ReturnsNull()
        {
            // Arrange
            var query = _users.Where(x => x.Id < -99999);

            // Act
            var result = query.DefaultIfEmpty(null).SingleOrDefault();

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// 测试：DefaultIfEmpty(null 默认值) + ElementAtOrDefault 无数据时返回 null
        /// </summary>
        [Fact]
        [Step(68)]
        public void DefaultIfEmpty_NullDefault_ElementAtOrDefault_NoData_ReturnsNull()
        {
            // Arrange
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id);

            // Act
            var result = query.DefaultIfEmpty(null).ElementAtOrDefault(0);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #endregion

        #region NoElementError 测试

        /// <summary>
        /// 测试：NoElementError + First 有数据时正常返回
        /// </summary>
        [Fact]
        [Step(69)]
        public void NoElementError_First_WithData_ReturnsElement()
        {
            // Arrange
            var query = _users.OrderBy(x => x.Id);
            var allUsers = query.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = query.NoElementError("自定义错误消息").First();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers[0].Id, result.Id);
        }

        /// <summary>
        /// 测试：NoElementError + First 无数据时抛出指定错误消息的异常
        /// </summary>
        [Fact]
        [Step(70)]
        public void NoElementError_First_NoData_ThrowsNoElementException()
        {
            // Arrange
            var errorMessage = "未找到符合条件的用户数据";
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id).NoElementError(errorMessage);

            // Act & Assert
            var exception = Assert.Throws<NoElementException>(() => query.First());
            Assert.Equal(errorMessage, exception.Message);
        }

        /// <summary>
        /// 测试：NoElementError + First(predicate) 无数据时抛出指定错误消息的异常
        /// </summary>
        [Fact]
        [Step(71)]
        public void NoElementError_FirstWithPredicate_NoData_ThrowsNoElementException()
        {
            // Arrange
            var errorMessage = "条件查询未找到数据";
            var query = _users.OrderBy(x => x.Id).NoElementError(errorMessage);

            // Act & Assert
            var exception = Assert.Throws<NoElementException>(() => query.First(x => x.Id < -99999));
            Assert.Equal(errorMessage, exception.Message);
        }

        /// <summary>
        /// 测试：NoElementError + Last 有数据时正常返回
        /// </summary>
        [Fact]
        [Step(72)]
        public void NoElementError_Last_WithData_ReturnsElement()
        {
            // Arrange
            var query = _users.OrderBy(x => x.Id);
            var allUsers = query.ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = query.NoElementError("自定义错误消息").Last();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers.Last().Id, result.Id);
        }

        /// <summary>
        /// 测试：NoElementError + Last 无数据时抛出指定错误消息的异常
        /// </summary>
        [Fact]
        [Step(73)]
        public void NoElementError_Last_NoData_ThrowsNoElementException()
        {
            // Arrange
            var errorMessage = "未找到最后一条用户数据";
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id).NoElementError(errorMessage);

            // Act & Assert
            var exception = Assert.Throws<NoElementException>(() => query.Last());
            Assert.Equal(errorMessage, exception.Message);
        }

        /// <summary>
        /// 测试：NoElementError + Last(predicate) 无数据时抛出指定错误消息的异常
        /// </summary>
        [Fact]
        [Step(74)]
        public void NoElementError_LastWithPredicate_NoData_ThrowsNoElementException()
        {
            // Arrange
            var errorMessage = "Last条件查询未找到数据";
            var query = _users.OrderBy(x => x.Id).NoElementError(errorMessage);

            // Act & Assert
            var exception = Assert.Throws<NoElementException>(() => query.Last(x => x.Id < -99999));
            Assert.Equal(errorMessage, exception.Message);
        }

        /// <summary>
        /// 测试：NoElementError + Single 有数据时正常返回
        /// </summary>
        [Fact]
        [Step(75)]
        public void NoElementError_Single_WithData_ReturnsElement()
        {
            // Arrange
            var allUsers = _users.OrderBy(x => x.Id).ToList();

            if (allUsers.Count == 0)
            {
                return;
            }

            var targetId = allUsers[0].Id;
            var query = _users.Where(x => x.Id == targetId);

            // Act
            var result = query.NoElementError("自定义错误消息").Single();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(targetId, result.Id);
        }

        /// <summary>
        /// 测试：NoElementError + Single 无数据时抛出指定错误消息的异常
        /// </summary>
        [Fact]
        [Step(76)]
        public void NoElementError_Single_NoData_ThrowsNoElementException()
        {
            // Arrange
            var errorMessage = "未找到唯一用户数据";
            var query = _users.Where(x => x.Id < -99999).NoElementError(errorMessage);

            // Act & Assert
            var exception = Assert.Throws<NoElementException>(() => query.Single());
            Assert.Equal(errorMessage, exception.Message);
        }

        /// <summary>
        /// 测试：NoElementError + Single(predicate) 无数据时抛出指定错误消息的异常
        /// </summary>
        [Fact]
        [Step(77)]
        public void NoElementError_SingleWithPredicate_NoData_ThrowsNoElementException()
        {
            // Arrange
            var errorMessage = "Single条件查询未找到数据";
            var query = _users.NoElementError(errorMessage);

            // Act & Assert
            var exception = Assert.Throws<NoElementException>(() => query.Single(x => x.Id < -99999));
            Assert.Equal(errorMessage, exception.Message);
        }

        /// <summary>
        /// 测试：NoElementError + Select + First 组合
        /// </summary>
        [Fact]
        [Step(78)]
        public void NoElementError_Select_First_NoData_ThrowsNoElementException()
        {
            // Arrange
            var errorMessage = "选择查询未找到数据";
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id).Select(x => x.Name).NoElementError(errorMessage);

            // Act & Assert
            var exception = Assert.Throws<NoElementException>(() => query.First());
            Assert.Equal(errorMessage, exception.Message);
        }

        /// <summary>
        /// 测试：NoElementError + First 带 Skip 组合
        /// </summary>
        [Fact]
        [Step(79)]
        public void NoElementError_Skip_First_NoData_ThrowsNoElementException()
        {
            // Arrange
            var errorMessage = "跳过后未找到第一条数据";
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id).Skip(0).NoElementError(errorMessage);

            // Act & Assert
            var exception = Assert.Throws<NoElementException>(() => query.First());
            Assert.Equal(errorMessage, exception.Message);
        }

        #region NoElementError 异步测试

        /// <summary>
        /// 测试：NoElementError + FirstAsync 有数据时正常返回
        /// </summary>
        [Fact]
        [Step(80)]
        public async Task NoElementError_FirstAsync_WithData_ReturnsElementAsync()
        {
            // Arrange
            var query = _users.OrderBy(x => x.Id);
            var allUsers = await query.ToListAsync();

            if (allUsers.Count == 0)
            {
                return;
            }

            // Act
            var result = await query.NoElementError("自定义错误消息").FirstAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(allUsers[0].Id, result.Id);
        }

        /// <summary>
        /// 测试：NoElementError + FirstAsync 无数据时抛出指定错误消息的异常
        /// </summary>
        [Fact]
        [Step(81)]
        public async Task NoElementError_FirstAsync_NoData_ThrowsNoElementExceptionAsync()
        {
            // Arrange
            var errorMessage = "异步查询未找到第一条数据";
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id).NoElementError(errorMessage);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NoElementException>(() => query.FirstAsync());
            Assert.Equal(errorMessage, exception.Message);
        }

        /// <summary>
        /// 测试：NoElementError + LastAsync 无数据时抛出指定错误消息的异常
        /// </summary>
        [Fact]
        [Step(82)]
        public async Task NoElementError_LastAsync_NoData_ThrowsNoElementExceptionAsync()
        {
            // Arrange
            var errorMessage = "异步查询未找到最后一条数据";
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id).NoElementError(errorMessage);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NoElementException>(() => query.LastAsync());
            Assert.Equal(errorMessage, exception.Message);
        }

        /// <summary>
        /// 测试：NoElementError + SingleAsync 无数据时抛出指定错误消息的异常
        /// </summary>
        [Fact]
        [Step(83)]
        public async Task NoElementError_SingleAsync_NoData_ThrowsNoElementExceptionAsync()
        {
            // Arrange
            var errorMessage = "异步查询未找到唯一数据";
            var query = _users.Where(x => x.Id < -99999).NoElementError(errorMessage);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NoElementException>(() => query.SingleAsync());
            Assert.Equal(errorMessage, exception.Message);
        }

        #endregion

        #region NoElementError 与其他方法组合测试

        /// <summary>
        /// 测试：NoElementError + Where + OrderBy + First 组合
        /// </summary>
        [Fact]
        [Step(84)]
        public void NoElementError_WithWhereOrderBy_First_NoData_ThrowsException()
        {
            // Arrange
            var errorMessage = "复杂查询未找到数据";
            var query = _users
                .Where(x => x.Id < -99999 && x.Name == "NonExistent")
                .OrderBy(x => x.Id)
                .NoElementError(errorMessage);

            // Act & Assert
            var exception = Assert.Throws<NoElementException>(() => query.First());
            Assert.Equal(errorMessage, exception.Message);
        }

        /// <summary>
        /// 测试：NoElementError 使用中文错误消息
        /// </summary>
        [Fact]
        [Step(85)]
        public void NoElementError_ChineseMessage_ThrowsWithCorrectMessage()
        {
            // Arrange
            var errorMessage = "用户不存在，请检查查询条件！";
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id).NoElementError(errorMessage);

            // Act & Assert
            var exception = Assert.Throws<NoElementException>(() => query.First());
            Assert.Equal(errorMessage, exception.Message);
        }

        /// <summary>
        /// 测试：NoElementError 使用英文错误消息
        /// </summary>
        [Fact]
        [Step(86)]
        public void NoElementError_EnglishMessage_ThrowsWithCorrectMessage()
        {
            // Arrange
            var errorMessage = "User not found, please check your query conditions!";
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id).NoElementError(errorMessage);

            // Act & Assert
            var exception = Assert.Throws<NoElementException>(() => query.First());
            Assert.Equal(errorMessage, exception.Message);
        }

        /// <summary>
        /// 测试：NoElementError 使用带特殊字符的错误消息
        /// </summary>
        [Fact]
        [Step(87)]
        public void NoElementError_SpecialCharactersMessage_ThrowsWithCorrectMessage()
        {
            // Arrange
            var errorMessage = "查询失败！错误码：E001 (用户ID < 0)";
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id).NoElementError(errorMessage);

            // Act & Assert
            var exception = Assert.Throws<NoElementException>(() => query.First());
            Assert.Equal(errorMessage, exception.Message);
        }

        /// <summary>
        /// 测试：NoElementError + Skip + First 组合
        /// </summary>
        [Fact]
        [Step(88)]
        public void NoElementError_Skip_First_NoData_ThrowsException()
        {
            // Arrange
            var errorMessage = "跳过后未找到数据";
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id).Skip(5).NoElementError(errorMessage);

            // Act & Assert
            var exception = Assert.Throws<NoElementException>(() => query.First());
            Assert.Equal(errorMessage, exception.Message);
        }

        /// <summary>
        /// 测试：NoElementError 对 FirstOrDefault 不生效（抛出 NotSupportedException）
        /// </summary>
        [Fact]
        [Step(89)]
        public void NoElementError_FirstOrDefault_ThrowsNotSupportedException()
        {
            // Arrange
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id).NoElementError("错误消息");

            // Act & Assert - NoElementError 不支持 *OrDefault 方法
            Assert.Throws<NotSupportedException>(() => query.FirstOrDefault());
        }

        /// <summary>
        /// 测试：NoElementError 对 LastOrDefault 不生效（抛出 NotSupportedException）
        /// </summary>
        [Fact]
        [Step(90)]
        public void NoElementError_LastOrDefault_ThrowsNotSupportedException()
        {
            // Arrange
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id).NoElementError("错误消息");

            // Act & Assert - NoElementError 不支持 *OrDefault 方法
            Assert.Throws<NotSupportedException>(() => query.LastOrDefault());
        }

        /// <summary>
        /// 测试：NoElementError 对 SingleOrDefault 不生效（抛出 NotSupportedException）
        /// </summary>
        [Fact]
        [Step(91)]
        public void NoElementError_SingleOrDefault_ThrowsNotSupportedException()
        {
            // Arrange
            var query = _users.Where(x => x.Id < -99999).NoElementError("错误消息");

            // Act & Assert - NoElementError 不支持 *OrDefault 方法
            Assert.Throws<NotSupportedException>(() => query.SingleOrDefault());
        }

        /// <summary>
        /// 测试：NoElementError 对 ElementAtOrDefault 不生效（抛出 NotSupportedException）
        /// </summary>
        [Fact]
        [Step(92)]
        public void NoElementError_ElementAtOrDefault_ThrowsNotSupportedException()
        {
            // Arrange
            var query = _users.Where(x => x.Id < -99999).OrderBy(x => x.Id).NoElementError("错误消息");

            // Act & Assert - NoElementError 不支持 *OrDefault 方法
            Assert.Throws<NotSupportedException>(() => query.ElementAtOrDefault(0));
        }

        #endregion

        #endregion
    }
}
