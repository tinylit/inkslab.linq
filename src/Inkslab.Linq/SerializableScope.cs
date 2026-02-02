using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Linq
{
    /// <summary>
    /// 串行化范围（在该范围中，相同连接字符串获取相同数据库连接）。
    /// </summary>
    /// <remarks>
    /// 使用示例：
    /// <code>
    /// await using (new SerializableScope())
    /// {
    ///   // 在该范围内，相同连接字符串获取相同数据库连接。
    ///   await ...;
    /// }
    /// </code>
    /// 注意事项：
    /// - 串行化范围内的不能同时存在多个事务。
    /// - 串行化范围支持嵌套，内层范围共享外层范围的数据库连接。
    /// - 串行化范围适用于需要在多个操作中共享数据库连接的场景，如事务处理等。
    /// </remarks>
    public class SerializableScope : IAsyncDisposable, IDisposable
    {
        private readonly Serializable _serializable;
        private readonly Serializable _previousSerializable;

        /// <summary>
        /// 串行化范围（在该范围中，相同连接字符串获取相同数据库连接）。
        /// </summary>
        public SerializableScope()
        {
            _serializable = _previousSerializable = Serializable.Current;

            if (_serializable is null)
            {
                Serializable.Current = _serializable = new Serializable();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Serializable.Current = _previousSerializable;

            if (_previousSerializable is null)
            {
                _serializable.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            Serializable.Current = _previousSerializable;

            if (_previousSerializable is null)
            {
                await _serializable.DisposeAsync();
            }

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// 串行化。
    /// </summary>
    public class Serializable : IAsyncDisposable, IDisposable
    {
        private static readonly AsyncLocal<SerializableHolder> _serializableCurrent = new AsyncLocal<SerializableHolder>();

        /// <summary>
        /// 当前可序列化。
        /// </summary>
        public static Serializable Current
        {
            get
            {
                var serializableHolder = _serializableCurrent.Value;

                if (serializableHolder is null)
                {
                    return null;
                }

                var serializable = serializableHolder.Serializable;

                if (serializable is null || serializable._disposed)
                {
                    return null;
                }

                return serializable;
            }

            set
            {
                var holder = _serializableCurrent.Value;

                if (holder is null)
                {

                }
                else
                {
                    holder.Serializable = null;
                }

                if (value is null)
                {

                }
                else
                {
                    _serializableCurrent.Value = new SerializableHolder { Serializable = value };
                }
            }
        }

        private readonly object _lockObject = new object();
        private readonly Dictionary<string, DbConnection> _connections = new Dictionary<string, DbConnection>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 串行化。
        /// </summary>
        public Serializable() => Trusteeship = Transactions.Transaction.Current is null && System.Transactions.Transaction.Current is null;

        /// <summary>
        /// 托管状态。
        /// </summary>
        public bool Trusteeship { private set; get; }

        /// <summary>
        /// 获取数据库连接。
        /// </summary>
        /// <param name="connections">数据库连接集合。</param>
        /// <param name="connectionStrings">连接字符串。</param>
        /// <returns>是否成功获取数据库连接。</returns>
        public DbConnection Get(IConnections connections, IConnection connectionStrings)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Serializable));
            }

            if (connections is null)
            {
                throw new ArgumentNullException(nameof(connections));
            }

            if (connectionStrings is null)
            {
                throw new ArgumentNullException(nameof(connectionStrings));
            }

            if (_connections.TryGetValue(connectionStrings.Strings, out var connection))
            {
                return connection;
            }

            lock (_lockObject)
            {
                if (_connections.TryGetValue(connectionStrings.Strings, out connection))
                {
                    return connection;
                }

                connection = connections.Get(connectionStrings);

                _connections[connectionStrings.Strings] = connection;
            }

            return connection;
        }

        /// <summary>
        /// 状态。
        /// </summary>
        private bool _disposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (var connection in _connections.Values)
            {
                if (connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }

                connection.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (var connection in _connections.Values)
            {
                if (connection.State != ConnectionState.Closed)
                {
                    await connection.CloseAsync();
                }

                await connection.DisposeAsync();
            }

            GC.SuppressFinalize(this);
        }

        private sealed class SerializableHolder
        {
            public Serializable Serializable;
        }
    }
}