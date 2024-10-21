using System;

namespace Inkslab.Linq.Annotations
{
    /// <summary>
    /// 表。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TableAttribute : Attribute
    {
        /// <summary>
        /// 表。
        /// </summary>
        /// <param name="name">名称。</param>
        /// <exception cref="ArgumentException">“<paramref name="name"/>”为“null”或空字符。</exception>
        public TableAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"“{nameof(name)}”不能为 null 或空。", nameof(name));
            }

            Name = name;
        }

        /// <summary>
        /// 获取将映射到的表的类名称。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 获取或设置将类映射到的表的架构。
        /// </summary>
        public string Schema { get; set; }
    }
}
