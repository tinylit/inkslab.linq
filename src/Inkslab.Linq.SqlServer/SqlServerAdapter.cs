using System.Collections.Generic;
using System.Reflection;

namespace Inkslab.Linq.SqlServer
{
    /// <summary>
    /// SqlServer 适配器。
    /// </summary>
    public class SqlServerAdapter : IDbAdapter
    {
        private readonly SqlServerCorrectSettings _settings;

        /// <summary>
        /// 适配器。
        /// </summary>
        /// <param name="formatters">格式化器。</param>
        public SqlServerAdapter(IEnumerable<IFormatter> formatters)
        {
            _settings = new SqlServerCorrectSettings(formatters);
        }

        /// <inheritdoc/>
        public DatabaseEngine Engine => DatabaseEngine.SqlServer;
        /// <inheritdoc/>
        public IDbCorrectSettings Settings => _settings;
        /// <inheritdoc/>
        public IReadOnlyDictionary<MethodInfo, IMethodVisitor> Visitors { get; } = new Dictionary<MethodInfo, IMethodVisitor>();
    }
}
