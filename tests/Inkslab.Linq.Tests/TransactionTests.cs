using Inkslab.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Linq.Tests
{
    public class TransactionTests
    {
        private readonly IDatabase _database;
        private readonly IRepository<User> _userRpts;
        private readonly IQueryable<User> _users;

        public TransactionTests(IDatabase database, IRepository<User> userRpts, IQueryable<User> users)
        {
            _database = database;
            _userRpts = userRpts;
            _users = users;
        }

        [Fact]
        public async Task RollbackAsync()
        {
            await using (var transaction = new TransactionUnit())
            {
                await _userRpts.UpdateAsync(x => new User
                {
                    DateAt = DateTime.Now
                });
            }
        }

        [Fact]
        public async Task CommitAsync()
        {
            await using (var transaction = new TransactionUnit())
            {
                await _userRpts.UpdateAsync(x => new User
                {
                    DateAt = DateTime.Now
                });

                await transaction.CompleteAsync();
            }
        }

        [Fact]
        public async Task ReadonlyAsync()
        {
            await using (var transaction = new TransactionUnit())
            {
                await _users.OrderBy(x => x.Id).FirstOrDefaultAsync();

                //? 不会有事务。
                await transaction.CompleteAsync();
            }
        }

        [Fact]
        public async Task TestAsync()
        {
            await using (var transaction = new TransactionUnit())
            {
                string sql = "SELECT * FROM `user` WHERE id = @id";

                await _database.FirstOrDefaultAsync<User>(sql, new { id = 1 });

                await transaction.CompleteAsync();
            }
        }

        [Fact]
        public async Task WriteServerCommitAsync()
        {
            await using (var transaction = new TransactionUnit())
            {
                int length = 200;

                var users = new List<User>(length);

                for (int i = 0; i < length; i++)
                {
                    users.Add(new User { Name = $"测试：{i:000}", DateAt = DateTime.Now });
                }

                int rows = _userRpts.Timeout(100).Ignore().Into(users).Execute();

                await transaction.CompleteAsync();
            }
        }
    }
}
