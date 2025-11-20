using System.Collections.Generic;
using System.Reflection;

namespace Inkslab.Linq.PostgreSQL
{
    /// <summary>
    /// PostgreSQL 适配器。
    /// </summary>
    public class PostgreSQLAdapter : IDbAdapter
    {
        private readonly PostgreSQLCorrectSettings _settings;

        /// <summary>
        /// 适配器。
        /// </summary>
        public PostgreSQLAdapter()
        {
            _settings = new PostgreSQLCorrectSettings();
        }

        /// <inheritdoc/>
        public IDbCorrectSettings Settings => _settings;

        /// <inheritdoc/>
        public IReadOnlyDictionary<MethodInfo, IMethodVisitor> Visitors { get; } = new Dictionary<MethodInfo, IMethodVisitor>();
    }
}
