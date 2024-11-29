using Inkslab.Linq.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public QuerybleTests(IQueryable<User> users, IQueryable<UserEx> userExes)
        {
            _users = users;
            _userExes = userExes;
        }

        [Fact]
        public void TestSimple()
        {
            var linq = from x in _users
                       where x.Id == 100
                       orderby x.DateAt, x.Name
                       select x.Id;

            var results = linq.ToList();
        }

        [Fact]
        public void TestGroupBySimple()
        {
            var linq = from x in _users
                       where x.Id == 100
                       group x by x.Id
                        into g
                       orderby g.Key descending
                       select new { g.Key };

            var results = linq.ToList();
        }

        [Fact]
        public void TestGroupByElement()
        {
            var now = DateTime.Now;

            var linq = from x in _users
                       where x.Id == 100
                       group new { x.Name, x.DateAt } by x.Id
                        into g
                       orderby g.Key descending
                       select new { g.Key, Total = g.Count(), Count = g.Where(x => x.DateAt > now).Count() };

            var results = linq.ToList();
        }

        [Fact]
        public void TestGroupByToMax()
        {
            var now = DateTime.Now;

            var linq = from x in _users
                       where x.Id == 100
                       group x by x.Id
                        into g
                       orderby g.Key descending
                       select new { g.Key, Name = g.Max(y => y.Name), Total = g.Count() };

            var results = linq.ToList();
        }

        [Fact]
        public void TestJoinGroupByToMax()
        {
            var now = DateTime.Now;

            var linq = from x in _users
                       join y in _userExes
                       on x.Id equals y.Id
                       where x.Id == 100
                       group x by x.Id
                        into g
                       orderby g.Key descending
                       select new { g.Key, Name = g.Max(y => y.Name), Total = g.Count() };

            var results = linq.ToList();
        }

        [Fact]
        public void TestJoinGroupByTakeSkipToMax()
        {
            var now = DateTime.Now;

            var linq = from x in _users
                       join y in _userExes
                       on x.Id equals y.Id
                       where x.Id == 100
                       group x by x.Id
                        into g
                       orderby g.Key descending
                       select new { g.Key, Name = g.Max(y => y.Name), Total = g.Count() };

            var results = linq.Skip(1).Take(1).ToList();
        }

        [Fact]
        public void TestGroupByHavingElement()
        {
            var now = DateTime.Now;

            var linq = from x in _users
                       where x.Id == 100
                       group new { x.Name, x.DateAt } by x.Id into g
                       where g.Count() > 1
                       orderby g.Key descending
                       select g.Key;

            var results = linq.ToList();
        }

        [Fact]
        public void TestGroupByHavingElementCount()
        {
            var now = DateTime.Now;

            var linq = from x in _users
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
                           DistinctCount = g.Where(x => x.DateAt > now).Select(x => x.Name).Distinct().Count()
                       };

            var results = linq.ToList();
        }

        [Fact]
        public void TestGroupByHavingMoreElementCount()
        {
            var now = DateTime.Now;

            var linq = from x in _users
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
                           DistinctCount = g.Where(x => x.DateAt > now).Select(x => x.Name).Distinct().Count()
                       };

            var results = linq.ToList();
        }

        [Fact]
        public void TestGroupByHavingMoreKey()
        {
            var now = DateTime.Now;

            var linq = from x in _users
                       where x.Id == 100
                       group new { x.Name, x.DateAt } by new { x.Id, x.Name } into g
                       where g.Count() > 1
                       orderby g.Key descending
                       select g.Key;

            var results = linq.ToList();
        }

        [Fact]
        public void TestGroupByHavingElemenMax()
        {
            var now = DateTime.Now;

            var linq = from x in _users
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

        [Fact]
        public void TestJoinSimple()
        {
            var linq = from x in _users
                       join y in _userExes
                       on x.Id equals y.Id
                       orderby x.Id descending
                       select new { x.Id, y.RoleType };

            var results = linq.ToList();
        }

        [Fact]
        public void TestJoinWhere()
        {
            var linq = from x in _users.Where(y => y.DateAt > DateTime.UnixEpoch)
                       join y in _userExes.Where(x => x.Age > 18)
                       on x.Id equals y.Id
                       orderby x.Id descending
                       select new UserDto { Id = x.Id, Role = y.RoleType, Name = x.Name };

            var results = linq.ToList();
        }

        [Fact]
        public void TestLeftJoinWhere()
        {
            var linq = from x in _users//.Where(y => y.DateAt > DateTime.UnixEpoch)
                       join y in _userExes.Where(x => x.Age > 18)
                       on x.Id equals y.Id
                       into xy
                       from c in xy.DefaultIfEmpty()
                       orderby x.Id descending
                       select new UserDto { Id = x.Id, Role = c.RoleType, Name = x.Name };

            var results = linq.ToList();
        }

        [Fact]
        public void TestJoinSelectMany()
        {
            var linq = from x in _users
                       join y in _userExes
                       on x.Id equals y.Id into g
                       from z in g
                       orderby x.Id descending
                       select new { x.Id, z.RoleType };

            var results = linq.ToList();
        }

        [Fact]
        public void TestLeftJoinSelectMany()
        {
            var linq = from x in _users
                       join y in _userExes
                       on x.Id equals y.Id into g
                       from z in g.DefaultIfEmpty()
                       orderby x.Id descending
                       select new { x.Id, z.RoleType };

            var results = linq.ToList();
        }

        [Fact]
        public void TestLeftJoinSelectElementNotNullMany()
        {
            var linq = from x in _users
                       join y in _userExes
                       on x.Id equals y.Id
                       into g
                       from t in g.DefaultIfEmpty()
                       join z in _userExes on x.Id equals z.Id + 1
                       into g2
                       from t2 in g.DefaultIfEmpty()
                       where t == null
                       orderby x.Id descending
                       select new { x.Id, t2.RoleType, IsValid = t2 != null };

            var results = linq.ToList();
        }

        [Fact]
        public void TestCrossJoinSelectMany()
        {
            var linq = from x in _users
                       from y in _userExes
                       orderby x.Id descending
                       select new { x.Id, y.RoleType };

            var results = linq.ToList();
        }

        [Fact]
        public void TestCrossJoinWhereElementNullSelectMany()
        {
            var linq = from x in _users
                       from y in _userExes
                       orderby x.Id descending
                       where x == null || y == null
                       select new { x.Id, y.RoleType };

            var results = linq.ToList();
        }

        [Fact]
        public void TestJoinSelfSimple()
        {
            var linq = from x in _users
                       join y in _users
                       on x.Id equals y.Id + 1
                       orderby x.Id descending
                       select new { x.Id, y.Name };

            var results = linq.ToList();
        }

        [Fact]
        public void TestJoinSelfSelectMany()
        {
            var linq = from x in _users
                       join y in _users
                       on x.Id equals y.Id + 1 into g
                       from z in g
                       orderby x.Id descending
                       select new { x.Id, z.Name };

            var results = linq.ToList();
        }

        [Fact]
        public void TestLeftJoinSelfSelectMany()
        {
            var linq = from x in _users
                       join y in _users
                       on x.Id equals y.Id + 1 into g
                       from z in g.DefaultIfEmpty()
                       orderby x.Id descending
                       select new { x.Id, z.Name };

            var results = linq.ToList();
        }

        [Fact]
        public void TestCrossJoinSelfSelectMany()
        {
            var linq = from x in _users
                       from y in _users
                       orderby x.Id descending
                       select new { x.Id, x.Name, RelationId = y.Id, RelationName = y.Name };

            var results = linq.ToList();
        }

        [Fact]
        public void SimpleCount()
        {
            var linq = from x in _users
                       where x.Id == 100
                       orderby x.DateAt, x.Name
                       select x;

            var count = linq.Count();
        }

        [Fact]
        public void WhereCount()
        {
            var linq = from x in _users
                       where x.Id == 100
                       select x;

            var count = linq.Count(x => x.Name.StartsWith("何"));
        }

        [Fact]
        public void SelectCount()
        {
            var linq = from x in _users
                       where x.Id == 100
                       select x.Id;

            var count = linq.Count();
        }

        [Fact]
        public void SelectMultiCount()
        {
            var linq = from x in _users
                       where x.Id == 100
                       select new { x.Id, x.Name };

            var count = linq.Count();//? 生成 SELECT COUNT(*) FROM `user` AS `x` WHERE `x`.`id` = 100
        }

        [Fact]
        public void DistinctSelectMultiCount()
        {
            var linq = from x in _users
                       where x.Id == 100
                       select new { x.Id, x.Name };

            var count = linq.Distinct().Count();//? 生成 SELECT COUNT(DISTINCT `x`.`id`, `x`.`name`) FROM `user` AS `x` WHERE `x`.`id` = 100
        }

        [Fact]
        public void DistinctSelectCount()
        {
            var linq = from x in _users
                       where x.Id == 100
                       select x.Name;

            var count = linq.Distinct().Count();
        }

        [Fact]
        public void GroupBySelectCount()
        {
            var linq = from x in _users
                       group x by x.Name
                       into g
                       where g.Count() > 1
                       select g.Key;

            var count = linq.Count();
        }

        [Fact]
        public void GroupByDistinctCount()
        {
            var linq = from x in _users
                       group x by new { x.Name, x.DateAt }
                       into g
                       where g.Count() > 1
                       select g.Key;

            var count = linq.Select(x => x.Name).Distinct().Count();
        }

        [Fact]
        public void SimpleMax()
        {
            var linq = from x in _users
                       where x.Id == 100
                       orderby x.DateAt, x.Name
                       select x;

            // 异常：1、需指定聚合字段。2、不支持排序。
            Assert.Throws<DSyntaxErrorException>(() => linq.Max());
        }

        [Fact]
        public void SelectMax()
        {
            var linq = from x in _users
                       where x.Id == 100
                       select x.Id;

            var max = linq.Max();
        }

        [Fact]
        public void DistinctSelectMax()
        {
            var linq = from x in _users
                       where x.Id == 100
                       select x.Name;

            var max = linq.Distinct().Max();
        }

        [Fact]
        public void GroupBySelectMax()
        {
            var linq = from x in _users
                       group x by x.Name
                       into g
                       where g.Count() > 1
                       select g.Key;

            var max = linq.Max();
        }

        [Fact]
        public void GroupByDistinctMax()
        {
            var linq = from x in _users
                       group x by x.Name
                       into g
                       where g.Count() > 1
                       select g.Key;

            var max = linq.Distinct().Max();
        }

        [Fact]
        public void GroupByCondition()
        {
            var linq = from x in _users
                       group x by x.Name
                       into g
                       where L2S.Condition(g, x => x.Count() > 1)
                       select g.Key;

            var max = linq.Distinct().Max();
        }

        [Fact]
        public void Union()
        {
            var linq = from x in _users
                       join y in _userExes
                       on x.Id equals y.Id
                       select new { x.Id, y.RoleType, Type = 1 };

            var linq2 = from x in _users
                        join y in _userExes
                        on x.Id equals y.Id into g
                        from z in g.DefaultIfEmpty()
                        select new { x.Id, z.RoleType, Type = 2 };

            var results = linq.Union(linq2).ToList();
        }

        [Fact]
        public void ConcatSelectDistinct()
        {
            var linq = from x in _users
                       join y in _userExes
                       on x.Id equals y.Id
                       select new { x.Id, y.RoleType, Type = 1 };

            var linq2 = from x in _users
                        join y in _userExes
                        on x.Id equals y.Id into g
                        from z in g.DefaultIfEmpty()
                        select new { x.Id, z.RoleType, Type = 2 };

            var results = linq.Concat(linq2)
                .Select(x => string.Concat(x.RoleType, x.Type))
                .Distinct()
                .ToList();
        }

        [Fact] //? MySQL 不支持 Except 语法。
        public void UnionWhere()
        {
            var linq = from x in _users
                       join y in _userExes
                       on x.Id equals y.Id
                       select new { x.Id, y.RoleType, Type = 1 };

            var linq2 = from x in _users
                        join y in _userExes
                        on x.Id equals y.Id into g
                        from z in g.DefaultIfEmpty()
                        select new { x.Id, z.RoleType, Type = 2 };

            var results = linq.Union(linq2)
                .Where(x => x.RoleType == 2)
                .Select(x => string.Concat(x.RoleType, x.Type))
                .Distinct()
                .ToList();
        }

        [Fact]
        public void UnionNestedTake()
        {
            var linq = from x in _users
                       join y in _userExes
                       on x.Id equals y.Id
                       orderby x.Id descending
                       select new { x.Id, y.RoleType, Type = 1 };

            var linq2 = from x in _users
                        join y in _userExes
                        on x.Id equals y.Id into g
                        from z in g.DefaultIfEmpty()
                        orderby x.Id descending
                        select new { x.Id, z.RoleType, Type = 2 };

            var results = linq.Take(10).Union(linq2.Take(10)).ToList();
        }

        [Fact]
        public void UnionTake()
        {
            var linq = from x in _users
                       join y in _userExes
                       on x.Id equals y.Id
                       orderby x.Id descending
                       select new { x.Id, y.RoleType, Type = 1 };

            var linq2 = from x in _users
                        join y in _userExes
                        on x.Id equals y.Id into g
                        from z in g.DefaultIfEmpty()
                        orderby x.Id descending
                        select new { x.Id, z.RoleType, Type = 2 };

            var results = linq.Take(10).Union(linq2.Take(10))
                .OrderBy(x => x.Id)
                .Take(10)
                .ToList();
        }

        /// <summary>
        /// 任意测试。
        /// </summary>
        [Fact]
        public void TestAny()
        {
            var linq = from x in _users
                       where x.Id == 100
                       select x.Id;

            bool any = linq.Any();
        }

        /// <summary>
        /// 全部测试。
        /// </summary>
        [Fact]
        public void TestAll()
        {
            bool all = _users.All(x => x.Id < 100);
        }

        /// <summary>
        /// 内置任意满足测试。
        /// </summary>
        [Fact]
        public void TestNestedAny()
        {
            var linq = from x in _users
                       where x.Id == 100 && _userExes.Any(y => y.Id == x.Id && y.Age > 12)
                       orderby x.DateAt, x.Name
                       select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 内置所有满足测试。
        /// </summary>
        [Fact]
        public void TestNestedAll()
        {
            var linq = from x in _users
                       where x.Id == 100 && _userExes.All(y => y.Id == x.Id && y.Age > 12)
                       orderby x.DateAt, x.Name
                       select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 内置包含测试。
        /// </summary>
        [Fact]
        public void TestNestedContains()
        {
            var linq = from x in _users
                       where x.Id == 100 && _userExes.Where(y => y.Age > 12).Select(y => y.Id).Contains(x.Id)
                       orderby x.DateAt, x.Name
                       select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 内置包含测试。
        /// </summary>
        [Fact]
        public void TestNestedMemoryContains()
        {
            var ids = new List<long> { 1, 2, 5 };

            var linq = from x in _users
                       where x.Id == 100 && ids.Contains(x.Id)
                       orderby x.DateAt, x.Name
                       select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 内置包含测试。
        /// </summary>
        [Fact]
        public void TestNestedMemoryAny()
        {
            int length = 50;

            var users = new List<User>(length);

            for (int i = 0; i < length; i++)
            {
                users.Add(new User { Id = i + 256, Name = $"测试：{i:000}", DateAt = DateTime.Now.AddMinutes(i) });
            }

            var linq = from x in _users
                       where x.Id == 100 && users.Any(y => x.Id == y.Id)
                       orderby x.DateAt, x.Name
                       select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 同表多个别名。
        /// </summary>
        [Fact]
        public void TestLambdaMultiAliasName()
        {
            var user = _users.Where(x => x.Id > 100).OrderByDescending(z => z.DateAt).FirstOrDefault(y => y.Id < 1000);
        }
    }
}
