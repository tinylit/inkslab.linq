using System.Collections.Generic;
using System.Data.Common;

namespace Inkslab.Linq
{
    /// <summary>
    /// 管理连接。
    /// </summary>
    public class DefaultConnections : IConnections
    {
        private readonly IEnumerable<IDbConnectionFactory> _factories;

        /// <summary>
        /// 默认连接管理器。
        /// </summary>
        /// <param name="factories">工厂。</param>
        public DefaultConnections(IEnumerable<IDbConnectionFactory> factories)
        {
            _factories = factories;
        }

        /// <inheritdoc/>
        public DbConnection Get(IConnection databaseStrings)
        {
            foreach (var factory in _factories)
            {
                if (factory.Engine == databaseStrings.Engine)
                {
                    return factory.Create(databaseStrings.Strings);
                }
            }

            throw new KeyNotFoundException($"未找到匹配的数据库引擎：{databaseStrings.Engine}, 请检查连接字符串或工厂配置。");
        }
    }
}
