using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Inkslab.Linq
{
    /// <summary>
    /// 数据库构建器。
    /// </summary>
    public sealed class DatabaseLinqBuilder
    {
        private readonly DatabaseEngine _engine;
        private readonly Type _dbAdapterType;
        private readonly IServiceCollection _services;

        /// <summary>
        /// 数据库构建器。
        /// </summary>
        /// <param name="engine">数据库引擎。</param>
        /// <param name="dbAdapterType">数据库适配器类型。</param>
        /// <param name="services">服务集合。</param>
        public DatabaseLinqBuilder(DatabaseEngine engine, Type dbAdapterType, IServiceCollection services)
        {
            _engine = engine;
            _dbAdapterType = dbAdapterType;
            _services = services;
        }

        /// <summary>
        /// 服务池。
        /// </summary>
        public IServiceCollection Services => _services;

        /// <summary>
        /// 使用 Linq 语法（全局只能注册一次）。
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

            var serviceType = typeof(DbStrictAdapter);

            if (_services.Any(x => x.ServiceType == serviceType))
            {
                throw new InvalidOperationException($"当前方法“{nameof(UseLinq)}”已注册，请勿重复注册！");
            }

            _services.AddSingleton(sp => new DbStrictAdapter(_engine, (IDbAdapter)sp.GetRequiredService(_dbAdapterType)))
                .AddSingleton<IDatabaseStrings>(new DatabaseStrings(_engine, connectionStrings))
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
            var serviceType = typeof(TConnectionStrings);

            if (_services.Any(x => x.ServiceType == serviceType))
            {
                throw new InvalidOperationException($"一个类型只能用于一个数据库连接，数据库连接（{serviceType.Name}）不能重复注册！");
            }

            _services.AddSingleton<TConnectionStrings>();

            _services.AddSingleton<IDatabaseStrings<TConnectionStrings>>(sp => new DatabaseStrings<TConnectionStrings>(_engine, sp.GetRequiredService<TConnectionStrings>()));

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

            var serviceType = typeof(TConnectionStrings);

            if (_services.Any(x => x.ServiceType == serviceType))
            {
                throw new InvalidOperationException($"一个类型只能用于一个数据库连接，数据库连接（{serviceType.Name}）不能重复注册！");
            }

            _services.AddSingleton(connectionStrings);

            _services.AddSingleton<IDatabaseStrings<TConnectionStrings>>(new DatabaseStrings<TConnectionStrings>(_engine, connectionStrings));

            return this;
        }

        private class DatabaseStrings<TConnectionStrings> : IDatabaseStrings<TConnectionStrings> where TConnectionStrings : class, IConnectionStrings
        {
            private readonly TConnectionStrings _connectionStrings;

            public DatabaseStrings(DatabaseEngine engine, TConnectionStrings connectionStrings)
            {
                Engine = engine;
                _connectionStrings = connectionStrings;
            }

            public DatabaseEngine Engine { get; }

            public string Strings => _connectionStrings.Strings;
        }

        private class DatabaseStrings : IDatabaseStrings
        {
            public DatabaseStrings(DatabaseEngine engine, string connectionStrings)
            {
                Engine = engine;
                Strings = connectionStrings;
            }

            public string Strings { get; }

            public DatabaseEngine Engine { get; }
        }
    }
}
