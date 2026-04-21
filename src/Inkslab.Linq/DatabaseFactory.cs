using System;
using System.Collections.Generic;
using System.Linq;

namespace Inkslab.Linq
{
    /// <summary>
    /// 数据库工厂默认实现。
    /// </summary>
    /// <remarks>
    /// 每次 <see cref="Create"/> 调用仅构造轻量 <see cref="IDatabase"/> 会话对象，
    /// 共享全局 <see cref="IDatabaseExecutor"/> 与 <see cref="IConnections"/>，
    /// 不产生额外的连接或适配器开销。
    /// </remarks>
    public sealed class DatabaseFactory : IDatabaseFactory
    {
        private readonly IDatabaseExecutor _executor;
        private readonly HashSet<DatabaseEngine> _engines;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="executor">数据库执行器。</param>
        /// <param name="connectionFactories">已注册的连接工厂集合（用于校验引擎可用性）。</param>
        public DatabaseFactory(IDatabaseExecutor executor, IEnumerable<IDbConnectionFactory> connectionFactories)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));

            if (connectionFactories is null)
            {
                throw new ArgumentNullException(nameof(connectionFactories));
            }

            _engines = new HashSet<DatabaseEngine>(connectionFactories.Select(x => x.Engine));
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<DatabaseEngine> RegisteredEngines => _engines;

        /// <inheritdoc/>
        public IDatabase Create(DatabaseEngine engine, string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException($"“{nameof(connectionString)}”不能为 null 或空。", nameof(connectionString));
            }

            if (!_engines.Contains(engine))
            {
                throw new InvalidOperationException($"数据库引擎“{engine}”未注册，请先调用对应的 Use{engine}() 扩展方法。");
            }

            return new Database(_executor, new DatabaseStrings(engine, connectionString));
        }

        private sealed class DatabaseStrings : IDatabaseStrings
        {
            public DatabaseStrings(DatabaseEngine engine, string strings)
            {
                Engine = engine;
                Strings = strings;
            }

            public DatabaseEngine Engine { get; }

            public string Strings { get; }
        }
    }
}
