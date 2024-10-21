using System;
using System.Collections.Generic;

namespace Inkslab.Linq
{
    /// <summary>
    /// 网格读取器。
    /// </summary>
    public interface IDbGridReader : IDisposable
    {
        /// <summary>
        /// 读取第一个。
        /// </summary>
        /// <param name="rowStyle">行格式。</param>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <returns>元素。</returns>
        T Read<T>(RowStyle rowStyle);

        /// <summary>
        /// 读取列表。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <returns>元素集合。</returns>
        List<T> Read<T>();

        /// <summary>
        /// 读取元素迭代器。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <returns>元素集合迭代器。</returns>
        IEnumerable<T> Query<T>();
    }
}