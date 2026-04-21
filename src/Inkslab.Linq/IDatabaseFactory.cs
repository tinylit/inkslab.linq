using System.Collections.Generic;

namespace Inkslab.Linq
{
    /// <summary>
    /// 数据库工厂。
    /// </summary>
    /// <remarks>
    /// 按运行时传入的引擎与连接字符串创建 <see cref="IDatabase"/> 会话。<br/>
    /// 适用于多租户、多数据源切换、按上下文动态选库等场景；不依赖 <c>UseLinq</c> 的单例注册。<br/>
    /// 注册方式：先调用一次或多次 <c>UseXxx()</c>（如 <c>UseMySql</c>、<c>UsePostgreSQL</c>、<c>UseSqlServer</c>），
    /// 然后调用 <c>services.AddDatabaseFactory()</c>。
    /// </remarks>
    public interface IDatabaseFactory
    {
        /// <summary>
        /// 已注册可用的数据库引擎集合。
        /// </summary>
        IReadOnlyCollection<DatabaseEngine> RegisteredEngines { get; }

        /// <summary>
        /// 根据指定引擎与连接字符串创建数据库会话。
        /// </summary>
        /// <param name="engine">数据库引擎。</param>
        /// <param name="connectionString">连接字符串。</param>
        /// <returns>数据库会话。</returns>
        IDatabase Create(DatabaseEngine engine, string connectionString);
    }
}
