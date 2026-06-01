using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Linq.Abilities;
using Inkslab.Linq.Annotations;
using Inkslab.Linq.Enums;
using Inkslab.Linq.MySql;
using Inkslab.Linq.PostgreSQL;
using Inkslab.Linq.SqlServer;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// INSERT 自增 ID 反写能力测试。
    /// 验证：对带有 [Key]+[DatabaseGenerated] 的自增主键，Execute() 执行后将数据库生成的 ID
    /// 反写到实体对象上。不依赖真实数据库连接，使用伪执行器捕获调用。
    /// </summary>
    public class AbilityPopulateIdentityTests
    {
        // ─────────────────────────────────────────────────────────────────
        // 辅助：构建 RepositoryRouter，注入伪执行器
        // ─────────────────────────────────────────────────────────────────

        private static RepositoryRouter<User> CreateMySqlRouter(IdCapturingExecutor executor)
        {
            return new RepositoryRouter<User>(
                executor,
                new DbStrictAdapter(DatabaseEngine.MySQL, new MySqlAdapter()),
                new StubDatabaseStrings(DatabaseEngine.MySQL),
                NullLogger<RepositoryRouter<User>>.Instance
            );
        }

        private static RepositoryRouter<User> CreatePostgreSqlRouter(IdCapturingExecutor executor)
        {
            return new RepositoryRouter<User>(
                executor,
                new DbStrictAdapter(DatabaseEngine.PostgreSQL, new PostgreSQLAdapter()),
                new StubDatabaseStrings(DatabaseEngine.PostgreSQL),
                NullLogger<RepositoryRouter<User>>.Instance
            );
        }

        private static RepositoryRouter<User> CreateSqlServerRouter(IdCapturingExecutor executor)
        {
            return new RepositoryRouter<User>(
                executor,
                new DbStrictAdapter(DatabaseEngine.SqlServer, new SqlServerAdapter()),
                new StubDatabaseStrings(DatabaseEngine.SqlServer),
                NullLogger<RepositoryRouter<User>>.Instance
            );
        }

        // ─────────────────────────────────────────────────────────────────
        // 1. MySQL —— 单实体插入 → ID 反写 + SQL 含 LAST_INSERT_ID()
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// MySQL 单实体 INSERT + 显式 PopulateIdentity()：Execute() 后，实体 Id 被设为数据库生成值，
        /// 并且生成的 SQL 包含 "LAST_INSERT_ID()"。
        /// </summary>
        [Fact]
        public void MySQL_SingleEntity_PopulateIdentity_WritesBackId_AndSqlContainsLastInsertId()
        {
            var executor = new IdCapturingExecutor(expectedId: 99);
            var router = CreateMySqlRouter(executor);
            var user = new User { Name = "alice" };

            router.AsInsertable(new[] { user }, ignore: false, shardingKey: null, commandTimeout: null)
                  .PopulateIdentity()
                  .Execute();

            Assert.Equal(99, user.Id);
            Assert.NotNull(executor.LastSqlText);
            Assert.Contains("LAST_INSERT_ID()", executor.LastSqlText, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// MySQL 单实体 INSERT + 显式 PopulateIdentity()（异步路径）：ExecuteAsync() 后，实体 Id 被正确反写。
        /// </summary>
        [Fact]
        public async Task MySQL_SingleEntity_PopulateIdentity_ExecuteAsync_WritesBackIdAsync()
        {
            var executor = new IdCapturingExecutor(expectedId: 77);
            var router = CreateMySqlRouter(executor);
            var user = new User { Name = "bob" };

            await router.AsInsertable(new[] { user }, ignore: false, shardingKey: null, commandTimeout: null)
                        .PopulateIdentity()
                        .ExecuteAsync();

            Assert.Equal(77, user.Id);
        }

        // ─────────────────────────────────────────────────────────────────
        // 2. PostgreSQL —— 单/多实体插入 → ID 反写 + SQL 含 RETURNING
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// PostgreSQL 单实体 INSERT + 显式 PopulateIdentity()：Execute() 后，实体 Id 被反写，
        /// SQL 包含 "RETURNING"。
        /// </summary>
        [Fact]
        public void PostgreSQL_SingleEntity_PopulateIdentity_WritesBackId_AndSqlContainsReturning()
        {
            var executor = new IdCapturingExecutor(expectedId: 55);
            var router = CreatePostgreSqlRouter(executor);
            var user = new User { Name = "charlie" };

            router.AsInsertable(new[] { user }, ignore: false, shardingKey: null, commandTimeout: null)
                  .PopulateIdentity()
                  .Execute();

            Assert.Equal(55, user.Id);
            Assert.NotNull(executor.LastSqlText);
            Assert.Contains("RETURNING", executor.LastSqlText, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// PostgreSQL 多实体批量 INSERT + 显式 PopulateIdentity()：所有实体 Id 均被正确反写（RETURNING 返回多行）。
        /// </summary>
        [Fact]
        public void PostgreSQL_MultipleEntities_PopulateIdentity_WritesBackAllIds()
        {
            var executor = new IdCapturingExecutor(new List<long> { 10, 11, 12 });
            var router = CreatePostgreSqlRouter(executor);
            var users = new[]
            {
                new User { Name = "u1" },
                new User { Name = "u2" },
                new User { Name = "u3" },
            };

            router.AsInsertable(users, ignore: false, shardingKey: null, commandTimeout: null)
                  .PopulateIdentity()
                  .Execute();

            Assert.Equal(10, users[0].Id);
            Assert.Equal(11, users[1].Id);
            Assert.Equal(12, users[2].Id);
        }

        // ─────────────────────────────────────────────────────────────────
        // 3. SQL Server —— 单/多实体插入 → ID 反写 + SQL 含 OUTPUT INSERTED
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// SQL Server 单实体 INSERT + 显式 PopulateIdentity()：实体 Id 被反写，
        /// SQL 包含 "OUTPUT INSERTED"。
        /// </summary>
        [Fact]
        public void SqlServer_SingleEntity_PopulateIdentity_WritesBackId_AndSqlContainsOutputInserted()
        {
            var executor = new IdCapturingExecutor(expectedId: 200);
            var router = CreateSqlServerRouter(executor);
            var user = new User { Name = "dave" };

            router.AsInsertable(new[] { user }, ignore: false, shardingKey: null, commandTimeout: null)
                  .PopulateIdentity()
                  .Execute();

            Assert.Equal(200, user.Id);
            Assert.NotNull(executor.LastSqlText);
            Assert.Contains("OUTPUT INSERTED", executor.LastSqlText, StringComparison.OrdinalIgnoreCase);
        }

        // ─────────────────────────────────────────────────────────────────
        // 4. 默认未开启 PopulateIdentity() —— 任何路径都不反写 ID
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 未调用 PopulateIdentity() 时，Ignore 路径不反写 ID，实体 Id 保持为 0。
        /// </summary>
        [Fact]
        public void Default_IgnoreMode_DoesNotWriteBackId()
        {
            var executor = new IdCapturingExecutor(expectedId: 999);
            var router = CreateMySqlRouter(executor);
            var user = new User { Name = "eve" };

            router.AsInsertable(new[] { user }, ignore: true, shardingKey: null, commandTimeout: null)
                  .Execute();

            Assert.Equal(0, user.Id);
            Assert.False(executor.QueryWasCalled);
            Assert.False(executor.QueryMultipleWasCalled);
        }

        /// <summary>
        /// 行为变更：未调用 PopulateIdentity() 时，即便实体有自增主键、非 Ignore、普通批量，
        /// 也不应触发反写 IO。验证显式开关的兜底语义。
        /// </summary>
        [Fact]
        public void Default_NoPopulateIdentity_HasAutoIncrementKey_DoesNotCallQuery()
        {
            var executor = new IdCapturingExecutor(expectedId: 42);
            var router = CreateMySqlRouter(executor);
            var user = new User { Name = "default" };

            router.AsInsertable(new[] { user }, ignore: false, shardingKey: null, commandTimeout: null)
                  .Execute();

            Assert.Equal(0, user.Id);
            Assert.False(executor.QueryWasCalled);
            Assert.False(executor.QueryMultipleWasCalled);
        }

        // ─────────────────────────────────────────────────────────────────
        // 5. 无自增主键的实体 —— 不调用 Query/QueryMultiple
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 无 [DatabaseGenerated] 标记的主键实体，Insert 不触发 ID 反写逻辑，
        /// 不调用 Query&lt;long&gt; / QueryMultiple。
        /// </summary>
        [Fact]
        public void NoAutoIncrementKey_DoesNotCallQueryForIds()
        {
            // UserEx 有手动主键（无 [DatabaseGenerated]）
            var executor = new IdCapturingExecutor(expectedId: 888);
            var userExRouter = new RepositoryRouter<UserEx>(
                executor,
                new DbStrictAdapter(DatabaseEngine.MySQL, new MySqlAdapter()),
                new StubDatabaseStrings(DatabaseEngine.MySQL),
                NullLogger<RepositoryRouter<UserEx>>.Instance
            );
            var userEx = new UserEx { Id = 5, RoleType = 1 };

            // 不应调用 Query<long> / QueryMultiple，走普通 Execute 路径
            userExRouter.AsInsertable(new[] { userEx }, ignore: false, shardingKey: null, commandTimeout: null)
                        .Execute();

            Assert.False(executor.QueryWasCalled);
            Assert.False(executor.QueryMultipleWasCalled);
        }

        // ─────────────────────────────────────────────────────────────────
        // 6. 复合自增主键 —— 反写路径不应启用
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 实体存在多个 [Key]+[DatabaseGenerated] 字段时，反写目标不唯一，
        /// 必须放弃自增反写，走普通 INSERT 路径：不调用 Query/QueryMultiple，
        /// 生成的 SQL 也不含 LAST_INSERT_ID()/RETURNING/OUTPUT INSERTED。
        /// </summary>
        [Fact]
        public void MultipleAutoIncrementKeys_DoesNotWriteBack()
        {
            var executor = new IdCapturingExecutor(expectedId: 1);
            var router = new RepositoryRouter<UserCompositeAutoIncrKey>(
                executor,
                new DbStrictAdapter(DatabaseEngine.MySQL, new MySqlAdapter()),
                new StubDatabaseStrings(DatabaseEngine.MySQL),
                NullLogger<RepositoryRouter<UserCompositeAutoIncrKey>>.Instance
            );
            var entity = new UserCompositeAutoIncrKey { Name = "x" };

            router.AsInsertable(new[] { entity }, ignore: false, shardingKey: null, commandTimeout: null)
                  .Execute();

            Assert.False(executor.QueryWasCalled);
            Assert.False(executor.QueryMultipleWasCalled);
        }

        [Table("user_composite_auto_incr")]
        public sealed class UserCompositeAutoIncrKey
        {
            [Key]
            [Field("id_a")]
            [DatabaseGenerated]
            public long IdA { get; set; }

            [Key]
            [Field("id_b")]
            [DatabaseGenerated]
            public long IdB { get; set; }

            [Field("name")]
            public string Name { get; set; }
        }

        // ─────────────────────────────────────────────────────────────────
        // 7. 复合主键中仅一个 [DatabaseGenerated] —— 新规则下也不识别为自增
        //    （旧规则会取唯一的只读主键反写，但复合主键场景下 RETURNING/LAST_INSERT_ID 的回填目标不可靠）
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 收紧后的规则：自增识别条件为「Keys.Count == 1 且该 key ∈ ReadOnlys」。
        /// 复合主键中即便只有一个键被标记 [DatabaseGenerated]，也不再走反写路径。
        /// </summary>
        [Fact]
        public void CompositeKey_WithSingleDatabaseGenerated_DoesNotWriteBack()
        {
            var executor = new IdCapturingExecutor(expectedId: 1);
            var router = new RepositoryRouter<UserCompositePartialAutoIncrKey>(
                executor,
                new DbStrictAdapter(DatabaseEngine.MySQL, new MySqlAdapter()),
                new StubDatabaseStrings(DatabaseEngine.MySQL),
                NullLogger<RepositoryRouter<UserCompositePartialAutoIncrKey>>.Instance
            );
            var entity = new UserCompositePartialAutoIncrKey { IdB = 7, Name = "y" };

            router.AsInsertable(new[] { entity }, ignore: false, shardingKey: null, commandTimeout: null)
                  .Execute();

            Assert.False(executor.QueryWasCalled);
            Assert.False(executor.QueryMultipleWasCalled);
        }

        [Table("user_composite_partial_auto_incr")]
        public sealed class UserCompositePartialAutoIncrKey
        {
            [Key]
            [Field("id_a")]
            [DatabaseGenerated]
            public long IdA { get; set; }

            [Key]
            [Field("id_b")]
            public long IdB { get; set; }

            [Field("name")]
            public string Name { get; set; }
        }

        // ─────────────────────────────────────────────────────────────────
        // 7b. 可空自增主键（long? / int?）—— BuildIdentitySetter 应解包 Nullable<T> 后回填
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 自增主键属性为可空整型（<c>long?</c>）时，回填仍应成功：BuildIdentitySetter 必须先解包
        /// Nullable&lt;T&gt; 的底层类型再做整型转换，否则会落入 Convert.ChangeType(value, typeof(long?))
        /// 在运行时抛 InvalidCastException。
        /// </summary>
        [Fact]
        public void NullableLongAutoIncrementKey_PopulateIdentity_WritesBackId()
        {
            var executor = new IdCapturingExecutor(expectedId: 123);
            var router = new RepositoryRouter<UserNullableLongAutoIncrKey>(
                executor,
                new DbStrictAdapter(DatabaseEngine.MySQL, new MySqlAdapter()),
                new StubDatabaseStrings(DatabaseEngine.MySQL),
                NullLogger<RepositoryRouter<UserNullableLongAutoIncrKey>>.Instance
            );
            var entity = new UserNullableLongAutoIncrKey { Name = "n" };

            router.AsInsertable(new[] { entity }, ignore: false, shardingKey: null, commandTimeout: null)
                  .PopulateIdentity()
                  .Execute();

            Assert.Equal(123L, entity.Id);
        }

        /// <summary>
        /// 自增主键属性为可空 <c>int?</c> 时，反写应解包到 int 后再转换并装回 Nullable&lt;int&gt;。
        /// </summary>
        [Fact]
        public void NullableIntAutoIncrementKey_PopulateIdentity_WritesBackId()
        {
            var executor = new IdCapturingExecutor(expectedId: 456);
            var router = new RepositoryRouter<UserNullableIntAutoIncrKey>(
                executor,
                new DbStrictAdapter(DatabaseEngine.MySQL, new MySqlAdapter()),
                new StubDatabaseStrings(DatabaseEngine.MySQL),
                NullLogger<RepositoryRouter<UserNullableIntAutoIncrKey>>.Instance
            );
            var entity = new UserNullableIntAutoIncrKey { Name = "n" };

            router.AsInsertable(new[] { entity }, ignore: false, shardingKey: null, commandTimeout: null)
                  .PopulateIdentity()
                  .Execute();

            Assert.Equal(456, entity.Id);
        }

        [Table("user_nullable_long_auto_incr")]
        public sealed class UserNullableLongAutoIncrKey
        {
            [Key]
            [Field("id")]
            [DatabaseGenerated]
            public long? Id { get; set; }

            [Field("name")]
            public string Name { get; set; }
        }

        [Table("user_nullable_int_auto_incr")]
        public sealed class UserNullableIntAutoIncrKey
        {
            [Key]
            [Field("id")]
            [DatabaseGenerated]
            public int? Id { get; set; }

            [Field("name")]
            public string Name { get; set; }
        }

        // ─────────────────────────────────────────────────────────────────
        // 8. PopulateIdentity() 但无可反写自增主键 —— fail-fast 抛 InvalidOperationException
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 显式调用 PopulateIdentity() 但实体不满足「单主键 + [DatabaseGenerated]」时，
        /// 立刻抛 InvalidOperationException（不等到 Execute），避免静默失败。
        /// </summary>
        [Fact]
        public void PopulateIdentity_NoAutoIncrementKey_ThrowsInvalidOperation()
        {
            var executor = new IdCapturingExecutor(expectedId: 1);
            var userExRouter = new RepositoryRouter<UserEx>(
                executor,
                new DbStrictAdapter(DatabaseEngine.MySQL, new MySqlAdapter()),
                new StubDatabaseStrings(DatabaseEngine.MySQL),
                NullLogger<RepositoryRouter<UserEx>>.Instance
            );
            var userEx = new UserEx { Id = 1, RoleType = 1 };

            var insertable = userExRouter.AsInsertable(
                new[] { userEx }, ignore: false, shardingKey: null, commandTimeout: null);

            Assert.Throws<InvalidOperationException>(() => insertable.PopulateIdentity());
        }

        // ─────────────────────────────────────────────────────────────────
        // 9. Bulk 路径（>100 行）+ PopulateIdentity() —— 拆批走 RETURNING/LAST_INSERT_ID
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// PostgreSQL Bulk 拆批反写：150 行 → 2 批（100 + 50），每批一次 Query&lt;long&gt; 调用，
        /// 所有实体 Id 按顺序反写。
        /// </summary>
        [Fact]
        public void PostgreSQL_Bulk_PopulateIdentity_BatchesIntoTwoQueries()
        {
            var batch1Ids = Enumerable.Range(1, 100).Select(i => (long)i).ToList();
            var batch2Ids = Enumerable.Range(101, 50).Select(i => (long)i).ToList();
            var executor = new IdCapturingExecutor(new[] { batch1Ids, batch2Ids });
            var router = CreatePostgreSqlRouter(executor);

            var users = Enumerable.Range(0, 150).Select(i => new User { Name = "u" + i }).ToArray();

            router.AsInsertable(users, ignore: false, shardingKey: null, commandTimeout: null)
                  .PopulateIdentity()
                  .Execute();

            Assert.Equal(2, executor.QueryCallCount);
            for (int i = 0; i < 150; i++)
            {
                Assert.Equal(i + 1, users[i].Id);
            }
        }

        /// <summary>
        /// MySQL 批量反写（多语句分批，零连续假设）：MySQL 无多行 RETURNING，但把多组「单行 INSERT + SELECT LAST_INSERT_ID()」
        /// 放进一条命令，单次 QueryMultiple 取回 2N 个有序结果集，每个标量紧跟自己那条单行 INSERT，逐行精确。
        /// 150 行、每行 1 个插入参数 → 分块 K=100 → 2 次 QueryMultiple（100+50）。故意用非连续 ID 证明不依赖连续性。
        /// </summary>
        [Fact]
        public void MySQL_Batch_PopulateIdentity_MultiStatementChunks()
        {
            var expectedIds = Enumerable.Range(1, 150).Select(i => (long)(i * 10)).ToList();
            var executor = IdCapturingExecutor.Scalar(
                ScalarChunk(expectedIds.Take(100).ToArray()),
                ScalarChunk(expectedIds.Skip(100).ToArray())
            );
            var router = CreateMySqlRouter(executor);
            var users = Enumerable.Range(0, 150).Select(i => new User { Name = "u" + i }).ToArray();

            router.AsInsertable(users, ignore: false, shardingKey: null, commandTimeout: null)
                  .PopulateIdentity()
                  .Execute();

            Assert.Equal(2, executor.QueryMultipleCallCount);
            for (int i = 0; i < 150; i++)
            {
                Assert.Equal(expectedIds[i], users[i].Id);
            }
        }

        /// <summary>
        /// MySQL 非 ignore ≤K 多行：一次 QueryMultiple 完成（不逐行往返），SQL 含 LAST_INSERT_ID()。
        /// </summary>
        [Fact]
        public void MySQL_MultiRow_PopulateIdentity_SingleRoundTrip()
        {
            var executor = IdCapturingExecutor.Scalar(ScalarChunk(7, 8, 9));
            var router = CreateMySqlRouter(executor);
            var users = new[]
            {
                new User { Name = "a" },
                new User { Name = "b" },
                new User { Name = "c" },
            };

            router.AsInsertable(users, ignore: false, shardingKey: null, commandTimeout: null)
                  .PopulateIdentity()
                  .Execute();

            Assert.Equal(1, executor.QueryMultipleCallCount);
            Assert.Equal(7, users[0].Id);
            Assert.Equal(8, users[1].Id);
            Assert.Equal(9, users[2].Id);
            Assert.Contains("LAST_INSERT_ID()", executor.LastSqlText, StringComparison.OrdinalIgnoreCase);
        }

        // ─────────────────────────────────────────────────────────────────
        // 10. Ignore 路径 + PopulateIdentity() —— 单行降级，每行一次 RTT
        //     被冲突跳过的行（MySQL: LAST_INSERT_ID=0；PostgreSQL: RETURNING 空集）保持原 Id
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// PostgreSQL Ignore + PopulateIdentity()：3 行单行降级。中间一行被冲突跳过（返回空 RETURNING），
        /// 其余两行反写 ID，跳过行 Id 保持 0。
        /// </summary>
        [Fact]
        public void PostgreSQL_Ignore_PopulateIdentity_SingleRowDegradation()
        {
            var executor = new IdCapturingExecutor(new[]
            {
                new List<long> { 1 },           //! 行 0 成功插入
                new List<long>(),               //! 行 1 冲突被忽略（RETURNING 空集）
                new List<long> { 3 },           //! 行 2 成功插入
            });
            var router = CreatePostgreSqlRouter(executor);
            var users = new[]
            {
                new User { Name = "a" },
                new User { Name = "b" },
                new User { Name = "c" },
            };

            router.AsInsertable(users, ignore: true, shardingKey: null, commandTimeout: null)
                  .PopulateIdentity()
                  .Execute();

            Assert.Equal(3, executor.QueryCallCount);
            Assert.Equal(1, users[0].Id);
            Assert.Equal(0, users[1].Id);   //! 被忽略，保持初始值
            Assert.Equal(3, users[2].Id);
        }

        /// <summary>
        /// MySQL Ignore + PopulateIdentity()（多语句分批）：3 行一次 QueryMultiple 完成。
        /// 用 ROW_COUNT()（1=插入/0=忽略）可靠判定每行是否插入——中间行被冲突跳过（rc=0）保持原值，
        /// 其 LAST_INSERT_ID 结果集（这里特意置脏值 999）应被忽略，不写入实体。SQL 含 ROW_COUNT()。
        /// </summary>
        [Fact]
        public void MySQL_Ignore_PopulateIdentity_RowCountGated()
        {
            var executor = IdCapturingExecutor.Scalar(ScalarIgnoreChunk(
                (id: 10, inserted: true),
                (id: 999, inserted: false),   //! 被忽略；脏 id 证明 rc=0 时不取 LAST_INSERT_ID
                (id: 12, inserted: true)
            ));
            var router = CreateMySqlRouter(executor);
            var users = new[]
            {
                new User { Name = "a" },
                new User { Name = "b" },
                new User { Name = "c" },
            };

            router.AsInsertable(users, ignore: true, shardingKey: null, commandTimeout: null)
                  .PopulateIdentity()
                  .Execute();

            Assert.Equal(1, executor.QueryMultipleCallCount);
            Assert.Equal(10, users[0].Id);
            Assert.Equal(0, users[1].Id);   //! 被忽略，保持初始值（未取脏 id 999）
            Assert.Equal(12, users[2].Id);
            Assert.Contains("ROW_COUNT()", executor.LastSqlText, StringComparison.OrdinalIgnoreCase);
        }

        // ─────────────────────────────────────────────────────────────────
        // 11. 严格长度校验 —— 返回的 IDs 与实体数不一致直接抛 InvalidOperationException
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// PostgreSQL 普通批量反写：RETURNING 返回的 ID 数与实体数不匹配应抛 InvalidOperationException，
        /// 不再静默截断。
        /// </summary>
        [Fact]
        public void PostgreSQL_PopulateIdentity_IdsCountMismatch_ThrowsInvalidOperation()
        {
            //! 3 个实体，但只返回 2 个 ID — 触发 InsertCommand.WriteBackIds 长度校验。
            var executor = new IdCapturingExecutor(new List<long> { 1, 2 });
            var router = CreatePostgreSqlRouter(executor);
            var users = new[]
            {
                new User { Name = "a" },
                new User { Name = "b" },
                new User { Name = "c" },
            };

            var executable = router
                .AsInsertable(users, ignore: false, shardingKey: null, commandTimeout: null)
                .PopulateIdentity();

            Assert.Throws<InvalidOperationException>(() => executable.Execute());
        }

        /// <summary>
        /// MySQL 普通批量反写：LAST_INSERT_ID() 返回 0 或缺失视为反常，抛 InvalidOperationException。
        /// </summary>
        [Fact]
        public void MySQL_PopulateIdentity_MissingLastInsertId_ThrowsInvalidOperation()
        {
            //! LAST_INSERT_ID()=0：非 Ignore 路径每行必须成功（仅 Ignore 允许 rc=0 跳过）。
            var executor = new IdCapturingExecutor(expectedId: 0);
            var router = CreateMySqlRouter(executor);
            var users = new[] { new User { Name = "a" } };

            var executable = router
                .AsInsertable(users, ignore: false, shardingKey: null, commandTimeout: null)
                .PopulateIdentity();

            Assert.Throws<InvalidOperationException>(() => executable.Execute());
        }

        // ─────────────────────────────────────────────────────────────────
        // 12. PopulateIdentity() 引擎兼容矩阵 —— 不支持组合抛 NotSupportedException
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// SqlServer 不支持原生 IGNORE 语义；显式开启反写 + Ignore 时应在 PopulateIdentity() 调用处
        /// 立刻抛 NotSupportedException（fail-fast，不等到 Execute）。
        /// </summary>
        [Fact]
        public void SqlServer_Ignore_PopulateIdentity_ThrowsNotSupported()
        {
            var executor = new IdCapturingExecutor(expectedId: 1);
            var router = CreateSqlServerRouter(executor);
            var user = new User { Name = "x" };

            var insertable = router.AsInsertable(
                new[] { user }, ignore: true, shardingKey: null, commandTimeout: null);

            Assert.Throws<NotSupportedException>(() => insertable.PopulateIdentity());
        }

        // ─────────────────────────────────────────────────────────────────
        // 13. SQLite —— 与 PostgreSQL 同构：RETURNING 反写 + INSERT OR IGNORE
        // ─────────────────────────────────────────────────────────────────

        private static RepositoryRouter<User> CreateRouter(DatabaseEngine engine, IdCapturingExecutor executor)
        {
            return new RepositoryRouter<User>(
                executor,
                new DbStrictAdapter(engine, new StubAdapter()),
                new StubDatabaseStrings(engine),
                NullLogger<RepositoryRouter<User>>.Instance
            );
        }

        /// <summary>SQLite 单实体反写：实体 Id 被反写，SQL 含 "RETURNING"。</summary>
        [Fact]
        public void SQLite_SingleEntity_PopulateIdentity_WritesBackId_AndSqlContainsReturning()
        {
            var executor = new IdCapturingExecutor(expectedId: 55);
            var router = CreateRouter(DatabaseEngine.SQLite, executor);
            var user = new User { Name = "s" };

            router.AsInsertable(new[] { user }, ignore: false, shardingKey: null, commandTimeout: null)
                  .PopulateIdentity()
                  .Execute();

            Assert.Equal(55, user.Id);
            Assert.NotNull(executor.LastSqlText);
            Assert.Contains("RETURNING", executor.LastSqlText, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>SQLite 多实体反写：RETURNING 多行按位置反写。</summary>
        [Fact]
        public void SQLite_MultipleEntities_PopulateIdentity_WritesBackAllIds()
        {
            var executor = new IdCapturingExecutor(new List<long> { 10, 11, 12 });
            var router = CreateRouter(DatabaseEngine.SQLite, executor);
            var users = new[]
            {
                new User { Name = "u1" },
                new User { Name = "u2" },
                new User { Name = "u3" },
            };

            router.AsInsertable(users, ignore: false, shardingKey: null, commandTimeout: null)
                  .PopulateIdentity()
                  .Execute();

            Assert.Equal(10, users[0].Id);
            Assert.Equal(11, users[1].Id);
            Assert.Equal(12, users[2].Id);
        }

        /// <summary>SQLite Bulk 拆批反写：150 行 → 2 批（100 + 50），每批一次 Query&lt;long&gt;。</summary>
        [Fact]
        public void SQLite_Bulk_PopulateIdentity_BatchesIntoTwoQueries()
        {
            var batch1Ids = Enumerable.Range(1, 100).Select(i => (long)i).ToList();
            var batch2Ids = Enumerable.Range(101, 50).Select(i => (long)i).ToList();
            var executor = new IdCapturingExecutor(new[] { batch1Ids, batch2Ids });
            var router = CreateRouter(DatabaseEngine.SQLite, executor);

            var users = Enumerable.Range(0, 150).Select(i => new User { Name = "u" + i }).ToArray();

            router.AsInsertable(users, ignore: false, shardingKey: null, commandTimeout: null)
                  .PopulateIdentity()
                  .Execute();

            Assert.Equal(2, executor.QueryCallCount);
            for (int i = 0; i < 150; i++)
            {
                Assert.Equal(i + 1, users[i].Id);
            }
        }

        /// <summary>SQLite Ignore + 反写：单行降级，中间行空 RETURNING 被忽略保持原值。</summary>
        [Fact]
        public void SQLite_Ignore_PopulateIdentity_SingleRowDegradation()
        {
            var executor = new IdCapturingExecutor(new[]
            {
                new List<long> { 1 },
                new List<long>(),
                new List<long> { 3 },
            });
            var router = CreateRouter(DatabaseEngine.SQLite, executor);
            var users = new[]
            {
                new User { Name = "a" },
                new User { Name = "b" },
                new User { Name = "c" },
            };

            router.AsInsertable(users, ignore: true, shardingKey: null, commandTimeout: null)
                  .PopulateIdentity()
                  .Execute();

            Assert.Equal(3, executor.QueryCallCount);
            Assert.Equal(1, users[0].Id);
            Assert.Equal(0, users[1].Id);
            Assert.Equal(3, users[2].Id);
        }

        // ─────────────────────────────────────────────────────────────────
        // 14. DB2 —— FINAL TABLE 反写（RETURNING 族）
        // ─────────────────────────────────────────────────────────────────

        /// <summary>DB2 单实体反写：SQL 含 "FINAL TABLE"，Id 被反写。</summary>
        [Fact]
        public void DB2_SingleEntity_PopulateIdentity_WritesBackId_AndSqlContainsFinalTable()
        {
            var executor = new IdCapturingExecutor(expectedId: 70);
            var router = CreateRouter(DatabaseEngine.DB2, executor);
            var user = new User { Name = "d" };

            router.AsInsertable(new[] { user }, ignore: false, shardingKey: null, commandTimeout: null)
                  .PopulateIdentity()
                  .Execute();

            Assert.Equal(70, user.Id);
            Assert.NotNull(executor.LastSqlText);
            Assert.Contains("FINAL TABLE", executor.LastSqlText, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>DB2 多实体反写：FINAL TABLE 多行按位置反写。</summary>
        [Fact]
        public void DB2_MultipleEntities_PopulateIdentity_WritesBackAllIds()
        {
            var executor = new IdCapturingExecutor(new List<long> { 5, 6 });
            var router = CreateRouter(DatabaseEngine.DB2, executor);
            var users = new[]
            {
                new User { Name = "u1" },
                new User { Name = "u2" },
            };

            router.AsInsertable(users, ignore: false, shardingKey: null, commandTimeout: null)
                  .PopulateIdentity()
                  .Execute();

            Assert.Equal(5, users[0].Id);
            Assert.Equal(6, users[1].Id);
        }

        // ─────────────────────────────────────────────────────────────────
        // 15. Sybase —— @@IDENTITY 多语句分批精确反写（无连续假设）
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Sybase 批量反写（多语句分批，零连续假设）：把多组「单行 INSERT + SELECT @@IDENTITY」放进一条命令，
        /// 单次 QueryMultiple 取回有序结果集，每个标量紧跟自己那条单行 INSERT，逐行精确。
        /// 3 行（≤K）→ 1 次 QueryMultiple。故意用非连续 ID（10,11,99）验证不再倒推；SQL 含 "@@IDENTITY"。
        /// </summary>
        [Fact]
        public void Sybase_Batch_PopulateIdentity_MultiStatementSingleRoundTrip()
        {
            var executor = IdCapturingExecutor.Scalar(ScalarChunk(10, 11, 99));
            var router = CreateRouter(DatabaseEngine.Sybase, executor);
            var users = new[]
            {
                new User { Name = "a" },
                new User { Name = "b" },
                new User { Name = "c" },
            };

            router.AsInsertable(users, ignore: false, shardingKey: null, commandTimeout: null)
                  .PopulateIdentity()
                  .Execute();

            Assert.Equal(1, executor.QueryMultipleCallCount);
            Assert.Equal(10, users[0].Id);
            Assert.Equal(11, users[1].Id);
            Assert.Equal(99, users[2].Id);
            Assert.NotNull(executor.LastSqlText);
            Assert.Contains("@@IDENTITY", executor.LastSqlText, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Sybase 非 Ignore 反写：某行 @@IDENTITY 读不到有效值（=0）视为反常，抛 InvalidOperationException，
        /// 不静默放过。
        /// </summary>
        [Fact]
        public void Sybase_PopulateIdentity_MissingIdentity_ThrowsInvalidOperation()
        {
            var executor = new IdCapturingExecutor(expectedId: 0);
            var router = CreateRouter(DatabaseEngine.Sybase, executor);
            var users = new[] { new User { Name = "a" } };

            var executable = router
                .AsInsertable(users, ignore: false, shardingKey: null, commandTimeout: null)
                .PopulateIdentity();

            Assert.Throws<InvalidOperationException>(() => executable.Execute());
        }

        // ─────────────────────────────────────────────────────────────────
        // 16. Oracle —— 无法读取多行 identity，PopulateIdentity() fail-fast
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Oracle 不支持批量自增反写：PopulateIdentity() 立刻抛 NotSupportedException。</summary>
        [Fact]
        public void Oracle_PopulateIdentity_ThrowsNotSupported()
        {
            var executor = new IdCapturingExecutor(expectedId: 1);
            var router = CreateRouter(DatabaseEngine.Oracle, executor);
            var user = new User { Name = "o" };

            var insertable = router.AsInsertable(
                new[] { user }, ignore: false, shardingKey: null, commandTimeout: null);

            Assert.Throws<NotSupportedException>(() => insertable.PopulateIdentity());
        }

        // ─────────────────────────────────────────────────────────────────
        // 17. Ignore + 反写：无原生 IGNORE 的引擎 fail-fast
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Oracle / DB2 / Sybase 无干净的 INSERT 忽略语义：Ignore + 反写 fail-fast。</summary>
        [Theory]
        [InlineData(DatabaseEngine.Oracle)]
        [InlineData(DatabaseEngine.DB2)]
        [InlineData(DatabaseEngine.Sybase)]
        public void Engines_Ignore_PopulateIdentity_ThrowsNotSupported(DatabaseEngine engine)
        {
            var executor = new IdCapturingExecutor(expectedId: 1);
            var router = CreateRouter(engine, executor);
            var user = new User { Name = "x" };

            var insertable = router.AsInsertable(
                new[] { user }, ignore: true, shardingKey: null, commandTimeout: null);

            Assert.Throws<NotSupportedException>(() => insertable.PopulateIdentity());
        }

        // ─────────────────────────────────────────────────────────────────
        // Stubs / Fakes
        // ─────────────────────────────────────────────────────────────────

        /// <summary>无驱动引擎（SQLite/Oracle/DB2/Sybase）的最小桩适配器，仅供构造 DbStrictAdapter。</summary>
        private sealed class StubAdapter : IDbAdapter
        {
            public IDbCorrectSettings Settings { get; } = new StubCorrectSettings();

            public IReadOnlyDictionary<System.Reflection.MethodInfo, IMethodVisitor> Visitors { get; }
                = new Dictionary<System.Reflection.MethodInfo, IMethodVisitor>();
        }

        private sealed class StubCorrectSettings : IDbCorrectSettings
        {
            public string Name(string name) => string.Concat("\"", name, "\"");
            public string ParamterName(string name) => string.Concat("@", name);
            public string ToSQL(string sql, int take, int skip, string orderBy) => sql;
        }

        private sealed class StubDatabaseStrings : IDatabaseStrings
        {
            public StubDatabaseStrings(DatabaseEngine engine) => Engine = engine;
            public DatabaseEngine Engine { get; }
            public string Strings => "Server=stub;";
        }

        // ── 标量族（MySQL/Sybase）多语句分批的结果集序列构造助手 ──
        //! 每条命令含多组「单行 INSERT + 标量 SELECT」，结果集按语句顺序返回。
        //! 非 ignore：每行 [空(INSERT), [id]]；MySQL ignore：每行 [空(INSERT), [ROW_COUNT], [id]]。

        /// <summary>非 ignore：把每行 id 展开为 [空, [id]] 的有序结果集序列（一次 QueryMultiple 调用的全部结果集）。</summary>
        private static List<List<long>> ScalarChunk(params long[] ids)
        {
            var seq = new List<List<long>>();
            foreach (var id in ids)
            {
                seq.Add(new List<long>());        // INSERT 结果集
                seq.Add(new List<long> { id });   // LAST_INSERT_ID() / @@IDENTITY
            }
            return seq;
        }

        /// <summary>MySQL ignore：每行展开为 [空, [插入与否 1/0], [id]]；被跳过行的 id 应被忽略（rc=0）。</summary>
        private static List<List<long>> ScalarIgnoreChunk(params (long id, bool inserted)[] rows)
        {
            var seq = new List<List<long>>();
            foreach (var (id, inserted) in rows)
            {
                seq.Add(new List<long>());                    // INSERT 结果集
                seq.Add(new List<long> { inserted ? 1 : 0 }); // ROW_COUNT()
                seq.Add(new List<long> { id });               // LAST_INSERT_ID()
            }
            return seq;
        }

        /// <summary>
        /// 捕获 Query&lt;long&gt;（RETURNING 族）/ QueryMultiple（标量族）调用并回放预设结果。
        /// Query 通道按调用顺序逐批返回 ID 列表；QueryMultiple 通道按调用顺序逐个返回「有序结果集序列」，
        /// 模拟单连接多语句的 2N/3N 个结果集。Execute() 等非反写路径返回 1。
        /// </summary>
        private sealed class IdCapturingExecutor : IDatabaseExecutor
        {
            private readonly List<List<long>> _queryBatches = new List<List<long>>();
            private readonly List<List<List<long>>> _gridSequences = new List<List<List<long>>>();
            private int _nextBatch;
            private int _nextGrid;

            //! 单实体：同时喂 Query 通道（RETURNING 单行）与 QueryMultiple 通道（标量单行 [空,[id]]），
            //! 使同一构造器可服务两族的单实体用例。
            public IdCapturingExecutor(long expectedId)
            {
                _queryBatches.Add(new List<long> { expectedId });
                _gridSequences.Add(ScalarChunk(expectedId));
            }

            //! RETURNING 族多行（一次 Query 返回整批 id）。
            public IdCapturingExecutor(List<long> ids) => _queryBatches.Add(ids);

            //! RETURNING 族多次 Query（Bulk 拆批 / Ignore 逐行）。
            public IdCapturingExecutor(IEnumerable<List<long>> batches) => _queryBatches.AddRange(batches);

            //! 标量族：每次 QueryMultiple 调用对应一个有序结果集序列（见 ScalarChunk/ScalarIgnoreChunk）。
            private IdCapturingExecutor(IEnumerable<List<List<long>>> gridSequences)
                => _gridSequences.AddRange(gridSequences);

            public static IdCapturingExecutor Scalar(params List<List<long>>[] gridSequences)
                => new IdCapturingExecutor((IEnumerable<List<List<long>>>)gridSequences);

            public string LastSqlText { get; private set; }
            public bool QueryWasCalled { get; private set; }
            public bool QueryMultipleWasCalled { get; private set; }
            public int QueryCallCount { get; private set; }
            public int QueryMultipleCallCount { get; private set; }

            private List<long> NextBatch()
            {
                if (_queryBatches.Count == 0) return new List<long>();
                if (_nextBatch < _queryBatches.Count) return _queryBatches[_nextBatch++];
                return _queryBatches[_queryBatches.Count - 1];
            }

            private List<List<long>> NextGrid()
            {
                if (_gridSequences.Count == 0) return new List<List<long>>();
                if (_nextGrid < _gridSequences.Count) return _gridSequences[_nextGrid++];
                return _gridSequences[_gridSequences.Count - 1];
            }

            public List<T> Query<T>(IConnection databaseStrings, CommandSql commandSql)
            {
                QueryWasCalled = true;
                QueryCallCount++;
                LastSqlText = commandSql.Text;
                if (typeof(T) == typeof(long))
                    return NextBatch().Cast<T>().ToList();
                return new List<T>();
            }

            public IDbGridReader QueryMultiple(IConnection databaseStrings, CommandSql commandSql)
            {
                QueryMultipleWasCalled = true;
                QueryMultipleCallCount++;
                LastSqlText = commandSql.Text;
                return new FakeGridReader(NextGrid());
            }

            // 普通 Execute：非 ID-writeback 路径使用（如 ignore 模式、无自增主键）
            public int Execute(IConnection databaseStrings, CommandSql commandSql) => 1;

            public Task<int> ExecuteAsync(IConnection databaseStrings, CommandSql commandSql, CancellationToken cancellationToken = default)
                => Task.FromResult(1);

            // 异步 Query
            public async IAsyncEnumerable<T> QueryAsync<T>(IConnection databaseStrings, CommandSql commandSql)
            {
                QueryWasCalled = true;
                QueryCallCount++;
                LastSqlText = commandSql.Text;
                foreach (var item in NextBatch())
                {
                    if (typeof(T) == typeof(long))
                    {
                        yield return (T)(object)item;
                    }
                }
                await Task.CompletedTask;
            }

            public Task<IAsyncDbGridReader> QueryMultipleAsync(IConnection databaseStrings, CommandSql commandSql, CancellationToken cancellationToken = default)
            {
                QueryMultipleWasCalled = true;
                QueryMultipleCallCount++;
                LastSqlText = commandSql.Text;
                return Task.FromResult<IAsyncDbGridReader>(new FakeAsyncGridReader(NextGrid()));
            }

            // 其他方法 - 本测试中不会触及
            public T Read<T>(IConnection databaseStrings, CommandSql<T> commandSql)
                => throw new NotImplementedException("Not expected in writeback tests.");
            public Task<T> ReadAsync<T>(IConnection databaseStrings, CommandSql<T> commandSql, CancellationToken cancellationToken = default)
                => throw new NotImplementedException("Not expected in writeback tests.");
            public int WriteToServer(IConnection databaseStrings, DataTable dataTable, int? commandTimeout = null)
                => throw new NotImplementedException("Not expected in writeback tests.");
            public Task<int> WriteToServerAsync(IConnection databaseStrings, DataTable dataTable, int? commandTimeout = null, CancellationToken cancellationToken = default)
                => throw new NotImplementedException("Not expected in writeback tests.");
            public int ExecuteMultiple(IConnection databaseStrings, Action<IMultipleExecutor> multipleAction, int? commandTimeout = null)
                => throw new NotImplementedException("Not expected in writeback tests.");
            public Task<int> ExecuteMultipleAsync(IConnection databaseStrings, Func<IAsyncMultipleExecutor, Task> multipleAction, int? commandTimeout = null, CancellationToken cancellationToken = default)
                => throw new NotImplementedException("Not expected in writeback tests.");
        }

        /// <summary>
        /// 同步 GridReader 伪实现：按构造时给定的「有序结果集序列」逐个返回，每次 Read&lt;long&gt;() 消费一个结果集。
        /// RETURNING 单行返回 [行]，INSERT/无行语句返回空集。越界返回空集。
        /// </summary>
        private sealed class FakeGridReader : IDbGridReader
        {
            private readonly List<List<long>> _resultSets;
            private int _index;

            public FakeGridReader(List<List<long>> resultSets) => _resultSets = resultSets;

            public List<T> Read<T>()
            {
                var set = _index < _resultSets.Count ? _resultSets[_index] : new List<long>();
                _index++;
                if (typeof(T) == typeof(long))
                    return set.Cast<T>().ToList();
                return new List<T>();
            }

            public T Read<T>(RowStyle rowStyle) => throw new NotImplementedException();
            public IEnumerable<T> Query<T>() => throw new NotImplementedException();
            public void Dispose() { }
        }

        /// <summary>异步 GridReader 伪实现，语义同 <see cref="FakeGridReader"/>。</summary>
        private sealed class FakeAsyncGridReader : IAsyncDbGridReader
        {
            private readonly List<List<long>> _resultSets;
            private int _index;

            public FakeAsyncGridReader(List<List<long>> resultSets) => _resultSets = resultSets;

            public Task<List<T>> ReadAsync<T>(CancellationToken cancellationToken = default)
            {
                var set = _index < _resultSets.Count ? _resultSets[_index] : new List<long>();
                _index++;
                if (typeof(T) == typeof(long))
                    return Task.FromResult(set.Cast<T>().ToList());
                return Task.FromResult(new List<T>());
            }

            public Task<T> ReadAsync<T>(RowStyle rowStyle, CancellationToken cancellationToken = default)
                => throw new NotImplementedException();

            public IAsyncEnumerable<T> QueryAsync<T>() => throw new NotImplementedException();

            public ValueTask DisposeAsync() => default;
        }
    }
}
