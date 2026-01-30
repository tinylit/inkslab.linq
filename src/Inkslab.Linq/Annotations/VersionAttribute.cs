using System;

namespace Inkslab.Linq.Annotations
{
    /// <summary>
    /// 版本幂等标识（增删改会自动生成有序增长的值），支持属性类型如下：<br/>
    /// <see cref="int"/>：递增。<br/>
    /// <see cref="long"/>：获取 <see cref="DateTime.Ticks"/> of <seealso cref="DateTime.Now"/>。<br/>
    /// <see cref="double"/>：获取 <see cref="DateTime.UtcNow"/> - <seealso cref="DateTime.UnixEpoch"/>。<br/>
    /// <see cref="DateTime"/>：获取 <see cref="DateTime.Now"/>。<br/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class VersionAttribute : Attribute { }
}
