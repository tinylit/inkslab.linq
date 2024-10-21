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
        /// 使用Linq。
        /// </summary>
        /// <param name="services">服务池。</param>
        /// <param name="connectionStrings">数据库链接。</param>
        /// <returns>服务池。</returns>
        public static DatabaseLinqBuilder UseSqlServer(this IServiceCollection services, string connectionStrings)
        {
            return services.AddSingleton<IBulkAssistant, SqlServerBulkAssistant>()
                 .UseLinq<SqlServerAdapter>(connectionStrings, connectionString =>
                 {
                     return new SqlConnection(connectionString);
                 });
        }
    }
}
