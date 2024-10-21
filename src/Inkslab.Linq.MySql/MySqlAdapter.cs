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
        /// <param name="formatters">格式化器。</param>
        public MySqlAdapter(IEnumerable<IFormatter> formatters)
        {
            _settings = new MySqlCorrectSettings(formatters);
        }

        /// <inheritdoc/>
        public DatabaseEngine Engine => DatabaseEngine.MySQL;
        /// <inheritdoc/>
        public IDbCorrectSettings Settings => _settings;
        /// <inheritdoc/>
        public IReadOnlyDictionary<MethodInfo, IMethodVisitor> Visitors { get; } = new Dictionary<MethodInfo, IMethodVisitor>();
    }
}
