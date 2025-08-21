using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Inkslab.Linq
{
    /// <summary>
    /// 数据适配器。
    /// </summary>
    public interface IDbAdapter
    {
        /// <summary>
        /// 设置。
        /// </summary>
        IDbCorrectSettings Settings { get; }

        /// <summary>
        /// 访问器。
        /// </summary>
        IReadOnlyDictionary<MethodInfo, IMethodVisitor> Visitors { get; }
    }

    /// <summary>
    /// 数据库严格适配器。
    /// </summary>
    public sealed class DbStrictAdapter
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="engine">数据库引擎。</param>
        /// <param name="adapter">数据库适配器。</param>
        public DbStrictAdapter(DatabaseEngine engine, IDbAdapter adapter)
        {
            Engine = engine;
            Settings = adapter.Settings;
            Visitors = adapter.Visitors;
        }

        /// <summary>
        /// 数据库引擎。
        /// </summary>
        public DatabaseEngine Engine { get; }

        /// <summary>
        /// 设置。
        /// </summary>
        public IDbCorrectSettings Settings { get; }

        /// <summary>
        /// 访问器。
        /// </summary>
        public IReadOnlyDictionary<MethodInfo, IMethodVisitor> Visitors { get; }
    }
}
