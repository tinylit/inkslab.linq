using System.Collections.Generic;
using System.Data.Common;

namespace Inkslab.Linq
{
    /// <summary>
    /// 管理连接。
    /// </summary>
    public class DefaultConnections : IConnections
    {
        private readonly Dictionary<DatabaseEngine, IDbConnectionFactory> _factoryMaps = new Dictionary<DatabaseEngine, IDbConnectionFactory>(1);

        /// <summary>
        /// 默认连接管理器。
        /// </summary>
        /// <param name="factories">工厂。</param>
        public DefaultConnections(IEnumerable<IDbConnectionFactory> factories)
        {
            foreach (var factory in factories)
            {
                _factoryMaps[factory.Engine] = factory;
            }
        }

        /// <inheritdoc/>
        public DbConnection Get(IConnection databaseStrings)
        {
            if (_factoryMaps.TryGetValue(databaseStrings.Engine, out var factory))
            {
                return factory.Create(databaseStrings.Strings);
            }

            throw new KeyNotFoundException($"未找到匹配的数据库引擎：{databaseStrings.Engine}, 请检查连接字符串或工厂配置。");
        }
    }
}