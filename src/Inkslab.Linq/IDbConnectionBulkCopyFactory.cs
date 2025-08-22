using System.Data.Common;

namespace Inkslab.Linq
{
    /// <summary>
    /// 数据库连接批量复制工厂。
    /// </summary>
    public interface IDbConnectionBulkCopyFactory
    {
        /// <summary>
        /// 获取数据库引擎类型。
        /// </summary>
        DatabaseEngine Engine { get; }

        /// <summary>
        /// 创建一个新的数据库连接批量复制实例。
        /// </summary>
        /// <param name="connection">数据库连接。</param>
        /// <param name="transaction">数据库事务。</param>
        /// <returns>数据库连接批量复制实例。</returns>
        IDatabaseBulkCopy Create(DbConnection connection, DbTransaction transaction = null);
    }
}