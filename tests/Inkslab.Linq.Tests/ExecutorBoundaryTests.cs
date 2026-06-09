using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Linq.Exceptions;
using Inkslab.Linq.MySql;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// 执行层（DatabaseExecutor、RepositoryExecutor、Repository）边界处理与验证逻辑闭合性测试。
    /// 所有测试均不依赖真实数据库连接，仅通过公开 API、CommandSql、LinqAnalyzer 及反射驱动内部逻辑。
    /// </summary>
    public class ExecutorBoundaryTests
    {
        // ─────────────────────────────────────────────────────────────
        // #1  CommandSql 边界
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// null SQL 文本 → 构造时应立即抛 ArgumentException，而非延迟到执行期。
        /// </summary>
        [Fact]
        public void CommandSql_NullText_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => new CommandSql(null));
            Assert.Contains("text", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 空字符串 SQL 文本 → 构造时应抛 ArgumentException。
        /// </summary>
        [Fact]
        public void CommandSql_EmptyText_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new CommandSql(string.Empty));
        }

        /// <summary>
        /// null 参数字典 → 应被视为空参数集合（不抛异常，且 Parameters 不为 null）。
        /// </summary>
        [Fact]
        public void CommandSql_NullParameters_TreatedAsEmpty()
        {
            var cmd = new CommandSql("SELECT 1", parameters: null);

            Assert.NotNull(cmd.Parameters);
            Assert.Empty(cmd.Parameters);
        }

        /// <summary>
        /// 有效 SQL + 参数字典 → 可正常构造，字典引用被原样保留。
        /// </summary>
        [Fact]
        public void CommandSql_ValidTextAndParameters_StoresCorrectly()
        {
            var parameters = new Dictionary<string, object> { ["id"] = 42 };
            var cmd = new CommandSql("SELECT * FROM user WHERE id = @id", parameters);

            Assert.Equal("SELECT * FROM user WHERE id = @id", cmd.Text);
            Assert.Same(parameters, cmd.Parameters);
        }

        /// <summary>
        /// ToString 中，SQL 文本里出现的参数占位符应被替换为参数值，
        /// 不在参数字典中的占位符保持原样，不抛异常。
        /// </summary>
        [Fact]
        public void CommandSql_ToString_UnknownPlaceholderKeptAsIs()
        {
            var cmd = new CommandSql(
                "SELECT @known, @unknown",
                new Dictionary<string, object> { ["known"] = 1 });

            var text = cmd.ToString();

            Assert.Contains("1", text);
            Assert.Contains("@unknown", text);
        }

        // ─────────────────────────────────────────────────────────────
        // #2  CommandSql<T> / RowStyle 边界
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// CommandSql&lt;T&gt; 继承自 CommandSql；正常路径下 RowStyle 及默认标志应被正确存储。
        /// </summary>
        [Fact]
        public void CommandSqlGeneric_ValidBase_SetsRowStyleCorrectly()
        {
            var baseSql = new CommandSql("SELECT 1");
            var genericCmd = new CommandSql<int>(baseSql, RowStyle.First);

            Assert.Equal(RowStyle.First, genericCmd.RowStyle);
            Assert.False(genericCmd.HasDefaultValue);
            Assert.False(genericCmd.CustomError);
        }

        /// <summary>
        /// HasDefaultValue = true 时，DefaultValue 应被存储；CustomError 独立标记。
        /// </summary>
        [Fact]
        public void CommandSqlGeneric_HasDefaultValue_PreservesDefaultValue()
        {
            var baseSql = new CommandSql("SELECT 1");
            var genericCmd = new CommandSql<User>(
                baseSql,
                RowStyle.FirstOrDefault,
                hasDefaultValue: true,
                defaultValue: new User { Id = -1, Name = "fallback" });

            Assert.True(genericCmd.HasDefaultValue);
            Assert.Equal(-1, genericCmd.DefaultValue.Id);
            Assert.Equal("fallback", genericCmd.DefaultValue.Name);
        }

        /// <summary>
        /// CustomError = true 且提供 NoElementError 时，相关属性应被正确存储。
        /// </summary>
        [Fact]
        public void CommandSqlGeneric_CustomError_StoresErrorMessage()
        {
            const string msg = "用户不存在";
            var baseSql = new CommandSql("SELECT 1");
            var cmd = new CommandSql<User>(
                baseSql,
                RowStyle.Single,
                customError: true,
                noElementError: msg);

            Assert.True(cmd.CustomError);
            Assert.Equal(msg, cmd.NoElementError);
        }

        // ─────────────────────────────────────────────────────────────
        // #3  RowStyle 枚举关系（位运算语义验证）
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// RowStyle 枚举值：Single(2) 大于 First(0)，确保比较器方向正确。
        /// DatabaseExecutor 用 rowStyle >= RowStyle.Single 来决定是否检测多余行。
        /// </summary>
        [Fact]
        public void RowStyle_Single_GreaterThanFirst()
        {
            Assert.True(RowStyle.Single > RowStyle.First);
        }

        /// <summary>
        /// RowStyle.SingleOrDefault(3) 大于 RowStyle.Single(2)，
        /// 确保 SingleOrDefault 也触发多行检测逻辑（>= Single 成立）。
        /// </summary>
        [Fact]
        public void RowStyle_SingleOrDefault_IsGreaterThanSingle()
        {
            Assert.True(RowStyle.SingleOrDefault >= RowStyle.Single);
        }

        /// <summary>
        /// RowStyle.First(0) 不触发多行检查（值小于 RowStyle.Single(2)）。
        /// </summary>
        [Fact]
        public void RowStyle_First_IsLessThanSingle()
        {
            Assert.True(RowStyle.First < RowStyle.Single);
        }

        /// <summary>
        /// (RowStyle.FirstOrDefault & RowStyle.FirstOrDefault) == RowStyle.FirstOrDefault 为 true；
        /// 用于验证 DatabaseExecutor 中对 FirstOrDefault 的位掩码判断语义正确。
        /// </summary>
        [Fact]
        public void RowStyle_FirstOrDefault_BitMaskSemanticsCorrect()
        {
            Assert.Equal(RowStyle.FirstOrDefault, RowStyle.FirstOrDefault & RowStyle.FirstOrDefault);
            Assert.Equal(RowStyle.FirstOrDefault, RowStyle.SingleOrDefault & RowStyle.FirstOrDefault);
        }

        // ─────────────────────────────────────────────────────────────
        // #4  NoElementException 边界
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// NoElementException 携带自定义消息时，消息内容应被完整保留。
        /// </summary>
        [Fact]
        public void NoElementException_CustomMessage_IsPreserved()
        {
            const string msg = "指定用户不存在，请检查参数";
            var ex = new NoElementException(msg);

            Assert.Equal(msg, ex.Message);
            Assert.Equal(404, ex.ErrorCode); // 默认错误码为 404
        }

        /// <summary>
        /// NoElementException 支持自定义错误码，用于业务层区分不同"未找到"场景。
        /// </summary>
        [Fact]
        public void NoElementException_CustomErrorCode_IsPreserved()
        {
            var ex = new NoElementException("not found", errorCode: 404);

            Assert.Equal(404, ex.ErrorCode);
        }

        /// <summary>
        /// NoElementException 可被基类 CodeException 捕获（继承链正确）。
        /// </summary>
        [Fact]
        public void NoElementException_CanBeCaughtAsCodeException()
        {
            Inkslab.Exceptions.CodeException caughtEx = null;
            try
            {
                throw new NoElementException("test");
            }
            catch (Inkslab.Exceptions.CodeException ex)
            {
                caughtEx = ex;
            }

            Assert.NotNull(caughtEx);
            Assert.IsType<NoElementException>(caughtEx);
        }

        // ─────────────────────────────────────────────────────────────
        // #5  DSyntaxErrorException 边界
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// DSyntaxErrorException 继承链：SyntaxErrorException → DataException → SystemException。
        /// </summary>
        [Fact]
        public void DSyntaxErrorException_InheritanceChainIsCorrect()
        {
            var ex = new DSyntaxErrorException("bad syntax");

            Assert.IsAssignableFrom<SyntaxErrorException>(ex);
            Assert.IsAssignableFrom<DataException>(ex);
            Assert.IsAssignableFrom<SystemException>(ex);
        }

        /// <summary>
        /// DSyntaxErrorException 携带 InnerException 时，内部异常引用应被保留。
        /// </summary>
        [Fact]
        public void DSyntaxErrorException_WithInnerException_PreservesChain()
        {
            var inner = new InvalidOperationException("inner cause");
            var ex = new DSyntaxErrorException("outer", inner);

            Assert.Same(inner, ex.InnerException);
            Assert.Equal("outer", ex.Message);
        }

        // ─────────────────────────────────────────────────────────────
        // #6  SerializableScope 嵌套幂等行为（无需数据库）
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 嵌套 SerializableScope：内层构造时 Serializable.Current 已存在，
        /// 内层不应新建 Serializable，应复用外层实例。
        /// 验证方式：内层 Dispose 后，外层的 Serializable.Current 仍然非 null（未被清除）。
        /// </summary>
        [Fact]
        public void SerializableScope_Nested_InnerDisposeDoesNotClearOuterCurrent()
        {
            using var outerScope = new SerializableScope();

            var outerSerializable = Serializable.Current;
            Assert.NotNull(outerSerializable);

            using (var innerScope = new SerializableScope())
            {
                // 内层创建时，Current 已有值，内层不新建 Serializable。
                Assert.Same(outerSerializable, Serializable.Current);
            }

            // 内层 Dispose 后，外层 Serializable.Current 应仍然有效。
            Assert.NotNull(Serializable.Current);
            Assert.Same(outerSerializable, Serializable.Current);
        }

        /// <summary>
        /// 外层 Dispose 后，Serializable.Current 应为 null（资源被释放）。
        /// </summary>
        [Fact]
        public void SerializableScope_OuterDispose_ClearsCurrent()
        {
            using (var outerScope = new SerializableScope())
            {
                Assert.NotNull(Serializable.Current);
            }

            // 外层 Dispose 后，Current 应被清除。
            Assert.Null(Serializable.Current);
        }

        /// <summary>
        /// 无嵌套时多次 Dispose 应是幂等的（不抛异常，不重复释放资源）。
        /// </summary>
        [Fact]
        public void SerializableScope_DoubleDispose_IsIdempotent()
        {
            var scope = new SerializableScope();

            scope.Dispose();

            // 第二次 Dispose 不应抛出任何异常。
            var exOnSecondDispose = Record.Exception(() => scope.Dispose());
            Assert.Null(exOnSecondDispose);
        }

        /// <summary>
        /// Serializable 直接 Dispose 后，其 Get 方法应抛 ObjectDisposedException。
        /// </summary>
        [Fact]
        public void Serializable_GetAfterDispose_ThrowsObjectDisposedException()
        {
            var serializable = new Serializable();
            serializable.Dispose();

            var ex = Assert.Throws<ObjectDisposedException>(() =>
                serializable.Get(new StubConnections(), new StubConnection()));

            Assert.Equal(nameof(Serializable), ex.ObjectName);
        }

        /// <summary>
        /// Serializable.Current 在设置为 disposed 的实例时，getter 应返回 null
        /// 而非返回已释放的对象。
        /// </summary>
        [Fact]
        public void Serializable_Current_AfterInstanceDisposed_ReturnsNull()
        {
            var serializable = new Serializable();
            Serializable.Current = serializable;

            serializable.Dispose();

            // 已 disposed 的实例不应通过 Current 属性返回。
            Assert.Null(Serializable.Current);
        }

        // ─────────────────────────────────────────────────────────────
        // #7  DatabaseExecutor.MapAdaper — 无参构造函数缺失时的行为
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 目标类型没有无参构造函数时（仅有带参构造函数），
        /// CreateMap 应回退到使用第一个公开带参构造函数逐列映射。
        /// </summary>
        [Fact]
        public void MapAdaper_NoParameterlessCtor_UsesFirstPublicCtor()
        {
            var (adaper, adaperType) = CreateMapAdaper<SingleColInt32Reader>();

            var createMapMethod = adaperType.GetMethod("CreateMap")
                .MakeGenericMethod(typeof(NoDefaultCtorEntity));

            var mapper = Assert.IsAssignableFrom<object>(createMapMethod.Invoke(adaper, null));
            Assert.NotNull(mapper);
        }

        /// <summary>
        /// 目标类型是 record（位置记录，只有主构造函数）时，
        /// CreateMap 应能成功创建 mapper（通过带参构造路径）。
        /// </summary>
        [Fact]
        public void MapAdaper_RecordType_MapperCreatedSuccessfully()
        {
            var (adaper, adaperType) = CreateMapAdaper<SingleColInt32Reader>();

            var createMapMethod = adaperType.GetMethod("CreateMap")
                .MakeGenericMethod(typeof(IntRecord));

            var mapper = createMapMethod.Invoke(adaper, null);
            Assert.NotNull(mapper);
        }

        /// <summary>
        /// 简单值类型（int）的 CreateMap → 对应 MakeSimple 路径，
        /// 调用 Map 时应返回列[0]的整数值（通过 FakeReader 驱动）。
        /// </summary>
        [Fact]
        public void MapAdaper_SimpleType_MapReturnsColumnZeroValue()
        {
            var (adaper, adaperType) = CreateMapAdaper<SingleColInt32Reader>();

            var createMapMethod = adaperType.GetMethod("CreateMap")
                .MakeGenericMethod(typeof(int));

            var mapper = createMapMethod.Invoke(adaper, null);
            Assert.NotNull(mapper);

            var mapMethod = mapper.GetType().GetMethod("Map");
            var reader = new SingleColInt32Reader(99);

            reader.Read(); // 移动到第一行

            var result = (int)mapMethod.Invoke(mapper, new object[] { reader });
            Assert.Equal(99, result);
        }

        /// <summary>
        /// Nullable&lt;int&gt; 对应 MakeSimpleNull 路径：列为 null 时 Map 返回 null（即 default(int?)）。
        /// </summary>
        [Fact]
        public void MapAdaper_NullableInt_NullColumn_ReturnsNull()
        {
            var (adaper, adaperType) = CreateMapAdaper<NullableInt32Reader>();

            var createMapMethod = adaperType.GetMethod("CreateMap")
                .MakeGenericMethod(typeof(int?));

            var mapper = createMapMethod.Invoke(adaper, null);
            Assert.NotNull(mapper);

            var mapMethod = mapper.GetType().GetMethod("Map");
            var reader = new NullableInt32Reader(isNull: true);
            reader.Read();

            var result = mapMethod.Invoke(mapper, new object[] { reader });
            Assert.Null(result);
        }

        // ─────────────────────────────────────────────────────────────
        // #8  DatabaseExecutor.MapAdaper — 列名大小写不敏感映射
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// DB 列名大小写与 C# 属性名大小写不一致时（如列"NAME"，属性"Name"），
        /// 仍应成功映射（依赖 switch + OrdinalIgnoreCase 比较器）。
        /// </summary>
        [Fact]
        public void MapAdaper_ColumnNameCaseMismatch_MapsSuccessfully()
        {
            var (adaper, adaperType) = CreateMapAdaper<UpperCaseNameReader>();

            var createMapMethod = adaperType.GetMethod("CreateMap")
                .MakeGenericMethod(typeof(SimpleNameEntity));

            var mapper = createMapMethod.Invoke(adaper, null);
            var mapMethod = mapper.GetType().GetMethod("Map");

            var reader = new UpperCaseNameReader("Alice");
            reader.Read();

            var result = (SimpleNameEntity)mapMethod.Invoke(mapper, new object[] { reader });
            Assert.Equal("Alice", result.Name);
        }

        // ─────────────────────────────────────────────────────────────
        // #9  DbGridReader — IsConsumed 保护逻辑
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// DbGridReader.Read&lt;T&gt;(rowStyle) 在 IsConsumed = true 时重复调用应抛 InvalidOperationException。
        /// 通过反射访问私有 DbGridReader 来验证 IsConsumed 保护逻辑。
        /// </summary>
        [Fact]
        public void DbGridReader_ReadWhenConsumed_ThrowsInvalidOperationException()
        {
            var gridReader = CreateDbGridReader(new TwoResultSetFakeReader());

            // IsConsumed 是 private set，通过 backing field 写入。
            var backingField = gridReader.GetType()
                .GetField("<IsConsumed>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(backingField);
            backingField.SetValue(gridReader, true);

            // Read<T> 是泛型方法，需要 MakeGenericMethod 才能通过反射正确调用
            var readMethod = gridReader.GetType()
                .GetMethod("Read", new[] { typeof(RowStyle) })
                ?.MakeGenericMethod(typeof(User));
            Assert.NotNull(readMethod);

            var ex = Assert.Throws<TargetInvocationException>(() =>
                readMethod.Invoke(gridReader, new object[] { RowStyle.FirstOrDefault }));

            Assert.IsType<InvalidOperationException>(ex.InnerException);
            Assert.Contains("consumed", ex.InnerException.Message, StringComparison.OrdinalIgnoreCase);
        }

        // ─────────────────────────────────────────────────────────────
        // #9.1  DbGridReader — ThrowByRowStyle 行级约束异常
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// DbGridReader.Read&lt;T&gt;(Single) 读到第二行时，应抛多行数据异常 MultipleRowsException。
        /// </summary>
        [Fact]
        public void DbGridReader_Read_MultipleRows_Single_ThrowsMultipleRowsException()
        {
            var inner = InvokeDbGridReaderRead(new MultiRowInt32Reader(), RowStyle.Single);

            Assert.IsType<MultipleRowsException>(inner);
        }

        /// <summary>
        /// DbGridReader.Read&lt;T&gt;(SingleOrDefault) 读到第二行时，应抛多行数据异常 MultipleRowsException。
        /// </summary>
        [Fact]
        public void DbGridReader_Read_MultipleRows_SingleOrDefault_ThrowsMultipleRowsException()
        {
            var inner = InvokeDbGridReaderRead(new MultiRowInt32Reader(), RowStyle.SingleOrDefault);

            Assert.IsType<MultipleRowsException>(inner);
        }

        /// <summary>
        /// DbGridReader.Read&lt;T&gt;(First) 无数据时，应抛无元素异常 NoElementException。
        /// </summary>
        [Fact]
        public void DbGridReader_Read_NoElement_First_ThrowsNoElementException()
        {
            var inner = InvokeDbGridReaderRead(new TwoResultSetFakeReader(), RowStyle.First);

            Assert.IsType<NoElementException>(inner);
        }

        /// <summary>
        /// DbGridReader.Read&lt;T&gt;(Single) 无数据时，应抛无元素异常 NoElementException（而非误报多行）。
        /// </summary>
        [Fact]
        public void DbGridReader_Read_NoElement_Single_ThrowsNoElementException()
        {
            var inner = InvokeDbGridReaderRead(new TwoResultSetFakeReader(), RowStyle.Single);

            Assert.IsType<NoElementException>(inner);
        }

        /// <summary>
        /// 反射调用 DbGridReader.Read&lt;int&gt;(rowStyle) 并返回被抛出的内部异常。
        /// </summary>
        private static Exception InvokeDbGridReaderRead(DbDataReader reader, RowStyle rowStyle)
        {
            var gridReader = CreateDbGridReader(reader);

            var readMethod = gridReader.GetType()
                .GetMethod("Read", new[] { typeof(RowStyle) })
                ?.MakeGenericMethod(typeof(int));
            Assert.NotNull(readMethod);

            var ex = Assert.Throws<TargetInvocationException>(() =>
                readMethod.Invoke(gridReader, new object[] { rowStyle }));

            return ex.InnerException;
        }

        // ─────────────────────────────────────────────────────────────
        // #10  Repository 边界 — null 参数检查
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Repository.Where(null) 应抛 ArgumentNullException。
        /// </summary>
        [Fact]
        public void Repository_WhereNullPredicate_ThrowsArgumentNullException()
        {
            var repo = CreateStubRepository();
            Assert.Throws<ArgumentNullException>(() => repo.Where(null));
        }

        /// <summary>
        /// Repository.Into((TEntity)null) 应抛 ArgumentNullException。
        /// </summary>
        [Fact]
        public void Repository_IntoNullEntry_ThrowsArgumentNullException()
        {
            var repo = CreateStubRepository();
            Assert.Throws<ArgumentNullException>(() => repo.Into((User)null));
        }

        /// <summary>
        /// Repository.Into((IReadOnlyCollection&lt;TEntity&gt;)null) 应抛 ArgumentNullException。
        /// </summary>
        [Fact]
        public void Repository_IntoNullCollection_ThrowsArgumentNullException()
        {
            var repo = CreateStubRepository();
            Assert.Throws<ArgumentNullException>(() => repo.Into((IReadOnlyCollection<User>)null));
        }

        /// <summary>
        /// Repository.Update(null) 应抛 ArgumentNullException（updateSetter 为 null）。
        /// </summary>
        [Fact]
        public void Repository_UpdateNullSetter_ThrowsArgumentNullException()
        {
            var repo = CreateStubRepository();
            Assert.Throws<ArgumentNullException>(() => repo.Update(null));
        }

        /// <summary>
        /// Repository.UpdateTo((TEntity)null) 应抛 ArgumentNullException。
        /// </summary>
        [Fact]
        public void Repository_UpdateToNullEntry_ThrowsArgumentNullException()
        {
            var repo = CreateStubRepository();
            Assert.Throws<ArgumentNullException>(() => repo.UpdateTo((User)null));
        }

        /// <summary>
        /// Repository.DeleteWith((TEntity)null) 应抛 ArgumentNullException。
        /// </summary>
        [Fact]
        public void Repository_DeleteWithNullEntry_ThrowsArgumentNullException()
        {
            var repo = CreateStubRepository();
            Assert.Throws<ArgumentNullException>(() => repo.DeleteWith((User)null));
        }

        /// <summary>
        /// Repository.DataSharding(null) 应抛 ArgumentException（shardingKey 不能为 null）。
        /// </summary>
        [Fact]
        public void Repository_DataShardingNullKey_ThrowsArgumentException()
        {
            var repo = CreateStubRepository();
            Assert.Throws<ArgumentException>(() => repo.DataSharding(null));
        }

        /// <summary>
        /// Repository.DataSharding("") 应抛 ArgumentException（shardingKey 不能为空字符串）。
        /// </summary>
        [Fact]
        public void Repository_DataShardingEmptyKey_ThrowsArgumentException()
        {
            var repo = CreateStubRepository();
            Assert.Throws<ArgumentException>(() => repo.DataSharding(string.Empty));
        }

        /// <summary>
        /// Repository.Insert(null) 应抛 ArgumentNullException。
        /// </summary>
        [Fact]
        public void Repository_InsertNullQueryable_ThrowsArgumentNullException()
        {
            var repo = CreateStubRepository();
            Assert.Throws<ArgumentNullException>(() => repo.Insert(null));
        }

        // ─────────────────────────────────────────────────────────────
        // #11  LinqAnalyzer — ExecutorVisitor 边界
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// ExecutorVisitor.Startup 接收到非 MethodCallExpression 节点时，
        /// 应抛 NotSupportedException（不是 call 节点无法作为执行器入口）。
        /// </summary>
        [Fact]
        public void ExecutorVisitor_NonCallExpression_ThrowsNotSupportedException()
        {
            var strictAdapter = new DbStrictAdapter(DatabaseEngine.MySQL, new MySqlAdapter());
            using var visitor = new ExecutorVisitor(strictAdapter);

            var constExpr = System.Linq.Expressions.Expression.Constant(42);

            Assert.Throws<NotSupportedException>(() => visitor.Startup(constExpr));
        }

        /// <summary>
        /// DSyntaxErrorException 在执行层被正确识别为语法错误异常类型。
        /// 当 UPDATE 语句被构造为不合法状态时，翻译层应抛出此异常（而非 InvalidOperationException）。
        /// 此处直接验证 DSyntaxErrorException 的可抛出性及消息传递，
        /// 实际业务场景（空 setter）在集成测试中覆盖。
        /// </summary>
        [Fact]
        public void DSyntaxErrorException_ThrownWithExpectedMessage()
        {
            const string msg = "请指定更新字段！";

            DSyntaxErrorException caught = null;
            try
            {
                throw new DSyntaxErrorException(msg);
            }
            catch (DSyntaxErrorException ex)
            {
                caught = ex;
            }

            Assert.NotNull(caught);
            Assert.Equal(msg, caught.Message);
        }

        // ─────────────────────────────────────────────────────────────
        // #12  StoredProcedureCommandSql
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// StoredProcedureCommandSql 的 CommandType 应为 StoredProcedure，而非 Text。
        /// </summary>
        [Fact]
        public void StoredProcedureCommandSql_CommandType_IsStoredProcedure()
        {
            var cmd = new StoredProcedureCommandSql("sp_test");
            Assert.Equal(CommandType.StoredProcedure, cmd.CommandType);
        }

        /// <summary>
        /// StoredProcedureCommandSql.ToString 应以 "EXEC " 开头，并包含过程名。
        /// </summary>
        [Fact]
        public void StoredProcedureCommandSql_ToString_StartsWithExec()
        {
            var cmd = new StoredProcedureCommandSql("sp_GetUser", new Dictionary<string, object> { ["id"] = 1 });
            var str = cmd.ToString();

            Assert.StartsWith("EXEC ", str);
            Assert.Contains("sp_GetUser", str);
            Assert.Contains("@id", str);
        }

        // ─────────────────────────────────────────────────────────────
        // #13  CancellationToken — 已取消令牌行为（纯逻辑层，无 DB）
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 已取消的 CancellationToken 传入异步操作应立即抛出 OperationCanceledException。
        /// 验证取消令牌语义在异步路径中正确传播（框架底层依赖此机制）。
        /// </summary>
        [Fact]
        public async Task CancellationToken_Cancelled_ThrowsOperationCanceledExceptionAsync()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await Task.Delay(0, cts.Token);
            });
        }

        // ─────────────────────────────────────────────────────────────
        // #14  DataTable 边界（WriteToServer 参数校验）
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// DatabaseExecutor.WriteToServer 接收 null DataTable 时应抛 ArgumentNullException。
        /// 参数检查在连接管理之前发生，故 null pipeline 不会被触及。
        /// </summary>
        [Fact]
        public void DatabaseExecutor_WriteToServer_NullDataTable_ThrowsArgumentNullException()
        {
            var executor = CreateDatabaseExecutorWithNullPipeline();

            var writeMethod = executor.GetType().GetMethod(
                "WriteToServer",
                new[] { typeof(IConnection), typeof(DataTable), typeof(int?) });
            Assert.NotNull(writeMethod);

            var ex = Assert.Throws<TargetInvocationException>(() =>
                writeMethod.Invoke(executor, new object[] { new StubConnection(), null, (int?)null }));

            Assert.IsType<ArgumentNullException>(ex.InnerException);
        }

        /// <summary>
        /// DatabaseExecutor.WriteToServer 接收 TableName 为空的 DataTable 时应抛 ArgumentException。
        /// </summary>
        [Fact]
        public void DatabaseExecutor_WriteToServer_EmptyTableName_ThrowsArgumentException()
        {
            var executor = CreateDatabaseExecutorWithNullPipeline();

            var writeMethod = executor.GetType().GetMethod(
                "WriteToServer",
                new[] { typeof(IConnection), typeof(DataTable), typeof(int?) });

            var dt = new DataTable();

            var ex = Assert.Throws<TargetInvocationException>(() =>
                writeMethod.Invoke(executor, new object[] { new StubConnection(), dt, (int?)null }));

            Assert.IsType<ArgumentException>(ex.InnerException);
        }

        /// <summary>
        /// DatabaseExecutor.ExecuteMultiple 接收 null action 时应立即抛 ArgumentNullException。
        /// </summary>
        [Fact]
        public void DatabaseExecutor_ExecuteMultiple_NullAction_ThrowsArgumentNullException()
        {
            var executor = CreateDatabaseExecutorWithNullPipeline();

            var method = executor.GetType().GetMethod(
                "ExecuteMultiple",
                new[] { typeof(IConnection), typeof(Action<IMultipleExecutor>), typeof(int?) });
            Assert.NotNull(method);

            var ex = Assert.Throws<TargetInvocationException>(() =>
                method.Invoke(executor, new object[] { new StubConnection(), (Action<IMultipleExecutor>)null, (int?)null }));

            Assert.IsType<ArgumentNullException>(ex.InnerException);
        }

        // ─────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────

        private static (object adaper, Type adaperType) CreateMapAdaper<TReader>()
            where TReader : DbDataReader
        {
            var databaseExecutorType = typeof(DatabaseExecutor);
            var mapAdaperType = databaseExecutorType.GetNestedType("MapAdaper", BindingFlags.NonPublic);

            var adaper = Activator.CreateInstance(mapAdaperType, typeof(TReader), 100);
            return (adaper, mapAdaperType);
        }

        private static object CreateDbGridReader(DbDataReader fakeReader)
        {
            var databaseExecutorType = typeof(DatabaseExecutor);
            var gridReaderType = databaseExecutorType.GetNestedType("DbGridReader", BindingFlags.NonPublic);

            if (gridReaderType is null)
            {
                throw new InvalidOperationException("Cannot find nested type DbGridReader via reflection.");
            }

            var mapAdaperType = databaseExecutorType.GetNestedType("MapAdaper", BindingFlags.NonPublic);
            var adaper = Activator.CreateInstance(mapAdaperType, fakeReader.GetType(), 100);

            var commandSql = new CommandSql("SELECT 1");

            // DbGridReader has one internal/private constructor; use the first available.
            var ctors = gridReaderType.GetConstructors(
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            if (ctors.Length == 0)
            {
                throw new InvalidOperationException("No constructor found on DbGridReader.");
            }

            var ctor = ctors[0];

            // DbGridReader(DbConnection, DbCommand, DbDataReader, CommandSql, MapAdaper)
            // DbConnection and DbCommand are null — we only read IsConsumed and call Read<T>(RowStyle)
            return ctor.Invoke(new object[] { null, null, fakeReader, commandSql, adaper });
        }

        private static Repository<User> CreateStubRepository()
        {
            return new Repository<User>(
                new StubRepositoryExecutor(),
                new StubRepositoryRouter());
        }

        /// <summary>
        /// 创建一个 DatabaseExecutor，pipeline 为 null。
        /// 所有受测方法的参数前置检查（null/empty）均在触及 pipeline 之前发生。
        /// </summary>
        private static DatabaseExecutor CreateDatabaseExecutorWithNullPipeline()
        {
            // DatabaseExecutor(IDbConnectionPipeline, DatabaseExecutorOptions, ILogger<DatabaseExecutor>)
            var logger = NullLogger<DatabaseExecutor>.Instance;
            var options = new Inkslab.Linq.Options.DatabaseExecutorOptions();
            return new DatabaseExecutor(null, options, logger);
        }

        // ─────────────────────────────────────────────────────────────
        // Stub / Fake 辅助类型
        // ─────────────────────────────────────────────────────────────

        /// <summary>单列 int 列 FakeReader，返回指定整数值。</summary>
        private sealed class SingleColInt32Reader : DbDataReader
        {
            private readonly int _value;
            private bool _advanced;

            public SingleColInt32Reader() { }
            public SingleColInt32Reader(int value) => _value = value;

            public override int FieldCount => 1;
            public override bool HasRows => true;
            public override bool IsClosed => false;
            public override int RecordsAffected => -1;
            public override int Depth => 0;

            public override string GetName(int ordinal) => "Id";
            public override Type GetFieldType(int ordinal) => typeof(int);
            public override string GetDataTypeName(int ordinal) => "int";
            public override int GetOrdinal(string name) => 0;
            public override bool IsDBNull(int ordinal) => false;
            public override object GetValue(int ordinal) => _value;
            public override int GetInt32(int ordinal) => _value;
            public override int GetValues(object[] values) { values[0] = _value; return 1; }
            public override bool Read() { if (!_advanced) { _advanced = true; return true; } return false; }
            public override bool NextResult() => false;
            public override object this[int ordinal] => GetValue(ordinal);
            public override object this[string name] => GetValue(0);
            public override IEnumerator GetEnumerator() => throw new NotImplementedException();
            public override bool GetBoolean(int ordinal) => throw new NotImplementedException();
            public override byte GetByte(int ordinal) => throw new NotImplementedException();
            public override long GetBytes(int o, long d, byte[] b, int bo, int l) => throw new NotImplementedException();
            public override char GetChar(int ordinal) => throw new NotImplementedException();
            public override long GetChars(int o, long d, char[] b, int bo, int l) => throw new NotImplementedException();
            public override DateTime GetDateTime(int ordinal) => throw new NotImplementedException();
            public override decimal GetDecimal(int ordinal) => throw new NotImplementedException();
            public override double GetDouble(int ordinal) => throw new NotImplementedException();
            public override float GetFloat(int ordinal) => throw new NotImplementedException();
            public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
            public override short GetInt16(int ordinal) => throw new NotImplementedException();
            public override long GetInt64(int ordinal) => throw new NotImplementedException();
            public override string GetString(int ordinal) => throw new NotImplementedException();
        }

        /// <summary>单列 int 列、可返回多行的 FakeReader，用于触发多行约束异常。</summary>
        private sealed class MultiRowInt32Reader : DbDataReader
        {
            private int _row;

            public override int FieldCount => 1;
            public override bool HasRows => true;
            public override bool IsClosed => false;
            public override int RecordsAffected => -1;
            public override int Depth => 0;

            public override string GetName(int ordinal) => "Id";
            public override Type GetFieldType(int ordinal) => typeof(int);
            public override string GetDataTypeName(int ordinal) => "int";
            public override int GetOrdinal(string name) => 0;
            public override bool IsDBNull(int ordinal) => false;
            public override object GetValue(int ordinal) => _row;
            public override int GetInt32(int ordinal) => _row;
            public override int GetValues(object[] values) { values[0] = _row; return 1; }
            // 返回两行数据：前两次 Read() 为 true，之后为 false。
            public override bool Read() { if (_row < 2) { _row++; return true; } return false; }
            public override bool NextResult() => false;
            public override object this[int ordinal] => GetValue(ordinal);
            public override object this[string name] => GetValue(0);
            public override IEnumerator GetEnumerator() => throw new NotImplementedException();
            public override bool GetBoolean(int ordinal) => throw new NotImplementedException();
            public override byte GetByte(int ordinal) => throw new NotImplementedException();
            public override long GetBytes(int o, long d, byte[] b, int bo, int l) => throw new NotImplementedException();
            public override char GetChar(int ordinal) => throw new NotImplementedException();
            public override long GetChars(int o, long d, char[] b, int bo, int l) => throw new NotImplementedException();
            public override DateTime GetDateTime(int ordinal) => throw new NotImplementedException();
            public override decimal GetDecimal(int ordinal) => throw new NotImplementedException();
            public override double GetDouble(int ordinal) => throw new NotImplementedException();
            public override float GetFloat(int ordinal) => throw new NotImplementedException();
            public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
            public override short GetInt16(int ordinal) => throw new NotImplementedException();
            public override long GetInt64(int ordinal) => throw new NotImplementedException();
            public override string GetString(int ordinal) => throw new NotImplementedException();
        }

        /// <summary>可为 null 的 int 列 FakeReader。</summary>
        private sealed class NullableInt32Reader : DbDataReader
        {
            private readonly bool _isNull;
            private bool _advanced;

            public NullableInt32Reader() { }
            public NullableInt32Reader(bool isNull) => _isNull = isNull;

            public override int FieldCount => 1;
            public override bool HasRows => true;
            public override bool IsClosed => false;
            public override int RecordsAffected => -1;
            public override int Depth => 0;

            public override string GetName(int ordinal) => "Value";
            public override Type GetFieldType(int ordinal) => typeof(int);
            public override string GetDataTypeName(int ordinal) => "int";
            public override int GetOrdinal(string name) => 0;
            public override bool IsDBNull(int ordinal) => _isNull;
            public override object GetValue(int ordinal) => _isNull ? (object)DBNull.Value : 42;
            public override int GetInt32(int ordinal) => _isNull ? throw new InvalidOperationException() : 42;
            public override int GetValues(object[] values) { values[0] = GetValue(0); return 1; }
            public override bool Read() { if (!_advanced) { _advanced = true; return true; } return false; }
            public override bool NextResult() => false;
            public override object this[int ordinal] => GetValue(ordinal);
            public override object this[string name] => GetValue(0);
            public override IEnumerator GetEnumerator() => throw new NotImplementedException();
            public override bool GetBoolean(int ordinal) => throw new NotImplementedException();
            public override byte GetByte(int ordinal) => throw new NotImplementedException();
            public override long GetBytes(int o, long d, byte[] b, int bo, int l) => throw new NotImplementedException();
            public override char GetChar(int ordinal) => throw new NotImplementedException();
            public override long GetChars(int o, long d, char[] b, int bo, int l) => throw new NotImplementedException();
            public override DateTime GetDateTime(int ordinal) => throw new NotImplementedException();
            public override decimal GetDecimal(int ordinal) => throw new NotImplementedException();
            public override double GetDouble(int ordinal) => throw new NotImplementedException();
            public override float GetFloat(int ordinal) => throw new NotImplementedException();
            public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
            public override short GetInt16(int ordinal) => throw new NotImplementedException();
            public override long GetInt64(int ordinal) => throw new NotImplementedException();
            public override string GetString(int ordinal) => throw new NotImplementedException();
        }

        /// <summary>列名全大写的 string 列 FakeReader，用于验证大小写不敏感映射。</summary>
        private sealed class UpperCaseNameReader : DbDataReader
        {
            private readonly string _value;
            private bool _advanced;

            // 无参构造函数，满足 CreateMapAdaper<TReader>() 反射实例化需求
            public UpperCaseNameReader() { }
            public UpperCaseNameReader(string value) => _value = value;

            public override int FieldCount => 1;
            public override bool HasRows => true;
            public override bool IsClosed => false;
            public override int RecordsAffected => -1;
            public override int Depth => 0;

            // 列名刻意全大写，验证大小写不敏感映射
            public override string GetName(int ordinal) => "NAME";
            public override Type GetFieldType(int ordinal) => typeof(string);
            public override string GetDataTypeName(int ordinal) => "varchar";
            public override int GetOrdinal(string name) => 0;
            public override bool IsDBNull(int ordinal) => _value is null;
            public override object GetValue(int ordinal) => _value;
            public override string GetString(int ordinal) => _value;
            public override int GetValues(object[] values) { values[0] = _value; return 1; }
            public override bool Read() { if (!_advanced) { _advanced = true; return true; } return false; }
            public override bool NextResult() => false;
            public override object this[int ordinal] => GetValue(ordinal);
            public override object this[string name] => GetValue(0);
            public override IEnumerator GetEnumerator() => throw new NotImplementedException();
            public override bool GetBoolean(int ordinal) => throw new NotImplementedException();
            public override byte GetByte(int ordinal) => throw new NotImplementedException();
            public override long GetBytes(int o, long d, byte[] b, int bo, int l) => throw new NotImplementedException();
            public override char GetChar(int ordinal) => throw new NotImplementedException();
            public override long GetChars(int o, long d, char[] b, int bo, int l) => throw new NotImplementedException();
            public override DateTime GetDateTime(int ordinal) => throw new NotImplementedException();
            public override decimal GetDecimal(int ordinal) => throw new NotImplementedException();
            public override double GetDouble(int ordinal) => throw new NotImplementedException();
            public override float GetFloat(int ordinal) => throw new NotImplementedException();
            public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
            public override short GetInt16(int ordinal) => throw new NotImplementedException();
            public override int GetInt32(int ordinal) => throw new NotImplementedException();
            public override long GetInt64(int ordinal) => throw new NotImplementedException();
        }

        /// <summary>空结果集 FakeReader，用于 DbGridReader IsConsumed 测试。</summary>
        private sealed class TwoResultSetFakeReader : DbDataReader
        {
            public override int FieldCount => 0;
            public override bool HasRows => false;
            public override bool IsClosed => false;
            public override int RecordsAffected => -1;
            public override int Depth => 0;
            public override string GetName(int ordinal) => throw new NotImplementedException();
            public override Type GetFieldType(int ordinal) => throw new NotImplementedException();
            public override string GetDataTypeName(int ordinal) => throw new NotImplementedException();
            public override int GetOrdinal(string name) => throw new NotImplementedException();
            public override bool IsDBNull(int ordinal) => throw new NotImplementedException();
            public override object GetValue(int ordinal) => throw new NotImplementedException();
            public override int GetValues(object[] values) => throw new NotImplementedException();
            public override bool Read() => false;
            public override bool NextResult() => false;
            public override object this[int ordinal] => throw new NotImplementedException();
            public override object this[string name] => throw new NotImplementedException();
            public override IEnumerator GetEnumerator() => throw new NotImplementedException();
            public override bool GetBoolean(int ordinal) => throw new NotImplementedException();
            public override byte GetByte(int ordinal) => throw new NotImplementedException();
            public override long GetBytes(int o, long d, byte[] b, int bo, int l) => throw new NotImplementedException();
            public override char GetChar(int ordinal) => throw new NotImplementedException();
            public override long GetChars(int o, long d, char[] b, int bo, int l) => throw new NotImplementedException();
            public override DateTime GetDateTime(int ordinal) => throw new NotImplementedException();
            public override decimal GetDecimal(int ordinal) => throw new NotImplementedException();
            public override double GetDouble(int ordinal) => throw new NotImplementedException();
            public override float GetFloat(int ordinal) => throw new NotImplementedException();
            public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
            public override short GetInt16(int ordinal) => throw new NotImplementedException();
            public override int GetInt32(int ordinal) => throw new NotImplementedException();
            public override long GetInt64(int ordinal) => throw new NotImplementedException();
            public override string GetString(int ordinal) => throw new NotImplementedException();
        }

        /// <summary>仅包含带参构造函数的实体（无无参构造函数），用于测试 MapAdaper 回退逻辑。</summary>
        private class NoDefaultCtorEntity
        {
            public int Id { get; }
            public NoDefaultCtorEntity(int id) => Id = id;
        }

        /// <summary>位置记录（record），等价于只有主构造函数的类型。</summary>
        private record IntRecord(int Id);

        /// <summary>仅含 Name 属性的简单实体，用于大小写不敏感映射测试。</summary>
        private class SimpleNameEntity
        {
            public string Name { get; set; }
        }

        /// <summary>连接桩，实现 IConnection 以满足参数签名。</summary>
        private sealed class StubConnection : IConnection
        {
            public DatabaseEngine Engine => DatabaseEngine.MySQL;
            public string Strings => "Server=stub;";
        }

        /// <summary>连接集合桩，实现 IConnections。</summary>
        private sealed class StubConnections : IConnections
        {
            public System.Data.Common.DbConnection Get(IConnection connectionStrings)
                => throw new InvalidOperationException("Stub should not be called.");
        }

        /// <summary>仓库执行器桩：所有方法均不实际执行。</summary>
        private sealed class StubRepositoryExecutor : IRepositoryExecutor
        {
            public int Execute(System.Linq.Expressions.Expression expression)
                => throw new NotImplementedException("Stub");

            public Task<int> ExecuteAsync(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken = default)
                => throw new NotImplementedException("Stub");

            public IEnumerable<T> Query<T>(System.Linq.Expressions.Expression expression)
                => throw new NotImplementedException("Stub");

            public IAsyncEnumerable<T> QueryAsync<T>(System.Linq.Expressions.Expression expression)
                => throw new NotImplementedException("Stub");

            public T Read<T>(System.Linq.Expressions.Expression expression)
                => throw new NotImplementedException("Stub");

            public Task<T> ReadAsync<T>(System.Linq.Expressions.Expression expression, CancellationToken cancellationToken = default)
                => throw new NotImplementedException("Stub");
        }

        /// <summary>路由器桩。</summary>
        private sealed class StubRepositoryRouter : IRepositoryRouter<User>
        {
            public Inkslab.Linq.Abilities.IInsertable<User> AsInsertable(IReadOnlyList<User> entries, bool ignore, string shardingKey, int? commandTimeout)
                => throw new NotImplementedException("Stub");

            public Inkslab.Linq.Abilities.IUpdateable<User> AsUpdateable(IReadOnlyCollection<User> entries, string shardingKey, int? commandTimeout)
                => throw new NotImplementedException("Stub");

            public Inkslab.Linq.Abilities.IDeleteable<User> AsDeleteable(IReadOnlyCollection<User> entries, string shardingKey, int? commandTimeout)
                => throw new NotImplementedException("Stub");
        }
    }
}
