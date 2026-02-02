using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Linq
{
    /// <summary>
    /// 可序列化范围。
    /// </summary>
    public class SerializableScope : IAsyncDisposable, IDisposable
    {
        private readonly Serializable _serializable;
        private readonly Serializable _previousSerializable;

        /// <summary>
        /// 可序列化范围。
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
    /// 可序列化。
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
        /// 状态。
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// 获取数据库连接。
        /// </summary>
        /// <param name="connections">数据库连接集合。</param>
        /// <param name="connectionStrings">连接字符串。</param>
        /// <returns>数据库连接。</returns>
        public DbConnection Get(IConnections connections, IConnection connectionStrings)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Serializable));
            }

            ArgumentNullException.ThrowIfNull(connections);
            ArgumentNullException.ThrowIfNull(connectionStrings);
            
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
        /// 尝试添加数据库连接。
        /// </summary>
        /// <param name="connectionStrings">连接字符串。</param>
        /// <param name="connection">数据库连接。</param>
        /// <returns>是否添加成功。</returns>
        public bool TryAdd(IConnection connectionStrings, DbConnection connection)
        {
            ArgumentNullException.ThrowIfNull(connectionStrings);
            ArgumentNullException.ThrowIfNull(connection);
            
            return _connections.TryAdd(connectionStrings.Strings, connection);
        }

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