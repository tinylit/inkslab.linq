using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using Inkslab.Linq;
using Inkslab.Linq.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Microsoft.Extensions.DependencyInjection
#pragma warning restore IDE0130 // 命名空间与文件夹结构不匹配
{
    /// <summary>
    /// Linq 服务池。
    /// </summary>
    public static class InkslabLinqServiceCollectionExtensions
    {
        private class DbConnectionFactory : IDbConnectionFactory
        {
            private readonly Func<string, DbConnection> _factory;

            public DbConnectionFactory(DatabaseEngine engine, Func<string, DbConnection> factory)
            {
                Engine = engine;

                _factory = factory;
            }

            public DatabaseEngine Engine { get; }

            public DbConnection Create(string connectionString)
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new ArgumentException($"“{nameof(connectionString)}”不能为 null 或空。", nameof(connectionString));
                }

                return _factory(connectionString);
            }
        }

        /// <summary>
        /// 使用引擎。
        /// </summary>
        /// <param name="services">服务池。</param>
        /// <param name="engine">数据库引擎。</param>
        /// <param name="factory">数据库工厂。</param>
        /// <returns>服务池。</returns>
        public static DatabaseLinqBuilder UseEngine<TDbAdapter>(this IServiceCollection services, DatabaseEngine engine, DbProviderFactory factory) where TDbAdapter : class, IDbAdapter
        {
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return services.UseEngine<TDbAdapter>(engine, connectionString =>
            {
                var connection = factory.CreateConnection();

                if (connection is null)
                {
                    throw new AbandonedMutexException();
                }

                connection.ConnectionString = connectionString;

                return connection;
            });
        }

        /// <summary>
        /// 使用引擎。
        /// </summary>
        /// <param name="services">服务池。</param>
        /// <param name="engine">数据库引擎。</param>
        /// <param name="factory">数据库工厂。</param>
        /// <returns>服务池。</returns>
        public static DatabaseLinqBuilder UseEngine<TDbAdapter>(this IServiceCollection services, DatabaseEngine engine, Func<string, DbConnection> factory) where TDbAdapter : class, IDbAdapter
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(engine, factory));

            // 获取或创建共享的 DatabaseExecutorOptions 单例
            var optionsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DatabaseExecutorOptions));
            DatabaseExecutorOptions sharedOptions;
            if (optionsDescriptor?.ImplementationInstance is DatabaseExecutorOptions existing)
            {
                sharedOptions = existing;
            }
            else
            {
                sharedOptions = new DatabaseExecutorOptions();
                services.AddSingleton(sharedOptions);
            }

            services.TryAddSingleton<TDbAdapter>();
            services.TryAddSingleton<IDbConnectionPipeline, DbConnectionPipeline>();
            services.TryAddSingleton(typeof(IDatabase<>), typeof(Database<>));
            services.TryAddSingleton<IDatabaseExecutor, DatabaseExecutor>();
            services.TryAddSingleton<IConnections, DefaultConnections>();

            return new DatabaseLinqBuilder(engine, typeof(TDbAdapter), services, sharedOptions);
        }

        /// <summary>
        /// 注册数据库工厂 <see cref="IDatabaseFactory"/>。
        /// </summary>
        /// <remarks>
        /// 必须在调用一次或多次 <c>UseXxx()</c>（如 <c>UseMySql</c>、<c>UsePostgreSQL</c>、<c>UseSqlServer</c>）后调用。
        /// 注册后可通过注入 <see cref="IDatabaseFactory"/>，在运行时按引擎 + 连接字符串创建 <see cref="IDatabase"/> 会话。
        /// </remarks>
        /// <param name="services">服务池。</param>
        /// <returns>服务池。</returns>
        public static IServiceCollection AddDatabaseFactory(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<IDatabaseFactory, DatabaseFactory>();

            return services;
        }
    }
}
