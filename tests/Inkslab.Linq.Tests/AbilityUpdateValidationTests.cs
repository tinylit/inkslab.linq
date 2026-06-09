using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Linq.Annotations;
using Inkslab.Linq.Enums;
using Inkslab.Linq.MySql;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// UpdateTo 能力字段验证测试。
    /// 验证更新时：条件字段（Key / Version）和更新字段（SET 列）
    /// 均会执行 DataAnnotations 属性验证，违反时抛出 <see cref="ValidationException"/>。
    /// 所有测试均不依赖真实数据库连接。
    /// </summary>
    public class AbilityUpdateValidationTests
    {
        // ─────────────────────────────────────────────────────────────────
        // 测试实体
        // ─────────────────────────────────────────────────────────────────

        [Table("update_validation_test")]
        private class ValidatedProduct
        {
            [Key]
            [Field("id")]
            public int Id { get; set; }

            [Required]
            [MaxLength(100)]
            [Field("name")]
            public string Name { get; set; }

            [Range(0, 9999)]
            [Field("price")]
            public decimal Price { get; set; }

            [MaxLength(500)]
            [Field("description")]
            public string Description { get; set; }
        }

        // ─────────────────────────────────────────────────────────────────
        // 辅助
        // ─────────────────────────────────────────────────────────────────

        private static RepositoryRouter<ValidatedProduct> CreateRouter()
        {
            return new RepositoryRouter<ValidatedProduct>(
                new NeverReachedExecutor(),
                new DbStrictAdapter(DatabaseEngine.MySQL, new MySqlAdapter()),
                new StubDatabaseStrings(),
                NullLogger<RepositoryRouter<ValidatedProduct>>.Instance
            );
        }

        // ─────────────────────────────────────────────────────────────────
        // 1. SET 字段（更新字段）验证
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 更新字段 Name 标注 [Required]，传入 null 时应抛 ValidationException。
        /// </summary>
        [Fact]
        public void UpdateTo_RequiredUpdateField_NullValue_ThrowsValidationException()
        {
            var router = CreateRouter();
            var entity = new ValidatedProduct { Id = 1, Name = null };

            Assert.Throws<ValidationException>(() =>
                router.AsUpdateable(new[] { entity }, null, null).Execute());
        }

        /// <summary>
        /// 更新字段 Name 标注 [MaxLength(100)]，超长字符串应抛 ValidationException。
        /// </summary>
        [Fact]
        public void UpdateTo_MaxLengthUpdateField_Exceeded_ThrowsValidationException()
        {
            var router = CreateRouter();
            var entity = new ValidatedProduct
            {
                Id = 1,
                Name = new string('X', 101), // 超出 MaxLength(100)
                Price = 10
            };

            Assert.Throws<ValidationException>(() =>
                router.AsUpdateable(new[] { entity }, null, null).Execute());
        }

        /// <summary>
        /// 更新字段 Price 标注 [Range(0, 9999)]，负数应抛 ValidationException。
        /// </summary>
        [Fact]
        public void UpdateTo_RangeUpdateField_BelowMin_ThrowsValidationException()
        {
            var router = CreateRouter();
            var entity = new ValidatedProduct { Id = 1, Name = "ok", Price = -1 };

            Assert.Throws<ValidationException>(() =>
                router.AsUpdateable(new[] { entity }, null, null).Execute());
        }

        /// <summary>
        /// 更新字段 Price 超过上限 9999 时应抛 ValidationException。
        /// </summary>
        [Fact]
        public void UpdateTo_RangeUpdateField_AboveMax_ThrowsValidationException()
        {
            var router = CreateRouter();
            var entity = new ValidatedProduct { Id = 1, Name = "ok", Price = 10000 };

            Assert.Throws<ValidationException>(() =>
                router.AsUpdateable(new[] { entity }, null, null).Execute());
        }

        /// <summary>
        /// Description 标注 [MaxLength(500)]，超长字符串应抛 ValidationException。
        /// </summary>
        [Fact]
        public void UpdateTo_MaxLengthOptionalField_Exceeded_ThrowsValidationException()
        {
            var router = CreateRouter();
            var entity = new ValidatedProduct
            {
                Id = 1,
                Name = "ok",
                Price = 100,
                Description = new string('D', 501) // 超出 MaxLength(500)
            };

            Assert.Throws<ValidationException>(() =>
                router.AsUpdateable(new[] { entity }, null, null).Execute());
        }

        // ─────────────────────────────────────────────────────────────────
        // 2. 字段范围限定（Set / SetExcept）时验证仍然有效
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 使用 Set() 限定只更新 Name，但 Name 为空 → 应抛 ValidationException。
        /// </summary>
        [Fact]
        public void UpdateTo_SetLimitedFields_RequiredViolation_ThrowsValidationException()
        {
            var router = CreateRouter();
            var entity = new ValidatedProduct { Id = 1, Name = null };

            Assert.Throws<ValidationException>(() =>
                router.AsUpdateable(new[] { entity }, null, null)
                      .Set(x => x.Name)
                      .Execute());
        }

        // ─────────────────────────────────────────────────────────────────
        // 3. 所有字段有效时 → 不抛 ValidationException，执行到达 DB 调用层
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 所有更新字段有效时，验证通过，执行流进入 DB 层（NeverReachedExecutor 在此抛出
        /// NotImplementedException 标志"验证已通过，SQL 已生成"）。
        /// </summary>
        [Fact]
        public void UpdateTo_AllFieldsValid_NoValidationException_ReachesExecutor()
        {
            var router = CreateRouter();
            var entity = new ValidatedProduct { Id = 1, Name = "valid name", Price = 99, Description = "ok" };

            // 验证通过后进入 NeverReachedExecutor → 抛 NotImplementedException
            // 这证明 ValidationException 没有被抛出
            Assert.Throws<NotImplementedException>(() =>
                router.AsUpdateable(new[] { entity }, null, null).Execute());
        }

        /// <summary>
        /// 批量（多实体）更新：第二个实体违反 [Required] 时抛 ValidationException，
        /// 第一个实体不影响（验证在 SQL 生成阶段逐行发生）。
        /// </summary>
        [Fact]
        public void UpdateTo_BatchEntities_SecondEntityViolatesRequired_ThrowsValidationException()
        {
            var router = CreateRouter();
            var entities = new[]
            {
                new ValidatedProduct { Id = 1, Name = "valid", Price = 10 },
                new ValidatedProduct { Id = 2, Name = null, Price = 20 }, // 违反 [Required]
            };

            Assert.Throws<ValidationException>(() =>
                router.AsUpdateable(entities, null, null).Execute());
        }

        // ─────────────────────────────────────────────────────────────────
        // 4. 枚举字段验证（回归：列映射会把枚举转为基础类型，
        //    校验时必须使用属性原始值，否则 Validator 抛类型不匹配 ArgumentException）
        // ─────────────────────────────────────────────────────────────────

        private enum OrderStatus
        {
            Pending = 0,
            Paid = 1,
            Shipped = 2,
        }

        private enum OrderKind
        {
            Normal = 1,
            Gift = 2,
        }

        [Table("update_validation_enum_test")]
        private class ValidatedOrder
        {
            [Key]
            [Field("id")]
            public int Id { get; set; }

            [Field("status")]
            public OrderStatus Status { get; set; }

            [Field("kind")]
            public OrderKind? Kind { get; set; }
        }

        private static RepositoryRouter<ValidatedOrder> CreateOrderRouter()
        {
            return new RepositoryRouter<ValidatedOrder>(
                new NeverReachedExecutor(),
                new DbStrictAdapter(DatabaseEngine.MySQL, new MySqlAdapter()),
                new StubDatabaseStrings(),
                NullLogger<RepositoryRouter<ValidatedOrder>>.Instance
            );
        }

        /// <summary>
        /// 更新含（非空）枚举字段的实体：列映射会把枚举转为基础类型，
        /// 但 DataAnnotations 校验必须使用属性原始枚举值。
        /// 修复前会抛 ArgumentException（“值必须是 OrderStatus 类型”），
        /// 修复后校验通过、执行流到达 DB 层（NeverReachedExecutor 抛 NotImplementedException）。
        /// </summary>
        [Fact]
        public void UpdateTo_EnumField_ValidValue_ReachesExecutor()
        {
            var router = CreateOrderRouter();
            var entity = new ValidatedOrder
            {
                Id = 1,
                Status = OrderStatus.Paid,
                Kind = OrderKind.Gift,
            };

            Assert.Throws<NotImplementedException>(() =>
                router.AsUpdateable(new[] { entity }, null, null).Execute());
        }

        /// <summary>
        /// 可空枚举字段取值为 null 时，校验同样不应因类型不匹配而抛 ArgumentException。
        /// </summary>
        [Fact]
        public void UpdateTo_NullableEnumField_NullValue_ReachesExecutor()
        {
            var router = CreateOrderRouter();
            var entity = new ValidatedOrder
            {
                Id = 1,
                Status = OrderStatus.Pending,
                Kind = null,
            };

            Assert.Throws<NotImplementedException>(() =>
                router.AsUpdateable(new[] { entity }, null, null).Execute());
        }

        // ─────────────────────────────────────────────────────────────────
        // Stubs
        // ─────────────────────────────────────────────────────────────────

        private sealed class StubDatabaseStrings : IDatabaseStrings
        {
            public DatabaseEngine Engine => DatabaseEngine.MySQL;
            public string Strings => "Server=stub;";
        }

        /// <summary>
        /// 不应被调用的执行器：如果验证逻辑未在到达 DB 层之前拦截，
        /// Execute() 会被调用并抛 NotImplementedException，
        /// 这在测试中用于区分"验证通过到达 DB"和"未到达 DB"。
        /// </summary>
        private sealed class NeverReachedExecutor : IDatabaseExecutor
        {
            public int Execute(IConnection databaseStrings, CommandSql commandSql)
                => throw new NotImplementedException("DB layer reached.");

            public Task<int> ExecuteAsync(IConnection databaseStrings, CommandSql commandSql, CancellationToken cancellationToken = default)
                => throw new NotImplementedException("DB layer reached.");

            public T Read<T>(IConnection databaseStrings, CommandSql<T> commandSql)
                => throw new NotImplementedException();
            public Task<T> ReadAsync<T>(IConnection databaseStrings, CommandSql<T> commandSql, CancellationToken cancellationToken = default)
                => throw new NotImplementedException();
            public System.Collections.Generic.List<T> Query<T>(IConnection databaseStrings, CommandSql commandSql)
                => throw new NotImplementedException();
            public IAsyncEnumerable<T> QueryAsync<T>(IConnection databaseStrings, CommandSql commandSql)
                => throw new NotImplementedException();
            public IDbGridReader QueryMultiple(IConnection databaseStrings, CommandSql commandSql)
                => throw new NotImplementedException();
            public Task<IAsyncDbGridReader> QueryMultipleAsync(IConnection databaseStrings, CommandSql commandSql, CancellationToken cancellationToken = default)
                => throw new NotImplementedException();
            public int WriteToServer(IConnection databaseStrings, DataTable dataTable, int? commandTimeout = null)
                => throw new NotImplementedException();
            public Task<int> WriteToServerAsync(IConnection databaseStrings, DataTable dataTable, int? commandTimeout = null, CancellationToken cancellationToken = default)
                => throw new NotImplementedException();
            public int ExecuteMultiple(IConnection databaseStrings, Action<IMultipleExecutor> multipleAction, int? commandTimeout = null)
                => throw new NotImplementedException();
            public Task<int> ExecuteMultipleAsync(IConnection databaseStrings, Func<IAsyncMultipleExecutor, Task> multipleAction, int? commandTimeout = null, CancellationToken cancellationToken = default)
                => throw new NotImplementedException();
        }
    }
}
