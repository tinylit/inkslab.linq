using System;

namespace Inkslab.Linq
{
    /// <summary>
    /// T-SQL 领域。
    /// </summary>
    public interface ISqlDomain : IDisposable
    {
        /// <summary>
        /// 领域内容是否为空。
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// 领域有值。
        /// </summary>
        bool HasValue { get; }

        /// <summary>
        /// 插入指针回扫到领域起点。
        /// </summary>
        /// <returns></returns>
        void Flyback();

        /// <summary>
        /// 丢弃领域内容。
        /// </summary>
        void Discard();

        /// <summary>
        /// 获取领域内生成的T-SQL片段。
        /// </summary>
        /// <returns>T-SQL</returns>
        string ToString();
    }
}
