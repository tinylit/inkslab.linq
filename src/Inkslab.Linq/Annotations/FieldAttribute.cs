using System;

namespace Inkslab.Linq.Annotations
{
    /// <summary>
    /// 名称（字段名称）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class FieldAttribute : Attribute
    {
        /// <summary>
        /// 名称。
        /// </summary>
        /// <param name="name">名称。</param>
        public FieldAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"“{nameof(name)}”不能为 null 或空白。", nameof(name));
            }

            if (char.IsWhiteSpace(name[0]) || char.IsWhiteSpace(name[name.Length - 1]))
            {
                throw new ArgumentException($"“{nameof(name)}”不能以空白字符开头或结尾。", nameof(name));
            }

            Name = name;
        }

        /// <summary>
        /// 获取将映射到的类的字段名称。
        /// </summary>
        public string Name { get; }
    }
}
