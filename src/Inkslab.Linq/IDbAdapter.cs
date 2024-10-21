using System.Collections.Generic;
using System.Reflection;

namespace Inkslab.Linq
{
    /// <summary>
    /// 数据适配器。
    /// </summary>
    public interface IDbAdapter
    {
        /// <summary>
        /// 数据库引擎。
        /// </summary>
        DatabaseEngine Engine { get; }

        /// <summary>
        /// 设置。
        /// </summary>
        IDbCorrectSettings Settings { get; }

        /// <summary>
        /// 访问器。
        /// </summary>
        IReadOnlyDictionary<MethodInfo, IMethodVisitor> Visitors { get; }
    }
}
