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

        [Fact]
        public void TestSimple()
        {
            var linq = from x in _users where x.Id == 100 orderby x.DateAt, x.Name select x.Id;

            var results = linq.ToList();
        }

        [Fact]
        public void TestShardingSimple()
        {
            var linq =
                from x in _userShardings.DataSharding("2025")
                where x.Id == 100
                orderby x.DateAt, x.Name
                select x.Id;

            var results = linq.ToList();
        }

        [Fact]
        public void TestGroupBySimple()
        {
            var linq =
                from x in _users
                where x.Id == 100
                group x by x.Id into g
                orderby g.Key descending
                select new { g.Key };

            var results = linq.ToList();
        }

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

            var results = linq.ToList();
        }

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

            var results = linq.ToList();
        }

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

            var results = linq.ToList();
        }

        [Fact]
        public void TestShardingJoinGroupByToMax()
        {
            var now = DateTime.Now;

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
        }

        [Fact]
        public void TestJoinGroupByTakeSkipToMax()
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

            var results = linq.Skip(1).Take(1).ToList();
        }

        [Fact]
        public void TestGroupByHavingElement()
        {
            var now = DateTime.Now;

            var linq =
                from x in _users
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
        }

        [Fact]
        public void TestGroupByHavingMoreElementCount()
        {
            var now = DateTime.Now;

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
        }

        [Fact]
        public void TestGroupByHavingMoreKey()
        {
            var now = DateTime.Now;

            var linq =
                from x in _users
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

        [Fact]
        public void TestJoinSimple()
        {
            var linq =
                from x in _users
                join y in _userExes on x.Id equals y.Id
                orderby x.Id descending
                select new { x.Id, y.RoleType };

            var results = linq.ToList();
        }

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

            var results = linq.ToList();
        }

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

            var results = linq.ToList();
        }

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

            var results = linq.ToList();
        }

        [Fact]
        public void TestShardingLeftJoinWhere()
        {
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
        }

        [Fact]
        public void TestJoinSelectMany()
        {
            var linq =
                from x in _users
                join y in _userExes on x.Id equals y.Id into g
                from z in g
                orderby x.Id descending
                select new { x.Id, z.RoleType };

            var results = linq.ToList();
        }

        [Fact]
        public void TestShardingJoinSelectMany()
        {
            var linq =
                from x in _userShardings.DataSharding("2025")
                join y in _userExes on x.Id equals y.Id into g
                from z in g
                orderby x.Id descending
                select new { x.Id, z.RoleType };

            var results = linq.ToList();
        }

        [Fact]
        public void TestLeftJoinSelectMany()
        {
            var linq =
                from x in _users
                join y in _userExes on x.Id equals y.Id into g
                from z in g.DefaultIfEmpty()
                orderby x.Id descending
                select new { x.Id, z.RoleType };

            var results = linq.ToList();
        }

        [Fact]
        public void TestLeftJoinSelectElementNotNullMany()
        {
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
        }

        [Fact]
        public void TestCrossJoinSelectMany()
        {
            var linq =
                from x in _users
                from y in _userExes
                where x.Id < 10 && y.Id < 10
                orderby x.Id descending
                select new { x.Id, y.RoleType };

            var results = linq.ToList();
        }

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

            var results = linq.ToList();
        }

        [Fact]
        public void TestJoinSelfSimple()
        {
            var linq =
                from x in _users
                join y in _users on x.Id equals y.Id + 1
                orderby x.Id descending
                select new { x.Id, y.Name };

            var results = linq.ToList();
        }

        [Fact]
        public void TestJoinSelfSelectMany()
        {
            var linq =
                from x in _users
                join y in _users on x.Id equals y.Id + 1 into g
                from z in g
                orderby x.Id descending
                select new { x.Id, z.Name };

            var results = linq.ToList();
        }

        [Fact]
        public void TestLeftJoinSelfSelectMany()
        {
            var linq =
                from x in _users
                join y in _users on x.Id equals y.Id + 1 into g
                from z in g.DefaultIfEmpty()
                orderby x.Id descending
                select new { x.Id, z.Name };

            var results = linq.ToList();
        }

        [Fact]
        public void TestShardingLeftJoinSelfSelectMany()
        {
            var linq =
                from x in _users
                join y in _userShardings.DataSharding("2025") on x.Id equals y.Id + 1 into g
                from z in g.DefaultIfEmpty()
                orderby x.Id descending
                select new { x.Id, z.Name };

            var results = linq.ToList();
        }

        [Fact]
        public void TestCrossJoinSelfSelectMany()
        {
            var linq =
                from x in _users
                from y in _users
                orderby x.Id descending
                select new
                {
                    x.Id,
                    x.Name,
                    RelationId = y.Id,
                    RelationName = y.Name
                };

            var results = linq.ToList();
        }

        [Fact]
        public void TestShardingCrossJoinSelfSelectMany()
        {
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

            var results = linq.ToList();
        }

        [Fact]
        public void SimpleCount()
        {
            var linq = from x in _users where x.Id == 100 orderby x.DateAt, x.Name select x;

            var count = linq.Count();
        }

        [Fact]
        public void WhereCount()
        {
            var linq = from x in _users where x.Id == 100 select x;

            var count = linq.Count(x => x.Name.StartsWith("测试"));
        }

        [Fact]
        public void SelectCount()
        {
            var linq = from x in _users where x.Id == 100 select x.Id;

            var count = linq.Count();
        }

        [Fact]
        public void SelectMultiCount()
        {
            var linq = from x in _users where x.Id == 100 select new { x.Id, x.Name };

            var count = linq.Count(); //? 生成 SELECT COUNT(*) FROM `user` AS `x` WHERE `x`.`id` = 100
        }

        [Fact]
        public void DistinctSelectMultiCount()
        {
            var linq = from x in _users where x.Id == 100 select new { x.Id, x.Name };

            var count = linq.Distinct().Count(); //? 生成 SELECT COUNT(DISTINCT `x`.`id`, `x`.`name`) FROM `user` AS `x` WHERE `x`.`id` = 100
        }

        [Fact]
        public void DistinctSelectCount()
        {
            var linq = from x in _users where x.Id == 100 select x.Name;

            var count = linq.Distinct().Count();
        }

        [Fact]
        public void GroupBySelectCount()
        {
            var linq = from x in _users group x by x.Name into g where g.Count() > 1 select g.Key;

            var count = linq.Count();
        }

        [Fact]
        public void GroupByDistinctCount()
        {
            var linq =
                from x in _users
                group x by new { x.Name, x.DateAt } into g
                where g.Count() > 1
                select g.Key;

            var count = linq.Select(x => x.Name).Distinct().Count();
        }

        [Fact]
        public void SimpleMax()
        {
            var linq = from x in _users where x.Id == 100 orderby x.DateAt, x.Name select x;

            // 异常：1、需指定聚合字段。2、不支持排序。
            Assert.Throws<DSyntaxErrorException>(() => linq.Max());
        }

        [Fact]
        public void SelectMax()
        {
            var linq = from x in _users where x.Id == 100 select x.Id;

            var max = linq.Max();
        }

        [Fact]
        public void DistinctSelectMax()
        {
            var linq = from x in _users where x.Id == 100 select x.Name;

            var max = linq.Distinct().Max();
        }

        [Fact]
        public void GroupBySelectMax()
        {
            var linq = from x in _users group x by x.Name into g where g.Count() > 1 select g.Key;

            var max = linq.Max();
        }

        [Fact]
        public void GroupByDistinctMax()
        {
            var linq = from x in _users group x by x.Name into g where g.Count() > 1 select g.Key;

            var max = linq.Distinct().Max();
        }

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

            var results = linq.Union(linq2).ToList();
        }

        [Fact]
        public void ConcatSelectDistinct()
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

            var results = linq.Concat(linq2)
                .Select(x => string.Concat(x.RoleType, x.Type))
                .Distinct()
                .ToList();
        }

        [Fact] //? MySQL 不支持 Except 语法。
        public void UnionWhere()
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

            var results = linq.Union(linq2)
                .Where(x => x.RoleType == 2)
                .Select(x => string.Concat(x.RoleType, x.Type))
                .Distinct()
                .ToList();
        }

        [Fact]
        public void UnionNestedTake()
        {
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
        }

        [Fact]
        public void UnionTake()
        {
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
        }

        /// <summary>
        /// 任意测试。
        /// </summary>
        [Fact]
        public void TestAny()
        {
            var linq = from x in _users where x.Id == 100 select x.Id;

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
            var linq =
                from x in _users
                where x.Id == 100 && _userExes.Any(y => y.Id == x.Id && y.Age > 12)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 内置任意满足测试。
        /// </summary>
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
        /// 内置任意满足测试。
        /// </summary>
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
        [Fact]
        public void TestNestedAll()
        {
            var linq =
                from x in _users
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
            var linq =
                from x in _users
                where
                    x.Id == 100 && _userExes.Where(y => y.Age > 12).Select(y => y.Id).Contains(x.Id)
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

            var linq =
                from x in _users
                where x.Id == 100 && ids.Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 内置包含测试。
        /// </summary>
        [Fact]
        public void TestNestedMemoryHashContains()
        {
            var ids = new HashSet<long> { 1, 2, 5 };

            var linq =
                from x in _users
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

            var results = linq.ToList();
        }

        /// <summary>
        /// 同表多个别名。
        /// </summary>
        [Fact]
        public void TestLambdaMultiAliasName()
        {
            var user = _users
                .Where(x => x.Id > 100)
                .OrderByDescending(z => z.DateAt)
                .FirstOrDefault(y => y.Id < 1000);
        }

        /// <summary>
        /// 内置包含测试。
        /// </summary>
        [Fact]
        public void TestNestedMemoryContainsMultiSelect()
        {
            var ids = new List<long> { 1, 2, 5 };

            var linq =
                from x in _users
                where x.Id == 100 && ids.Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = linq.ToList();
            var results2 = linq.ToList();
        }

        /// <summary>
        /// 内置包含测试。
        /// </summary>
        [Fact]
        public void TestNestedMemoryContainsMultiSelect2()
        {
            var ids = new List<long> { 1, 2, 5 };

            var linq =
                from x in _users
                where x.Id == 100 && ids.Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = linq.ToList();
            var results2 = linq.Count();
        }

        /// <summary>
        /// 内置包含测试。
        /// </summary>
        [Fact]
        public async Task TestNestedMemoryContainsMultiSelectAsync()
        {
            var ids = new List<long> { 1, 2, 5 };

            var linq =
                from x in _users
                where x.Id == 100 && ids.Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = await linq.ToListAsync();
            var results2 = await linq.ToListAsync();
        }

        /// <summary>
        /// 内置包含测试。
        /// </summary>
        [Fact]
        public async Task TestNestedMemoryContainsMultiSelect2Async()
        {
            var ids = new List<long> { 1, 2, 5 };

            var linq =
                from x in _users
                where x.Id == 100 && ids.Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            await Task.WhenAll(linq.ToListAsync(), linq.ToListAsync());
        }

        /// <summary>
        /// 内置包含测试。
        /// </summary>
        [Fact]
        public async Task TestNestedMemoryContainsMultiSelect3Async()
        {
            var ids = new List<long> { 1, 2, 5 };

            var linq =
                from x in _users
                where x.Id == 100 && ids.Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            await Task.WhenAll(linq.ToListAsync(), linq.CountAsync());
        }

        /// <summary>
        /// 内置包含测试。
        /// </summary>
        [Fact]
        public async Task TestNestedMemoryContainsMultiSelect4Async()
        {
            var ids = new List<long> { 1, 2, 5 };

            var linq =
                from x in _users
                where x.Id == 100 && ids.Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = await linq.ToListAsync();
            var results2 = linq.Count();
        }

        /// <summary>
        /// 内置包含测试。
        /// </summary>
        [Fact]
        public async Task TestNestedMemoryContainsMultiSelect5Async()
        {
            var ids = new List<long> { 1, 2, 5 };

            var linq =
                from x in _users
                where x.Id == 100 && ids.Contains(x.Id)
                orderby x.DateAt, x.Name
                select x.Id;

            var results = linq.Count();
            var results2 = await linq.CountAsync();
        }

        /// <summary>
        /// 获取所有数据测试。
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetAllTestAsync()
        {
            await _users.ToListAsync();
        }

        /// <summary>
        /// 获取所有数据条数测试。
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task GetCountAllTestAsync()
        {
            await _users.CountAsync();
        }

        /// <summary>
        /// 测试自定义排序。
        /// </summary>
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
        /// 测试自定义排序。
        /// </summary>
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
        /// 测试自定义多混合排序。
        /// </summary>
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
        /// 测试自定义排序倒序。
        /// </summary>
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
        /// 测试自定义排序倒序。
        /// </summary>
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
        [Fact]
        public void TestOrderByConditional()
        {
            var linq =
                from x in _userExes
                where x.Id == 100
                orderby x.Id > 100 ? x.Age : x.RoleType, x.DateAt descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试三目运算排序倒序。
        /// </summary>
        [Fact]
        public void TestOrderByDescendingConditional()
        {
            var linq =
                from x in _userExes
                where x.Id == 100
                orderby x.Id > 100 ? x.Age : x.RoleType descending, x.DateAt descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试合并运算排序。
        /// </summary>
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
        [Fact]
        public void TestOrderByDescendingCoalesce()
        {
            var linq =
                from x in _users
                where x.Id == 100
                orderby x.Name ?? string.Empty descending, x.DateAt descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试二元运算排序倒序。
        /// </summary>
        [Fact]
        public void TestOrderByBinary()
        {
            var linq =
                from x in _userExes
                where x.Id == 100
                orderby x.Age + x.RoleType, x.DateAt descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试一元运算排序倒序。
        /// </summary>
        [Fact]
        public void TestOrderByUnary()
        {
            var linq =
                from x in _userExes
                where x.Id == 100
                orderby ~x.Age, x.DateAt descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试字符串方法排序倒序。
        /// </summary>
        [Fact]
        public void TestOrderByStringMethod()
        {
            var linq =
                from x in _users
                where x.Id == 100
                orderby x.Name.Substring(2, 5), x.DateAt descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试字符串截取。
        /// </summary>
        [Fact]
        public void TestSubstring()
        {
            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt descending
                select new { x.Id, x.Name, NameSubstring = x.Name.Substring(2, 5) };

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试字符串空判断。
        /// </summary>
        [Fact]
        public void TestIsNullOrEmpty()
        {
            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt descending
                select new { x.Id, x.Name, Null = string.IsNullOrEmpty(x.Name) };

            var results = linq.ToList();
        }

        /// <summary>
        /// 常量字符串空判断。
        /// </summary>
        [Fact]
        public void TestIsNullOrEmptyPlainVariable()
        {
            var name = "测试";

            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt descending
                select new { x.Id, x.Name, Null = string.IsNullOrEmpty(name) };

            var results = linq.ToList();
        }

        /// <summary>
        /// 常量字符串空判断。
        /// </summary>
        [Fact]
        public void TestIsNullOrEmptyByEmpty()
        {
            string name = string.Empty;

            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt descending
                select new { x.Id, x.Name, Null = string.IsNullOrEmpty(name) };

            var results = linq.ToList();
        }

        /// <summary>
        /// 常量字符串空判断。
        /// </summary>
        [Fact]
        public void TestIsNullOrEmptyByNull()
        {
            string name = null;

            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt descending
                select new { x.Id, x.Name, Null = string.IsNullOrEmpty(name) };

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试字符串替换。
        /// </summary>
        [Fact]
        public void TestReplaceString()
        {
            var linq =
                from x in _users
                where x.Id == 100
                orderby x.DateAt descending
                select new { x.Id, x.Name, NickName = x.Name.Replace("测试", "测试2") };

            var results = linq.ToList();
        }

        /// <summary>
        /// 测试字符串截取。
        /// </summary>
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

        [Fact]
        public void TestNullableEqualNull()
        {
            var date = new DateTime(2025, 4, 20);
            var takeTime = date.AddDays(-5D);
            var offsetTime = date.AddSeconds(-5D);

            var messages = _publisheds
                    .Where(x => x.Status == 0 && (null == x.ExpiresAt || x.ExpiresAt.Value > date))
                    .Where(x => x.DeliverTime > takeTime && x.DeliverTime <= offsetTime)
                    .ToList();

            Assert.Single(messages);
        }

        [Fact]
        public void TestNullableNotEqualNull()
        {
            var date = new DateTime(2025, 4, 20);
            var takeTime = date.AddDays(-5D);
            var offsetTime = date.AddSeconds(-5D);

            var messages = _publisheds
                    .Where(x => x.Status == 0 && null != x.ExpiresAt)
                    .Where(x => x.DeliverTime > takeTime && x.DeliverTime <= offsetTime)
                    .ToList();

            Assert.Empty(messages);
        }

        [Fact]
        public void TestSkipWhileNullableNotEqualNull()
        {
            var date = new DateTime(2025, 4, 20);
            var takeTime = date.AddDays(-5D);
            var offsetTime = date.AddSeconds(-5D);

            var messages = _publisheds
                    .Where(x => x.Status == 0)
                    .SkipWhile(x => null != x.ExpiresAt)
                    .Where(x => x.DeliverTime > takeTime && x.DeliverTime <= offsetTime)
                    .ToList();

            Assert.Single(messages);
        }

        [Fact]
        public void TestNonullableEqualNull()
        {
            int? status = null;
            string name = null;
            var date = new DateTime(2025, 4, 20);
            var takeTime = date.AddDays(-5D);
            var offsetTime = date.AddSeconds(-5D);

            var messages = _publisheds
                    .Where(x => x.Status == status && (null == x.ExpiresAt || x.ExpiresAt > date) && x.ExchangeName != name)
                    .Where(x => x.DeliverTime > takeTime && x.DeliverTime <= offsetTime)
                    .ToList();

            Assert.Single(messages);
        }

        /// <summary>
        /// 布尔字段。
        /// </summary>
        [Fact]
        public void TestBooleanFieldOnly()
        {
            var linq =
                from x in _users
                where x.IsAdministrator
                orderby x.DateAt descending
                select x.Id;

            var results = linq.ToList();
        }

        /// <summary>
        /// 布尔字段。
        /// </summary>
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
        /// 布尔字段。
        /// </summary>
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
        /// 布尔字段。
        /// </summary>
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
        /// 布尔字段。
        /// </summary>
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
        /// 布尔字段。
        /// </summary>
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
        /// 布尔字段。
        /// </summary>
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
        /// 布尔字段。
        /// </summary>
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
        /// 布尔字段。
        /// </summary>
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
        /// 布尔字段。
        /// </summary>
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

        [Fact]
        public void ToStringTest()
        {
            var id = 100;

            var linq =
                from x in _users
                where x.Name == id.ToString()
                orderby x.DateAt descending
                select x;

            var results = linq.ToList();
        }
    }
}
