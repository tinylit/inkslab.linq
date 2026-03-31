using System.Collections.Generic;

namespace Inkslab.Linq.Options
{
    /// <summary>
    /// 数据库执行器配置项。
    /// </summary>
    public class DatabaseExecutorOptions
    {
        /// <summary>
        /// 默认实体映射缓存容量。
        /// </summary>
        public const int DefaultMappingCapacity = 1000;

        private readonly Dictionary<DatabaseEngine, int> _capacities = new Dictionary<DatabaseEngine, int>();

        /// <summary>
        /// 获取指定引擎的实体映射缓存容量。
        /// <para>若未单独配置，返回 <see cref="MappingCapacity"/> 的值。</para>
        /// </summary>
        /// <param name="engine">数据库引擎。</param>
        public int GetMappingCapacity(DatabaseEngine engine) =>
            _capacities.TryGetValue(engine, out var capacity) ? capacity : MappingCapacity;

        /// <summary>
        /// 为指定引擎设置实体映射缓存容量。
        /// </summary>
        /// <param name="engine">数据库引擎。</param>
        /// <param name="capacity">缓存容量，必须大于 0。</param>
        public void SetMappingCapacity(DatabaseEngine engine, int capacity)
        {
            _capacities[engine] = capacity;
        }

        /// <summary>
        /// 全局默认实体映射缓存容量（每种数据库读取器所能缓存的实体类型映射数量上限）。
        /// <para>容量决定了 LFU 缓存可持有的最大条目数，超出后驱逐最低频次的条目。</para>
        /// <para>默认值：<see cref="DefaultMappingCapacity"/>。</para>
        /// </summary>
        public int MappingCapacity { get; set; } = DefaultMappingCapacity;
    }
}