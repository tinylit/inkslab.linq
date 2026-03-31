using System;
using System.Collections.Generic;
using System.Reflection;
using Inkslab.Linq.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// 实体映射 LFU 缓存容量相关单元测试。
    /// 覆盖 <see cref="DatabaseExecutorOptions"/>、<see cref="DatabaseEngineOptions"/>
    /// 及 <see cref="DatabaseLinqBuilder.Configure"/> 三个层次的容量配置逻辑。
    /// </summary>
    public class MappingCapacityTests
    {
        #region DatabaseExecutorOptions — 默认值

        /// <summary>
        /// <see cref="DatabaseExecutorOptions.DefaultMappingCapacity"/> 常量值应为 1000。
        /// </summary>
        [Fact]
        public void DefaultMappingCapacity_ConstantIs1000()
        {
            Assert.Equal(1000, DatabaseExecutorOptions.DefaultMappingCapacity);
        }

        /// <summary>
        /// 新建 <see cref="DatabaseExecutorOptions"/> 时，全局 <see cref="DatabaseExecutorOptions.MappingCapacity"/>
        /// 应等于 <see cref="DatabaseExecutorOptions.DefaultMappingCapacity"/>。
        /// </summary>
        [Fact]
        public void DatabaseExecutorOptions_DefaultMappingCapacity_EqualsConstant()
        {
            var options = new DatabaseExecutorOptions();

            Assert.Equal(DatabaseExecutorOptions.DefaultMappingCapacity, options.MappingCapacity);
        }

        #endregion

        #region DatabaseExecutorOptions — GetMappingCapacity

        /// <summary>
        /// 未为指定引擎单独配置时，<see cref="DatabaseExecutorOptions.GetMappingCapacity"/>
        /// 应回退至全局 <see cref="DatabaseExecutorOptions.MappingCapacity"/>。
        /// </summary>
        [Fact]
        public void GetMappingCapacity_NoEngineOverride_ReturnsglobalDefault()
        {
            var options = new DatabaseExecutorOptions();

            var capacity = options.GetMappingCapacity(DatabaseEngine.MySQL);

            Assert.Equal(DatabaseExecutorOptions.DefaultMappingCapacity, capacity);
        }

        /// <summary>
        /// 修改全局 <see cref="DatabaseExecutorOptions.MappingCapacity"/> 后，
        /// 未单独配置的引擎应返回新的全局值。
        /// </summary>
        [Fact]
        public void GetMappingCapacity_GlobalChanged_ReturnsNewGlobal()
        {
            var options = new DatabaseExecutorOptions { MappingCapacity = 500 };

            var capacity = options.GetMappingCapacity(DatabaseEngine.SqlServer);

            Assert.Equal(500, capacity);
        }

        /// <summary>
        /// 通过 <see cref="DatabaseExecutorOptions.SetMappingCapacity"/> 为引擎单独设置容量后，
        /// <see cref="DatabaseExecutorOptions.GetMappingCapacity"/> 应返回该专属值，而非全局默认。
        /// </summary>
        [Fact]
        public void GetMappingCapacity_AfterSetMappingCapacity_ReturnsEngineSpecificValue()
        {
            var options = new DatabaseExecutorOptions();
            options.SetMappingCapacity(DatabaseEngine.MySQL, 200);

            var capacity = options.GetMappingCapacity(DatabaseEngine.MySQL);

            Assert.Equal(200, capacity);
        }

        /// <summary>
        /// 为引擎单独设置容量后，其他引擎不受影响，仍返回全局默认值。
        /// </summary>
        [Fact]
        public void GetMappingCapacity_OtherEngineUnaffected_ReturnsGlobalDefault()
        {
            var options = new DatabaseExecutorOptions();
            options.SetMappingCapacity(DatabaseEngine.MySQL, 200);

            var sqlServerCapacity = options.GetMappingCapacity(DatabaseEngine.SqlServer);
            var postgreCapacity = options.GetMappingCapacity(DatabaseEngine.PostgreSQL);

            Assert.Equal(DatabaseExecutorOptions.DefaultMappingCapacity, sqlServerCapacity);
            Assert.Equal(DatabaseExecutorOptions.DefaultMappingCapacity, postgreCapacity);
        }

        /// <summary>
        /// 全局值改变不会覆盖已通过 <see cref="DatabaseExecutorOptions.SetMappingCapacity"/> 单独设置的引擎容量。
        /// </summary>
        [Fact]
        public void GetMappingCapacity_GlobalChangeDoesNotOverrideEngineSpecific()
        {
            var options = new DatabaseExecutorOptions();
            options.SetMappingCapacity(DatabaseEngine.MySQL, 300);
            options.MappingCapacity = 2000;

            var mysqlCapacity = options.GetMappingCapacity(DatabaseEngine.MySQL);
            var sqlServerCapacity = options.GetMappingCapacity(DatabaseEngine.SqlServer);

            Assert.Equal(300, mysqlCapacity);
            Assert.Equal(2000, sqlServerCapacity);
        }

        #endregion

        #region DatabaseExecutorOptions — SetMappingCapacity

        /// <summary>
        /// 多次调用 <see cref="DatabaseExecutorOptions.SetMappingCapacity"/> 时，后者覆盖前者。
        /// </summary>
        [Fact]
        public void SetMappingCapacity_CalledTwice_LastValueWins()
        {
            var options = new DatabaseExecutorOptions();
            options.SetMappingCapacity(DatabaseEngine.MySQL, 100);
            options.SetMappingCapacity(DatabaseEngine.MySQL, 800);

            Assert.Equal(800, options.GetMappingCapacity(DatabaseEngine.MySQL));
        }

        /// <summary>
        /// 多个不同引擎可以独立设置各自的容量。
        /// </summary>
        [Fact]
        public void SetMappingCapacity_MultipleEngines_IndependentValues()
        {
            var options = new DatabaseExecutorOptions();
            options.SetMappingCapacity(DatabaseEngine.MySQL, 100);
            options.SetMappingCapacity(DatabaseEngine.SqlServer, 200);
            options.SetMappingCapacity(DatabaseEngine.PostgreSQL, 300);

            Assert.Equal(100, options.GetMappingCapacity(DatabaseEngine.MySQL));
            Assert.Equal(200, options.GetMappingCapacity(DatabaseEngine.SqlServer));
            Assert.Equal(300, options.GetMappingCapacity(DatabaseEngine.PostgreSQL));
        }

        #endregion

        #region DatabaseEngineOptions — 默认值

        /// <summary>
        /// 新建 <see cref="DatabaseEngineOptions"/> 时，
        /// <see cref="DatabaseEngineOptions.MappingCapacity"/> 应等于
        /// <see cref="DatabaseExecutorOptions.DefaultMappingCapacity"/>。
        /// </summary>
        [Fact]
        public void DatabaseEngineOptions_DefaultMappingCapacity_EqualsGlobalConstant()
        {
            var engineOptions = new DatabaseEngineOptions();

            Assert.Equal(DatabaseExecutorOptions.DefaultMappingCapacity, engineOptions.MappingCapacity);
        }

        /// <summary>
        /// <see cref="DatabaseEngineOptions.MappingCapacity"/> 可以被设置为任意正整数。
        /// </summary>
        [Fact]
        public void DatabaseEngineOptions_SetMappingCapacity_ReturnsAssignedValue()
        {
            var engineOptions = new DatabaseEngineOptions { MappingCapacity = 42 };

            Assert.Equal(42, engineOptions.MappingCapacity);
        }

        #endregion

        #region DatabaseLinqBuilder.Configure — 参数校验

        /// <summary>
        /// <see cref="DatabaseLinqBuilder.Configure"/> 传入 null 时，
        /// 应抛出 <see cref="ArgumentNullException"/>。
        /// </summary>
        [Fact]
        public void Configure_NullDelegate_ThrowsArgumentNullException()
        {
            var builder = CreateBuilder(DatabaseEngine.MySQL);

            Assert.Throws<ArgumentNullException>(() => builder.Configure(null));
        }

        /// <summary>
        /// <see cref="DatabaseLinqBuilder.Configure"/> 中将 <see cref="DatabaseEngineOptions.MappingCapacity"/>
        /// 设置为 0 时，应抛出 <see cref="ArgumentOutOfRangeException"/>。
        /// </summary>
        [Fact]
        public void Configure_MappingCapacityZero_ThrowsArgumentOutOfRangeException()
        {
            var builder = CreateBuilder(DatabaseEngine.MySQL);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                builder.Configure(o => o.MappingCapacity = 0));
        }

        /// <summary>
        /// <see cref="DatabaseLinqBuilder.Configure"/> 中将 <see cref="DatabaseEngineOptions.MappingCapacity"/>
        /// 设置为负数时，应抛出 <see cref="ArgumentOutOfRangeException"/>。
        /// </summary>
        [Fact]
        public void Configure_MappingCapacityNegative_ThrowsArgumentOutOfRangeException()
        {
            var builder = CreateBuilder(DatabaseEngine.MySQL);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                builder.Configure(o => o.MappingCapacity = -1));
        }

        #endregion

        #region DatabaseLinqBuilder.Configure — 正常流程

        /// <summary>
        /// <see cref="DatabaseLinqBuilder.Configure"/> 以有效容量调用后，
        /// 引擎专属容量应通过共享 <see cref="DatabaseExecutorOptions"/> 生效。
        /// </summary>
        [Fact]
        public void Configure_ValidCapacity_SetsEngineSpecificCapacity()
        {
            var sharedOptions = new DatabaseExecutorOptions();
            var builder = CreateBuilder(DatabaseEngine.MySQL, sharedOptions);

            builder.Configure(o => o.MappingCapacity = 512);

            Assert.Equal(512, sharedOptions.GetMappingCapacity(DatabaseEngine.MySQL));
        }

        /// <summary>
        /// <see cref="DatabaseLinqBuilder.Configure"/> 对一个引擎生效后，
        /// 其他引擎的容量不受影响。
        /// </summary>
        [Fact]
        public void Configure_ValidCapacity_DoesNotAffectOtherEngines()
        {
            var sharedOptions = new DatabaseExecutorOptions();
            var mysqlBuilder = CreateBuilder(DatabaseEngine.MySQL, sharedOptions);
            mysqlBuilder.Configure(o => o.MappingCapacity = 512);

            var sqlServerCapacity = sharedOptions.GetMappingCapacity(DatabaseEngine.SqlServer);

            Assert.Equal(DatabaseExecutorOptions.DefaultMappingCapacity, sqlServerCapacity);
        }

        /// <summary>
        /// <see cref="DatabaseLinqBuilder.Configure"/> 应返回当前构建器实例（支持链式调用）。
        /// </summary>
        [Fact]
        public void Configure_ValidCapacity_ReturnsSameBuilderInstance()
        {
            var builder = CreateBuilder(DatabaseEngine.MySQL);

            var returned = builder.Configure(o => o.MappingCapacity = 128);

            Assert.Same(builder, returned);
        }

        /// <summary>
        /// 同一引擎多次调用 <see cref="DatabaseLinqBuilder.Configure"/> 时，后者覆盖前者。
        /// </summary>
        [Fact]
        public void Configure_CalledTwiceForSameEngine_LastValueWins()
        {
            var sharedOptions = new DatabaseExecutorOptions();
            var builder = CreateBuilder(DatabaseEngine.MySQL, sharedOptions);

            builder.Configure(o => o.MappingCapacity = 100)
                   .Configure(o => o.MappingCapacity = 999);

            Assert.Equal(999, sharedOptions.GetMappingCapacity(DatabaseEngine.MySQL));
        }

        /// <summary>
        /// 两个不同引擎的构建器各自调用 <see cref="DatabaseLinqBuilder.Configure"/> 后，
        /// 共享 <see cref="DatabaseExecutorOptions"/> 中应同时保存两个引擎的专属容量。
        /// </summary>
        [Fact]
        public void Configure_TwoEngines_BothCapacitiesStoredInSharedOptions()
        {
            var sharedOptions = new DatabaseExecutorOptions();
            var mysqlBuilder = CreateBuilder(DatabaseEngine.MySQL, sharedOptions);
            var sqlServerBuilder = CreateBuilder(DatabaseEngine.SqlServer, sharedOptions);

            mysqlBuilder.Configure(o => o.MappingCapacity = 400);
            sqlServerBuilder.Configure(o => o.MappingCapacity = 600);

            Assert.Equal(400, sharedOptions.GetMappingCapacity(DatabaseEngine.MySQL));
            Assert.Equal(600, sharedOptions.GetMappingCapacity(DatabaseEngine.SqlServer));
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 创建一个带有独立 <see cref="DatabaseExecutorOptions"/> 的 <see cref="DatabaseLinqBuilder"/>。
        /// </summary>
        private static DatabaseLinqBuilder CreateBuilder(DatabaseEngine engine)
            => CreateBuilder(engine, new DatabaseExecutorOptions());

        /// <summary>
        /// 使用指定 <see cref="DatabaseExecutorOptions"/> 创建 <see cref="DatabaseLinqBuilder"/>。
        /// </summary>
        private static DatabaseLinqBuilder CreateBuilder(DatabaseEngine engine, DatabaseExecutorOptions sharedOptions)
        {
            var services = new ServiceCollection();
            return new DatabaseLinqBuilder(engine, typeof(StubDbAdapter), services, sharedOptions);
        }

        /// <summary>
        /// 用于满足 <see cref="DatabaseLinqBuilder"/> 构造函数所需适配器类型的桩类型。
        /// </summary>
        private class StubDbAdapter : IDbAdapter
        {
            public IDbCorrectSettings Settings { get; } = new StubCorrectSettings();

            public IReadOnlyDictionary<MethodInfo, IMethodVisitor> Visitors { get; } =
                new Dictionary<MethodInfo, IMethodVisitor>();

            private class StubCorrectSettings : IDbCorrectSettings
            {
                public string Name(string name) => name;
                public string ParamterName(string name) => "@" + name;
                public string ToSQL(string sql, int take, int skip, string orderBy) => sql;
            }
        }

        #endregion
    }
}
