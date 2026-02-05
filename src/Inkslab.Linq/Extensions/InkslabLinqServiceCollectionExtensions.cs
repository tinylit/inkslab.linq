using System;
using System.Data.Common;
using System.Threading;
using Inkslab.Linq;
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
            
            services.TryAddSingleton<TDbAdapter>();
            services.TryAddSingleton<IDbConnectionPipeline, DbConnectionPipeline>();
            services.TryAddSingleton(typeof(IDatabase<>), typeof(Database<>));
            services.TryAddSingleton<IDatabaseExecutor, DatabaseExecutor>();
            services.TryAddSingleton<IConnections, DefaultConnections>();

            return new DatabaseLinqBuilder(engine, typeof(TDbAdapter), services);
        }
    }
}
