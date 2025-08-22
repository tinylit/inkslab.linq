using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Linq
{
    /// <summary>
    /// 数据库链接管道。
    /// </summary>
    public interface IDbConnectionPipeline
    {
        /// <summary>
        /// 获取数据库链接。
        /// </summary>
        /// <param name="databaseStrings">数据库链接字符串。</param>
        /// <returns>数据库链接。</returns>
        DbConnection Get(IConnection databaseStrings);

        /// <summary>
        /// 获取数据库批量操作助手。
        /// </summary>
        /// <param name="databaseStrings">数据库链接字符串。</param>
        IDatabaseBulkCopy Create(IConnection databaseStrings);

        /// <summary>
        /// 获取数据库批量操作助手。
        /// </summary>
        /// <param name="databaseStrings">数据库链接字符串。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        Task<IDatabaseBulkCopy> CreateAsync(IConnection databaseStrings, CancellationToken cancellationToken);
    }
}
