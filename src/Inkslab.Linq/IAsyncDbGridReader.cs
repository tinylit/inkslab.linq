using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Linq
{
    /// <summary>
    /// 异步网格读取器。
    /// </summary>
    public interface IAsyncDbGridReader : IAsyncDisposable
    {
        /// <summary>
        /// 读取第一个。
        /// </summary>
        /// <param name="rowStyle">行格式。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <returns>元素。</returns>
        Task<T> ReadAsync<T>(RowStyle rowStyle, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取列表。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <returns>元素集合。</returns>
        Task<List<T>> ReadAsync<T>(CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取元素迭代器。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <returns>元素集合迭代器。</returns>
        IAsyncEnumerable<T> QueryAsync<T>();
    }
}