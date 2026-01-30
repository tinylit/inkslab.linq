using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Inkslab.Linq.Annotations;
using Inkslab.Transactions;
using Xunit;
using XunitPlus;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// 仓储测试类。
    /// </summary>
    /// <remarks>
    /// 使用 "xunitPlus" 作为测试框架，以支持按步骤执行测试。
    /// </remarks>
    [TestPriority(1)]
    public class RepositoryTests
    {

        /// <summary>
        /// 渠道配置表。
        /// </summary>
        [Table("channel_config")]
        public class ChannelConfig
        {
            /// <summary>
            /// 主键ID。
            /// </summary>
            [Key]
            [Field("id")]
            public long Id { get; set; }

            /// <summary>
            /// 渠道类型(短信、极光推送、站内信)。
            /// </summary>
            [Field("types")]
            public int Types { get; set; }

            /// <summary>
            /// 对外请求地址。
            /// </summary>
            [Field("request_address")]
            [MaxLength(200)]
            public string RequestAddress { get; set; }

            /// <summary>
            /// 备注。
            /// </summary>
            [Field("remark")]
            [MaxLength(16171)]
            public string Remark { get; set; }

            /// <summary>
            /// 是否启用。
            /// </summary>
            [Field("is_valid")]
            public bool IsValid { get; set; }

            /// <summary>
            /// 创建人。
            /// </summary>
            [Field("create_by")]
            public long CreateBy { get; set; }

            /// <summary>
            /// 创建时间。
            /// </summary>
            [Field("create_time")]
            public DateTime CreateTime { get; set; }

            /// <summary>
            /// 修改人。
            /// </summary>
            [Field("update_by")]
            public long? UpdateBy { get; set; }

            /// <summary>
            /// 修改时间。
            /// </summary>
            [Field("update_time")]
            public DateTime? UpdateTime { get; set; }
        }

        private readonly IRepository<User> _userRpo;
        private readonly IRepository<UserSharding> _userShardingRpo;
        private readonly IQueryable<User> _users;
        private readonly IQueryable<UserEx> _userExes;
        private readonly IRepository<ChannelConfig> _channelConfigRpo;

        public RepositoryTests(
            IRepository<User> userRpo,
            IRepository<UserSharding> userShardingRpo,
            IQueryable<User> users,
            IQueryable<UserEx> userExes,
            IRepository<ChannelConfig> channelConfigRpo
        )
        {
            _userRpo = userRpo;
            _userShardingRpo = userShardingRpo;
            _users = users;
            _userExes = userExes;
            _channelConfigRpo = channelConfigRpo;
        }

        /// <summary>
        /// 插入。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// INSERT IGNORE INTO `user`(`name`, `date`) SELECT `x`.`name`, '2026-01-30 16:31:13.142' FROM `user` AS `x` INNER JOIN `user_ex` AS `y` ON `x`.`id` = `y`.`id` ORDER BY `x`.`id` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void InsertLinq()
        {
            var linq =
                from x in _users
                join y in _userExes on x.Id equals y.Id
                orderby x.Id descending
                select new User { Name = x.Name, DateAt = DateTime.Now };

            _userRpo.Timeout(10).Ignore().Insert(linq);
        }

        /// <summary>
        /// 分片插入。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// INSERT IGNORE INTO `user_2025`(`name`, `date`) SELECT `x`.`name`, NOW() FROM `user` AS `x` INNER JOIN `user_ex` AS `y` ON `x`.`id` = `y`.`id` ORDER BY `x`.`id` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void ShardingInsertLinq()
        {
            var linq =
                from x in _users
                join y in _userExes on x.Id equals y.Id
                orderby x.Id descending
                select new UserSharding { Name = x.Name, DateAt = DateTime.Now };

            _userShardingRpo.DataSharding("2025").Timeout(10).Ignore().Insert(linq);
        }

        /// <summary>
        /// 更新所有。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// UPDATE `user` AS `x` SET `x`.date = NOW()
        /// </code>
        /// </remarks>
        [Fact]
        public void UpdateAll()
        {
            _userRpo.Update(x => new User { DateAt = DateTime.Now });
        }

        /// <summary>
        /// 条件更新。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// UPDATE `user` AS `x` SET `x`.date = NOW(), `x`.nullable = IFNULL(1, 0) WHERE EXISTS(SELECT `id`, `role`, `age`, `date` FROM `user_ex` AS `y` WHERE `y`.`role` = 2 AND `x`.`id` = `y`.`id`)
        /// </code>
        /// </remarks>
        [Fact]
        public void UpdateLinq()
        {
            bool? nullable = true;

            _userRpo
                .Timeout(500)
                .Where(x => _userExes.Where(y => y.RoleType == 2).Any(y => x.Id == y.Id))
                .Update(x => new User { DateAt = DateTime.Now, Nullable = nullable ?? false });
        }

        /// <summary>
        /// 删除（子查询）。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// DELETE FROM `user` AS `x` WHERE `x`.`id` IN(SELECT `y`.`id` FROM `user_ex` AS `y` WHERE `y`.`role` = 2)
        /// </code>
        /// </remarks>
        [Fact]
        public void DeleteByLinq()
        {
            _userRpo
                .Where(x => _userExes.Where(y => y.RoleType == 2).Select(y => y.Id).Contains(x.Id))
                .Delete();
        }

        /// <summary>
        /// 删除所有。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// DELETE FROM `user` AS `g`
        /// </code>
        /// </remarks>
        [Fact]
        public void DeleteAll()
        {
            _userRpo.DeleteWith();
        }

        /// <summary>
        /// 按条件删除。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// DELETE FROM `user` AS `x` WHERE `x`.`id` &lt; 100
        /// </code>
        /// </remarks>
        [Fact]
        public void DeleteByLinqWhere()
        {
            _userRpo.Delete(x => x.Id < 100);
        }

        /// <summary>
        /// 批量插入。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// INSERT IGNORE INTO `user`(`name`,`date`,`is_administrator`,`nullable`)VALUES('测试：000',NOW(),False,null),('测试：001',NOW(),False,null),...
        /// </code>
        /// </remarks>
        [Fact]
        public void Insert()
        {
            int length = 50;

            var users = new List<User>(length);

            for (int i = 0; i < length; i++)
            {
                users.Add(new User { Name = $"测试：{i:000}", DateAt = DateTime.Now });
            }

            int rows = _userRpo.Ignore().Into(users).Execute();
        }

        /// <summary>
        /// 分片批量插入。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// INSERT IGNORE INTO `user_2025`(`name`,`date`,`is_administrator`,`nullable`)VALUES('测试：000',NOW(),False,null),('测试：001',NOW(),False,null),...
        /// </code>
        /// </remarks>
        [Fact]
        public void ShardingInsert()
        {
            int length = 50;

            var users = new List<UserSharding>(length);

            for (int i = 0; i < length; i++)
            {
                users.Add(new UserSharding { Name = $"测试：{i:000}", DateAt = DateTime.Now });
            }

            int rows = _userShardingRpo.DataSharding("2025").Ignore().Into(users).Execute();
        }

        /// <summary>
        /// 批量更新。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// UPDATE `user` SET `name`='测试：000',`date`=NOW(),`is_administrator`=False,`nullable`=null WHERE `id`=256 AND `date`='2026-01-30 16:30:00.412';UPDATE `user` SET ...
        /// </code>
        /// </remarks>
        [Fact]
        public void Update()
        {
            int length = 50;

            var users = new List<User>(length);

            for (int i = 0; i < length; i++)
            {
                users.Add(
                    new User
                    {
                        Id = i + 256,
                        Name = $"测试：{i:000}",
                        DateAt = DateTime.Now.AddMinutes(i)
                    }
                );
            }

            int rows = _userRpo.UpdateTo(users).Execute();
        }

        /// <summary>
        /// 批量删除。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// DELETE FROM `user` WHERE `id`=256 AND `date`='2026-01-30 16:29:59.800';DELETE FROM `user` WHERE `id`=257 AND `date`='2026-01-30 16:30:59.800';...
        /// </code>
        /// </remarks>
        [Fact]
        public void Delete()
        {
            int length = 50;

            var users = new List<User>(length);

            for (int i = 0; i < length; i++)
            {
                users.Add(
                    new User
                    {
                        Id = i + 256,
                        Name = $"测试：{i:000}",
                        DateAt = DateTime.Now.AddMinutes(i)
                    }
                );
            }

            int rows = _userRpo.DeleteWith(users).Execute();
        }

        /// <summary>
        /// 大批量插入。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// INSERT IGNORE INTO `user`(`name`,`date`,`is_administrator`,`nullable`)VALUES('测试：000',NOW(),False,null),('测试：001',NOW(),False,null),...
        /// </code>
        /// </remarks>
        [Fact]
        public void InsertBulk()
        {
            int length = 200;

            var users = new List<User>(length);

            for (int i = 0; i < length; i++)
            {
                users.Add(new User { Name = $"测试：{i:000}", DateAt = DateTime.Now });
            }

            int rows = _userRpo.Timeout(100).Ignore().Into(users).Execute();
        }

        /// <summary>
        /// 大批量更新。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// UPDATE `user` SET `name`='测试：000',`date`=NOW(),`is_administrator`=False,`nullable`=null WHERE `id`=1 AND `date`='...';UPDATE `user` SET ...
        /// </code>
        /// </remarks>
        [Fact]
        public void UpdateBulk()
        {
            int length = 200;

            var users = new List<User>(length);

            for (int i = 0; i < length; i++)
            {
                users.Add(
                    new User
                    {
                        Id = i + 1,
                        Name = $"测试：{i:000}",
                        DateAt = DateTime.Now.AddMinutes(i)
                    }
                );
            }

            int rows = _userRpo.UpdateTo(users).Execute();
        }

        /// <summary>
        /// 大批量删除。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// DELETE FROM `user` WHERE `id`=1 AND `date`='2026-01-30 16:29:59.800';DELETE FROM `user` WHERE `id`=2 AND `date`='2026-01-30 16:30:59.800';...
        /// </code>
        /// </remarks>
        [Fact]
        public void DeleteBulk()
        {
            int length = 200;

            var users = new List<User>(length);

            for (int i = 0; i < length; i++)
            {
                users.Add(
                    new User
                    {
                        Id = i + 1,
                        Name = $"测试：{i:000}",
                        DateAt = DateTime.Now.AddMinutes(i)
                    }
                );
            }

            int rows = _userRpo.DeleteWith(users).Execute();
        }

        /// <summary>
        /// 更新带Trim函数。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// UPDATE `channel_config` AS `x` SET `x`.types = 2, `x`.request_address = TRIM('  http://example.com/api  '), `x`.remark = 'Updated via LINQ', `x`.update_by = 1000000000000000000, `x`.update_time = NOW() WHERE `x`.`id` = 1
        /// </code>
        /// </remarks>
        [Fact]
        public void UpdateTrim()
        {
            var inDto = new
            {
                Id = 1L,
                Types = 2,
                RequestAddress = "  http://example.com/api  ",
                Remark = "Updated via LINQ"
            };

            _channelConfigRpo
               .Where(x => x.Id == inDto.Id)
               .Update(x => new ChannelConfig
               {
                   Types = inDto.Types,
                   RequestAddress = inDto.RequestAddress.Trim(),
                   Remark = inDto.Remark,
                   UpdateBy = 1000000000000000000L,
                   UpdateTime = DateTime.Now
               });
        }

        /// <summary>
        /// 异步条件删除。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// DELETE FROM `user` AS `x` WHERE `x`.`id` &lt; 100
        /// </code>
        /// </remarks>
        [Fact]
        public async Task DeleteAsync()
        {
            await _userRpo.Where(x => x.Id < 100).DeleteAsync();
        }

        /// <summary>
        /// 异步删除所有。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// DELETE FROM `user` AS `g`
        /// </code>
        /// </remarks>
        [Fact]
        public async Task DeleteAllAsync()
        {
            using (var tran = new TransactionUnit())
            {
                await _userRpo.DeleteAsync();
            }
        }
    }
}
