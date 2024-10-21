using Inkslab.Linq.Enums;

namespace Inkslab.Linq
{
    /// <summary>
    /// 字段。
    /// </summary>
    public readonly struct Field
    {
        /// <summary>
        /// 初始字段属性。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <param name="primaryKey">是否主键。</param>
        /// <param name="readOnly">是否只读。</param>
        /// <param name="version">版本控制。</param>
        public Field(string name, bool primaryKey, bool readOnly, VersionKind version = VersionKind.None)
        {
            Name = name;
            PrimaryKey = primaryKey;
            ReadOnly = readOnly;
            Version = version;
        }

        /// <summary>
        /// 字段名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 主键。
        /// </summary>
        public bool PrimaryKey { get; }

        /// <summary>
        /// 只读。
        /// </summary>
        public bool ReadOnly { get; }

        /// <summary>
        /// 版本控制。
        /// </summary>
        public VersionKind Version { get; }
    }
}
