using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;

namespace Inkslab.Linq
{
    /// <summary>
    /// 数据库构建器。
    /// </summary>
    public sealed class DatabaseLinqBuilder
    {
        private readonly IServiceCollection _services;

        /// <summary>
        /// 数据库构建器。
        /// </summary>
        /// <param name="services">服务集合。</param>
        public DatabaseLinqBuilder(IServiceCollection services)
        {
            _services = services;
        }

        /// <summary>
        /// 服务池。
        /// </summary>
        public IServiceCollection Services => _services;

        /// <summary>
        /// 使用 Linq 语法。
        /// </summary>
        /// <param name="connectionStrings">数据库链接。</param>
        /// <remarks>
        /// 在项目中，通过注入“<see cref="IQueryable{TEntity}"/>”、“<seealso cref="IRepository{TEntity}"/>”或“<seealso cref="IDatabase"/>”访问数据库。
        /// </remarks>
        /// <returns>数据库构建器。</returns>
        public DatabaseLinqBuilder UseLinq(string connectionStrings)
        {
            if (string.IsNullOrEmpty(connectionStrings))
            {
                throw new ArgumentException($"“{nameof(connectionStrings)}”不能为 null 或空。", nameof(connectionStrings));
            }

            _services.AddSingleton<IConnectionStrings>(new ConnectionStrings(connectionStrings))
                .AddSingleton<IDatabase, Database>()
                .AddSingleton(typeof(IQueryable<>), typeof(Queryable<>))
                .AddSingleton(typeof(IRepository<>), typeof(Repository<>));

            _services.TryAddSingleton(typeof(IRepositoryRouter<>), typeof(RepositoryRouter<>));
            _services.TryAddSingleton<IRepositoryExecutor, RepositoryExecutor>();

            return this;
        }

        /// <summary>
        /// 使用数据库。
        /// </summary>
        /// <typeparam name="TConnectionStrings">数据库链接。</typeparam>
        /// <remarks>
        /// 请在项目中，通过注入“<see cref="IDatabase{TConnectionStrings}"/>”访问数据库。
        /// </remarks>
        /// <returns>数据库构建器。</returns>
        public DatabaseLinqBuilder UseDatabase<TConnectionStrings>() where TConnectionStrings : class, IConnectionStrings
        {
            _services.AddSingleton<TConnectionStrings>();

            return this;
        }

        /// <summary>
        /// 使用数据库。
        /// </summary>
        /// <typeparam name="TConnectionStrings">数据库链接。</typeparam>
        /// <param name="connectionStrings">数据库链接。</param>
        /// <remarks>
        /// 请在项目中，通过注入“<see cref="IDatabase{TConnectionStrings}"/>”访问数据库。
        /// </remarks>
        /// <returns>数据库构建器。</returns>
        public DatabaseLinqBuilder UseDatabase<TConnectionStrings>(TConnectionStrings connectionStrings) where TConnectionStrings : class, IConnectionStrings
        {
            if (connectionStrings is null)
            {
                throw new ArgumentNullException(nameof(connectionStrings));
            }

            _services.AddSingleton(connectionStrings);

            return this;
        }

        private class ConnectionStrings : IConnectionStrings
        {
            private readonly string _connectionStrings;

            public ConnectionStrings(string connectionStrings)
            {
                _connectionStrings = connectionStrings;
            }

            public string Strings => _connectionStrings;
        }
    }
}
