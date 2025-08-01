﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Linq.Tests
{
    public class RepositoryTests
    {
        private readonly IRepository<User> _userRpo;
        private readonly IRepository<UserSharding> _userShardingRpo;
        private readonly IQueryable<User> _users;
        private readonly IQueryable<UserEx> _userExes;

        public RepositoryTests(
            IRepository<User> userRpo,
            IRepository<UserSharding> userShardingRpo,
            IQueryable<User> users,
            IQueryable<UserEx> userExes
        )
        {
            _userRpo = userRpo;
            _userShardingRpo = userShardingRpo;
            _users = users;
            _userExes = userExes;
        }

        /// <summary>
        /// 插入。
        /// </summary>
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
        /// 插入。
        /// </summary>
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
        /// 更新。
        /// </summary>
        [Fact]
        public void UpdateAll()
        {
            _userRpo.Update(x => new User { DateAt = DateTime.Now });
        }

        /// <summary>
        /// 更新。
        /// </summary>
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
        /// 删除。
        /// </summary>
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
        [Fact]
        public void DeleteAll()
        {
            _userRpo.DeleteWith();
        }

        /// <summary>
        /// 按条件删除。
        /// </summary>
        [Fact]
        public void DeleteByLinqWhere()
        {
            _userRpo.Delete(x => x.Id < 100);
        }

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
    }
}
