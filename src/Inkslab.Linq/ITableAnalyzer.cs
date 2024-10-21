using Inkslab.Linq.Options;
using System;

namespace Inkslab.Linq
{
    /// <summary>
    /// 表分析器。
    /// </summary>
    public interface ITableAnalyzer
    {
        /// <summary>
        /// 分析表。
        /// </summary>
        /// <param name="tableType">表类型。</param>
        /// <returns>表名称和字段消息。</returns>
        TabelOptions Table(Type tableType);
    }
}
