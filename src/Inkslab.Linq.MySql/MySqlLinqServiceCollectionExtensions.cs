﻿using Inkslab.Linq;
using Inkslab.Linq.MySql;
using MySqlConnector;

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Microsoft.Extensions.DependencyInjection
#pragma warning restore IDE0130 // 命名空间与文件夹结构不匹配
{
    /// <summary>
    /// Linq 服务池。
    /// </summary>
    public static class MySqlLinqServiceCollectionExtensions
    {
        /// <summary>
        /// 使用 MySQL 数据库引擎。
        /// </summary>
        /// <param name="services">服务池。</param>
        /// <returns>服务池。</returns>
        public static DatabaseLinqBuilder UseMySql(this IServiceCollection services)
        {
            return services.AddSingleton<IBulkAssistant, MySqlBulkAssistant>()
                 .UseEngine<MySqlAdapter>(connectionString =>
                 {
                     return new MySqlConnection(connectionString);
                 });
        }
    }
}
