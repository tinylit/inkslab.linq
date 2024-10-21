using System.Collections.Generic;
using Inkslab.Linq.Enums;

namespace Inkslab.Linq
{
    /// <summary>
    /// 表信息。
    /// </summary>
    public interface ITableInfo
    {
        /// <summary>
        /// 表的架构。
        /// </summary>
        public string Schema { get; }

        /// <summary>
        /// 表名称。
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 主键（属性名称）。
        /// </summary>
        IReadOnlyCollection<string> Keys { get; }

        /// <summary>
        /// 只读字段（属性名称）。
        /// </summary>
        IReadOnlyCollection<string> ReadOnlys { get; }

        /// <summary>
        /// 版本幂等字段（属性名称）。
        /// </summary>
        IReadOnlyDictionary<string, VersionKind> Versions { get; }

        /// <summary>
        /// 全部字段（属性名称：字段信息）。
        /// </summary>
        IReadOnlyDictionary<string, string> Fields { get; }
    }
}
