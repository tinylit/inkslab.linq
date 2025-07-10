using System.Data.Common;

namespace Inkslab.Linq
{
    /// <summary>
    /// 事务链接（在事务单元“<see cref="Transactions.TransactionUnit"/>”或事务范围“<see cref="System.Transactions.TransactionScope"/>”内的链接）。
    /// </summary>
    public interface ITransactionLink
    {
        /// <summary>
        /// 事务。
        /// </summary>
        DbTransaction Transaction { get; }

        /// <summary>
        /// 链接。
        /// </summary>
        DbConnection Connection { get; }
    }
}
