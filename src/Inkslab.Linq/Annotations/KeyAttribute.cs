using System;

namespace Inkslab.Linq.Annotations
{
    /// <summary>
    /// 是否主键。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class KeyAttribute : Attribute
    {
    }
}
