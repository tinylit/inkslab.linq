using System.Data.Common;

namespace Inkslab.Linq
{
    /// <summary>
    /// 数据库工厂。
    /// </summary>
    public interface IDbConnectionFactory
    {
        /// <summary> 创建数据库连接。 </summary>
        /// <returns></returns>
        DbConnection Create(string connectionString);
    }
}
