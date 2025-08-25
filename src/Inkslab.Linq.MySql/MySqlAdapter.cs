using System.Collections.Generic;
using System.Reflection;

namespace Inkslab.Linq.MySql
{
    /// <summary>
    /// MySQL 适配器。
    /// </summary>
    public class MySqlAdapter : IDbAdapter
    {
        private readonly MySqlCorrectSettings _settings;

        /// <summary>
        /// 适配器。
        /// </summary>
        public MySqlAdapter()
        {
            _settings = new MySqlCorrectSettings();
        }

        /// <inheritdoc/>
        public DatabaseEngine Engine => DatabaseEngine.MySQL;
        /// <inheritdoc/>
        public IDbCorrectSettings Settings => _settings;
        /// <inheritdoc/>
        public IReadOnlyDictionary<MethodInfo, IMethodVisitor> Visitors { get; } = new Dictionary<MethodInfo, IMethodVisitor>();
    }
}
