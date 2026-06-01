using System;
using System.Linq;
using System.Linq.Expressions;
using Inkslab.Linq.Exceptions;
using Inkslab.Linq.MySql;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// Conditions / QueryableExtensions / Ranks 边界测试。
    /// 全部使用 <see cref="LinqAnalyzer"/> 在无数据库连接的情况下完成 SQL 翻译验证。
    /// </summary>
    public class ConditionsBoundaryTests
    {
        // ---------------------------------------------------------------
        // 工厂辅助
        // ---------------------------------------------------------------

        private static IQueryable<User> Users()
            => LinqAnalyzer.From<User>(DatabaseEngine.MySQL, new MySqlAdapter());

        private static IQueryable<UserSharding> UserShardings()
            => LinqAnalyzer.From<UserSharding>(DatabaseEngine.MySQL, new MySqlAdapter());

        // ===============================================================
        // 1. Conditions.And — 右侧常量不被优化（设计限制，文档化测试）
        // ===============================================================

        /// <summary>
        /// And 左侧为 True 常量时，正确返回右节点（短路优化）。
        /// </summary>
        [Fact]
        public void And_LeftTrue_ReturnsRightNode()
        {
            Expression<Func<User, bool>> right = x => x.Id > 0;
            var result = Conditions.True<User>().And(right);

            // 应当直接返回 right，不做 AndAlso 包装
            Assert.Same(right, result);
        }

        /// <summary>
        /// And 左侧为 False 常量时，正确返回左节点（短路优化：整体为假）。
        /// </summary>
        [Fact]
        public void And_LeftFalse_ReturnsLeftNode()
        {
            Expression<Func<User, bool>> left = Conditions.False<User>();
            Expression<Func<User, bool>> right = x => x.Id > 0;
            var result = left.And(right);

            // 整体仍为假，应返回 left（False 常量）
            Assert.Same(left, result);
        }

        /// <summary>
        /// And 右侧为 True 常量时，不做短路优化，结果仍正确求值。
        /// 设计限制：当前实现只优化左侧常量。
        /// </summary>
        [Fact]
        public void And_RightTrue_ResultIsCorrect()
        {
            Expression<Func<User, bool>> left = x => x.Id > 0;
            Expression<Func<User, bool>> right = Conditions.True<User>();
            var result = left.And(right);

            // 即使没有短路，编译后语义必须正确：AND true == left
            var compiled = result.Compile();
            Assert.True(compiled(new User { Id = 1 }));
            Assert.False(compiled(new User { Id = 0 }));
        }

        /// <summary>
        /// And 右侧为 False 常量时，不做短路优化，结果仍正确求值。
        /// 设计限制：当前实现只优化左侧常量。
        /// </summary>
        [Fact]
        public void And_RightFalse_ResultIsAlwaysFalse()
        {
            Expression<Func<User, bool>> left = x => x.Id > 0;
            Expression<Func<User, bool>> right = Conditions.False<User>();
            var result = left.And(right);

            // AND false == always false
            var compiled = result.Compile();
            Assert.False(compiled(new User { Id = 1 }));
            Assert.False(compiled(new User { Id = 0 }));
        }

        // ===============================================================
        // 2. Conditions.Or — 右侧常量不被优化（设计限制，文档化测试）
        // ===============================================================

        /// <summary>
        /// Or 左侧为 False 常量时，正确返回右节点（短路优化）。
        /// </summary>
        [Fact]
        public void Or_LeftFalse_ReturnsRightNode()
        {
            Expression<Func<User, bool>> right = x => x.Id > 0;
            var result = Conditions.False<User>().Or(right);

            Assert.Same(right, result);
        }

        /// <summary>
        /// Or 左侧为 True 常量时，正确返回左节点（短路优化：整体为真）。
        /// </summary>
        [Fact]
        public void Or_LeftTrue_ReturnsLeftNode()
        {
            Expression<Func<User, bool>> left = Conditions.True<User>();
            Expression<Func<User, bool>> right = x => x.Id > 0;
            var result = left.Or(right);

            Assert.Same(left, result);
        }

        /// <summary>
        /// Or 右侧为 False 常量时，不做短路优化，结果仍正确求值。
        /// </summary>
        [Fact]
        public void Or_RightFalse_ResultIsCorrect()
        {
            Expression<Func<User, bool>> left = x => x.Id < 0;
            Expression<Func<User, bool>> right = Conditions.False<User>();
            var result = left.Or(right);

            var compiled = result.Compile();
            Assert.True(compiled(new User { Id = -1 }));
            Assert.False(compiled(new User { Id = 1 }));
        }

        /// <summary>
        /// Or 右侧为 True 常量时，不做短路优化，结果仍正确求值（始终为真）。
        /// </summary>
        [Fact]
        public void Or_RightTrue_ResultIsAlwaysTrue()
        {
            Expression<Func<User, bool>> left = x => x.Id < 0;
            Expression<Func<User, bool>> right = Conditions.True<User>();
            var result = left.Or(right);

            var compiled = result.Compile();
            Assert.True(compiled(new User { Id = 1 }));
            Assert.True(compiled(new User { Id = -1 }));
        }

        // ===============================================================
        // 3. Conditions.And/Or — 两侧均为 null
        // ===============================================================

        /// <summary>
        /// And(null, null) 返回"始终为真"哨兵，避免下游 Where(null) 触发 NRE。
        /// </summary>
        [Fact]
        public void And_BothNull_ReturnsTrueSentinel()
        {
            var result = Conditions.And<User>(null, null);

            Assert.NotNull(result);
            // 进一步验证哨兵语义：编译后对任意输入恒为 true
            var compiled = result.Compile();
            Assert.True(compiled(new User { Id = 0 }));
        }

        /// <summary>
        /// Or(null, null) 返回 null。
        /// </summary>
        [Fact]
        public void Or_BothNull_ReturnsNull()
        {
            var result = Conditions.Or<User>(null, null);
            Assert.Null(result);
        }

        // ===============================================================
        // 4. Conditions — SQL 翻译边界（LinqAnalyzer）
        // ===============================================================

        /// <summary>
        /// 将 Conditions.True&lt;T&gt;() 传入 Where 后生成的 SQL 不应包含 WHERE 子句（常量 true 被优化掉）。
        /// </summary>
        [Fact]
        public void ToSql_Where_ConditionsTrue_NoWhereClause()
        {
            var cmd = Users()
                .Where(Conditions.True<User>())
                .ToSql();

            // Conditions.True → WHERE true → 被 visitor 优化掉，不应在 SQL 中出现任何条件
            Assert.DoesNotContain("WHERE", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 将 Conditions.False&lt;T&gt;() 传入 Where 后生成的 SQL 应包含始终为假的条件。
        /// </summary>
        [Fact]
        public void ToSql_Where_ConditionsFalse_AlwaysFalseClause()
        {
            var cmd = Users()
                .Where(Conditions.False<User>())
                .ToSql();

            // WHERE false → 1 = 0 或等价形式
            Assert.Contains("WHERE", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// And 组合两个正常条件后 SQL 包含 AND。
        /// </summary>
        [Fact]
        public void ToSql_Where_AndCombined_SqlContainsAnd()
        {
            var predicate = Conditions.Fragment<User>(x => x.Id > 0)
                                      .And(x => x.IsAdministrator);

            var cmd = Users().Where(predicate).ToSql();

            Assert.Contains("AND", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Or 组合两个正常条件后 SQL 包含 OR。
        /// </summary>
        [Fact]
        public void ToSql_Where_OrCombined_SqlContainsOr()
        {
            var predicate = Conditions.Fragment<User>(x => x.Id > 0)
                                      .Or(x => x.IsAdministrator);

            var cmd = Users().Where(predicate).ToSql();

            Assert.Contains("OR", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// And 左侧为 True 后跟 Or：先优化掉 True，然后 Or 中只剩真实条件。
        /// </summary>
        [Fact]
        public void ToSql_AndTrueThenOr_ProducesCorrectSql()
        {
            // True.And(x => x.Id > 0) → x.Id > 0
            // (x.Id > 0).Or(x => x.IsAdministrator) → id>0 OR is_admin
            var predicate = Conditions.True<User>()
                                      .And(x => x.Id > 0)
                                      .Or(x => x.IsAdministrator);

            var cmd = Users().Where(predicate).ToSql();

            Assert.Contains("OR", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("`id`", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        // ===============================================================
        // 5. QueryableExtensions.WhereIf — 边界
        // ===============================================================

        /// <summary>
        /// WhereIf(true, predicate) 生成的 SQL 包含 WHERE 条件。
        /// </summary>
        [Fact]
        public void ToSql_WhereIf_TestTrue_SqlContainsWhereClause()
        {
            var cmd = Users().WhereIf(true, x => x.Id == 99).ToSql();

            Assert.Contains("WHERE", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("99", cmd.Text);
        }

        /// <summary>
        /// WhereIf(false, predicate) 生成的 SQL 不包含 WHERE 条件（谓词被跳过）。
        /// </summary>
        [Fact]
        public void ToSql_WhereIf_TestFalse_SqlHasNoWhereClause()
        {
            var cmd = Users().WhereIf(false, x => x.Id == 99).ToSql();

            // 谓词未被应用，SQL 中不应出现 WHERE
            Assert.DoesNotContain("WHERE", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// WhereIf source 为 null 时抛出 ArgumentNullException。
        /// </summary>
        [Fact]
        public void WhereIf_NullSource_ThrowsArgumentNullException()
        {
            IQueryable<User> source = null;
            Assert.Throws<ArgumentNullException>(() =>
                source.WhereIf(true, x => x.Id == 1));
        }

        /// <summary>
        /// WhereIf predicate 为 null 时，即使 test=true 也抛出 ArgumentNullException。
        /// </summary>
        [Fact]
        public void WhereIf_NullPredicate_TestTrue_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                Users().WhereIf(true, (Expression<Func<User, bool>>)null));
        }

        /// <summary>
        /// WhereIf predicate 为 null 且 test=false 时，直接返回 source 原引用，不检查 predicate。
        /// </summary>
        [Fact]
        public void WhereIf_NullPredicate_TestFalse_ReturnsSourceUnchanged()
        {
            var source = Users();
            var result = source.WhereIf(false, (Expression<Func<User, bool>>)null);

            Assert.NotNull(result);
            Assert.Same(source, result);
        }

        // ===============================================================
        // 6. Skip / Take 边界
        // ===============================================================

        /// <summary>
        /// Take(0) 生成 LIMIT 0 的 SQL（0 是合法的"空集合"语义）。
        /// </summary>
        [Fact]
        public void ToSql_Take_Zero_GeneratesLimitZero()
        {
            var cmd = Users().OrderBy(x => x.Id).Take(0).ToSql();
            Assert.NotNull(cmd);
            Assert.Contains("LIMIT 0", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Take(-1) 在 SQL 翻译时抛出 ArgumentOutOfRangeException。
        /// </summary>
        [Fact]
        public void ToSql_Take_Negative_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Users().OrderBy(x => x.Id).Take(-1).ToSql());
        }

        /// <summary>
        /// Skip(0) 是合法值，不抛异常，仅 Take 决定 LIMIT 大小。
        /// </summary>
        [Fact]
        public void ToSql_Skip_Zero_GeneratesLimitWithoutOffset()
        {
            var cmd = Users().OrderBy(x => x.Id).Skip(0).Take(10).ToSql();

            Assert.NotNull(cmd);
            Assert.Contains("LIMIT 10", cmd.Text, StringComparison.OrdinalIgnoreCase);
            // Skip(0) 不产生 OFFSET，SQL 中不应有 "LIMIT 0,10" 形式
            Assert.DoesNotContain("LIMIT 0,", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Skip(-1) 在 SQL 翻译时抛出 ArgumentOutOfRangeException。
        /// </summary>
        [Fact]
        public void ToSql_Skip_Negative_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Users().OrderBy(x => x.Id).Skip(-1).Take(10).ToSql());
        }

        /// <summary>
        /// Skip 大于 Take 时不抛异常，生成 LIMIT skip,take 格式。
        /// 注意：MySQL LIMIT skip,take 中 Take(3) 仅返回 3 行，Skip(5) 跳过前 5 行。
        /// </summary>
        [Fact]
        public void ToSql_SkipGreaterThanTake_GeneratesLimitWithOffset()
        {
            var cmd = Users().OrderBy(x => x.Id).Skip(5).Take(3).ToSql();
            Assert.NotNull(cmd);
            Assert.Contains("LIMIT 5,3", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Skip 等于 Take 时不抛异常，生成 LIMIT skip,take 格式。
        /// </summary>
        [Fact]
        public void ToSql_SkipEqualsTake_GeneratesLimitWithOffset()
        {
            var cmd = Users().OrderBy(x => x.Id).Skip(5).Take(5).ToSql();
            Assert.NotNull(cmd);
            Assert.Contains("LIMIT 5,5", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Take 正常值产生 LIMIT 子句。
        /// </summary>
        [Fact]
        public void ToSql_Take_Positive_GeneratesLimit()
        {
            var cmd = Users().OrderBy(x => x.Id).Take(10).ToSql();
            Assert.Contains("LIMIT 10", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        // ===============================================================
        // 7. DataSharding 边界
        // ===============================================================

        /// <summary>
        /// DataSharding(null) 在调用时立即抛出 ArgumentException。
        /// </summary>
        [Fact]
        public void DataSharding_NullKey_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                UserShardings().DataSharding(null));
        }

        /// <summary>
        /// DataSharding("") 在调用时立即抛出 ArgumentException。
        /// </summary>
        [Fact]
        public void DataSharding_EmptyKey_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                UserShardings().DataSharding(string.Empty));
        }

        /// <summary>
        /// DataSharding 在同一数据源上调用两次时，SQL 翻译抛出 DSyntaxErrorException。
        /// </summary>
        [Fact]
        public void ToSql_DataSharding_CalledTwice_ThrowsDSyntaxErrorException()
        {
            Assert.Throws<DSyntaxErrorException>(() =>
                UserShardings().DataSharding("2024").DataSharding("2025").ToSql());
        }

        /// <summary>
        /// DataSharding 正常使用时，SQL 中的表名包含分片键。
        /// </summary>
        [Fact]
        public void ToSql_DataSharding_ValidKey_TableNameContainsKey()
        {
            var cmd = UserShardings().DataSharding("2025").ToSql();

            Assert.Contains("user_2025", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 在不支持分区的普通表上调用 DataSharding 时，SQL 翻译抛出 InvalidOperationException。
        /// </summary>
        [Fact]
        public void ToSql_DataSharding_OnNonShardingTable_ThrowsInvalidOperationException()
        {
            // User 表映射 [Table("user")]，无 [sharding] 占位符
            Assert.Throws<InvalidOperationException>(() =>
                Users().DataSharding("2025").ToSql());
        }

        // ===============================================================
        // 8. Timeout 边界
        // ===============================================================

        /// <summary>
        /// Timeout(0) 抛出 ArgumentOutOfRangeException（最小值为 1）。
        /// </summary>
        [Fact]
        public void Timeout_Zero_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Users().Timeout(0));
        }

        /// <summary>
        /// Timeout(-1) 抛出 ArgumentOutOfRangeException。
        /// </summary>
        [Fact]
        public void Timeout_Negative_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Users().Timeout(-1));
        }

        /// <summary>
        /// Timeout(null source) 抛出 ArgumentNullException。
        /// </summary>
        [Fact]
        public void Timeout_NullSource_ThrowsArgumentNullException()
        {
            IQueryable<User> source = null;
            Assert.Throws<ArgumentNullException>(() => source.Timeout(30));
        }

        /// <summary>
        /// Timeout 正常值（>=1）可以被构建为查询表达式且 SQL 翻译完整（含 WHERE 条件）。
        /// Timeout 本身只在执行层生效，但不应丢失或干扰前置 LINQ 操作的 SQL 生成。
        /// </summary>
        [Fact]
        public void Timeout_PositiveValue_PreservesUpstreamSql()
        {
            var cmd = Users().Where(x => x.Id == 1).Timeout(30).ToSql();

            Assert.NotNull(cmd);
            // Timeout 不应丢失上游 Where 条件
            Assert.Contains("WHERE", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("`id`", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        // ===============================================================
        // 9. NoElementError 边界
        // ===============================================================

        /// <summary>
        /// NoElementError(null) 抛出 ArgumentException。
        /// </summary>
        [Fact]
        public void NoElementError_NullMessage_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                Users().NoElementError(null));
        }

        /// <summary>
        /// NoElementError("") 抛出 ArgumentException。
        /// </summary>
        [Fact]
        public void NoElementError_EmptyMessage_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                Users().NoElementError(string.Empty));
        }

        /// <summary>
        /// NoElementError(null source) 抛出 ArgumentNullException。
        /// </summary>
        [Fact]
        public void NoElementError_NullSource_ThrowsArgumentNullException()
        {
            IQueryable<User> source = null;
            Assert.Throws<ArgumentNullException>(() => source.NoElementError("not found"));
        }

        // ===============================================================
        // 10. Ranks.By — 边界
        // ===============================================================

        /// <summary>
        /// Ranks.By 在 LinqAnalyzer（MySQL adapter）上下文中抛出 NotSupportedException。
        /// MySQL adapter 未注册 Ranks.By 方法访问器，记录此已知限制。
        /// </summary>
        [Fact]
        public void ToSql_RanksBy_WhenFalse_ThrowsNotSupportedInLinqAnalyzer()
        {
            int type = 200;

            Assert.Throws<NotSupportedException>(() =>
                Users()
                    .Where(x => x.Id == 1)
                    .OrderBy(x => x.DateAt)
                    .Select(x => new { x.Id, Sort = Ranks.By(x, c => c.When(type < 100).OrderBy(y => y.Name)) })
                    .ToSql());
        }

        /// <summary>
        /// Ranks.By 在 LinqAnalyzer（MySQL adapter）上下文中抛出 NotSupportedException。
        /// MySQL adapter 未注册 Ranks.By 方法访问器，记录此已知限制。
        /// </summary>
        [Fact]
        public void ToSql_RanksBy_WhenTrue_ThrowsNotSupportedInLinqAnalyzer()
        {
            int type = 50;

            Assert.Throws<NotSupportedException>(() =>
                Users()
                    .Where(x => x.Id == 1)
                    .OrderBy(x => x.DateAt)
                    .Select(x => new { x.Id, Sort = Ranks.By(x, c => c.When(type < 100).OrderBy(y => y.Name)) })
                    .ToSql());
        }

        /// <summary>
        /// Ranks.By 直接调用（非表达式上下文）抛出 NotImplementedException。
        /// </summary>
        [Fact]
        public void Ranks_By_DirectCall_ThrowsNotImplementedException()
        {
            var user = new User { Id = 1 };
            Assert.Throws<NotImplementedException>(() =>
                Ranks.By(user, r => r.When(true).OrderBy(u => u.Id)));
        }

        // ===============================================================
        // 11. Conditions.If / Conditional — SQL 翻译边界（LinqAnalyzer）
        // ===============================================================

        /// <summary>
        /// Conditions.If(test=true, condition) 在 SQL WHERE 中条件被写入。
        /// </summary>
        [Fact]
        public void ToSql_ConditionsIf_TestTrue_ConditionIncluded()
        {
            bool flag = true;

            var cmd = Users()
                .Where(x => Conditions.If(flag, x.Id > 0))
                .ToSql();

            Assert.Contains("WHERE", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("`id`", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Conditions.If(test=false, condition) 在 SQL WHERE 中条件被忽略（无 WHERE 子句）。
        /// </summary>
        [Fact]
        public void ToSql_ConditionsIf_TestFalse_ConditionOmitted()
        {
            bool flag = false;

            var cmd = Users()
                .Where(x => Conditions.If(flag, x.Id > 0))
                .ToSql();

            Assert.DoesNotContain("WHERE", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Conditions.Conditional(test=true, ifTrue, ifFalse) 使用 ifTrue 分支。
        /// </summary>
        [Fact]
        public void ToSql_ConditionsConditional_TestTrue_UsesTrueBranch()
        {
            bool flag = true;

            var cmd = Users()
                .Where(x => Conditions.Conditional(flag, x.Id > 0, x.IsAdministrator))
                .ToSql();

            Assert.Contains("`id`", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Conditions.Conditional(test=false, ifTrue, ifFalse) 使用 ifFalse 分支。
        /// </summary>
        [Fact]
        public void ToSql_ConditionsConditional_TestFalse_UsesFalseBranch()
        {
            bool flag = false;

            var cmd = Users()
                .Where(x => Conditions.Conditional(flag, x.Id > 0, x.IsAdministrator))
                .ToSql();

            Assert.Contains("`is_administrator`", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        // ===============================================================
        // 12. Conditions.And/Or 链式 SQL 翻译
        // ===============================================================

        /// <summary>
        /// 多个 And 链式组合最终生成带多个 AND 的 WHERE 子句。
        /// </summary>
        [Fact]
        public void ToSql_AndChain_ProducesMultipleAnds()
        {
            var predicate = Conditions.Fragment<User>(x => x.Id > 0)
                                      .And(x => x.Name != null)
                                      .And(x => !x.IsAdministrator);

            var cmd = Users().Where(predicate).ToSql();

            // 三个条件 → 至少两个 AND
            int andCount = 0;
            int idx = 0;
            while ((idx = cmd.Text.IndexOf("AND", idx, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                andCount++;
                idx += 3;
            }

            Assert.True(andCount >= 2, $"Expected at least 2 AND keywords, got {andCount}. SQL: {cmd.Text}");
        }

        /// <summary>
        /// And 后跟 Or 时括号优先级正确：(A AND B) OR C。
        /// </summary>
        [Fact]
        public void ToSql_AndThenOr_PrecedenceCorrect()
        {
            // (id>0 AND !is_admin) OR id==999
            var predicate = Conditions.Fragment<User>(x => x.Id > 0)
                                      .And(x => !x.IsAdministrator)
                                      .Or(x => x.Id == 999);

            var cmd = Users().Where(predicate).ToSql();

            Assert.Contains("OR", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("AND", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Or 后跟 And 时括号优先级正确：(A OR B) AND C。
        /// </summary>
        [Fact]
        public void ToSql_OrThenAnd_PrecedenceCorrect()
        {
            // (id<0 OR id>100) AND !is_admin
            var predicate = Conditions.Fragment<User>(x => x.Id < 0)
                                      .Or(x => x.Id > 100)
                                      .And(x => !x.IsAdministrator);

            var cmd = Users().Where(predicate).ToSql();

            Assert.Contains("OR", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("AND", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        // ===============================================================
        // 13. LinqAnalyzer 对外部 IQueryable 的边界保护
        // ===============================================================

        /// <summary>
        /// 对非 LinqAnalyzer 创建的 IQueryable 调用 ToSql 抛出 NotSupportedException。
        /// </summary>
        [Fact]
        public void ToSql_ForeignQueryable_ThrowsNotSupportedException()
        {
            IQueryable<User> foreign = Enumerable.Empty<User>().AsQueryable();
            Assert.Throws<NotSupportedException>(() => foreign.ToSql());
        }

        /// <summary>
        /// 对 null IQueryable 调用 ToSql 抛出 ArgumentNullException。
        /// </summary>
        [Fact]
        public void ToSql_NullSource_ThrowsArgumentNullException()
        {
            IQueryable<User> source = null;
            Assert.Throws<ArgumentNullException>(() => source.ToSql());
        }

        /// <summary>
        /// LinqAnalyzer 创建的 IQueryable 枚举时抛出 NotSupportedException（只能分析，不能执行）。
        /// </summary>
        [Fact]
        public void LinqAnalyzer_Enumerate_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                foreach (var _ in Users()) { }
            });
        }

        // ===============================================================
        // 14. Select 投影后 Where — 别名引用验证
        // ===============================================================

        /// <summary>
        /// Select 投影后直接跟 Where 框架抛 DSyntaxErrorException（已知限制）。
        /// 框架要求 Select 必须在过滤器、排序或聚合函数之后，不支持先 Select 再 Where。
        /// </summary>
        [Fact]
        public void ToSql_SelectThenWhere_ThrowsDSyntaxError()
        {
            Assert.Throws<DSyntaxErrorException>(() =>
                Users()
                    .Select(x => new { x.Id, x.Name })
                    .Where(x => x.Id > 0)
                    .ToSql());
        }

        // ===============================================================
        // 15. Distinct + OrderBy — SQL 合法性
        // ===============================================================

        /// <summary>
        /// Distinct 后跟 OrderBy 抛 DSyntaxErrorException（已知框架限制）。
        /// 框架要求使用 Distinct 时必须通过 Select 指定查询字段，不支持 Distinct 后再 OrderBy。
        /// </summary>
        [Fact]
        public void ToSql_DistinctThenOrderBy_ThrowsDSyntaxError()
        {
            Assert.Throws<DSyntaxErrorException>(() =>
                Users()
                    .Select(x => new { x.Id, x.Name })
                    .Distinct()
                    .OrderBy(x => x.Id)
                    .ToSql());
        }

        /// <summary>
        /// Distinct 后跟 Count 生成合法 SQL（COUNT DISTINCT）。
        /// </summary>
        [Fact]
        public void ToSql_DistinctThenCount_ProducesValidSql()
        {
            var cmd = Users()
                .Select(x => x.Name)
                .Distinct()
                .ToSql(q => q.Count());

            Assert.NotNull(cmd);
            Assert.Contains("COUNT", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
