using Inkslab.Linq;
using Inkslab.Linq.PostgreSQL;
using Npgsql;

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Microsoft.Extensions.DependencyInjection
#pragma warning restore IDE0130 // 命名空间与文件夹结构不匹配
{
    /// <summary>
    /// Linq 服务池。
    /// </summary>
    public static class PostgreSQLLinqServiceCollectionExtensions
    {
        /// <summary>
        /// 使用 MySQL 数据库引擎。
        /// </summary>
        /// <param name="services">服务池。</param>
        /// <returns>服务池。</returns>
        public static DatabaseLinqBuilder UsePostgreSQL(this IServiceCollection services)
        {
            return services.AddSingleton<IDbConnectionBulkCopyFactory, PostgreSQLBulkCopyFactory>()
                 .UseEngine<PostgreSQLAdapter>(DatabaseEngine.PostgreSQL, connectionString =>
                 {
                     return new NpgsqlConnection(connectionString);
                 });
        }
    }
}
