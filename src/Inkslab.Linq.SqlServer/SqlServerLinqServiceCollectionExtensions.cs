using Inkslab.Linq;
using Inkslab.Linq.SqlServer;
using Microsoft.Data.SqlClient;

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Microsoft.Extensions.DependencyInjection
#pragma warning restore IDE0130 // 命名空间与文件夹结构不匹配
{
    /// <summary>
    /// Linq 服务池。
    /// </summary>
    public static class SqlServerLinqServiceCollectionExtensions
    {
        /// <summary>
        /// 使用 SqlServer 数据库引擎。
        /// </summary>
        /// <param name="services">服务池。</param>
        /// <returns>服务池。</returns>
        public static DatabaseLinqBuilder UseSqlServer(this IServiceCollection services)
        {
            return services.AddSingleton<IDbConnectionBulkCopyFactory, SqlServerBulkCopyFactory>()
                 .UseEngine<SqlServerAdapter>(DatabaseEngine.SqlServer, connectionString =>
                 {
                     return new SqlConnection(connectionString);
                 });
        }
    }
}
