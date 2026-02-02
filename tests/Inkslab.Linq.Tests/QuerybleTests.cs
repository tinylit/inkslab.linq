using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inkslab.Linq.Exceptions;
using Xunit;

namespace Inkslab.Linq.Tests
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Role { get; set; }
    }

    public class QuerybleTests
    {
        private readonly IQueryable<User> _users;
        private readonly IQueryable<UserEx> _userExes;
        private readonly IQueryable<UserSharding> _userShardings;
        private readonly IQueryable<Published> _publisheds;

        public QuerybleTests(
            IQueryable<User> users,
            IQueryable<UserEx> userExes,
            IQueryable<UserSharding> userShardings,
            IQueryable<Published> publisheds
        )
        {
            _users = users;
            _userExes = userExes;
            _userShardings = userShardings;
            _publisheds = publisheds;
        }

        /// <summary>
        /// SQL预览:
        /// SELECT `x`.`id` FROM `user` AS `x` WHERE `x`.`id` = 100 ORDER BY `x`.`date`, `x`.`name`
        /// </summary>
        [Fact]
        public void TestSimple()
        {
            var linq = from x in _users where x.Id == 100 orderby x.DateAt, x.Name select x.Id;

            var allUsers = _users.ToList();

            var memoryResults = (from x in allUsers where x.Id == 100 orderby x.DateAt, x.Name select x.Id).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较相同id的内容是否相同，不关心顺序
            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i], results[i]);
            }
        }

        /// <summary>
        /// SQL预览:
        /// SELECT `x`.`id` FROM `user_2025` AS `x` WHERE `x`.`id` = 100 ORDER BY `x`.`date`, `x`.`name`
        /// </summary>
        [Fact]
        public void TestShardingSimple()
        {
            var linq =
                from x in _userShardings.DataSharding("2025")
                where x.Id == 100
                orderby x.DateAt, x.Name
                select x.Id;

            // 对于分片查询，我们用基本的_userShardings来做内存比较（模拟分片数据）
            var allUserShardings = _userShardings.DataSharding("2025").ToList();
            var memoryResults = (from x in allUserShardings where x.Id == 100 orderby x.DateAt, x.Name select x.Id).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较相同id的内容是否相同，不关心顺序
            Assert.Equal(memoryResults.OrderBy(x => x), results.OrderBy(x => x));
        }

        /// <summary>
        /// SQL预览:
        /// SELECT `x`.`id` AS `Key` FROM `user` AS `x` WHERE `x`.`id` = 100 GROUP BY `x`.`id` ORDER BY `x`.`id` DESC
        /// </summary>
        [Fact]
        public void TestGroupBySimple()
        {
            var linq =
                from x in _users
                where x.Id == 100
                group x by x.Id into g
                orderby g.Key descending
                select new { g.Key };

            var allUsers = _users.ToList();
            var memoryResults = (from x in allUsers
                                 where x.Id == 100
                                 group x by x.Id into g
                                 orderby g.Key descending
                                 select new { g.Key }).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Key, results[i].Key);
            }
        }

        /// <summary>
        /// SQL预览:
        /// SELECT `x`.`id` AS `Key`, COUNT(1) AS `Total`, COUNT(CASE WHEN `x`.`date` > ?now THEN 1 END) AS `Count` 
        /// FROM `user` AS `x` WHERE `x`.`id` = 100 GROUP BY `x`.`id` ORDER BY `x`.`id` DESC
        /// </summary>
        [Fact]
        public void TestGroupByElement()
        {
            var now = DateTime.Now;

            var linq =
                from x in _users
                where x.Id == 100
                group new { x.Name, x.DateAt } by x.Id into g
                orderby g.Key descending
                select new
                {
                    g.Key,
                    Total = g.Count(),
                    Count = g.Where(x => x.DateAt > now).Count()
                };

            var allUsers = _users.ToList();
            var memoryResults = (from x in allUsers
                                 where x.Id == 100
                                 group new { x.Name, x.DateAt } by x.Id into g
                                 orderby g.Key descending
                                 select new
                                 {
                                     g.Key,
                                     Total = g.Count(),
                                     Count = g.Where(x => x.DateAt > now).Count()
                                 }).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Key, results[i].Key);
                Assert.Equal(memoryResults[i].Total, results[i].Total);
                Assert.Equal(memoryResults[i].Count, results[i].Count);
            }
        }

        /// <summary>
        /// SQL预览:
        /// SELECT `x`.`id` AS `Key`, MAX(`x`.`name`) AS `Name`, COUNT(1) AS `Total` 
        /// FROM `user` AS `x` WHERE `x`.`id` = 100 GROUP BY `x`.`id` ORDER BY `x`.`id` DESC
        /// </summary>
        [Fact]
        public void TestGroupByToMax()
        {
            var now = DateTime.Now;

            var linq =
                from x in _users
                where x.Id == 100
                group x by x.Id into g
                orderby g.Key descending
                select new
                {
                    g.Key,
                    Name = g.Max(y => y.Name),
                    Total = g.Count()
                };

            var allUsers = _users.ToList();
            var memoryResults = (from x in allUsers
                                 where x.Id == 100
                                 group x by x.Id into g
                                 orderby g.Key descending
                                 select new
                                 {
                                     g.Key,
                                     Name = g.Max(y => y.Name),
                                     Total = g.Count()
                                 }).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Key, results[i].Key);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
                Assert.Equal(memoryResults[i].Total, results[i].Total);
            }
        }

        /// <summary>
        /// SQL预览:
        /// SELECT `x`.`id` AS `Key`, MAX(`x`.`name`) AS `Name`, COUNT(1) AS `Total` 
        /// FROM `user` AS `x` 
        /// INNER JOIN `user_ex` AS `y` ON `x`.`id` = `y`.`id` 
        /// WHERE `x`.`id` = 100 GROUP BY `x`.`id` ORDER BY `x`.`id` DESC
        /// </summary>
        /// <summary>
        /// Join + GroupBy + Max测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` AS `Key`,MAX(`x`.`Name`) AS `Name`,COUNT(1) AS `Total` FROM `fmk_users` AS `x` INNER JOIN `fmk_user_exs` AS `y` ON `x`.`Id`=`y`.`Id` WHERE `x`.`Id`=100 GROUP BY `x`.`Id` ORDER BY `x`.`Id` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestJoinGroupByToMax()
        {
            var now = DateTime.Now;

            var linq =
                from x in _users
                join y in _userExes on x.Id equals y.Id
                where x.Id == 100
                group x by x.Id into g
                orderby g.Key descending
                select new
                {
                    g.Key,
                    Name = g.Max(y => y.Name),
                    Total = g.Count()
                };

            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();
            var memoryResults = (from x in allUsers
                                 join y in allUserExes on x.Id equals y.Id
                                 where x.Id == 100
                                 group x by x.Id into g
                                 orderby g.Key descending
                                 select new
                                 {
                                     g.Key,
                                     Name = g.Max(y => y.Name),
                                     Total = g.Count()
                                 }).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Key, results[i].Key);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
                Assert.Equal(memoryResults[i].Total, results[i].Total);
            }
        }

        /// <summary>
        /// 分片Join + GroupBy + Max测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` AS `Key`,MAX(`x`.`Name`) AS `Name`,COUNT(1) AS `Total` FROM `fmk_users` AS `x` INNER JOIN `fmk_user_shardings_2025` AS `y` ON `x`.`Id`=`y`.`Id` WHERE `x`.`Id`=100 GROUP BY `x`.`Id` ORDER BY `x`.`Id` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestShardingJoinGroupByToMax()
        {
            var now = DateTime.Now;
            var allUsers = _users.ToList();
            var allUserShardings = _userShardings.DataSharding("2025").ToList();

            var memoryResults = (from x in allUsers
                                 join y in allUserShardings on x.Id equals y.Id
                                 where x.Id == 100
                                 group x by x.Id into g
                                 orderby g.Key descending
                                 select new
                                 {
                                     g.Key,
                                     Name = g.Max(y => y.Name),
                                     Total = g.Count()
                                 }).ToList();

            var linq =
                from x in _users
                join y in _userShardings.DataSharding("2025") on x.Id equals y.Id
                where x.Id == 100
                group x by x.Id into g
                orderby g.Key descending
                select new
                {
                    g.Key,
                    Name = g.Max(y => y.Name),
                    Total = g.Count()
                };

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Key, results[i].Key);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
                Assert.Equal(memoryResults[i].Total, results[i].Total);
            }
        }

        /// <summary>
        /// Join + GroupBy + Skip + Take测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` AS `Key`,MAX(`x`.`Name`) AS `Name`,COUNT(1) AS `Total` FROM `fmk_users` AS `x` INNER JOIN `fmk_user_exs` AS `y` ON `x`.`Id`=`y`.`Id` WHERE `x`.`Id`=100 GROUP BY `x`.`Id` ORDER BY `x`.`Id` DESC LIMIT 1 OFFSET 1
        /// </code>
        /// </remarks>
        [Fact]
        public void TestJoinGroupByTakeSkipToMax()
        {
            var now = DateTime.Now;
            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();

            var memoryResults = (from x in allUsers
                                 join y in allUserExes on x.Id equals y.Id
                                 where x.Id == 100
                                 group x by x.Id into g
                                 orderby g.Key descending
                                 select new
                                 {
                                     g.Key,
                                     Name = g.Max(y => y.Name),
                                     Total = g.Count()
                                 }).Skip(1).Take(1).ToList();

            var linq =
                from x in _users
                join y in _userExes on x.Id equals y.Id
                where x.Id == 100
                group x by x.Id into g
                orderby g.Key descending
                select new
                {
                    g.Key,
                    Name = g.Max(y => y.Name),
                    Total = g.Count()
                };

            var results = linq.Skip(1).Take(1).ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Key, results[i].Key);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
                Assert.Equal(memoryResults[i].Total, results[i].Total);
            }
        }

        /// <summary>
        /// SQL预览:
        /// SELECT `x`.`id` FROM `user` AS `x` WHERE `x`.`id` = 100 
        /// GROUP BY `x`.`id` HAVING COUNT(1) > 1 ORDER BY `x`.`id` DESC
        /// </summary>
        [Fact]
        public void TestGroupByHavingElement()
        {
            var now = DateTime.Now;
            var allUsers = _users.ToList();

            var memoryResults = (from x in allUsers
                                 where x.Id == 100
                                 group new { x.Name, x.DateAt } by x.Id into g
                                 where g.Count() > 1
                                 orderby g.Key descending
                                 select g.Key).ToList();

            var linq =
                from x in _users
                where x.Id == 100
                group new { x.Name, x.DateAt } by x.Id into g
                where g.Count() > 1
                orderby g.Key descending
                select g.Key;

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);
            Assert.Equal(memoryResults, results);
        }

        /// <summary>
        /// GroupBy + Having + 多个Count聚合测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` AS `Key`,COUNT(1) AS `Total`,SUM(CASE WHEN `x`.`DateAt`&gt;@__variable_now__ THEN 1 ELSE 0 END) AS `Count`,SUM(CASE WHEN `x`.`DateAt`&gt;@__variable_now__ THEN 1 ELSE 0 END) AS `WhereCount`,SUM(CASE WHEN `x`.`DateAt`&gt;@__variable_now__ THEN 1 ELSE 0 END) AS `SelectCount`,COUNT(DISTINCT CASE WHEN `x`.`DateAt`&gt;@__variable_now__ THEN `x`.`Name` ELSE NULL END) AS `DistinctCount` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 GROUP BY `x`.`Id` HAVING COUNT(1)&gt;1 ORDER BY `x`.`Id` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestGroupByHavingElementCount()
        {
            var now = DateTime.Now;
            var allUsers = _users.ToList();

            var memoryResults = (from x in allUsers
                                 where x.Id == 100
                                 group new { x.Name, x.DateAt } by x.Id into g
                                 where g.Count() > 1
                                 orderby g.Key descending
                                 select new
                                 {
                                     g.Key,
                                     Total = g.Count(),
                                     Count = g.Count(x => x.DateAt > now),
                                     WhereCount = g.Where(x => x.DateAt > now).Count(),
                                     SelectCount = g.Where(x => x.DateAt > now).Select(x => x.Name).Count(),
                                     DistinctCount = g.Where(x => x.DateAt > now)
                                         .Select(x => x.Name)
                                         .Distinct()
                                         .Count()
                                 }).ToList();

            var linq =
                from x in _users
                where x.Id == 100
                group new { x.Name, x.DateAt } by x.Id into g
                where g.Count() > 1
                orderby g.Key descending
                select new
                {
                    g.Key,
                    Total = g.Count(),
                    Count = g.Count(x => x.DateAt > now),
                    WhereCount = g.Where(x => x.DateAt > now).Count(),
                    SelectCount = g.Where(x => x.DateAt > now).Select(x => x.Name).Count(),
                    DistinctCount = g.Where(x => x.DateAt > now)
                        .Select(x => x.Name)
                        .Distinct()
                        .Count()
                };

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Key, results[i].Key);
                Assert.Equal(memoryResults[i].Total, results[i].Total);
                Assert.Equal(memoryResults[i].Count, results[i].Count);
                Assert.Equal(memoryResults[i].WhereCount, results[i].WhereCount);
                Assert.Equal(memoryResults[i].SelectCount, results[i].SelectCount);
                Assert.Equal(memoryResults[i].DistinctCount, results[i].DistinctCount);
            }
        }

        /// <summary>
        /// GroupBy多字段 + Having + 多个Count聚合测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`x`.`Name`,COUNT(1) AS `Total`,SUM(CASE WHEN `x`.`DateAt`&gt;@__variable_now__ THEN 1 ELSE 0 END) AS `Count`,... FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 GROUP BY `x`.`Id`,`x`.`Name` HAVING COUNT(1)&gt;1 ORDER BY `x`.`Id` DESC,`x`.`Name` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestGroupByHavingMoreElementCount()
        {
            var now = DateTime.Now;
            var allUsers = _users.ToList();

            var memoryResults = (from x in allUsers
                                 where x.Id == 100
                                 group new { x.Name, x.DateAt } by new { x.Id, x.Name } into g
                                 where g.Count() > 1
                                 orderby g.Key descending
                                 select new
                                 {
                                     g.Key.Id,
                                     g.Key.Name,
                                     Total = g.Count(),
                                     Count = g.Count(x => x.DateAt > now),
                                     WhereCount = g.Where(x => x.DateAt > now).Count(),
                                     SelectCount = g.Where(x => x.DateAt > now).Select(x => x.Name).Count(),
                                     DistinctCount = g.Where(x => x.DateAt > now)
                                         .Select(x => x.Name)
                                         .Distinct()
                                         .Count()
                                 }).ToList();

            var linq =
                from x in _users
                where x.Id == 100
                group new { x.Name, x.DateAt } by new { x.Id, x.Name } into g
                where g.Count() > 1
                orderby g.Key descending
                select new
                {
                    g.Key.Id,
                    g.Key.Name,
                    Total = g.Count(),
                    Count = g.Count(x => x.DateAt > now),
                    WhereCount = g.Where(x => x.DateAt > now).Count(),
                    SelectCount = g.Where(x => x.DateAt > now).Select(x => x.Name).Count(),
                    DistinctCount = g.Where(x => x.DateAt > now)
                        .Select(x => x.Name)
                        .Distinct()
                        .Count()
                };

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
                Assert.Equal(memoryResults[i].Total, results[i].Total);
                Assert.Equal(memoryResults[i].Count, results[i].Count);
                Assert.Equal(memoryResults[i].WhereCount, results[i].WhereCount);
                Assert.Equal(memoryResults[i].SelectCount, results[i].SelectCount);
                Assert.Equal(memoryResults[i].DistinctCount, results[i].DistinctCount);
            }
        }

        /// <summary>
        /// GroupBy多字段 + Having返回Key测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`x`.`Name` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 GROUP BY `x`.`Id`,`x`.`Name` HAVING COUNT(1)&gt;1 ORDER BY `x`.`Id` DESC,`x`.`Name` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestGroupByHavingMoreKey()
        {
            var now = DateTime.Now;
            var allUsers = _users.ToList();

            var memoryResults = (from x in allUsers
                                 where x.Id == 100
                                 group new { x.Name, x.DateAt } by new { x.Id, x.Name } into g
                                 where g.Count() > 1
                                 orderby g.Key descending
                                 select g.Key).ToList();

            var linq =
                from x in _users
                where x.Id == 100
                group new { x.Name, x.DateAt } by new { x.Id, x.Name } into g
                where g.Count() > 1
                orderby g.Key descending
                select g.Key;

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
            }
        }

        /// <summary>
        /// GroupBy + Having + Max聚合测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` AS `Key`,MAX(`x`.`DateAt`) AS `Max`,MAX(CASE WHEN `x`.`DateAt`&gt;@__variable_now__ THEN `x`.`Id` ELSE NULL END) AS `WhereMax`,MAX(CASE WHEN `x`.`DateAt`&gt;@__variable_now__ THEN `x`.`Id` ELSE NULL END) AS `SelectMax`,MAX(DISTINCT CASE WHEN `x`.`DateAt`&gt;@__variable_now__ THEN `x`.`Id` ELSE NULL END) AS `DistinctMax` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 GROUP BY `x`.`Id` HAVING SUM(CASE WHEN `x`.`DateAt`&gt;@__variable_now__ THEN 1 ELSE 0 END)&gt;1 ORDER BY `x`.`Id` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestGroupByHavingElemenMax()
        {
            var now = DateTime.Now;

            var linq =
                from x in _users
                where x.Id == 100
                group x by x.Id into g
                where g.Count(x => x.DateAt > now) > 1
                orderby g.Key descending
                select new
                {
                    g.Key,
                    Max = g.Max(x => x.DateAt),
                    WhereMax = g.Where(x => x.DateAt > now).Max(x => x.Id),
                    SelectMax = g.Where(x => x.DateAt > now).Select(x => x.Id).Max(),
                    DistinctMax = g.Where(x => x.DateAt > now).Select(x => x.Id).Distinct().Max()
                };

            var results = linq.ToList();
        }

        /// <summary>
        /// SQL预览:
        /// SELECT `x`.`id` AS `Id`, `y`.`role_type` AS `RoleType` 
        /// FROM `user` AS `x` INNER JOIN `user_ex` AS `y` ON `x`.`id` = `y`.`id` 
        /// ORDER BY `x`.`id` DESC
        /// </summary>
        [Fact]
        public void TestJoinSimple()
        {
            var linq =
                from x in _users
                join y in _userExes on x.Id equals y.Id
                orderby x.Id descending
                select new { x.Id, y.RoleType };

            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();
            var memoryResults = (from x in allUsers
                                 join y in allUserExes on x.Id equals y.Id
                                 orderby x.Id descending
                                 select new { x.Id, y.RoleType }).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].RoleType, results[i].RoleType);
            }
        }

        /// <summary>
        /// SQL预览:
        /// SELECT `x`.`id` AS `Id`, `y`.`role_type` AS `Role`, `x`.`name` AS `Name` 
        /// FROM `user` AS `x` 
        /// INNER JOIN `user_ex` AS `y` ON `y`.`age` > 18 AND `x`.`id` = `y`.`id` 
        /// WHERE `x`.`date` > ?DateTime_UnixEpoch ORDER BY `x`.`id` DESC
        /// </summary>
        [Fact]
        public void TestJoinWhere()
        {
            var linq =
                from x in _users.Where(y => y.DateAt > DateTime.UnixEpoch)
                join y in _userExes.Where(x => x.Age > 18) on x.Id equals y.Id
                orderby x.Id descending
                select new UserDto
                {
                    Id = x.Id,
                    Role = y.RoleType,
                    Name = x.Name
                };

            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();
            var memoryResults = (from x in allUsers.Where(y => y.DateAt > DateTime.UnixEpoch)
                                 join y in allUserExes.Where(x => x.Age > 18) on x.Id equals y.Id
                                 orderby x.Id descending
                                 select new UserDto
                                 {
                                     Id = x.Id,
                                     Role = y.RoleType,
                                     Name = x.Name
                                 }).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Role, results[i].Role);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
            }
        }

        /// <summary>
        /// 分片Join + Where测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`y`.`RoleType` AS `Role`,`x`.`Name` FROM `fmk_user_shardings_2025` AS `x` INNER JOIN `fmk_user_exs` AS `y` ON `y`.`Age`&gt;18 AND `x`.`Id`=`y`.`Id` WHERE `x`.`DateAt`&gt;@__variable_DateTime_UnixEpoch__ ORDER BY `x`.`Id` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestShardingJoinWhere()
        {
            var linq =
                from x in _userShardings
                    .DataSharding("2025")
                    .Where(y => y.DateAt > DateTime.UnixEpoch)
                join y in _userExes.Where(x => x.Age > 18) on x.Id equals y.Id
                orderby x.Id descending
                select new UserDto
                {
                    Id = x.Id,
                    Role = y.RoleType,
                    Name = x.Name
                };

            // 对于分片查询，使用基本的_userShardings做内存对比
            var allUserShardings = _userShardings.DataSharding("2025").ToList();
            var allUserExes = _userExes.ToList();
            var memoryResults = (from x in allUserShardings
                                   .Where(y => y.DateAt > DateTime.UnixEpoch)
                                 join y in allUserExes.Where(x => x.Age > 18) on x.Id equals y.Id
                                 orderby x.Id descending
                                 select new UserDto
                                 {
                                     Id = x.Id,
                                     Role = y.RoleType,
                                     Name = x.Name
                                 }).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Role, results[i].Role);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
            }
        }

        /// <summary>
        /// SQL预览:
        /// SELECT `x`.`id` AS `Id`, `c`.`role_type` AS `Role`, `x`.`name` AS `Name` 
        /// FROM `user` AS `x` LEFT JOIN `user_ex` AS `c` ON `c`.`age` > 18 AND `x`.`id` = `c`.`id` 
        /// ORDER BY `x`.`id` DESC
        /// </summary>
        [Fact]
        public void TestLeftJoinWhere()
        {
            var linq =
                from x in _users //.Where(y => y.DateAt > DateTime.UnixEpoch)
                join y in _userExes.Where(x => x.Age > 18) on x.Id equals y.Id into xy
                from c in xy.DefaultIfEmpty()
                orderby x.Id descending
                select new UserDto
                {
                    Id = x.Id,
                    Role = c.RoleType,
                    Name = x.Name
                };

            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();
            var memoryResults = (from x in allUsers //.Where(y => y.DateAt > DateTime.UnixEpoch)
                                 join y in allUserExes.Where(x => x.Age > 18) on x.Id equals y.Id into xy
                                 from c in xy.DefaultIfEmpty()
                                 orderby x.Id descending
                                 select new UserDto
                                 {
                                     Id = x.Id,
                                     Role = c?.RoleType ?? 0,
                                     Name = x.Name
                                 }).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Role, results[i].Role);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
            }
        }

        /// <summary>
        /// 分片Left Join + Where测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`c`.`RoleType` AS `Role`,`x`.`Name` FROM `fmk_user_shardings_2025` AS `x` LEFT JOIN `fmk_user_exs` AS `c` ON `c`.`Age`&gt;18 AND `x`.`Id`=`c`.`Id` ORDER BY `x`.`Id` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestShardingLeftJoinWhere()
        {
            var allUserShardings = _userShardings.DataSharding("2025").ToList();
            var allUserExes = _userExes.ToList();

            var memoryResults = (from x in allUserShardings //.Where(y => y.DateAt > DateTime.UnixEpoch)
                                 join y in allUserExes.Where(x => x.Age > 18) on x.Id equals y.Id into xy
                                 from c in xy.DefaultIfEmpty()
                                 orderby x.Id descending
                                 select new UserDto
                                 {
                                     Id = x.Id,
                                     Role = c?.RoleType ?? 0,
                                     Name = x.Name
                                 }).ToList();

            var linq =
                from x in _userShardings.DataSharding("2025") //.Where(y => y.DateAt > DateTime.UnixEpoch)
                join y in _userExes.Where(x => x.Age > 18) on x.Id equals y.Id into xy
                from c in xy.DefaultIfEmpty()
                orderby x.Id descending
                select new UserDto
                {
                    Id = x.Id,
                    Role = c.RoleType,
                    Name = x.Name
                };

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Role, results[i].Role);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
            }
        }

        /// <summary>
        /// SQL预览:
        /// SELECT `x`.`id` AS `Id`, `z`.`role_type` AS `RoleType` 
        /// FROM `user` AS `x` 
        /// INNER JOIN `user_ex` AS `z` ON `x`.`id` = `z`.`id` 
        /// ORDER BY `x`.`id` DESC
        /// </summary>
        [Fact]
        public void TestJoinSelectMany()
        {
            var linq =
                from x in _users
                join y in _userExes on x.Id equals y.Id into g
                from z in g
                orderby x.Id descending
                select new { x.Id, z.RoleType };

            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();
            var memoryResults = (from x in allUsers
                                 join y in allUserExes on x.Id equals y.Id into g
                                 from z in g
                                 orderby x.Id descending
                                 select new { x.Id, z.RoleType }).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].RoleType, results[i].RoleType);
            }
        }

        /// <summary>
        /// 分片Join + SelectMany测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`z`.`RoleType` FROM `fmk_user_shardings_2025` AS `x` INNER JOIN `fmk_user_exs` AS `z` ON `x`.`Id`=`z`.`Id` ORDER BY `x`.`Id` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestShardingJoinSelectMany()
        {
            var allUserShardings = _userShardings.DataSharding("2025").ToList();
            var allUserExes = _userExes.ToList();

            var memoryResults = (from x in allUserShardings
                                 join y in allUserExes on x.Id equals y.Id into g
                                 from z in g
                                 orderby x.Id descending
                                 select new { x.Id, z.RoleType }).ToList();

            var linq =
                from x in _userShardings.DataSharding("2025")
                join y in _userExes on x.Id equals y.Id into g
                from z in g
                orderby x.Id descending
                select new { x.Id, z.RoleType };

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].RoleType, results[i].RoleType);
            }
        }

        /// <summary>
        /// SQL预览:
        /// SELECT `x`.`id` AS `Id`, `z`.`role_type` AS `RoleType` 
        /// FROM `user` AS `x` 
        /// LEFT JOIN `user_ex` AS `z` ON `x`.`id` = `z`.`id` 
        /// ORDER BY `x`.`id` DESC
        /// </summary>
        [Fact]
        public void TestLeftJoinSelectMany()
        {
            var linq =
                from x in _users
                join y in _userExes on x.Id equals y.Id into g
                from z in g.DefaultIfEmpty()
                orderby x.Id descending
                select new { x.Id, z.RoleType };

            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();
            var memoryResults = (from x in allUsers
                                 join y in allUserExes on x.Id equals y.Id into g
                                 from z in g.DefaultIfEmpty()
                                 orderby x.Id descending
                                 select new { x.Id, RoleType = z?.RoleType ?? 0 }).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].RoleType, results[i].RoleType);
            }
        }

        /// <summary>
        /// Left Join + 多个表 + null判断测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`t2`.`RoleType`,(`t2`.`Id` IS NOT NULL) AS `IsValid` FROM `fmk_users` AS `x` LEFT JOIN `fmk_user_exs` AS `t` ON `x`.`Id`=`t`.`Id` LEFT JOIN `fmk_user_exs` AS `t2` ON `x`.`Id`=`t`.`Id` WHERE `t`.`Id` IS NULL ORDER BY `x`.`Id` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestLeftJoinSelectElementNotNullMany()
        {
            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();

            var memoryResults = (from x in allUsers
                                 join y in allUserExes on x.Id equals y.Id into g
                                 from t in g.DefaultIfEmpty()
                                 join z in allUserExes on x.Id equals z.Id + 1 into g2
                                 from t2 in g.DefaultIfEmpty()
                                 where t == null
                                 orderby x.Id descending
                                 select new
                                 {
                                     x.Id,
                                     RoleType = t2?.RoleType ?? 0,
                                     IsValid = t2 != null
                                 }).ToList();

            var linq =
                from x in _users
                join y in _userExes on x.Id equals y.Id into g
                from t in g.DefaultIfEmpty()
                join z in _userExes on x.Id equals z.Id + 1 into g2
                from t2 in g.DefaultIfEmpty()
                where t == null
                orderby x.Id descending
                select new
                {
                    x.Id,
                    t2.RoleType,
                    IsValid = t2 != null
                };

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].RoleType, results[i].RoleType);
                Assert.Equal(memoryResults[i].IsValid, results[i].IsValid);
            }
        }

        /// <summary>
        /// SQL预览:
        /// SELECT `x`.`id` AS `Id`, `y`.`role_type` AS `RoleType` 
        /// FROM `user` AS `x`, `user_ex` AS `y` 
        /// WHERE `x`.`id` < 10 AND `y`.`id` < 10 ORDER BY `x`.`id` DESC
        /// </summary>
        [Fact]
        public void TestCrossJoinSelectMany()
        {
            var linq =
                from x in _users
                from y in _userExes
                where x.Id < 10 && y.Id < 10
                orderby x.Id descending
                select new { x.Id, y.RoleType };

            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();
            var memoryResults = (from x in allUsers
                                 from y in allUserExes
                                 where x.Id < 10 && y.Id < 10
                                 orderby x.Id descending
                                 select new { x.Id, y.RoleType }).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].RoleType, results[i].RoleType);
            }
        }

        /// <summary>
        /// Cross Join + 元素null判断测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,CASE WHEN `x`.`Id` IS NOT NULL THEN `x`.`Id` ELSE `y`.`Id` END AS `Code` FROM `fmk_users` AS `x`,`fmk_user_exs` AS `y` WHERE `x`.`Id`&lt;10 AND `y`.`Id`&lt;10 AND (`x`.`Id` IS NULL OR `y`.`Id` IS NULL) ORDER BY `x`.`Id` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestCrossJoinWhereElementNullSelectMany()
        {
            var linq =
                from x in _users
                from y in _userExes
                where x.Id < 10 && y.Id < 10
                orderby x.Id descending
                where x == null || y == null
                select new { x.Id, Code = x != null ? x.Id : y.Id };

            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();
            var memoryResults = (from x in allUsers
                                 from y in allUserExes
                                 where x.Id < 10 && y.Id < 10
                                 orderby x.Id descending
                                 where x == null || y == null
                                 select new { x.Id, Code = x != null ? x.Id : y.Id }).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Code, results[i].Code);
            }
        }

        /// <summary>
        /// SQL预览:
        /// SELECT `x`.`id` AS `Id`, `y`.`name` AS `Name` 
        /// FROM `user` AS `x` 
        /// INNER JOIN `user` AS `y` ON `x`.`id` = `y`.`id` + 1 
        /// ORDER BY `x`.`id` DESC
        /// </summary>
        [Fact]
        public void TestJoinSelfSimple()
        {
            var linq =
                from x in _users
                join y in _users on x.Id equals y.Id + 1
                orderby x.Id descending
                select new { x.Id, y.Name };

            var allUsers = _users.ToList();
            var memoryResults = (from x in allUsers
                                 join y in allUsers on x.Id equals y.Id + 1
                                 orderby x.Id descending
                                 select new { x.Id, y.Name }).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
            }
        }

        /// <summary>
        /// 自关联Join + SelectMany测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`z`.`Name` FROM `fmk_users` AS `x` INNER JOIN `fmk_users` AS `z` ON `x`.`Id`=`z`.`Id`+1 ORDER BY `x`.`Id` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestJoinSelfSelectMany()
        {
            var linq =
                from x in _users
                join y in _users on x.Id equals y.Id + 1 into g
                from z in g
                orderby x.Id descending
                select new { x.Id, z.Name };

            var allUsers = _users.ToList();
            var memoryResults = (from x in allUsers
                                 join y in allUsers on x.Id equals y.Id + 1 into g
                                 from z in g
                                 orderby x.Id descending
                                 select new { x.Id, z.Name }).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
            }
        }

        /// <summary>
        /// 自关联Left Join + SelectMany测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`z`.`Name` FROM `fmk_users` AS `x` LEFT JOIN `fmk_users` AS `z` ON `x`.`Id`=`z`.`Id`+1 ORDER BY `x`.`Id` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestLeftJoinSelfSelectMany()
        {
            var linq =
                from x in _users
                join y in _users on x.Id equals y.Id + 1 into g
                from z in g.DefaultIfEmpty()
                orderby x.Id descending
                select new { x.Id, z.Name };

            var allUsers = _users.ToList();
            var memoryResults = (from x in allUsers
                                 join y in allUsers on x.Id equals y.Id + 1 into g
                                 from z in g.DefaultIfEmpty()
                                 orderby x.Id descending
                                 select new { x.Id, Name = z?.Name }).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
            }
        }

        /// <summary>
        /// 分片自关联Left Join测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`z`.`Name` FROM `fmk_users` AS `x` LEFT JOIN `fmk_user_shardings_2025` AS `z` ON `x`.`Id`=`z`.`Id`+1 ORDER BY `x`.`Id` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestShardingLeftJoinSelfSelectMany()
        {
            var allUsers = _users.ToList();
            var allUserShardings = _userShardings.DataSharding("2025").ToList();

            var memoryResults = (from x in allUsers
                                 join y in allUserShardings on x.Id equals y.Id + 1 into g
                                 from z in g.DefaultIfEmpty()
                                 orderby x.Id descending
                                 select new { x.Id, Name = z?.Name }).ToList();

            var linq =
                from x in _users
                join y in _userShardings.DataSharding("2025") on x.Id equals y.Id + 1 into g
                from z in g.DefaultIfEmpty()
                orderby x.Id descending
                select new { x.Id, z.Name };

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
            }
        }

        /// <summary>
        /// 自关联Cross Join测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`x`.`Name`,`y`.`Id` AS `RelationId`,`y`.`Name` AS `RelationName` FROM `fmk_users` AS `x`,`fmk_users` AS `y` WHERE `x`.`Id`&lt;150 AND `y`.`Id`&lt;150 ORDER BY `x`.`Id` DESC LIMIT 1
        /// </code>
        /// </remarks>
        [Fact]
        public void TestCrossJoinSelfSelectMany()
        {
            var linq =
                from x in _users
                from y in _users
                where x.Id < 150 && y.Id < 150
                orderby x.Id descending
                select new
                {
                    x.Id,
                    x.Name,
                    RelationId = y.Id,
                    RelationName = y.Name
                };

            var allUsers = _users.ToList();
            var memoryResults = (from x in allUsers
                                 from y in allUsers
                                 where x.Id < 150 && y.Id < 150
                                 orderby x.Id descending
                                 select new
                                 {
                                     x.Id,
                                     x.Name,
                                     RelationId = y.Id,
                                     RelationName = y.Name
                                 }).Take(1).ToList();

            var results = linq.Take(1).ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
                Assert.Equal(memoryResults[i].RelationId, results[i].RelationId);
                Assert.Equal(memoryResults[i].RelationName, results[i].RelationName);
            }
        }

        /// <summary>
        /// 分片Cross Join测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`x`.`Name`,`y`.`Id` AS `RelationId`,`y`.`Name` AS `RelationName` FROM `fmk_users` AS `x`,`fmk_user_shardings_2025` AS `y` ORDER BY `x`.`Id` DESC LIMIT 1
        /// </code>
        /// </remarks>
        [Fact]
        public void TestShardingCrossJoinSelfSelectMany()
        {
            var allUsers = _users.ToList();
            var allUserShardings = _userShardings.DataSharding("2025").ToList();

            var memoryResults = (from x in allUsers
                                 from y in allUserShardings
                                 orderby x.Id descending
                                 select new
                                 {
                                     x.Id,
                                     x.Name,
                                     RelationId = y.Id,
                                     RelationName = y.Name
                                 }).Take(1).ToList(); // 限制结果数量以避免大量数据

            var linq =
                from x in _users
                from y in _userShardings.DataSharding("2025")
                orderby x.Id descending
                select new
                {
                    x.Id,
                    x.Name,
                    RelationId = y.Id,
                    RelationName = y.Name
                };

            var results = linq.Take(1).ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
                Assert.Equal(memoryResults[i].RelationId, results[i].RelationId);
                Assert.Equal(memoryResults[i].RelationName, results[i].RelationName);
            }
        }

        /// <summary>
        /// SQL预览:
        /// SELECT COUNT(1) FROM `user` AS `x` WHERE `x`.`id` = 100
        /// </summary>
        [Fact]
        public void SimpleCount()
        {
            var linq = from x in _users where x.Id == 100 orderby x.DateAt, x.Name select x;

            var allUsers = _users.ToList();
            var memoryCount = (from x in allUsers where x.Id == 100 orderby x.DateAt, x.Name select x).Count();

            var count = linq.Count();

            Assert.Equal(memoryCount, count);
        }

        /// <summary>
        /// SQL预览:
        /// SELECT COUNT(CASE WHEN `x`.`name` LIKE '测试%' THEN 1 END) FROM `user` AS `x` WHERE `x`.`id` = 100
        /// </summary>
        [Fact]
        public void WhereCount()
        {
            var linq = from x in _users where x.Id == 100 select x;

            var allUsers = _users.ToList();
            var memoryCount = (from x in allUsers where x.Id == 100 select x).Count(x => x.Name.StartsWith("测试"));

            var count = linq.Count(x => x.Name.StartsWith("测试"));

            Assert.Equal(memoryCount, count);
        }

        /// <summary>
        /// SQL预览:
        /// SELECT COUNT(1) FROM `user` AS `x` WHERE `x`.`id` = 100
        /// </summary>
        [Fact]
        public void SelectCount()
        {
            var linq = from x in _users where x.Id == 100 select x.Id;

            var allUsers = _users.ToList();
            var memoryCount = (from x in allUsers where x.Id == 100 select x.Id).Count();

            var count = linq.Count();

            Assert.Equal(memoryCount, count);
        }

        /// <summary>
        /// SQL预览:
        /// SELECT COUNT(*) FROM `user` AS `x` WHERE `x`.`id` = 100
        /// </summary>
        [Fact]
        public void SelectMultiCount()
        {
            var allUsers = _users.ToList();
            var memoryCount = (from x in allUsers where x.Id == 100 select new { x.Id, x.Name }).Count();

            var linq = from x in _users where x.Id == 100 select new { x.Id, x.Name };

            var count = linq.Count(); //? 生成 SELECT COUNT(*) FROM `user` AS `x` WHERE `x`.`id` = 100

            Assert.Equal(memoryCount, count);
        }

        /// <summary>
        /// SQL预览:
        /// SELECT COUNT(DISTINCT `x`.`id`, `x`.`name`) FROM `user` AS `x` WHERE `x`.`id` = 100
        /// </summary>
        [Fact]
        public void DistinctSelectMultiCount()
        {
            var allUsers = _users.ToList();
            var memoryCount = (from x in allUsers where x.Id == 100 select new { x.Id, x.Name }).Distinct().Count();

            var linq = from x in _users where x.Id == 100 select new { x.Id, x.Name };

            var count = linq.Distinct().Count(); //? 生成 SELECT COUNT(DISTINCT `x`.`id`, `x`.`name`) FROM `user` AS `x` WHERE `x`.`id` = 100

            Assert.Equal(memoryCount, count);
        }

        /// <summary>
        /// SQL预览:
        /// SELECT COUNT(DISTINCT `x`.`name`) FROM `user` AS `x` WHERE `x`.`id` = 100
        /// </summary>
        [Fact]
        public void DistinctSelectCount()
        {
            var allUsers = _users.ToList();
            var memoryCount = (from x in allUsers where x.Id == 100 select x.Name).Distinct().Count();

            var linq = from x in _users where x.Id == 100 select x.Name;

            var count = linq.Distinct().Count();

            Assert.Equal(memoryCount, count);
        }

        /// <summary>
        /// SQL预览:
        /// SELECT COUNT(1) FROM (SELECT `x`.`name` AS `Key` FROM `user` AS `x` 
        /// GROUP BY `x`.`name` HAVING COUNT(1) > 1) AS `g`
        /// </summary>
        [Fact]
        public void GroupBySelectCount()
        {
            var allUsers = _users.ToList();
            var memoryCount = (from x in allUsers group x by x.Name into g where g.Count() > 1 select g.Key).Count();

            var linq = from x in _users group x by x.Name into g where g.Count() > 1 select g.Key;

            var count = linq.Count();

            Assert.Equal(memoryCount, count);
        }

        /// <summary>
        /// SQL预览:
        /// SELECT COUNT(DISTINCT `g`.`Name`) FROM (
        ///     SELECT `x`.`name` AS `Name`, `x`.`date` AS `DateAt` FROM `user` AS `x` 
        ///     GROUP BY `x`.`name`, `x`.`date` HAVING COUNT(1) > 1
        /// ) AS `g`
        /// </summary>
        [Fact]
        public void GroupByDistinctCount()
        {
            var allUsers = _users.ToList();
            var memoryCount = (from x in allUsers
                               group x by new { x.Name, x.DateAt } into g
                               where g.Count() > 1
                               select g.Key).Select(x => x.Name).Distinct().Count();

            var linq =
                from x in _users
                group x by new { x.Name, x.DateAt } into g
                where g.Count() > 1
                select g.Key;

            var count = linq.Select(x => x.Name).Distinct().Count();

            Assert.Equal(memoryCount, count);
        }

        /// <summary>
        /// 简单Max聚合异常测试。
        /// </summary>
        /// <remarks>
        /// <b>异常</b>：需指定聚合字段，不支持排序。
        /// </remarks>
        [Fact]
        public void SimpleMax()
        {
            var linq = from x in _users where x.Id == 100 orderby x.DateAt, x.Name select x;

            // 异常：1、需指定聚合字段。2、不支持排序。
            Assert.Throws<DSyntaxErrorException>(() => linq.Max());
        }

        /// <summary>
        /// SQL预览:
        /// SELECT MAX(`x`.`id`) FROM `user` AS `x` WHERE `x`.`id` = 100
        /// </summary>
        [Fact]
        public void SelectMax()
        {
            var linq = from x in _users where x.Id == 100 select x.Id;

            var allUsers = _users.ToList();
            var memoryMax = (from x in allUsers where x.Id == 100 select x.Id).Max();

            var max = linq.Max();

            Assert.Equal(memoryMax, max);
        }

        /// <summary>
        /// SQL预览:
        /// SELECT MAX(DISTINCT `x`.`name`) FROM `user` AS `x` WHERE `x`.`id` = 100
        /// </summary>
        [Fact]
        public void DistinctSelectMax()
        {
            var linq = from x in _users where x.Id == 100 select x.Name;

            var allUsers = _users.ToList();
            var memoryMax = (from x in allUsers where x.Id == 100 select x.Name).Distinct().Max();

            var max = linq.Distinct().Max();

            Assert.Equal(memoryMax, max);
        }

        /// <summary>
        /// GroupBy + Select Max测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT MAX(`g`.`Key`) FROM (SELECT `x`.`Name` AS `Key` FROM `fmk_users` AS `x` GROUP BY `x`.`Name` HAVING COUNT(1)&gt;1) AS `g`
        /// </code>
        /// </remarks>
        [Fact]
        public void GroupBySelectMax()
        {
            var linq = from x in _users group x by x.Name into g where g.Count() > 1 select g.Key;

            /*             var allUsers = _users.ToList();
                        var memoryMax = (from x in allUsers group x by x.Name into g where g.Count() > 1 select g.Key).Max(); */

            var max = linq.Max();

            // Assert.Equal(memoryMax, max); 字符串数据库和内存排序不一致，无法比较
        }

        /// <summary>
        /// GroupBy + Distinct + Max测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT MAX(DISTINCT `g`.`Key`) FROM (SELECT `x`.`Name` AS `Key` FROM `fmk_users` AS `x` GROUP BY `x`.`Name` HAVING COUNT(1)&gt;1) AS `g`
        /// </code>
        /// </remarks>
        [Fact]
        public void GroupByDistinctMax()
        {
            var linq = from x in _users group x by x.Name into g where g.Count() > 1 select g.Key;

            /*             var allUsers = _users.ToList();
                        var memoryMax = (from x in allUsers group x by x.Name into g where g.Count() > 1 select g.Key).Distinct().Max(); */

            var max = linq.Distinct().Max();

            // Assert.Equal(memoryMax, max); 字符串数据库和内存排序不一致，无法比较
        }

        /// <summary>
        /// GroupBy + Conditions.IsTrue测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT MAX(DISTINCT `g`.`Key`) FROM (SELECT `x`.`Name` AS `Key` FROM `fmk_users` AS `x` GROUP BY `x`.`Name` HAVING COUNT(1)&gt;1) AS `g`
        /// </code>
        /// </remarks>
        [Fact]
        public void GroupByConditionIsTrue()
        {
            var linq =
                from x in _users
                group x by x.Name into g
                where Conditions.IsTrue(g, x => x.Count() > 1)
                select g.Key;

            var max = linq.Distinct().Max();
        }

        /// <summary>
        /// GroupBy + Conditions.If变量测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT MAX(DISTINCT `g`.`Key`) FROM (SELECT `x`.`Name` AS `Key` FROM `fmk_users` AS `x` GROUP BY `x`.`Name` HAVING COUNT(1)&gt;1 AND `x`.`Name` LIKE '%测试%') AS `g`
        /// </code>
        /// </remarks>
        [Fact]
        public void GroupByConditionIfVariable()
        {
            string name = "测试";

            var linq =
                from x in _users
                group x by x.Name into g
                where Conditions.IsTrue(g, x => x.Count() > 1) && Conditions.If(g, !string.IsNullOrEmpty(name), x => x.Key.Contains(name))
                select g.Key;

            var max = linq.Distinct().Max();
        }

        /// <summary>
        /// GroupBy条件为null变量测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT MAX(DISTINCT `g`.`Key`) FROM (SELECT `x`.`Name` AS `Key` FROM `fmk_users` AS `x` GROUP BY `x`.`Name` HAVING 0) AS `g`
        /// </code>
        /// </remarks>
        [Fact]
        public void GroupByConditionIfVariableNull()
        {
            string name = null;

            var linq =
                from x in _users
                group x by x.Name into g
                where !string.IsNullOrEmpty(name) && !g.Key.Contains(name)
                select g.Key;

            var max = linq.Distinct().Max();
        }

        /// <summary>
        /// GroupBy + Conditions.If测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT MAX(DISTINCT `g`.`Key`) FROM (SELECT `x`.`Name` AS `Key` FROM `fmk_users` AS `x` GROUP BY `x`.`Name` HAVING CASE WHEN COUNT(1)&gt;1 THEN `x`.`Name` LIKE '%测试%' ELSE 1 END) AS `g`
        /// </code>
        /// </remarks>
        [Fact]
        public void GroupByConditionIf()
        {
            string name = "测试";

            var linq =
                from x in _users
                group x by x.Name into g
                where Conditions.If(g, g.Count() > 1, x => x.Key.Contains(name))
                select g.Key;

            var max = linq.Distinct().Max();
        }

        /// <summary>
        /// GroupBy + Conditions.Conditional变量测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT MAX(DISTINCT `g`.`Key`) FROM (SELECT `x`.`Name` AS `Key` FROM `fmk_users` AS `x` GROUP BY `x`.`Name` HAVING `x`.`Name` LIKE '%测试%') AS `g`
        /// </code>
        /// </remarks>
        [Fact]
        public void GroupByConditionConditionalVariable()
        {
            string name = "测试";

            var linq =
                from x in _users
                group x by x.Name into g
                where Conditions.Conditional(g, string.IsNullOrEmpty(name), x => x.Count() > 1, x => x.Key.Contains(name))
                select g.Key;

            var max = linq.Distinct().Max();
        }

        /// <summary>
        /// GroupBy + Conditions.Conditional测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT MAX(DISTINCT `g`.`Key`) FROM (SELECT `x`.`Name` AS `Key` FROM `fmk_users` AS `x` GROUP BY `x`.`Name` HAVING CASE WHEN COUNT(1)&gt;1 THEN `x`.`Name` LIKE '%测试%' ELSE `x`.`Name` LIKE '测试%' END) AS `g`
        /// </code>
        /// </remarks>
        [Fact]
        public void GroupByConditionConditional()
        {
            string name = "测试";

            var linq =
                from x in _users
                group x by x.Name into g
                where Conditions.Conditional(g, g.Count() > 1, x => x.Key.Contains(name), x => x.Key.StartsWith(name))
                select g.Key;

            var max = linq.Distinct().Max();
        }

        /// <summary>
        /// SQL预览:
        /// (SELECT `x`.`id` AS `Id`, `y`.`role_type` AS `RoleType`, 1 AS `Type` 
        ///  FROM `user` AS `x` INNER JOIN `user_ex` AS `y` ON `x`.`id` = `y`.`id`)
        /// UNION 
        /// (SELECT `x`.`id` AS `Id`, `z`.`role_type` AS `RoleType`, 2 AS `Type` 
        ///  FROM `user` AS `x` LEFT JOIN `user_ex` AS `z` ON `x`.`id` = `z`.`id`)
        /// </summary>
        [Fact]
        public void Union()
        {
            var linq =
                from x in _users
                join y in _userExes on x.Id equals y.Id
                select new
                {
                    x.Id,
                    y.RoleType,
                    Type = 1
                };

            var linq2 =
                from x in _users
                join y in _userExes on x.Id equals y.Id into g
                from z in g.DefaultIfEmpty()
                select new
                {
                    x.Id,
                    z.RoleType,
                    Type = 2
                };

            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();

            var memoryLinq1 = (from x in allUsers
                               join y in allUserExes on x.Id equals y.Id
                               select new
                               {
                                   x.Id,
                                   y.RoleType,
                                   Type = 1
                               }).ToList();

            var memoryLinq2 = (from x in allUsers
                               join y in allUserExes on x.Id equals y.Id into g
                               from z in g.DefaultIfEmpty()
                               select new
                               {
                                   x.Id,
                                   RoleType = z?.RoleType ?? 0,
                                   Type = 2
                               }).ToList();

            var memoryResults = memoryLinq1.Union(memoryLinq2).ToList();

            var results = linq.Union(linq2).ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同，按ID排序后比较
            var sortedMemoryResults = memoryResults.OrderBy(x => x.Id).ThenBy(x => x.Type).ToList();
            var sortedResults = results.OrderBy(x => x.Id).ThenBy(x => x.Type).ToList();

            for (int i = 0; i < sortedMemoryResults.Count; i++)
            {
                Assert.Equal(sortedMemoryResults[i].Id, sortedResults[i].Id);
                Assert.Equal(sortedMemoryResults[i].RoleType, sortedResults[i].RoleType);
                Assert.Equal(sortedMemoryResults[i].Type, sortedResults[i].Type);
            }
        }

        /// <summary>
        /// Concat + Select + Distinct 测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT DISTINCT CONCAT(`__g`.`RoleType`,`__g`.`Type`) AS `__col0__` FROM ((SELECT `x`.`Id`,`y`.`RoleType`,1 AS `Type` FROM `fmk_users` AS `x` INNER JOIN `fmk_user_exs` AS `y` ON `x`.`Id`=`y`.`Id`) UNION ALL (SELECT `x`.`Id`,`z`.`RoleType`,2 AS `Type` FROM `fmk_users` AS `x` LEFT JOIN `fmk_user_exs` AS `z` ON `x`.`Id`=`z`.`Id`)) AS `__g`
        /// </code>
        /// </remarks>
        [Fact]
        public void ConcatSelectDistinct()
        {
            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();

            var memoryLinq1 = (from x in allUsers
                               join y in allUserExes on x.Id equals y.Id
                               select new
                               {
                                   x.Id,
                                   y.RoleType,
                                   Type = 1
                               }).ToList();

            var memoryLinq2 = (from x in allUsers
                               join y in allUserExes on x.Id equals y.Id into g
                               from z in g.DefaultIfEmpty()
                               select new
                               {
                                   x.Id,
                                   RoleType = z?.RoleType ?? 0,
                                   Type = 2
                               }).ToList();

            var memoryResults = memoryLinq1.Concat(memoryLinq2)
                .Select(x => string.Concat(x.RoleType, x.Type))
                .Distinct()
                .ToList();

            var linq =
                from x in _users
                join y in _userExes on x.Id equals y.Id
                select new
                {
                    x.Id,
                    y.RoleType,
                    Type = 1
                };

            var linq2 =
                from x in _users
                join y in _userExes on x.Id equals y.Id into g
                from z in g.DefaultIfEmpty()
                select new
                {
                    x.Id,
                    RoleType = z == null ? 0 : z.RoleType,
                    Type = 2
                };

            var results = linq.Concat(linq2)
                .Select(x => string.Concat(x.RoleType, x.Type))
                .Distinct()
                .ToList();

            Assert.Equal(memoryResults.Count, results.Count);
            var sortedMemoryResults = memoryResults.OrderBy(x => x).ToList();
            var sortedResults = results.OrderBy(x => x).ToList();
            Assert.Equal(sortedMemoryResults, sortedResults);
        }

        /// <summary>
        /// Union + Where 测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT DISTINCT CONCAT(`__g`.`RoleType`,`__g`.`Type`) AS `__col0__` FROM ((SELECT `x`.`Id`,`y`.`RoleType`,1 AS `Type` FROM `fmk_users` AS `x` INNER JOIN `fmk_user_exs` AS `y` ON `x`.`Id`=`y`.`Id`) UNION (SELECT `x`.`Id`,`z`.`RoleType`,2 AS `Type` FROM `fmk_users` AS `x` LEFT JOIN `fmk_user_exs` AS `z` ON `x`.`Id`=`z`.`Id`)) AS `__g` WHERE `__g`.`RoleType`=2
        /// </code>
        /// </remarks>
        [Fact] //? MySQL 不支持 Except 语法。
        public void UnionWhere()
        {
            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();

            var memoryLinq1 = (from x in allUsers
                               join y in allUserExes on x.Id equals y.Id
                               select new
                               {
                                   x.Id,
                                   y.RoleType,
                                   Type = 1
                               }).ToList();

            var memoryLinq2 = (from x in allUsers
                               join y in allUserExes on x.Id equals y.Id into g
                               from z in g.DefaultIfEmpty()
                               select new
                               {
                                   x.Id,
                                   RoleType = z?.RoleType ?? 0,
                                   Type = 2
                               }).ToList();

            var memoryResults = memoryLinq1.Union(memoryLinq2)
                .Where(x => x.RoleType == 2)
                .Select(x => string.Concat(x.RoleType, x.Type))
                .Distinct()
                .ToList();

            var linq =
                from x in _users
                join y in _userExes on x.Id equals y.Id
                select new
                {
                    x.Id,
                    y.RoleType,
                    Type = 1
                };

            var linq2 =
                from x in _users
                join y in _userExes on x.Id equals y.Id into g
                from z in g.DefaultIfEmpty()
                select new
                {
                    x.Id,
                    z.RoleType,
                    Type = 2
                };

            var results = linq.Union(linq2)
                .Where(x => x.RoleType == 2)
                .Select(x => string.Concat(x.RoleType, x.Type))
                .Distinct()
                .ToList();

            Assert.Equal(memoryResults.Count, results.Count);
            var sortedMemoryResults = memoryResults.OrderBy(x => x).ToList();
            var sortedResults = results.OrderBy(x => x).ToList();
            Assert.Equal(sortedMemoryResults, sortedResults);
        }

        /// <summary>
        /// Union嵌套Take测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// (SELECT `x`.`Id`,`y`.`RoleType`,1 AS `Type` FROM `fmk_users` AS `x` INNER JOIN `fmk_user_exs` AS `y` ON `x`.`Id`=`y`.`Id` ORDER BY `x`.`Id` DESC LIMIT 10) UNION (SELECT `x`.`Id`,`z`.`RoleType`,2 AS `Type` FROM `fmk_users` AS `x` LEFT JOIN `fmk_user_exs` AS `z` ON `x`.`Id`=`z`.`Id` ORDER BY `x`.`Id` DESC LIMIT 10)
        /// </code>
        /// </remarks>
        [Fact]
        public void UnionNestedTake()
        {
            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();

            var memoryLinq1 = (from x in allUsers
                               join y in allUserExes on x.Id equals y.Id
                               orderby x.Id descending
                               select new
                               {
                                   x.Id,
                                   y.RoleType,
                                   Type = 1
                               }).Take(10).ToList();

            var memoryLinq2 = (from x in allUsers
                               join y in allUserExes on x.Id equals y.Id into g
                               from z in g.DefaultIfEmpty()
                               orderby x.Id descending
                               select new
                               {
                                   x.Id,
                                   RoleType = z?.RoleType ?? 0,
                                   Type = 2
                               }).Take(10).ToList();

            var memoryResults = memoryLinq1.Union(memoryLinq2).ToList();

            var linq =
                from x in _users
                join y in _userExes on x.Id equals y.Id
                orderby x.Id descending
                select new
                {
                    x.Id,
                    y.RoleType,
                    Type = 1
                };

            var linq2 =
                from x in _users
                join y in _userExes on x.Id equals y.Id into g
                from z in g.DefaultIfEmpty()
                orderby x.Id descending
                select new
                {
                    x.Id,
                    z.RoleType,
                    Type = 2
                };

            var results = linq.Take(10).Union(linq2.Take(10)).ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同，按ID排序后比较
            var sortedMemoryResults = memoryResults.OrderBy(x => x.Id).ThenBy(x => x.Type).ToList();
            var sortedResults = results.OrderBy(x => x.Id).ThenBy(x => x.Type).ToList();

            for (int i = 0; i < sortedMemoryResults.Count; i++)
            {
                Assert.Equal(sortedMemoryResults[i].Id, sortedResults[i].Id);
                Assert.Equal(sortedMemoryResults[i].RoleType, sortedResults[i].RoleType);
                Assert.Equal(sortedMemoryResults[i].Type, sortedResults[i].Type);
            }
        }

        /// <summary>
        /// Union Take 测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `__g`.`Id`,`__g`.`RoleType`,`__g`.`Type` FROM ((SELECT `x`.`Id`,`y`.`RoleType`,1 AS `Type` FROM `fmk_users` AS `x` INNER JOIN `fmk_user_exs` AS `y` ON `x`.`Id`=`y`.`Id` ORDER BY `x`.`Id` DESC LIMIT 10) UNION (SELECT `x`.`Id`,`z`.`RoleType`,2 AS `Type` FROM `fmk_users` AS `x` LEFT JOIN `fmk_user_exs` AS `z` ON `x`.`Id`=`z`.`Id` ORDER BY `x`.`Id` DESC LIMIT 10)) AS `__g` ORDER BY `__g`.`Id` ASC LIMIT 10
        /// </code>
        /// </remarks>
        [Fact]
        public void UnionTake()
        {
            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();

            var memoryLinq1 = (from x in allUsers
                               join y in allUserExes on x.Id equals y.Id
                               orderby x.Id descending
                               select new
                               {
                                   x.Id,
                                   y.RoleType,
                                   Type = 1
                               }).Take(10).ToList();

            var memoryLinq2 = (from x in allUsers
                               join y in allUserExes on x.Id equals y.Id into g
                               from z in g.DefaultIfEmpty()
                               orderby x.Id descending
                               select new
                               {
                                   x.Id,
                                   RoleType = z?.RoleType ?? 0,
                                   Type = 2
                               }).Take(10).ToList();

            var memoryResults = memoryLinq1.Union(memoryLinq2).OrderBy(x => x.Id).Take(10).ToList();

            var linq =
                from x in _users
                join y in _userExes on x.Id equals y.Id
                orderby x.Id descending
                select new
                {
                    x.Id,
                    y.RoleType,
                    Type = 1
                };

            var linq2 =
                from x in _users
                join y in _userExes on x.Id equals y.Id into g
                from z in g.DefaultIfEmpty()
                orderby x.Id descending
                select new
                {
                    x.Id,
                    z.RoleType,
                    Type = 2
                };

            var results = linq.Take(10).Union(linq2.Take(10)).OrderBy(x => x.Id).Take(10).ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].RoleType, results[i].RoleType);
                Assert.Equal(memoryResults[i].Type, results[i].Type);
            }
        }

        /// <summary>
        /// 任意测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT EXISTS(SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100)
        /// </code>
        /// </remarks>
        [Fact]
        public void TestAny()
        {
            var linq = from x in _users where x.Id == 100 select x.Id;

            var allUsers = _users.ToList();
            var memoryAny = (from x in allUsers where x.Id == 100 select x.Id).Any();

            bool any = linq.Any();

            Assert.Equal(memoryAny, any);
        }

        /// <summary>
        /// 全部测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT NOT EXISTS(SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE NOT (`x`.`Id`&lt;100))
        /// </code>
        /// </remarks>
        [Fact]
        public void TestAll()
        {
            var allUsers = _users.ToList();
            var memoryAll = allUsers.All(x => x.Id < 100);

            bool all = _users.All(x => x.Id < 100);

            Assert.Equal(memoryAll, all);
        }

        /// <summary>
        /// 内置任意满足测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND EXISTS(SELECT `y`.`Id` FROM `fmk_user_exs` AS `y` WHERE `y`.`Id`=`x`.`Id` AND `y`.`Age`&gt;12) ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestNestedAny()
        {
            var linq =
                from x in _users
                where x.Id == 100 && _userExes.Any(y => y.Id == x.Id && y.Age > 12)
                orderby x.DateAt, x.Name
                select x.Id;

            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();
            var memoryResults = (from x in allUsers
                                 where x.Id == 100 && allUserExes.Any(y => y.Id == x.Id && y.Age > 12)
                                 orderby x.DateAt, x.Name
                                 select x.Id).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较相同id的内容是否相同，不关心顺序
            Assert.Equal(memoryResults.OrderBy(x => x), results.OrderBy(x => x));
        }

        /// <summary>
        /// 内置任意满足测试（WhereIf变量）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND EXISTS(SELECT `y`.`Id` FROM `fmk_user_exs` AS `y` WHERE `y`.`Age`&gt;12 AND `y`.`Id`=`x`.`Id`) ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestWereIfVariableAny()
        {
            int? age = 12;

            var linq =
                from x in _users
                where x.Id == 100 && _userExes.WhereIf(age.HasValue, y => y.Age > age).Any(y => y.Id == x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 内置任意满足测试（WhereIf条件）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND EXISTS(SELECT `y`.`Id` FROM `fmk_user_exs` AS `y` WHERE (`x`.`DateAt`&gt;@__variable_now__ AND `y`.`Age`&gt;12) AND `y`.`Id`=`x`.`Id`) ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestWereIfAny()
        {
            var now = DateTime.Now;

            var linq =
                from x in _users
                where x.Id == 100 && _userExes.WhereIf(x.DateAt > now, y => y.Age > 12).Any(y => y.Id == x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 内置所有满足测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND NOT EXISTS(SELECT `y`.`Id` FROM `fmk_user_exs` AS `y` WHERE NOT (`y`.`Id`=`x`.`Id` AND `y`.`Age`&gt;12)) ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestNestedAll()
        {
            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();

            var memoryResults = (from x in allUsers
                                 where x.Id == 100 && allUserExes.All(y => y.Id == x.Id && y.Age > 12)
                                 orderby x.DateAt, x.Name
                                 select x.Id).ToList();

            var linq =
                from x in _users
                where x.Id == 100 && _userExes.All(y => y.Id == x.Id && y.Age > 12)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);
            Assert.Equal(memoryResults.OrderBy(x => x), results.OrderBy(x => x));
        }

        /// <summary>
        /// 内置包含测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`Id` IN(SELECT `y`.`Id` FROM `fmk_user_exs` AS `y` WHERE `y`.`Age`&gt;12) ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestNestedContains()
        {
            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();

            var memoryResults = (from x in allUsers
                                 where
                                     x.Id == 100 && allUserExes.Where(y => y.Age > 12).Select(y => y.Id).Contains(x.Id)
                                 orderby x.DateAt, x.Name
                                 select x.Id).ToList();

            var linq =
                from x in _users
                where
                    x.Id == 100 && _userExes.Where(y => y.Age > 12).Select(y => y.Id).Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);
            Assert.Equal(memoryResults.OrderBy(x => x), results.OrderBy(x => x));
        }

        /// <summary>
        /// 内置不包含测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`Id` NOT IN(SELECT `y`.`Id` FROM `fmk_user_exs` AS `y` WHERE `y`.`Age`&gt;12) ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestNestedNotContains()
        {
            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();

            var memoryResults = (from x in allUsers
                                 where
                                     x.Id == 100 && !allUserExes.Where(y => y.Age > 12).Select(y => y.Id).Contains(x.Id)
                                 orderby x.DateAt, x.Name
                                 select x.Id).ToList();

            var linq =
                from x in _users
                where
                    x.Id == 100 && !_userExes.Where(y => y.Age > 12).Select(y => y.Id).Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);
            Assert.Equal(memoryResults.OrderBy(x => x), results.OrderBy(x => x));
        }

        /// <summary>
        /// 内置多条件不包含测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND NOT (`x`.`Id` IN(SELECT `y`.`Id` FROM `fmk_user_exs` AS `y` WHERE `y`.`Age`&gt;12) OR EXISTS(SELECT `y`.`Id` FROM `fmk_user_exs` AS `y` WHERE `y`.`Age`&gt;12 AND `y`.`Id`=`x`.`Id`)) ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestNestedNotWrapContains()
        {
            var allUsers = _users.ToList();
            var allUserExes = _userExes.ToList();

            var memoryResults = (from x in allUsers
                                 where
                                     x.Id == 100 && !(allUserExes.Where(y => y.Age > 12).Select(y => y.Id).Contains(x.Id) || allUserExes.Where(y => y.Age > 12).Any(y => y.Id == x.Id))
                                 orderby x.DateAt, x.Name
                                 select x.Id).ToList();

            var linq =
                from x in _users
                where
                    x.Id == 100 && !(_userExes.Where(y => y.Age > 12).Select(y => y.Id).Contains(x.Id) || _userExes.Where(y => y.Age > 12).Any(y => y.Id == x.Id))
                orderby x.DateAt, x.Name
                select x.Id;

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);
            Assert.Equal(memoryResults.OrderBy(x => x), results.OrderBy(x => x));
        }

        /// <summary>
        /// 内存集合包含测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`Id` IN(1,2,5) ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestNestedMemoryContains()
        {
            var ids = new List<long> { 1, 2, 5 };

            var linq =
                from x in _users
                where x.Id == 100 && ids.Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 内存HashSet包含测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`Id` IN(1,2,5) ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestNestedMemoryHashContains()
        {
            var ids = new HashSet<long> { 1, 2, 5 };

            var linq =
                from x in _users
                where x.Id == 100 && ids.Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var allUsers = _users.ToList();
            var memoryResults = (from x in allUsers
                                 where x.Id == 100 && ids.Contains(x.Id)
                                 orderby x.DateAt, x.Name
                                 select x.Id).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较相同id的内容是否相同，不关心顺序
            Assert.Equal(memoryResults.OrderBy(x => x), results.OrderBy(x => x));
        }

        /// <summary>
        /// 内存集合Any测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`Id` IN(256,257,258,...) ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestNestedMemoryAny()
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

            var linq =
                from x in _users
                where x.Id == 100 && users.Any(y => x.Id == y.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var allUsers = _users.ToList();
            var memoryResults = (from x in allUsers
                                 where x.Id == 100 && users.Any(y => x.Id == y.Id)
                                 orderby x.DateAt, x.Name
                                 select x.Id).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较相同id的内容是否相同，不关心顺序
            Assert.Equal(memoryResults.OrderBy(x => x), results.OrderBy(x => x));
        }

        /// <summary>
        /// 同表多个别名。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`x`.`Account`,`x`.`Bcid`,`x`.`Name`,`x`.`Tel`,`x`.`Mail`,`x`.`Avatar`,`x`.`IsAdministrator`,`x`.`Sex`,`x`.`DateAt`,`x`.`Nullable` FROM `fmk_users` AS `x` WHERE `x`.`Id`&gt;100 AND `x`.`Id`&lt;1000 ORDER BY `x`.`DateAt` DESC LIMIT 1
        /// </code>
        /// </remarks>
        [Fact]
        public void TestLambdaMultiAliasName()
        {
            var allUsers = _users.ToList();
            var memoryUser = allUsers
                .Where(x => x.Id > 100)
                .OrderByDescending(z => z.DateAt)
                .FirstOrDefault(y => y.Id < 1000);

            var user = _users
                .Where(x => x.Id > 100)
                .OrderByDescending(z => z.DateAt)
                .FirstOrDefault(y => y.Id < 1000);

            if (memoryUser == null && user == null)
            {
                Assert.Null(user);
            }
            else if (memoryUser != null && user != null)
            {
                Assert.Equal(memoryUser.Id, user.Id);
                Assert.Equal(memoryUser.Name, user.Name);
                Assert.Equal(memoryUser.DateAt, user.DateAt);
            }
            else
            {
                Assert.Fail("Memory and database results don't match for null values");
            }
        }

        /// <summary>
        /// 内存集合包含多次查询测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`Id` IN(1,2,5) ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestNestedMemoryContainsMultiSelect()
        {
            var allUsers = _users.ToList();
            var ids = new List<long> { 1, 2, 5 };

            var memoryResults =
                (from x in allUsers
                 where x.Id == 100 && ids.Contains(x.Id)
                 orderby x.DateAt, x.Name
                 select x.Id).ToList();

            var linq =
                from x in _users
                where x.Id == 100 && ids.Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = linq.ToList();
            var results2 = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);
            Assert.Equal(memoryResults, results);
        }

        /// <summary>
        /// 内存集合包含多次查询测试2。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// -- 第一次查询
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`Id` IN(1,2,5) ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC
        /// -- 第二次查询
        /// SELECT COUNT(1) FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`Id` IN(1,2,5)
        /// </code>
        /// </remarks>
        [Fact]
        public void TestNestedMemoryContainsMultiSelect2()
        {
            var allUsers = _users.ToList();
            var ids = new List<long> { 1, 2, 5 };

            var memoryResults =
                (from x in allUsers
                 where x.Id == 100 && ids.Contains(x.Id)
                 orderby x.DateAt, x.Name
                 select x.Id).ToList();

            var linq =
                from x in _users
                where x.Id == 100 && ids.Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = linq.ToList();
            var results2 = linq.Count();

            Assert.Equal(memoryResults.Count, results.Count);
            Assert.Equal(memoryResults.Count, results2);
            Assert.Equal(memoryResults, results);
        }

        /// <summary>
        /// 内存集合包含多次异步查询测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`Id` IN(1,2,5) ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC
        /// </code>
        /// </remarks>
        [Fact]
        public async Task TestNestedMemoryContainsMultiSelectAsync()
        {
            var allUsers = _users.ToList();
            var ids = new List<long> { 1, 2, 5 };

            var memoryResults =
                (from x in allUsers
                 where x.Id == 100 && ids.Contains(x.Id)
                 orderby x.DateAt, x.Name
                 select x.Id).ToList();

            var linq =
                from x in _users
                where x.Id == 100 && ids.Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = await linq.ToListAsync();
            var results2 = await linq.ToListAsync();

            Assert.Equal(memoryResults.Count, results.Count);
            Assert.Equal(memoryResults, results);
        }

        /// <summary>
        /// 内存集合不包含多次异步查询测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`Id` NOT IN(1,2,5) ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC
        /// </code>
        /// </remarks>
        [Fact]
        public async Task TestNestedMemoryNotContainsMultiSelectAsync()
        {
            var allUsers = _users.ToList();
            var ids = new List<long> { 1, 2, 5 };

            var memoryResults =
                (from x in allUsers
                 where x.Id == 100 && !ids.Contains(x.Id)
                 orderby x.DateAt, x.Name
                 select x.Id).ToList();

            var linq =
                from x in _users
                where x.Id == 100 && !ids.Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = await linq.ToListAsync();
            var results2 = await linq.ToListAsync();

            Assert.Equal(memoryResults.Count, results.Count);
            Assert.Equal(memoryResults, results);
        }

        /// <summary>
        /// 内存集合包含多次异步查询2。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`Id` IN(1,2,5) ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC
        /// </code>
        /// </remarks>
        [Fact]
        public async Task TestNestedMemoryContainsMultiSelect2Async()
        {
            var allUsers = _users.ToList();
            var ids = new List<long> { 1, 2, 5 };

            var memoryResults =
                (from x in allUsers
                 where x.Id == 100 && ids.Contains(x.Id)
                 orderby x.DateAt, x.Name
                 select x.Id).ToList();

            var linq =
                from x in _users
                where x.Id == 100 && ids.Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = await Task.WhenAll(linq.ToListAsync(), linq.ToListAsync());

            Assert.Equal(memoryResults.Count, results[0].Count);
            Assert.Equal(memoryResults.Count, results[1].Count);
            Assert.Equal(memoryResults, results[0]);
            Assert.Equal(memoryResults, results[1]);
        }

        /// <summary>
        /// 内存集合包含多次异步查询3。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// -- 并行执行
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`Id` IN(1,2,5) ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC
        /// SELECT COUNT(1) FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`Id` IN(1,2,5)
        /// </code>
        /// </remarks>
        [Fact]
        public async Task TestNestedMemoryContainsMultiSelect3Async()
        {
            var allUsers = _users.ToList();
            var ids = new List<long> { 1, 2, 5 };

            var memoryResults =
                (from x in allUsers
                 where x.Id == 100 && ids.Contains(x.Id)
                 orderby x.DateAt, x.Name
                 select x.Id).ToList();

            var linq =
                from x in _users
                where x.Id == 100 && ids.Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var listTask = linq.ToListAsync();
            var countTask = linq.CountAsync();
            await Task.WhenAll(listTask, countTask);

            var results = await listTask;
            var count = await countTask;

            Assert.Equal(memoryResults.Count, results.Count);
            Assert.Equal(memoryResults.Count, count);
            Assert.Equal(memoryResults, results);
        }

        /// <summary>
        /// 内存集合包含多次异步查询4。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// -- 异步+同步查询
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`Id` IN(1,2,5) ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC
        /// SELECT COUNT(1) FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`Id` IN(1,2,5)
        /// </code>
        /// </remarks>
        [Fact]
        public async Task TestNestedMemoryContainsMultiSelect4Async()
        {
            var allUsers = _users.ToList();
            var ids = new List<long> { 1, 2, 5 };

            var memoryResults =
                (from x in allUsers
                 where x.Id == 100 && ids.Contains(x.Id)
                 orderby x.DateAt, x.Name
                 select x.Id).ToList();

            var linq =
                from x in _users
                where x.Id == 100 && ids.Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = await linq.ToListAsync();
            var results2 = linq.Count();

            Assert.Equal(memoryResults.Count, results.Count);
            Assert.Equal(memoryResults.Count, results2);
            Assert.Equal(memoryResults, results);
        }

        /// <summary>
        /// 内存集合包含多次异步查询5。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT COUNT(1) FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`Id` IN(1,2,5)
        /// </code>
        /// </remarks>
        [Fact]
        public async Task TestNestedMemoryContainsMultiSelect5Async()
        {
            var allUsers = _users.ToList();
            var ids = new List<long> { 1, 2, 5 };

            var memoryCount =
                (from x in allUsers
                 where x.Id == 100 && ids.Contains(x.Id)
                 orderby x.DateAt, x.Name
                 select x.Id).Count();

            var linq =
                from x in _users
                where x.Id == 100 && ids.Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = linq.Count();
            var results2 = await linq.CountAsync();

            Assert.Equal(memoryCount, results);
            Assert.Equal(memoryCount, results2);
        }

        /// <summary>
        /// 获取所有数据测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`x`.`Account`,`x`.`Bcid`,`x`.`Name`,`x`.`Tel`,`x`.`Mail`,`x`.`Avatar`,`x`.`IsAdministrator`,`x`.`Sex`,`x`.`DateAt`,`x`.`Nullable` FROM `fmk_users` AS `x`
        /// </code>
        /// </remarks>
        /// <returns></returns>
        [Fact]
        public async Task GetAllTestAsync()
        {
            await _users.ToListAsync();
        }

        /// <summary>
        /// 获取所有数据条数测试。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT COUNT(1) FROM `fmk_users` AS `x`
        /// </code>
        /// </remarks>
        /// <returns></returns>
        [Fact]
        public async Task GetCountAllTestAsync()
        {
            await _users.CountAsync();
        }

        /// <summary>
        /// 测试自定义排序。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC,`x`.`DateAt` ASC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestRankBy()
        {
            var type = 10;
            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt, x.Name, Ranks.By(x, c => c.When(type < 100).OrderBy(y => y.DateAt))
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试自定义排序（内部倒序）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC,`x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestRankByInnerDescending()
        {
            var type = 10;
            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt, x.Name, Ranks.By(x, c => c.When(type < 100).OrderByDescending(y => y.DateAt))
                select x.Id;

            var results = linq.ToList();
        }


        /// <summary>
        /// 测试自定义多排序。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC,`x`.`DateAt` DESC,`x`.`Id` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestRankByInnerMultiDescending()
        {
            var type = 10;
            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt, x.Name, Ranks.By(x, c => c.When(type < 100).OrderByDescending(y => y.DateAt).ThenByDescending(y => y.Id))
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试自定义多混合排序。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC,`x`.`DateAt` ASC,`x`.`Id` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestRankByInnerMultiBlendDescending()
        {
            var type = 10;
            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt, x.Name, Ranks.By(x, c => c.When(type < 100).OrderBy(y => y.DateAt).ThenByDescending(y => y.Id))
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试自定义多混合排序（带参数）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// -- type=10时：
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC,`x`.`DateAt` ASC,`x`.`Id` DESC
        /// -- type=100时（使用默认排序）：
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC,`x`.`DateAt` DESC,`x`.`Id` ASC
        /// </code>
        /// </remarks>
        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        public void TestRankByInnerMultiBlendArgumentDescending(int type)
        {
            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt, x.Name, Ranks.By(x, c => c.When(type < 100).OrderBy(y => y.DateAt).ThenByDescending(y => y.Id)
                        .DefaultByDescending(t => t.DateAt).DefaultBy(t => t.Id))
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试自定义排序倒序。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC,`x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestRankByDescending()
        {
            var type = 10;
            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt, x.Name, Ranks.By(x, c => c.When(type < 100).OrderBy(y => y.DateAt)) descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试自定义排序倒序和正常倒序。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC,`x`.`DateAt` ASC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestRankByRepeatDescending()
        {
            var type = 10;
            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt, x.Name, Ranks.By(x, c => c.When(type < 100).OrderByDescending(y => y.DateAt)) descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试自定义排序忽略。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestRankByIgnore()
        {
            var type = 10;
            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt, x.Name, Ranks.By(x, c => c.When(type > 100).OrderBy(y => y.DateAt))
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试自定义排序倒序忽略。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY `x`.`DateAt` ASC,`x`.`Name` ASC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestRankByDescendingIgnore()
        {
            var type = 10;
            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt, x.Name, Ranks.By(x, c => c.When(type > 100).OrderBy(y => y.DateAt)) descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试三目运算排序。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_user_exs` AS `x` WHERE `x`.`Id`=100 ORDER BY CASE WHEN `x`.`Id`&gt;100 THEN `x`.`Age` ELSE `x`.`RoleType` END ASC,`x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestOrderByConditional()
        {
            var allUserExes = _userExes.ToList();
            var memoryResults = (from x in allUserExes
                                 where x.Id == 100
                                 orderby x.Id > 100 ? x.Age : x.RoleType, x.DateAt descending
                                 select x.Id).ToList();

            var linq =
                from x in _userExes
                where x.Id == 100
                orderby x.Id > 100 ? x.Age : x.RoleType, x.DateAt descending
                select x.Id;

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);
            Assert.Equal(memoryResults, results);
        }

        /// <summary>
        /// 测试三目运算排序倒序。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_user_exs` AS `x` WHERE `x`.`Id`=100 ORDER BY CASE WHEN `x`.`Id`&gt;100 THEN `x`.`Age` ELSE `x`.`RoleType` END DESC,`x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestOrderByDescendingConditional()
        {
            var allUserExes = _userExes.ToList();
            var memoryResults = (from x in allUserExes
                                 where x.Id == 100
                                 orderby x.Id > 100 ? x.Age : x.RoleType descending, x.DateAt descending
                                 select x.Id).ToList();

            var linq =
                from x in _userExes
                where x.Id == 100
                orderby x.Id > 100 ? x.Age : x.RoleType descending, x.DateAt descending
                select x.Id;

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);
            Assert.Equal(memoryResults, results);
        }

        /// <summary>
        /// 测试合并运算排序。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY COALESCE(`x`.`Name`,'') ASC,`x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestOrderByCoalesce()
        {
            var linq =
                from x in _users
                where x.Id == 100
                orderby x.Name ?? string.Empty, x.DateAt descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试合并运算排序倒序。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY COALESCE(`x`.`Name`,'') DESC,`x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestOrderByDescendingCoalesce()
        {
            var allUsers = _users.ToList();
            var memoryResults = (from x in allUsers
                                 where x.Id == 100
                                 orderby x.Name ?? string.Empty descending, x.DateAt descending
                                 select x.Id).ToList();

            var linq =
                from x in _users
                where x.Id == 100
                orderby x.Name ?? string.Empty descending, x.DateAt descending
                select x.Id;

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);
            Assert.Equal(memoryResults, results);
        }

        /// <summary>
        /// 测试二元运算排序。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_user_exs` AS `x` WHERE `x`.`Id`=100 ORDER BY `x`.`Age`+`x`.`RoleType` ASC,`x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestOrderByBinary()
        {
            var allUserExes = _userExes.ToList();
            var memoryResults = (from x in allUserExes
                                 where x.Id == 100
                                 orderby x.Age + x.RoleType, x.DateAt descending
                                 select x.Id).ToList();

            var linq =
                from x in _userExes
                where x.Id == 100
                orderby x.Age + x.RoleType, x.DateAt descending
                select x.Id;

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);
            Assert.Equal(memoryResults, results);
        }

        /// <summary>
        /// 测试一元运算排序。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_user_exs` AS `x` WHERE `x`.`Id`=100 ORDER BY ~`x`.`Age` ASC,`x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestOrderByUnary()
        {
            var allUserExes = _userExes.ToList();
            var memoryResults = (from x in allUserExes
                                 where x.Id == 100
                                 orderby ~x.Age, x.DateAt descending
                                 select x.Id).ToList();

            var linq =
                from x in _userExes
                where x.Id == 100
                orderby ~x.Age, x.DateAt descending
                select x.Id;

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);
            Assert.Equal(memoryResults, results);
        }

        /// <summary>
        /// 测试字符串方法排序。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY SUBSTRING(`x`.`Name`,3,5) ASC,`x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestOrderByStringMethod()
        {
            var allUsers = _users.ToList();
            var memoryResults = (from x in allUsers
                                 where x.Id == 100 && x.Name != null && x.Name.Length >= 7
                                 orderby x.Name.Substring(2, 5), x.DateAt descending
                                 select x.Id).ToList();

            var linq =
                from x in _users
                where x.Id == 100
                orderby x.Name.Substring(2, 5), x.DateAt descending
                select x.Id;

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);
            Assert.Equal(memoryResults, results);
        }

        /// <summary>
        /// 测试字符串截取。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`x`.`Name`,SUBSTRING(`x`.`Name`,3,5) AS `NameSubstring` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY `x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestSubstring()
        {
            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt descending
                select new { x.Id, x.Name, NameSubstring = x.Name.Substring(2, 5) };

            var allUsers = _users.ToList();
            var memoryResults = (from x in allUsers
                                 where x.Id == 100
                                 orderby x.DateAt descending
                                 select new { x.Id, x.Name, NameSubstring = x.Name.Substring(2, 5) }).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
                Assert.Equal(memoryResults[i].NameSubstring, results[i].NameSubstring);
            }
        }

        /// <summary>
        /// 测试字符串空判断。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`x`.`Name`,(`x`.`Name` IS NULL OR `x`.`Name`='') AS `Null` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY `x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestIsNullOrEmpty()
        {
            var allUsers = _users.ToList();
            var memoryResults = (from x in allUsers
                                 where x.Id == 100
                                 orderby x.DateAt descending
                                 select new { x.Id, x.Name, Null = string.IsNullOrEmpty(x.Name) }).ToList();

            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt descending
                select new { x.Id, x.Name, Null = string.IsNullOrEmpty(x.Name) };

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
                Assert.Equal(memoryResults[i].Null, results[i].Null);
            }
        }

        /// <summary>
        /// 常量字符串空判断（普通变量）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`x`.`Name`,0 AS `Null` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY `x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestIsNullOrEmptyPlainVariable()
        {
            var name = "测试";
            var allUsers = _users.ToList();
            var memoryResults = (from x in allUsers
                                 where x.Id == 100
                                 orderby x.DateAt descending
                                 select new { x.Id, x.Name, Null = string.IsNullOrEmpty(name) }).ToList();

            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt descending
                select new { x.Id, x.Name, Null = string.IsNullOrEmpty(name) };

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
                Assert.Equal(memoryResults[i].Null, results[i].Null);
            }
        }

        /// <summary>
        /// 常量字符串空判断（空字符串）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`x`.`Name`,1 AS `Null` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY `x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestIsNullOrEmptyByEmpty()
        {
            string name = string.Empty;
            var allUsers = _users.ToList();
            var memoryResults = (from x in allUsers
                                 where x.Id == 100
                                 orderby x.DateAt descending
                                 select new { x.Id, x.Name, Null = string.IsNullOrEmpty(name) }).ToList();

            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt descending
                select new { x.Id, x.Name, Null = string.IsNullOrEmpty(name) };

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
                Assert.Equal(memoryResults[i].Null, results[i].Null);
            }
        }

        /// <summary>
        /// 常量字符串空判断（null）。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`x`.`Name`,1 AS `Null` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY `x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestIsNullOrEmptyByNull()
        {
            string name = null;
            var allUsers = _users.ToList();
            var memoryResults = (from x in allUsers
                                 where x.Id == 100
                                 orderby x.DateAt descending
                                 select new { x.Id, x.Name, Null = string.IsNullOrEmpty(name) }).ToList();

            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt descending
                select new { x.Id, x.Name, Null = string.IsNullOrEmpty(name) };

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
                Assert.Equal(memoryResults[i].Null, results[i].Null);
            }
        }

        /// <summary>
        /// 测试字符串替换。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`x`.`Name`,REPLACE(`x`.`Name`,'测试','测试2') AS `NickName` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY `x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestReplaceString()
        {
            var allUsers = _users.ToList();
            var memoryResults = (from x in allUsers
                                 where x.Id == 100 && x.Name != null
                                 orderby x.DateAt descending
                                 select new { x.Id, x.Name, NickName = x.Name.Replace("测试", "测试2") }).ToList();

            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt descending
                select new { x.Id, x.Name, NickName = x.Name.Replace("测试", "测试2") };

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
                Assert.Equal(memoryResults[i].NickName, results[i].NickName);
            }
        }

        /// <summary>
        /// 测试字符串查找位置。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`x`.`Name`,LOCATE('测试',`x`.`Name`,3)-1 AS `IndexOf` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY `x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestIndexOfString()
        {
            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt descending
                select new { x.Id, x.Name, IndexOf = x.Name.IndexOf("测试", 2) };

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试可空类型等于null。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`x`.`StatusId`,`x`.`ExchangeName`,`x`.`RoutingKey`,`x`.`Body`,`x`.`ExpiresAt`,`x`.`Status`,`x`.`DeliverTime` FROM `fmk_publisheds` AS `x` WHERE `x`.`Status`=0 AND (`x`.`ExpiresAt` IS NULL OR `x`.`ExpiresAt`&gt;@__variable_date__) AND `x`.`DeliverTime`&gt;@__variable_takeTime__ AND `x`.`DeliverTime`&lt;=@__variable_offsetTime__
        /// </code>
        /// </remarks>
        [Fact]
        public void TestNullableEqualNull()
        {
            var date = new DateTime(2025, 4, 20);
            var takeTime = date.AddDays(-5D);
            var offsetTime = date.AddSeconds(-5D);

            var allPublisheds = _publisheds.ToList();
            var memoryResults = allPublisheds
                    .Where(x => x.Status == 0 && (null == x.ExpiresAt || x.ExpiresAt.Value > date))
                    .Where(x => x.DeliverTime > takeTime && x.DeliverTime <= offsetTime)
                    .ToList();

            var messages = _publisheds
                    .Where(x => x.Status == 0 && (null == x.ExpiresAt || x.ExpiresAt.Value > date))
                    .Where(x => x.DeliverTime > takeTime && x.DeliverTime <= offsetTime)
                    .ToList();

            Assert.Equal(memoryResults.Count, messages.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, messages[i].Id);
                Assert.Equal(memoryResults[i].Status, messages[i].Status);
                Assert.Equal(memoryResults[i].ExpiresAt, messages[i].ExpiresAt);
                Assert.Equal(memoryResults[i].DeliverTime, messages[i].DeliverTime);
            }

            Assert.Single(messages);
        }

        /// <summary>
        /// 测试可空类型不等于null。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`x`.`StatusId`,`x`.`ExchangeName`,`x`.`RoutingKey`,`x`.`Body`,`x`.`ExpiresAt`,`x`.`Status`,`x`.`DeliverTime` FROM `fmk_publisheds` AS `x` WHERE `x`.`Status`=0 AND `x`.`ExpiresAt` IS NOT NULL AND `x`.`DeliverTime`&gt;@__variable_takeTime__ AND `x`.`DeliverTime`&lt;=@__variable_offsetTime__
        /// </code>
        /// </remarks>
        [Fact]
        public void TestNullableNotEqualNull()
        {
            var date = new DateTime(2025, 4, 20);
            var takeTime = date.AddDays(-5D);
            var offsetTime = date.AddSeconds(-5D);

            var allPublisheds = _publisheds.ToList();
            var memoryResults = allPublisheds
                    .Where(x => x.Status == 0 && null != x.ExpiresAt)
                    .Where(x => x.DeliverTime > takeTime && x.DeliverTime <= offsetTime)
                    .ToList();

            var messages = _publisheds
                    .Where(x => x.Status == 0 && null != x.ExpiresAt)
                    .Where(x => x.DeliverTime > takeTime && x.DeliverTime <= offsetTime)
                    .ToList();

            Assert.Equal(memoryResults.Count, messages.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, messages[i].Id);
                Assert.Equal(memoryResults[i].Status, messages[i].Status);
                Assert.Equal(memoryResults[i].ExpiresAt, messages[i].ExpiresAt);
                Assert.Equal(memoryResults[i].DeliverTime, messages[i].DeliverTime);
            }

            Assert.Empty(messages);
        }

        /// <summary>
        /// 测试SkipWhile可空类型不等于null。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`x`.`StatusId`,`x`.`ExchangeName`,`x`.`RoutingKey`,`x`.`Body`,`x`.`ExpiresAt`,`x`.`Status`,`x`.`DeliverTime` FROM `fmk_publisheds` AS `x` WHERE `x`.`Status`=0 AND `x`.`ExpiresAt` IS NULL AND `x`.`DeliverTime`&gt;@__variable_takeTime__ AND `x`.`DeliverTime`&lt;=@__variable_offsetTime__
        /// </code>
        /// </remarks>
        [Fact]
        public void TestSkipWhileNullableNotEqualNull()
        {
            var date = new DateTime(2025, 4, 20);
            var takeTime = date.AddDays(-5D);
            var offsetTime = date.AddSeconds(-5D);

            var allPublisheds = _publisheds.ToList();
            var memoryMessages = allPublisheds
                    .Where(x => x.Status == 0)
                    .SkipWhile(x => null != x.ExpiresAt)
                    .Where(x => x.DeliverTime > takeTime && x.DeliverTime <= offsetTime)
                    .ToList();

            var messages = _publisheds
                    .Where(x => x.Status == 0)
                    .SkipWhile(x => null != x.ExpiresAt)
                    .Where(x => x.DeliverTime > takeTime && x.DeliverTime <= offsetTime)
                    .ToList();

            Assert.Equal(memoryMessages.Count, messages.Count);

            for (int i = 0; i < memoryMessages.Count; i++)
            {
                Assert.Equal(memoryMessages[i].Id, messages[i].Id);
                Assert.Equal(memoryMessages[i].Status, messages[i].Status);
                Assert.Equal(memoryMessages[i].ExpiresAt, messages[i].ExpiresAt);
                Assert.Equal(memoryMessages[i].DeliverTime, messages[i].DeliverTime);
            }

            Assert.Single(messages);
        }

        /// <summary>
        /// 测试非null字段与null变量比较。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`x`.`StatusId`,`x`.`ExchangeName`,`x`.`RoutingKey`,`x`.`Body`,`x`.`ExpiresAt`,`x`.`Status`,`x`.`DeliverTime` FROM `fmk_publisheds` AS `x` WHERE (`x`.`ExpiresAt` IS NULL OR `x`.`ExpiresAt`&gt;@__variable_date__) AND `x`.`ExchangeName` IS NOT NULL AND `x`.`DeliverTime`&gt;@__variable_takeTime__ AND `x`.`DeliverTime`&lt;=@__variable_offsetTime__
        /// </code>
        /// </remarks>
        [Fact]
        public void TestNonullableEqualNull()
        {
            int? status = null;
            string name = null;
            var date = new DateTime(2025, 4, 20);
            var takeTime = date.AddDays(-5D);
            var offsetTime = date.AddSeconds(-5D);

            var allPublisheds = _publisheds.ToList();
            var memoryMessages = allPublisheds
                    .Where(x => (status is null || x.Status == status) && (null == x.ExpiresAt || x.ExpiresAt > date) && x.ExchangeName != name)
                    .Where(x => x.DeliverTime > takeTime && x.DeliverTime <= offsetTime)
                    .ToList();

            var messages = _publisheds
                    .Where(x => x.Status == status && (null == x.ExpiresAt || x.ExpiresAt > date) && x.ExchangeName != name)
                    .Where(x => x.DeliverTime > takeTime && x.DeliverTime <= offsetTime)
                    .ToList();

            Assert.Equal(memoryMessages.Count, messages.Count);

            for (int i = 0; i < memoryMessages.Count; i++)
            {
                Assert.Equal(memoryMessages[i].Id, messages[i].Id);
                Assert.Equal(memoryMessages[i].Status, messages[i].Status);
                Assert.Equal(memoryMessages[i].ExpiresAt, messages[i].ExpiresAt);
                Assert.Equal(memoryMessages[i].DeliverTime, messages[i].DeliverTime);
                Assert.Equal(memoryMessages[i].ExchangeName, messages[i].ExchangeName);
            }

            Assert.Single(messages);
        }

        /// <summary>
        /// 布尔字段查询。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`IsAdministrator` ORDER BY `x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestBooleanFieldOnly()
        {
            var linq =
                from x in _users
                where x.IsAdministrator
                orderby x.DateAt descending
                select x.Id;

            var allUsers = _users.ToList();
            var memoryResults = (from x in allUsers
                                 where x.IsAdministrator
                                 orderby x.DateAt descending
                                 select x.Id).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较相同id的内容是否相同，不关心顺序
            Assert.Equal(memoryResults.OrderBy(x => x), results.OrderBy(x => x));
        }

        /// <summary>
        /// 布尔字段与条件组合查询。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`IsAdministrator` ORDER BY `x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestBooleanField()
        {
            var linq =
                from x in _users
                where x.Id == 100 && x.IsAdministrator
                orderby x.DateAt descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 可空布尔字段Value查询。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Nullable` ORDER BY `x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestBooleanNullableFieldOnly()
        {
            var linq =
                from x in _users
                where x.Nullable.Value
                orderby x.DateAt descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 可空布尔字段HasValue查询。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Nullable` IS NOT NULL ORDER BY `x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestBooleanNullableHasValueFieldOnly()
        {
            var linq =
                from x in _users
                where x.Nullable.HasValue
                orderby x.DateAt descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 可空布尔字段Value与条件组合查询。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`Nullable` ORDER BY `x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestBooleanNullableField()
        {
            var linq =
                from x in _users
                where x.Id == 100 && x.Nullable.Value
                orderby x.DateAt descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 可空布尔字段HasValue与条件组合查询。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`Nullable` IS NOT NULL ORDER BY `x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestBooleanNullableHasValueField()
        {
            var linq =
                from x in _users
                where x.Id == 100 && x.Nullable.HasValue
                orderby x.DateAt descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 可空布尔字段合并运算查询。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND COALESCE(`x`.`Nullable`,`x`.`IsAdministrator`) ORDER BY `x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestBooleanNullableCoalesceField()
        {
            var linq =
                from x in _users
                where x.Id == 100 && (x.Nullable ?? x.IsAdministrator)
                orderby x.DateAt descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 可空布尔字段异或运算查询。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND `x`.`Nullable`^`x`.`IsAdministrator` ORDER BY `x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestBooleanNullableExclusiveOrField()
        {
            var linq =
                from x in _users
                where x.Id == 100 && (x.Nullable.Value ^ x.IsAdministrator)
                orderby x.DateAt descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 可空布尔字段HasValue异或运算查询。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND (`x`.`Nullable` IS NOT NULL)^`x`.`IsAdministrator` ORDER BY `x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestBooleanNullableHasValueExclusiveOrField()
        {
            var linq =
                from x in _users
                where x.Id == 100 && (x.Nullable.HasValue ^ x.IsAdministrator)
                orderby x.DateAt descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 可空布尔字段HasValue与运算查询。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 AND (`x`.`Nullable` IS NOT NULL)&amp;`x`.`IsAdministrator` ORDER BY `x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void TestBooleanNullableHasValueAndField()
        {
            var linq =
                from x in _users
                where x.Id == 100 && (x.Nullable.HasValue & x.IsAdministrator)
                orderby x.DateAt descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试ToString方法。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`x`.`Account`,`x`.`Bcid`,`x`.`Name`,`x`.`Tel`,`x`.`Mail`,`x`.`Avatar`,`x`.`IsAdministrator`,`x`.`Sex`,`x`.`DateAt`,`x`.`Nullable` FROM `fmk_users` AS `x` WHERE `x`.`Name`='100' ORDER BY `x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void ToStringTest()
        {
            var id = 100;

            var linq =
                from x in _users
                where x.Name == id.ToString()
                orderby x.DateAt descending
                select x;

            var allUsers = _users.ToList();
            var memoryResults = (from x in allUsers
                                 where x.Name == id.ToString()
                                 orderby x.DateAt descending
                                 select x).ToList();

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            // 比较结果是否相同
            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
                Assert.Equal(memoryResults[i].DateAt, results[i].DateAt);
                Assert.Equal(memoryResults[i].IsAdministrator, results[i].IsAdministrator);
                Assert.Equal(memoryResults[i].Nullable, results[i].Nullable);
            }
        }

        /// <summary>
        /// 可空类型投影查询。
        /// </summary>
        /// <remarks>
        /// <b>SQL预览</b>：
        /// <code>
        /// SELECT `x`.`Id`,`x`.`Name`,`x`.`DateAt`,`x`.`IsAdministrator`,COALESCE(`x`.`Nullable`,0) AS `Nullable` FROM `fmk_users` AS `x` WHERE `x`.`Id`=100 ORDER BY `x`.`DateAt` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void NullableSelect()
        {
            var id = 100;
            var allUsers = _users.ToList();

            var memoryResults = (from x in allUsers
                                 where x.Id == id
                                 orderby x.DateAt descending
                                 select new User
                                 {
                                     Id = x.Id,
                                     Name = x.Name,
                                     DateAt = x.DateAt,
                                     IsAdministrator = x.IsAdministrator,
                                     Nullable = x.Nullable ?? false
                                 }).ToList();

            var linq =
                from x in _users
                where x.Id == id
                orderby x.DateAt descending
                select new User
                {
                    Id = x.Id,
                    Name = x.Name,
                    DateAt = x.DateAt,
                    IsAdministrator = x.IsAdministrator,
                    Nullable = x.Nullable ?? false
                };

            var results = linq.ToList();

            Assert.Equal(memoryResults.Count, results.Count);

            for (int i = 0; i < memoryResults.Count; i++)
            {
                Assert.Equal(memoryResults[i].Id, results[i].Id);
                Assert.Equal(memoryResults[i].Name, results[i].Name);
                Assert.Equal(memoryResults[i].DateAt, results[i].DateAt);
                Assert.Equal(memoryResults[i].IsAdministrator, results[i].IsAdministrator);
                Assert.Equal(memoryResults[i].Nullable, results[i].Nullable);
            }
        }
    }
}
