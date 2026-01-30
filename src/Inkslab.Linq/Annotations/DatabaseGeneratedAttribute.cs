using System;

namespace Inkslab.Linq.Annotations
{
    /// <summary>
    /// 数据库生成特性（适用于实体模型的属性，指示数据库生成该列的值）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DatabaseGeneratedAttribute : Attribute
    {
    }
}