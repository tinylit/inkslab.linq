using Inkslab.Linq;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Microsoft.Extensions.DependencyInjection
#pragma warning restore IDE0130 // 命名空间与文件夹结构不匹配
{
    /// <summary>
    /// Linq 服务池。
    /// </summary>
    public static class InkslabLinqServiceCollectionExtensions
    {
        private class ConnectionStrings : IConnectionStrings
        {
            private readonly string _connectionStrings;

            public ConnectionStrings(string connectionStrings)
            {
                _connectionStrings = connectionStrings;
            }

            public string Strings => _connectionStrings;
        }

        private class DbConnectionFactory : IDbConnectionFactory
        {
            private readonly Func<string, DbConnection> _factory;

            public DbConnectionFactory(Func<string, DbConnection> factory)
            {
                _factory = factory;
            }
            public DbConnection Create(string connectionString)
            {
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new ArgumentException($"“{nameof(connectionString)}”不能为 null 或空。", nameof(connectionString));
                }

                return _factory(connectionString);
            }
        }

        private class BulkAssistant : IBulkAssistant
        {
            public int WriteToServer(DbConnection connection, DataTable dt, int? commandTimeout = null)
            {
                throw new NotImplementedException();
            }

            public int WriteToServer(DbConnection connection, DbTransaction transaction, DataTable dt, int? commandTimeout = null)
            {
                throw new NotImplementedException();
            }

            public Task<int> WriteToServerAsync(DbConnection connection, DataTable dt, int? commandTimeout = null, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public Task<int> WriteToServerAsync(DbConnection connection, DbTransaction transaction, DataTable dt, int? commandTimeout = null, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 使用Linq。
        /// </summary>
        /// <param name="services">服务池。</param>
        /// <param name="connectionStrings">数据库链接。</param>
        /// <param name="factory">数据库工厂。</param>
        /// <returns>服务池。</returns>
        public static DatabaseLinqBuilder UseLinq<TDbAdapter>(this IServiceCollection services, string connectionStrings, DbProviderFactory factory) where TDbAdapter : class, IDbAdapter
        {
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return services.UseLinq<TDbAdapter>(connectionStrings, connectionString =>
            {
                var connection = factory.CreateConnection();

                connection.ConnectionString = connectionStrings;

                return connection;
            });
        }

        /// <summary>
        /// 使用Linq。
        /// </summary>
        /// <param name="services">服务池。</param>
        /// <param name="connectionStrings">数据库链接。</param>
        /// <param name="factory">数据库工厂。</param>
        /// <returns>服务池。</returns>
        public static DatabaseLinqBuilder UseLinq<TDbAdapter>(this IServiceCollection services, string connectionStrings, Func<string, DbConnection> factory) where TDbAdapter : class, IDbAdapter
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrEmpty(connectionStrings))
            {
                throw new ArgumentException($"“{nameof(connectionStrings)}”不能为 null 或空。", nameof(connectionStrings));
            }

            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            services.AddSingleton<IDbAdapter, TDbAdapter>()
                .AddSingleton(static services => services.GetRequiredService<IDbAdapter>().Settings)
                .AddSingleton<IConnectionStrings>(new ConnectionStrings(connectionStrings))
                .AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(factory))
                .AddSingleton<IDbConnectionPipeline, DbConnectionPipeline>()
                .AddSingleton<IDatabaseExecutor, DatabaseExecutor>()
                .AddSingleton<IDatabase, Database>()
                .AddSingleton(typeof(IDatabase<>), typeof(Database<>))
                .AddScoped(typeof(IQueryable<>), typeof(Queryable<>))
                .AddScoped(typeof(IRepository<>), typeof(Repository<>));

            services.TryAddSingleton<IBulkAssistant, BulkAssistant>();
            services.TryAddSingleton<IConnections, DefaultConnections>();
            services.TryAddSingleton(typeof(IRepositoryRouter<>), typeof(RepositoryRouter<>));
            services.TryAddSingleton<IRepositoryExecutor, RepositoryExecutor>();

            return new DatabaseLinqBuilder(services);
        }
    }
}
