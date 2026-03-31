namespace Inkslab.Linq.Options
{
    /// <summary>
    /// 单个数据库引擎的执行器配置项，通过 <see cref="DatabaseLinqBuilder.Configure"/> 传入。
    /// </summary>
    public class DatabaseEngineOptions
    {
        /// <summary>
        /// 实体映射 LFU 缓存容量（当前引擎所能缓存的实体类型映射数量上限）。
        /// <para>容量决定了 LFU 缓存可持有的最大条目数，超出后驱逐最低频次的条目。</para>
        /// <para>默认值：<see cref="DatabaseExecutorOptions.DefaultMappingCapacity"/>。</para>
        /// </summary>
        public int MappingCapacity { get; set; } = DatabaseExecutorOptions.DefaultMappingCapacity;
    }
}