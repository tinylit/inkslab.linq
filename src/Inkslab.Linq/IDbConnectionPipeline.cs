using System.Data.Common;

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
        /// <param name="connectionStrings">数据库链接字符串。</param>
        /// <returns>数据库链接。</returns>
        DbConnection Get(string connectionStrings);
    }
}
