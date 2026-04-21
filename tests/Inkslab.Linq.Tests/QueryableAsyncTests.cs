using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using XunitPlus;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// QueryableAsync 集合转换方法测试。
    /// </summary>
    [TestPriority(30)]
    public class QueryableAsyncTests
    {
        private readonly IQueryable<User> _users;
        private readonly IDatabase _database;

        public QueryableAsyncTests(IQueryable<User> users, IDatabase database)
        {
            _users = users;
            _database = database;
        }

        [Fact]
        public async Task ToDictionaryAsync_KeySelector_ReturnsDictionaryAsync()
        {
            var query = _users.OrderBy(x => x.Id).Take(10);
            var expected = query.ToList().ToDictionary(x => x.Id);

            var result = await query.ToDictionaryAsync(x => x.Id);

            Assert.Equal(expected.Count, result.Count);
            Assert.Equal(expected.Keys.OrderBy(x => x), result.Keys.OrderBy(x => x));
            Assert.Equal(expected.Values.Select(x => x.Id).OrderBy(x => x), result.Values.Select(x => x.Id).OrderBy(x => x));
        }

        [Fact]
        public async Task ToDictionaryAsync_ElementSelector_ReturnsProjectedDictionaryAsync()
        {
            var query = _users.OrderBy(x => x.Id).Take(10);
            var expected = query.ToList().ToDictionary(x => x.Id, x => x.Name);

            var result = await query.ToDictionaryAsync(x => x.Id, x => x.Name);

            Assert.Equal(expected.Count, result.Count);
            Assert.Equal(expected.OrderBy(x => x.Key), result.OrderBy(x => x.Key));
        }

        [Fact]
        public async Task ToDictionaryAsync_DuplicateKey_ThrowsArgumentExceptionAsync()
        {
            if (_users.Count() < 2)
            {
                return;
            }

            var query = _users.OrderBy(x => x.Id);

            await Assert.ThrowsAsync<ArgumentException>(() => query.ToDictionaryAsync(_ => 1));
        }

        [Fact]
        public async Task ToDictionaryAsync_NullSource_ThrowsArgumentNullExceptionAsync()
        {
            IQueryable<User> source = null;

            await Assert.ThrowsAsync<ArgumentNullException>(() => source.ToDictionaryAsync(x => x.Id));
        }

        [Fact]
        public async Task ToDictionaryAsync_NullKeySelector_ThrowsArgumentNullExceptionAsync()
        {
            Func<User, int> keySelector = null;

            await Assert.ThrowsAsync<ArgumentNullException>(() => _users.ToDictionaryAsync(keySelector));
        }

        [Fact]
        public async Task ToDictionaryAsync_NullElementSelector_ThrowsArgumentNullExceptionAsync()
        {
            Func<User, string> elementSelector = null;

            await Assert.ThrowsAsync<ArgumentNullException>(() => _users.ToDictionaryAsync(x => x.Id, elementSelector));
        }

        /// <summary>
        /// 验证 QueryMultipleAsync 能正确响应 CancellationToken 取消请求。
        /// </summary>
        [Fact]
        public async Task QueryMultipleAsync_WithCancelledToken_ThrowsOperationCanceledExceptionAsync()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => _database.QueryMultipleAsync("SELECT 1;SELECT 2", cancellationToken: cts.Token));
        }

        /// <summary>
        /// 验证异步 FirstAsync 无数据时抛出 "contains no elements" 而非 "more than one element"。
        /// </summary>
        [Fact]
        public async Task FirstAsync_NoData_ThrowsCorrectNoElementsMessageAsync()
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _users.Where(x => x.Id == int.MinValue).OrderBy(x => x.Id).FirstAsync());

            Assert.Contains("no elements", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ToHashSetAsync_ReturnsDistinctElementsAsync()
        {
            var query = _users.OrderBy(x => x.Id).Take(10);
            var expected = query.ToList();

            var result = await query.ToHashSetAsync();

            Assert.Equal(expected.Count, result.Count);
            Assert.Equal(expected.Select(x => x.Id).OrderBy(x => x), result.Select(x => x.Id).OrderBy(x => x));
        }

        [Fact]
        public async Task ToHashSetAsync_WithComparer_UsesComparerAsync()
        {
            var query = _users.OrderBy(x => x.Id).Take(10).Select(x => x.Name);
            var expected = query.ToList();

            var result = await query.ToHashSetAsync(StringComparer.OrdinalIgnoreCase);

            Assert.Same(StringComparer.OrdinalIgnoreCase, result.Comparer);
            Assert.True(result.Count <= expected.Count);
            foreach (var name in expected)
            {
                Assert.Contains(name, result, StringComparer.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task ToHashSetAsync_NullSource_ThrowsArgumentNullExceptionAsync()
        {
            IQueryable<User> source = null;

            await Assert.ThrowsAsync<ArgumentNullException>(() => source.ToHashSetAsync());
            await Assert.ThrowsAsync<ArgumentNullException>(() => source.ToHashSetAsync(EqualityComparer<User>.Default));
        }
    }
}