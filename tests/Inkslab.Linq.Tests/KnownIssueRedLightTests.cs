using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using Inkslab.Linq.Annotations;
using Inkslab.Linq.Exceptions;
using Inkslab.Linq.MySql;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// 已修复缺陷的回归契约测试：每条用例对应一条历史 issue 与一处源码定位，
    /// 用于防止"修复后被回退"。当前全部为绿灯——若任何一条变红，说明生产代码退化。
    /// </summary>
    /// <remarks>
    /// 涵盖 issue 列表（修复定位）：
    ///   #1  Take(0) 视为合法、生成 LIMIT 0          (SqlWriter.TakeSize, MySqlCorrectSettings.ToSQL)
    ///   #2  WhereIf(false, null) 直接返回 source    (QueryableExtentions.WhereIf)
    ///   #3  Conditions.And(null, null) 返回 True 哨兵 (Conditions.And)
    ///   #4  Contains/StartsWith/EndsWith(常量 null) 抛 ArgumentNullException (ByStringCallVisitor)
    ///   #5  连续两次 OrderBy 后一次覆盖前一次       (SelectVisitor / SqlWriter.OrderByDeclaration)
    ///   #6  [Table]/[Field] 空白名称在构造时抛异常  (TableAttribute / FieldAttribute, IsNullOrWhiteSpace)
    ///   #7  [Version] 不支持类型异常消息含属性名     (DefaultTableAnalyzer)
    /// </remarks>
    public class KnownIssueRedLightTests
    {
        private static IQueryable<User> Users()
            => LinqAnalyzer.From<User>(DatabaseEngine.MySQL, new MySqlAdapter());

        // ═══════════════════════════════════════════════════════════
        // #1  Take(0) 应生成 LIMIT 0，是合法的"空集合"语义
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void Take_Zero_GeneratesLimitZeroSql()
        {
            var cmd = Users().OrderBy(x => x.Id).Take(0).ToSql();

            Assert.NotNull(cmd);
            Assert.Contains("LIMIT 0", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Take_Negative_ThrowsWithNegativeMessage()
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
                Users().OrderBy(x => x.Id).Take(-1).ToSql());

            // 错误消息必须明确指出"负数"，区分"零"是合法的
            Assert.Contains("负数", ex.Message);
            Assert.DoesNotContain("大于零", ex.Message);
        }

        // ═══════════════════════════════════════════════════════════
        // #2  WhereIf(false, null) 直接返回 source
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void WhereIf_FalseTest_NullPredicate_ReturnsSourceUnchanged()
        {
            var source = Users().OrderBy(x => x.Id);
            var result = source.WhereIf(false, (Expression<Func<User, bool>>)null);

            // test=false 时 predicate 根本不被检查，直接原样返回 source 引用
            Assert.NotNull(result);
            Assert.Same(source, result);
        }

        // ═══════════════════════════════════════════════════════════
        // #3  Conditions.And(null, null) 返回 True 哨兵
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void Conditions_And_BothNull_ReturnsTrueSentinel()
        {
            Expression<Func<User, bool>> left = null;
            Expression<Func<User, bool>> right = null;

            var result = left.And(right);

            Assert.NotNull(result);
            // 进一步验证：编译后对任意输入都返回 true
            var compiled = result.Compile();
            Assert.True(compiled(new User { Id = 0 }));
            Assert.True(compiled(new User { Id = 42 }));
        }

        [Fact]
        public void Conditions_And_BothNull_ResultIsSafeInWhere()
        {
            Expression<Func<User, bool>> left = null;
            Expression<Func<User, bool>> right = null;

            var combined = left.And(right);
            var cmd = Users().Where(combined).ToSql();

            Assert.NotNull(cmd);
            // True 常量被视作"始终为真"，应被优化为不输出 WHERE 子句
            Assert.DoesNotContain("WHERE", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        // ═══════════════════════════════════════════════════════════
        // #4  Contains/StartsWith/EndsWith(常量 null) 抛 ArgumentNullException
        //     注：与 C# string.Contains(null) 同语义；变量 null 走另一路径不抛。
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void Contains_NullConstant_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                Users().Where(x => x.Name.Contains(null)).ToSql());
        }

        [Fact]
        public void StartsWith_NullConstant_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                Users().Where(x => x.Name.StartsWith(null)).ToSql());
        }

        [Fact]
        public void EndsWith_NullConstant_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                Users().Where(x => x.Name.EndsWith(null)).ToSql());
        }

        // ═══════════════════════════════════════════════════════════
        // #5  连续两次 OrderBy：后一次覆盖前一次（与 IEnumerable.OrderBy 一致）
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void OrderBy_CalledTwice_OnlySecondFieldRemainsInOrderByClause()
        {
            var cmd = Users()
                .OrderBy(x => x.Id)
                .OrderBy(x => x.Name)
                .ToSql();

            var orderByPart = cmd.Text.Split(
                new[] { "ORDER BY", "order by" },
                StringSplitOptions.None);

            Assert.True(orderByPart.Length > 1, "SQL 应包含 ORDER BY 子句");

            var lastOrderBy = orderByPart[^1];
            // 第二次 OrderBy 后，`id` 字段不应再出现在排序子句中，`name` 必须出现
            Assert.DoesNotContain("`id`", lastOrderBy, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("`name`", lastOrderBy, StringComparison.OrdinalIgnoreCase);
        }

        // ═══════════════════════════════════════════════════════════
        // #6  [Table]/[Field] 空白名称在构造时抛 ArgumentException
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void TableAttribute_WhitespaceOnlyName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new TableAttribute("   "));
        }

        [Fact]
        public void FieldAttribute_WhitespaceOnlyName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new FieldAttribute("   "));
        }

        // ═══════════════════════════════════════════════════════════
        // #7  [Version] 不支持类型异常消息含属性名
        // ═══════════════════════════════════════════════════════════

        [Table("test_version_entity")]
        private class EntityWithGuidVersion
        {
            [System.ComponentModel.DataAnnotations.Key]
            public int Id { get; set; }

            [Version]
            public Guid VersionPropName { get; set; }
        }

        [Fact]
        public void Version_UnsupportedType_ExceptionMessageContainsPropertyName()
        {
            var analyzer = new DefaultTableAnalyzer();

            var ex = Assert.Throws<NotSupportedException>(() =>
                analyzer.Table(typeof(EntityWithGuidVersion)));

            Assert.Contains("VersionPropName", ex.Message);
        }
    }
}
