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

        /// <summary>
        /// SQL预览:
        /// UPDATE `user` SET `date` = ?x_DateAt
        /// 注意: 此测试演示事务回滚 - 未调用CompleteAsync()会自动回滚
        /// </summary>
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

        /// <summary>
        /// SQL预览:
        /// UPDATE `user` SET `date` = ?x_DateAt
        /// 注意: 此测试演示事务提交 - 调用CompleteAsync()提交事务
        /// </summary>
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

        /// <summary>
        /// SQL预览:
        /// SELECT `x`.`id` AS `Id`, `x`.`name` AS `Name`, `x`.`date` AS `DateAt`, `x`.`is_administrator` AS `IsAdministrator`, `x`.`nullable` AS `Nullable` 
        /// FROM `user` AS `x` ORDER BY `x`.`id` LIMIT 1
        /// 注意: 纯查询操作不会开启事务
        /// </summary>
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

        /// <summary>
        /// SQL预览:
        /// SELECT * FROM `user` WHERE id = @id
        /// </summary>
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

        /// <summary>
        /// SQL预览:
        /// INSERT IGNORE INTO `user`(`name`,`date`) VALUES(?entry0_Name,?entry0_DateAt),(?entry1_Name,?entry1_DateAt),...
        /// 注意: 批量插入200条数据，使用IGNORE策略忽略重复数据
        /// </summary>
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
