using System;

namespace Inkslab.Linq.Annotations
{
    /// <summary>
    /// 名称（字段名称）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FieldAttribute : Attribute
    {
        /// <summary>
        /// 名称。
        /// </summary>
        /// <param name="name">名称。</param>
        public FieldAttribute(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"“{nameof(name)}”不能为 null 或空。", nameof(name));
            }

            Name = name;
        }

        /// <summary>
        /// 获取将映射到的类的字段名称。
        /// </summary>
        public string Name { get; }
    }
}
