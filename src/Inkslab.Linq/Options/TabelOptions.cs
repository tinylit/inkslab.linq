using System.Collections.Generic;
using Inkslab.Linq.Enums;

namespace Inkslab.Linq.Options
{
    /// <summary>
    /// 列。
    /// </summary>
    public class Column
    {
        /// <summary>
        /// 字段。
        /// </summary>
        /// <param name="name">字段名称。</param>
        public Column(string name)
        {
            Name = name;
        }

        /// <summary>
        /// 字段名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 主键。
        /// </summary>
        public bool Key { get; set; }

        /// <summary>
        /// 只读。
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// 版本控制。
        /// </summary>
        public VersionKind Version { get; set; }

        /// <summary>
        /// 忽略。
        /// </summary>
        public bool Ignore { get; set; }
    }

    /// <summary>
    /// 表配置。
    /// </summary>
    public class TabelOptions
    {
        /// <summary>
        /// 表所在架构。
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// 表名称。
        /// </summary>
        public string Name { get; set; }

        private Dictionary<string, Column> _columns;

        /// <summary>
        /// 表字段。
        /// </summary>
        public Dictionary<string, Column> Columns { get => _columns ??= new Dictionary<string, Column>(); set => _columns = value; }
    }
}
