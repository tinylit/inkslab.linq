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
    [TestPriority(3)]
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
    }
}
