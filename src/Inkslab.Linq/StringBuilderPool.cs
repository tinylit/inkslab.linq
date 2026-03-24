using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace Inkslab.Linq
{
    /// <summary>
    /// StringBuilder 对象池，用于优化内存分配。
    /// </summary>
    internal static class StringBuilderPool
    {
        private static readonly ObjectPool<StringBuilder> _pool;

        static StringBuilderPool()
        {
            var provider = new DefaultObjectPoolProvider();
            var policy = new StringBuilderPooledObjectPolicy
            {
                InitialCapacity = 256,
                MaximumRetainedCapacity = 4096
            };
            _pool = provider.Create(policy);
        }

        /// <summary>
        /// 从对象池获取 StringBuilder 实例。
        /// </summary>
        /// <param name="capacity">初始容量（可选）。</param>
        /// <returns>StringBuilder 实例。</returns>
        public static StringBuilder Get(int capacity = 256)
        {
            var sb = _pool.Get();
            
            // 确保容量满足需求
            if (sb.Capacity < capacity)
            {
                sb.Capacity = capacity;
            }
            
            return sb;
        }

        /// <summary>
        /// 将 StringBuilder 归还到对象池。
        /// </summary>
        /// <param name="sb">要归还的 StringBuilder。</param>
        public static void Return(StringBuilder sb)
        {
            if (sb != null)
            {
                _pool.Return(sb);
            }
        }
    }
}
