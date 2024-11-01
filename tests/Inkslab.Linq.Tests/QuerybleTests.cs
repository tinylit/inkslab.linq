﻿using Inkslab.Linq.Exceptions;
using System;
using System.Linq;
using Xunit;

namespace Inkslab.Linq.Tests
{
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
                       orderby x.Date, x.Name
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
                       group new { x.Name, x.Date } by x.Id
                        into g
                       orderby g.Key descending
                       select new { g.Key, Total = g.Count(), Count = g.Where(x => x.Date > now).Count() };

            var results = linq.ToList();
        }

        [Fact]
        public void TestGroupByHavingElement()
        {
            var now = DateTime.Now;

            var linq = from x in _users
                       where x.Id == 100
                       group new { x.Name, x.Date } by x.Id into g
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
                       group new { x.Name, x.Date } by x.Id into g
                       where g.Count() > 1
                       orderby g.Key descending
                       select new
                       {
                           g.Key,
                           Total = g.Count(),
                           Count = g.Count(x => x.Date > now),
                           WhereCount = g.Where(x => x.Date > now).Count(),
                           SelectCount = g.Where(x => x.Date > now).Select(x => x.Name).Count(),
                           DistinctCount = g.Where(x => x.Date > now).Select(x => x.Name).Distinct().Count()
                       };

            var results = linq.ToList();
        }

        [Fact]
        public void TestGroupByHavingMoreElementCount()
        {
            var now = DateTime.Now;

            var linq = from x in _users
                       where x.Id == 100
                       group new { x.Name, x.Date } by new { x.Id, x.Name } into g
                       where g.Count() > 1
                       orderby g.Key descending
                       select new
                       {
                           g.Key.Id,
                           g.Key.Name,
                           Total = g.Count(),
                           Count = g.Count(x => x.Date > now),
                           WhereCount = g.Where(x => x.Date > now).Count(),
                           SelectCount = g.Where(x => x.Date > now).Select(x => x.Name).Count(),
                           DistinctCount = g.Where(x => x.Date > now).Select(x => x.Name).Distinct().Count()
                       };

            var results = linq.ToList();
        }

        [Fact]
        public void TestGroupByHavingMoreKey()
        {
            var now = DateTime.Now;

            var linq = from x in _users
                       where x.Id == 100
                       group new { x.Name, x.Date } by new { x.Id, x.Name } into g
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
                       where g.Count(x => x.Date > now) > 1
                       orderby g.Key descending
                       select new
                       {
                           g.Key,
                           Max = g.Max(x => x.Date),
                           WhereMax = g.Where(x => x.Date > now).Max(x => x.Id),
                           SelectMax = g.Where(x => x.Date > now).Select(x => x.Id).Max(),
                           DistinctMax = g.Where(x => x.Date > now).Select(x => x.Id).Distinct().Max()
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
                       select new { x.Id, y.Role };

            var results = linq.ToList();
        }

        [Fact]
        public void TestJoinWhere()
        {
            var linq = from x in _users.Where(y => y.Date > DateTime.UnixEpoch)
                       join y in _userExes.Where(x => x.Age > 18)
                       on x.Id equals y.Id
                       orderby x.Id descending
                       select new { x.Id, y.Role };

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
                       select new { x.Id, z.Role };

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
                       select new { x.Id, z.Role };

            var results = linq.ToList();
        }

        [Fact]
        public void TestCrossJoinSelectMany()
        {
            var linq = from x in _users
                       from y in _userExes
                       orderby x.Id descending
                       select new { x.Id, y.Role };

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
                       orderby x.Date, x.Name
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
                       group x by new { x.Name, x.Date }
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
                       orderby x.Date, x.Name
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
                       select new { x.Id, y.Role, Type = 1 };

            var linq2 = from x in _users
                        join y in _userExes
                        on x.Id equals y.Id into g
                        from z in g.DefaultIfEmpty()
                        select new { x.Id, z.Role, Type = 2 };

            var results = linq.Union(linq2).ToList();
        }

        [Fact]
        public void ConcatSelectDistinct()
        {
            var linq = from x in _users
                       join y in _userExes
                       on x.Id equals y.Id
                       select new { x.Id, y.Role, Type = 1 };

            var linq2 = from x in _users
                        join y in _userExes
                        on x.Id equals y.Id into g
                        from z in g.DefaultIfEmpty()
                        select new { x.Id, z.Role, Type = 2 };

            var results = linq.Concat(linq2)
                .Select(x => string.Concat(x.Role, x.Type))
                .Distinct()
                .ToList();
        }

        [Fact] //? MySQL 不支持 Except 语法。
        public void UnionWhere()
        {
            var linq = from x in _users
                       join y in _userExes
                       on x.Id equals y.Id
                       select new { x.Id, y.Role, Type = 1 };

            var linq2 = from x in _users
                        join y in _userExes
                        on x.Id equals y.Id into g
                        from z in g.DefaultIfEmpty()
                        select new { x.Id, z.Role, Type = 2 };

            var results = linq.Union(linq2)
                .Where(x => x.Role == 2)
                .Select(x => string.Concat(x.Role, x.Type))
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
                       select new { x.Id, y.Role, Type = 1 };

            var linq2 = from x in _users
                        join y in _userExes
                        on x.Id equals y.Id into g
                        from z in g.DefaultIfEmpty()
                        orderby x.Id descending
                        select new { x.Id, z.Role, Type = 2 };

            var results = linq.Take(10).Union(linq2.Take(10)).ToList();
        }

        [Fact]
        public void UnionTake()
        {
            var linq = from x in _users
                       join y in _userExes
                       on x.Id equals y.Id
                       orderby x.Id descending
                       select new { x.Id, y.Role, Type = 1 };

            var linq2 = from x in _users
                        join y in _userExes
                        on x.Id equals y.Id into g
                        from z in g.DefaultIfEmpty()
                        orderby x.Id descending
                        select new { x.Id, z.Role, Type = 2 };

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
                       orderby x.Date, x.Name
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
                       orderby x.Date, x.Name
                       select x.Id;

            var results = linq.ToList();
        }
    }
}
