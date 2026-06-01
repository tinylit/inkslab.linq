using System;
using System.Collections.Generic;
using System.Linq;
using Inkslab.Linq.Enums;
using Inkslab.Linq.Exceptions;
using Inkslab.Linq.MySql;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// 表达式树翻译层边界测试。
    /// 使用 LinqAnalyzer 在不连接数据库的情况下验证 SQL 翻译正确性。
    /// </summary>
    public class ExpressionBoundaryTests
    {
        // ──────────────────────────────────────────────
        // 辅助方法
        // ──────────────────────────────────────────────

        private static IQueryable<User> MySqlUsers()
            => LinqAnalyzer.From<User>(DatabaseEngine.MySQL, new MySqlAdapter());

        private static IQueryable<UserEx> MySqlUserEx()
            => LinqAnalyzer.From<UserEx>(DatabaseEngine.MySQL, new MySqlAdapter());

        // ══════════════════════════════════════════════
        // 1. NULL 常量与可空类型
        // ══════════════════════════════════════════════

        /// <summary>
        /// x.Field == null 应翻译为 IS NULL，而不是 = NULL。
        /// </summary>
        [Fact]
        public void NullComparison_Equal_ShouldGenerateIsNull()
        {
            var cmd = MySqlUsers()
                .Where(x => x.Name == null)
                .ToSql();

            Assert.Contains("IS NULL", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("= NULL", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// x.Field != null 应翻译为 IS NOT NULL，而不是 != NULL。
        /// </summary>
        [Fact]
        public void NullComparison_NotEqual_ShouldGenerateIsNotNull()
        {
            var cmd = MySqlUsers()
                .Where(x => x.Name != null)
                .ToSql();

            Assert.Contains("IS NOT NULL", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("!= NULL", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("<> NULL", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 可空 bool 字段 x.Nullable == null 应翻译为 IS NULL。
        /// </summary>
        [Fact]
        public void NullComparison_NullableBool_EqualNull_ShouldGenerateIsNull()
        {
            var cmd = MySqlUsers()
                .Where(x => x.Nullable == null)
                .ToSql();

            Assert.Contains("IS NULL", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 可空 bool 字段 x.Nullable != null 应翻译为 IS NOT NULL。
        /// </summary>
        [Fact]
        public void NullComparison_NullableBool_NotEqualNull_ShouldGenerateIsNotNull()
        {
            var cmd = MySqlUsers()
                .Where(x => x.Nullable != null)
                .ToSql();

            Assert.Contains("IS NOT NULL", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// x.Nullable.HasValue 应翻译为 IS NOT NULL。
        /// </summary>
        [Fact]
        public void NullableHasValue_ShouldGenerateIsNotNull()
        {
            var cmd = MySqlUsers()
                .Where(x => x.Nullable.HasValue)
                .ToSql();

            Assert.Contains("IS NOT NULL", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// !x.Nullable.HasValue 应翻译为 IS NULL。
        /// </summary>
        [Fact]
        public void NullableHasValue_Negated_ShouldGenerateIsNull()
        {
            var cmd = MySqlUsers()
                .Where(x => !x.Nullable.HasValue)
                .ToSql();

            Assert.Contains("IS NULL", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 变量 null 与字段比较：string localNull = null; x => x.Name == localNull
        /// 应翻译为 IS NULL。
        /// </summary>
        [Fact]
        public void NullComparison_LocalNullVariable_ShouldGenerateIsNull()
        {
            string localNull = null;

            var cmd = MySqlUsers()
                .Where(x => x.Name == localNull)
                .ToSql();

            Assert.Contains("IS NULL", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        // ══════════════════════════════════════════════
        // 2. 字符串方法 — null/空参数的边界行为
        // ══════════════════════════════════════════════

        /// <summary>
        /// Contains 参数为 null 时，ByStringCallVisitor 应当忽略该条件（domain.IsEmpty），
        /// 在普通场景下应生成 AlwaysTrue（1=1 或省略），而非抛异常。
        /// </summary>
        [Fact]
        public void StringContains_NullArgument_ShouldProduceAlwaysTrueOrSkip()
        {
            string search = null;

            // 不应抛出异常，结果 SQL 应不包含 LIKE
            var cmd = MySqlUsers()
                .Where(x => x.Name.Contains(search))
                .ToSql();

            Assert.NotNull(cmd);
            Assert.DoesNotContain("LIKE", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// StartsWith 参数为 null 时，应忽略条件，不抛异常。
        /// </summary>
        [Fact]
        public void StringStartsWith_NullArgument_ShouldProduceAlwaysTrueOrSkip()
        {
            string prefix = null;

            var cmd = MySqlUsers()
                .Where(x => x.Name.StartsWith(prefix))
                .ToSql();

            Assert.NotNull(cmd);
            Assert.DoesNotContain("LIKE", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// EndsWith 参数为 null 时，应忽略条件，不抛异常。
        /// </summary>
        [Fact]
        public void StringEndsWith_NullArgument_ShouldProduceAlwaysTrueOrSkip()
        {
            string suffix = null;

            var cmd = MySqlUsers()
                .Where(x => x.Name.EndsWith(suffix))
                .ToSql();

            Assert.NotNull(cmd);
            Assert.DoesNotContain("LIKE", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Contains 参数为空字符串时，应忽略条件（空字符串被视为忽略）。
        /// </summary>
        [Fact]
        public void StringContains_EmptyArgument_ShouldProduceAlwaysTrueOrSkip()
        {
            string search = string.Empty;

            var cmd = MySqlUsers()
                .Where(x => x.Name.Contains(search))
                .ToSql();

            Assert.NotNull(cmd);
            Assert.DoesNotContain("LIKE", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Contains 参数为正常字符串时，应正确生成 LIKE '%value%'。
        /// MySQL 使用 CONCAT('%', value, '%')。
        /// </summary>
        [Fact]
        public void StringContains_NormalArgument_ShouldGenerateLike()
        {
            string search = "test";

            var cmd = MySqlUsers()
                .Where(x => x.Name.Contains(search))
                .ToSql();

            Assert.Contains("LIKE", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("%", cmd.Text);
        }

        /// <summary>
        /// string.IsNullOrEmpty 参数为非空变量时，应生成 IS NULL OR = '' 的条件。
        /// </summary>
        [Fact]
        public void StringIsNullOrEmpty_WithField_ShouldGenerateIsNullOrEmpty()
        {
            var cmd = MySqlUsers()
                .Where(x => string.IsNullOrEmpty(x.Name))
                .ToSql();

            Assert.Contains("IS NULL", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("OR", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// string.IsNullOrEmpty 参数为 null 变量时，应忽略条件（always true 或跳过）。
        /// </summary>
        [Fact]
        public void StringIsNullOrEmpty_NullArgument_ShouldSkip()
        {
            string value = null;

            var cmd = MySqlUsers()
                .Where(x => string.IsNullOrEmpty(value))
                .ToSql();

            Assert.NotNull(cmd);
            // 当参数是常量 null 时，条件被静态折叠，不应再出现 IS NULL 字段判断
            // 整体 WHERE 应消失或变成 AlwaysTrue（1=1）
            Assert.DoesNotContain("IS NULL", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        // ══════════════════════════════════════════════
        // 3. OrderBy 多次调用行为（OrderBy → ThenBy vs OrderBy → OrderBy）
        // ══════════════════════════════════════════════

        /// <summary>
        /// 连续 OrderBy → ThenBy 应生成包含两个字段的 ORDER BY 子句。
        /// </summary>
        [Fact]
        public void OrderBy_ThenBy_ShouldProduceTwoSortFields()
        {
            var cmd = MySqlUsers()
                .OrderBy(x => x.Id)
                .ThenBy(x => x.Name)
                .ToSql();

            Assert.Contains("ORDER BY", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("`id`", cmd.Text);
            Assert.Contains("`name`", cmd.Text);
        }

        /// <summary>
        /// 连续两次 OrderBy 调用，后一个 OrderBy 会覆盖前一个（框架行为），
        /// 最终 ORDER BY 应包含第二次 OrderBy 的字段。
        /// </summary>
        [Fact]
        public void OrderBy_TwiceConsecutive_SecondOrderByAppears()
        {
            var cmd = MySqlUsers()
                .OrderBy(x => x.Id)
                .OrderBy(x => x.Name)
                .ToSql();

            Assert.Contains("ORDER BY", cmd.Text, StringComparison.OrdinalIgnoreCase);
            // 第二次 OrderBy 的字段必须出现
            Assert.Contains("`name`", cmd.Text);
        }

        /// <summary>
        /// OrderByDescending 单字段，SQL 应包含 DESC。
        /// </summary>
        [Fact]
        public void OrderByDescending_ShouldContainDesc()
        {
            var cmd = MySqlUsers()
                .OrderByDescending(x => x.Id)
                .ToSql();

            Assert.Contains("ORDER BY", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("DESC", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// OrderBy → ThenByDescending：两个字段，第二个字段有 DESC。
        /// </summary>
        [Fact]
        public void OrderBy_ThenByDescending_ShouldContainDescForSecondField()
        {
            var cmd = MySqlUsers()
                .OrderBy(x => x.Id)
                .ThenByDescending(x => x.Name)
                .ToSql();

            Assert.Contains("ORDER BY", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("`id`", cmd.Text);
            Assert.Contains("`name`", cmd.Text);
            Assert.Contains("DESC", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        // ══════════════════════════════════════════════
        // 4. Skip/Take 边界值
        // ══════════════════════════════════════════════

        /// <summary>
        /// Skip(0) 应合法翻译，不抛异常（偏移为 0 等价于不跳过）。
        /// </summary>
        [Fact]
        public void Skip_Zero_ShouldNotThrow()
        {
            var cmd = MySqlUsers()
                .OrderBy(x => x.Id)
                .Skip(0)
                .ToSql();

            Assert.NotNull(cmd);
            Assert.False(string.IsNullOrWhiteSpace(cmd.Text));
        }

        /// <summary>
        /// Take(0) 生成 LIMIT 0 的 SQL（0 是合法的"空集合"语义）。
        /// </summary>
        [Fact]
        public void Take_Zero_ShouldGenerateLimitZero()
        {
            var cmd = MySqlUsers().OrderBy(x => x.Id).Take(0).ToSql();

            Assert.NotNull(cmd);
            Assert.Contains("LIMIT 0", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Skip(10) + Take(5) 生成 MySQL 的 LIMIT skip,take 格式（"LIMIT 10,5"）。
        /// </summary>
        [Fact]
        public void Skip_And_Take_ShouldProduceLimitWithOffset()
        {
            var cmd = MySqlUsers()
                .OrderBy(x => x.Id)
                .Skip(10)
                .Take(5)
                .ToSql();

            Assert.NotNull(cmd);
            Assert.Contains("LIMIT 10,5", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 不带排序直接 Skip/Take 应抛出 DSyntaxErrorException。
        /// </summary>
        [Fact]
        public void Skip_WithoutOrderBy_ShouldThrowDSyntaxError()
        {
            Assert.Throws<DSyntaxErrorException>(() =>
            {
                MySqlUsers()
                    .Skip(5)
                    .ToSql();
            });
        }

        /// <summary>
        /// 不带排序直接 Take 应抛出 DSyntaxErrorException。
        /// </summary>
        [Fact]
        public void Take_WithoutOrderBy_ShouldThrowDSyntaxError()
        {
            Assert.Throws<DSyntaxErrorException>(() =>
            {
                MySqlUsers()
                    .Take(5)
                    .ToSql();
            });
        }

        // ══════════════════════════════════════════════
        // 5. GroupBy + Where (HAVING) 边界
        // ══════════════════════════════════════════════

        /// <summary>
        /// GroupBy 后 Select 聚合，应生成 GROUP BY 子句。
        /// </summary>
        [Fact]
        public void GroupBy_Select_ShouldGenerateGroupBy()
        {
            var cmd = MySqlUsers()
                .GroupBy(x => x.IsAdministrator)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToSql();

            Assert.Contains("GROUP BY", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("COUNT", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// GroupBy 后直接 Count（聚合），应生成包含 GROUP BY 的 SQL。
        /// </summary>
        [Fact]
        public void GroupBy_Count_ShouldGenerateGroupByWithCount()
        {
            var cmd = MySqlUsers()
                .GroupBy(x => x.IsAdministrator)
                .Select(g => g.Count())
                .ToSql();

            Assert.Contains("GROUP BY", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("COUNT", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// GroupBy 后 Sum 聚合，应生成 SUM 函数。
        /// </summary>
        [Fact]
        public void GroupBy_Sum_ShouldGenerateSumAggregation()
        {
            var cmd = MySqlUsers()
                .GroupBy(x => x.IsAdministrator)
                .Select(g => g.Sum(x => x.Id))
                .ToSql();

            Assert.Contains("SUM", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 在 GroupBy 之前使用 Select（不指定字段直接 Distinct）应抛出 DSyntaxErrorException。
        /// </summary>
        [Fact]
        public void Distinct_WithoutSelectFirst_ShouldThrowDSyntaxError()
        {
            Assert.Throws<DSyntaxErrorException>(() =>
            {
                MySqlUsers()
                    .Distinct()
                    .ToSql();
            });
        }

        // ══════════════════════════════════════════════
        // 6. 枚举与常量在 WHERE 中的处理
        // ══════════════════════════════════════════════

        /// <summary>
        /// 枚举值在 WHERE 条件中应作为整数常量嵌入 SQL（直接常量折叠，不含参数），
        /// 不应出现枚举名称字符串。
        /// (int)UserRole.Admin 是编译期常量，C# 编译器会将整个表达式直接折叠为 ConstantExpression(1)。
        /// </summary>
        [Fact]
        public void Enum_InWhereClause_ShouldBeTranslatedAsInteger()
        {
            var cmd = MySqlUserEx()
                .Where(x => x.RoleType == (int)UserRole.Admin)
                .ToSql();

            // 编译期常量 (int)UserRole.Admin == 1，应嵌入 SQL 文本中
            Assert.Contains("1", cmd.Text);
            // SQL 文本或参数中均不应出现枚举名称字符串
            Assert.DoesNotContain("Admin", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 枚举变量先预先 cast 为 int 再参与比较：值应出现在参数字典中（而非 SQL 文本）。
        /// 运行期变量通过参数化查询传递，值在 Parameters 中，SQL 文本中是参数占位符。
        /// </summary>
        [Fact]
        public void Enum_Variable_InWhereClause_ShouldBeParameter()
        {
            // 先将枚举转为 int，再在表达式中使用 int 变量
            int roleValue = (int)UserRole.Admin; // == 1

            var cmd = MySqlUserEx()
                .Where(x => x.RoleType == roleValue)
                .ToSql();

            Assert.NotNull(cmd);
            // 变量参数化查询：应有 WHERE 子句
            Assert.Contains("WHERE", cmd.Text, StringComparison.OrdinalIgnoreCase);
            // 参数值 1 应在 Parameters 中
            Assert.Contains(cmd.Parameters.Values, v => Equals(v, 1));
        }

        /// <summary>
        /// 将枚举变量直接 cast 为 int 在 LINQ 表达式中比较：
        /// x.RoleType == (int)role 中 (int)role 是 Convert(MemberAccess, Int32)。
        /// 框架生成带参数的 WHERE 子句，但参数值保留枚举原始类型（UserRole），
        /// 而非转换后的 int。这是一个已知的边界行为（Risk: Med）。
        /// </summary>
        [Fact]
        public void Enum_DirectCast_Variable_InWhere_ParameterIsEnum_NotInt_KnownBehavior()
        {
            UserRole role = UserRole.Admin;

            var cmd = MySqlUserEx()
                .Where(x => x.RoleType == (int)role)
                .ToSql();

            Assert.NotNull(cmd);
            // (int)role 是运行期转换，应生成带参数的 WHERE 子句
            Assert.Contains("WHERE", cmd.Text, StringComparison.OrdinalIgnoreCase);
            // 已知行为：框架将 Convert 节点的操作数值（UserRole 枚举）直接存入参数，未转换为 int
            // 实际传入数据库的是 UserRole.Admin，数据库驱动会将枚举隐式转换为整数
            Assert.Contains(cmd.Parameters.Values, v => Equals(v, UserRole.Admin));
        }

        // ══════════════════════════════════════════════
        // 7. 复杂布尔表达式 && / || 优先级括号
        // ══════════════════════════════════════════════

        /// <summary>
        /// (A && B) || C 应生成正确括号，避免优先级错误。
        /// </summary>
        [Fact]
        public void AndAlso_OrElse_ShouldGenerateCorrectParentheses()
        {
            var cmd = MySqlUsers()
                .Where(x => (x.Id > 1 && x.Name != null) || x.IsAdministrator)
                .ToSql();

            Assert.NotNull(cmd);
            Assert.Contains("(", cmd.Text);
            Assert.Contains(")", cmd.Text);
            Assert.Contains("OR", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("AND", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// A || (B && C) 应生成正确括号。
        /// </summary>
        [Fact]
        public void OrElse_AndAlso_ShouldGenerateCorrectParentheses()
        {
            var cmd = MySqlUsers()
                .Where(x => x.IsAdministrator || (x.Id > 1 && x.Name != null))
                .ToSql();

            Assert.NotNull(cmd);
            Assert.Contains("(", cmd.Text);
            Assert.Contains(")", cmd.Text);
            Assert.Contains("OR", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("AND", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 单一条件 A && B 不应有多余括号（可选：至少不抛异常，SQL 语义正确）。
        /// </summary>
        [Fact]
        public void SimpleAndAlso_ShouldProduceValidSql()
        {
            var cmd = MySqlUsers()
                .Where(x => x.Id > 0 && x.Name != null)
                .ToSql();

            Assert.NotNull(cmd);
            Assert.Contains("AND", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 三层嵌套 AND/OR 表达式应同时出现 AND、OR 关键字和括号，保留布尔优先级。
        /// </summary>
        [Fact]
        public void ThreeLevel_AndOr_ShouldPreserveOperatorsAndGrouping()
        {
            var cmd = MySqlUsers()
                .Where(x => (x.Id > 0 && x.IsAdministrator) || (x.Id < 100 && x.Name != null) || x.Nullable == null)
                .ToSql();

            Assert.NotNull(cmd);
            Assert.Contains("WHERE", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("AND", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("OR", cmd.Text, StringComparison.OrdinalIgnoreCase);
            // 括号必须出现以保持 AND > OR 优先级
            Assert.Contains("(", cmd.Text);
            Assert.Contains(")", cmd.Text);
        }

        // ══════════════════════════════════════════════
        // 8. 嵌套 Select / 投影边界
        // ══════════════════════════════════════════════

        /// <summary>
        /// Select 单字段投影应生成只包含该字段的 SELECT。
        /// </summary>
        [Fact]
        public void Select_SingleField_ShouldContainOnlyThatField()
        {
            var cmd = MySqlUsers()
                .Select(x => x.Id)
                .ToSql();

            Assert.Contains("`id`", cmd.Text);
        }

        /// <summary>
        /// Select 匿名类型多字段投影，生成的 SQL 应包含多个字段。
        /// </summary>
        [Fact]
        public void Select_AnonymousType_ShouldContainMultipleFields()
        {
            var cmd = MySqlUsers()
                .Select(x => new { x.Id, x.Name })
                .ToSql();

            Assert.Contains("`id`", cmd.Text);
            Assert.Contains("`name`", cmd.Text);
        }

        /// <summary>
        /// Select 后再次 Select 应抛出 DSyntaxErrorException（单个脚本不支持多次 Select）。
        /// </summary>
        [Fact]
        public void Select_Twice_ShouldThrowDSyntaxError()
        {
            Assert.Throws<DSyntaxErrorException>(() =>
            {
                MySqlUsers()
                    .Select(x => new { x.Id })
                    .Select(x => x.Id)
                    .ToSql();
            });
        }

        /// <summary>
        /// Where → Select（先过滤再投影）是合法的，不应抛异常。
        /// </summary>
        [Fact]
        public void Where_ThenSelect_ShouldProduceValidSql()
        {
            var cmd = MySqlUsers()
                .Where(x => x.Id > 0)
                .Select(x => x.Name)
                .ToSql();

            Assert.NotNull(cmd);
            Assert.Contains("`name`", cmd.Text);
            Assert.Contains("WHERE", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        // ══════════════════════════════════════════════
        // 9. Contains（集合 IN 子句）边界
        // ══════════════════════════════════════════════

        /// <summary>
        /// List.Contains 空集合应生成 AlwaysFalse（1=0 或等效），不生成 IN 子句。
        /// </summary>
        [Fact]
        public void ListContains_EmptyList_ShouldGenerateAlwaysFalse()
        {
            var ids = new List<int>();

            var cmd = MySqlUsers()
                .Where(x => ids.Contains(x.Id))
                .ToSql();

            // 空集合时，框架生成 AlwaysFalse，WHERE 中不应有 " IN " 操作符（空格确保不匹配字段名中的 in）
            Assert.NotNull(cmd);
            // 用独立空格检查 IN 关键字，避免与列名 IsAdministrator 中的 "in" 子串误匹配
            Assert.DoesNotContain(" IN ", cmd.Text, StringComparison.Ordinal);
        }

        /// <summary>
        /// List.Contains 单元素应生成 IN (value) 子句。
        /// </summary>
        [Fact]
        public void ListContains_SingleElement_ShouldGenerateIn()
        {
            var ids = new List<int> { 42 };

            var cmd = MySqlUsers()
                .Where(x => ids.Contains(x.Id))
                .ToSql();

            Assert.Contains("IN", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("42", cmd.Text);
        }

        /// <summary>
        /// List.Contains 多元素应生成 IN (v1, v2, ...) 子句。
        /// </summary>
        [Fact]
        public void ListContains_MultipleElements_ShouldGenerateIn()
        {
            var ids = new List<int> { 1, 2, 3 };

            var cmd = MySqlUsers()
                .Where(x => ids.Contains(x.Id))
                .ToSql();

            Assert.Contains("IN", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("1", cmd.Text);
            Assert.Contains("2", cmd.Text);
            Assert.Contains("3", cmd.Text);
        }

        // ══════════════════════════════════════════════
        // 10. Any / All 子查询边界
        // ══════════════════════════════════════════════

        /// <summary>
        /// Any() 无参数版本应生成 EXISTS 子查询。
        /// </summary>
        [Fact]
        public void Any_WithoutPredicate_ShouldGenerateExists()
        {
            var cmd = MySqlUsers()
                .ToSql(q => q.Any());

            Assert.Contains("EXISTS", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Any(predicate) 带条件版本应生成带 WHERE 的 EXISTS 子查询。
        /// </summary>
        [Fact]
        public void Any_WithPredicate_ShouldGenerateExistsWithWhere()
        {
            var cmd = MySqlUsers()
                .ToSql(q => q.Any(x => x.Id > 0));

            Assert.Contains("EXISTS", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// All(predicate) 应生成 NOT EXISTS 子查询。
        /// </summary>
        [Fact]
        public void All_WithPredicate_ShouldGenerateNotExists()
        {
            var cmd = MySqlUsers()
                .ToSql(q => q.All(x => x.Id > 0));

            Assert.Contains("NOT", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("EXISTS", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        // ══════════════════════════════════════════════
        // 11. First / Single 不带排序应抛异常
        // ══════════════════════════════════════════════

        /// <summary>
        /// First() 不带 OrderBy 应抛出 DSyntaxErrorException（必须排序）。
        /// </summary>
        [Fact]
        public void First_WithoutOrderBy_ShouldThrowDSyntaxError()
        {
            Assert.Throws<DSyntaxErrorException>(() =>
            {
                MySqlUsers()
                    .ToSql(q => q.First());
            });
        }

        /// <summary>
        /// Single() 不带 OrderBy 不应抛异常（框架对 Single 不强制要求排序，
        /// 只有 First/Last/Skip/Take 需要排序）。此测试记录这一设计决策。
        /// </summary>
        [Fact]
        public void Single_WithoutOrderBy_ShouldNotThrow()
        {
            // Single 不要求 OrderBy，这与 First 不同
            var cmd = MySqlUsers()
                .ToSql(q => q.Single());

            Assert.NotNull(cmd);
            Assert.False(string.IsNullOrWhiteSpace(cmd.Text));
        }

        /// <summary>
        /// First() 带 OrderBy 应合法翻译，生成 LIMIT 1。
        /// </summary>
        [Fact]
        public void First_WithOrderBy_ShouldGenerateLimitOne()
        {
            var cmd = MySqlUsers()
                .ToSql(q => q.OrderBy(x => x.Id).First());

            Assert.NotNull(cmd);
            Assert.Contains("LIMIT 1", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        // ══════════════════════════════════════════════
        // 12. 条件折叠：常量布尔表达式
        // ══════════════════════════════════════════════

        /// <summary>
        /// Where(x => true) 常量条件应被折叠：SQL 中不应出现 WHERE 子句（恒真等同于无过滤）。
        /// </summary>
        [Fact]
        public void ConstantTrue_InWhere_ShouldBeOptimizedAway()
        {
            var cmd = MySqlUsers()
                .Where(x => true)
                .ToSql();

            Assert.NotNull(cmd);
            // x => true 折叠后，WHERE 子句应被完全消除
            Assert.DoesNotContain("WHERE", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Where(x => false) 常量条件应被折叠为 AlwaysFalse：SQL 包含 WHERE 子句（恒假）。
        /// </summary>
        [Fact]
        public void ConstantFalse_InWhere_ShouldProduceWhereClause()
        {
            var cmd = MySqlUsers()
                .Where(x => false)
                .ToSql();

            Assert.NotNull(cmd);
            // x => false 应保留 WHERE 子句以确保查询返回 0 行
            Assert.Contains("WHERE", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        // ══════════════════════════════════════════════
        // 13. 字符串 Trim / ToUpper / ToLower
        // ══════════════════════════════════════════════

        /// <summary>
        /// Select 中 ToUpper 应生成 UPPER() 函数。
        /// </summary>
        [Fact]
        public void StringToUpper_InSelect_ShouldGenerateUpper()
        {
            var cmd = MySqlUsers()
                .Select(x => x.Name.ToUpper())
                .ToSql();

            Assert.Contains("UPPER", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Select 中 ToLower 应生成 LOWER() 函数。
        /// </summary>
        [Fact]
        public void StringToLower_InSelect_ShouldGenerateLower()
        {
            var cmd = MySqlUsers()
                .Select(x => x.Name.ToLower())
                .ToSql();

            Assert.Contains("LOWER", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        // ══════════════════════════════════════════════
        // 14. Count 聚合
        // ══════════════════════════════════════════════

        /// <summary>
        /// Count() 不带条件应生成 SELECT COUNT(*)。
        /// </summary>
        [Fact]
        public void Count_WithoutPredicate_ShouldGenerateCountStar()
        {
            var cmd = MySqlUsers()
                .ToSql(q => q.Count());

            Assert.Contains("COUNT", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Count(predicate) 带条件应生成带 WHERE 的 COUNT。
        /// </summary>
        [Fact]
        public void Count_WithPredicate_ShouldGenerateCountWithWhere()
        {
            var cmd = MySqlUsers()
                .ToSql(q => q.Count(x => x.IsAdministrator));

            Assert.Contains("COUNT", cmd.Text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("WHERE", cmd.Text, StringComparison.OrdinalIgnoreCase);
        }

        // ══════════════════════════════════════════════
        // 15. 数据分片边界
        // ══════════════════════════════════════════════

        /// <summary>
        /// 重复指定分片键应抛出 DSyntaxErrorException。
        /// </summary>
        [Fact]
        public void DataSharding_Duplicate_ShouldThrowDSyntaxError()
        {
            Assert.Throws<DSyntaxErrorException>(() =>
            {
                LinqAnalyzer.From<UserSharding>(DatabaseEngine.MySQL, new MySqlAdapter())
                    .DataSharding("001")
                    .DataSharding("002")
                    .ToSql();
            });
        }

        /// <summary>
        /// 普通表使用 DataSharding 应抛出 InvalidOperationException。
        /// </summary>
        [Fact]
        public void DataSharding_OnNonShardingTable_ShouldThrowInvalidOperation()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                MySqlUsers()
                    .DataSharding("001")
                    .ToSql();
            });
        }
    }

    // ══════════════════════════════════════════════
    // 辅助枚举（用于枚举翻译测试）
    // ══════════════════════════════════════════════

    /// <summary>
    /// 测试用角色枚举。
    /// </summary>
    public enum UserRole
    {
        None = 0,
        Admin = 1,
        Guest = 2
    }
}
