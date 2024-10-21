using Microsoft.Extensions.DependencyInjection;
using System;

namespace Inkslab.Linq
{
    /// <summary>
    /// 数据库构建器。
    /// </summary>
    public sealed class DatabaseLinqBuilder
    {
        private readonly IServiceCollection _services;

        internal DatabaseLinqBuilder(IServiceCollection services)
        {
            _services = services;
        }

        /// <summary>
        /// 使用数据库。
        /// </summary>
        /// <typeparam name="TConnectionStrings">数据库链接。</typeparam>
        /// <param name="configureServices">配置其它注入。</param>
        /// <remarks>
        /// 请在项目中，通过注入“<see cref="IDatabase{TConnectionStrings}"/>”访问数据库。
        /// </remarks>
        /// <returns>数据库构建器。</returns>
        public DatabaseLinqBuilder UseDatabase<TConnectionStrings>(Action<IServiceCollection> configureServices = null) where TConnectionStrings : class, IConnectionStrings
        {
            _services.AddSingleton<TConnectionStrings>();

            configureServices?.Invoke(_services);

            return this;
        }
    }
}
