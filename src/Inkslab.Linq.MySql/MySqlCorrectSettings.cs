using System.Collections.Generic;
using System.Text;

namespace Inkslab.Linq.MySql
{
    /// <summary>
    /// MySQL 矫正配置。
    /// </summary>
    public class MySqlCorrectSettings : IDbCorrectSettings
    {
        private readonly IReadOnlyCollection<IFormatter> _formatters;

        /// <summary>
        /// 适配器。
        /// </summary>
        public MySqlCorrectSettings()
        {
            _formatters = new List<IFormatter>(0);
        }

        /// <summary>
        /// 适配器。
        /// </summary>
        /// <param name="formatters">格式化器。</param>
        public MySqlCorrectSettings(IEnumerable<IFormatter> formatters)
        {
            _formatters = new List<IFormatter>(formatters);
        }

        /// <inheritdoc/>
        public DatabaseEngine Engine => DatabaseEngine.MySQL;

        /// <inheritdoc/>
        public IReadOnlyCollection<IFormatter> Formatters => _formatters;

        /// <inheritdoc/>
        public string Name(string name) => string.Concat("`", name, "`");
        /// <inheritdoc/>
        public string ParamterName(string name) => string.Concat("?", name);
        /// <inheritdoc/>
        public virtual string ToSQL(string sql, int take, int skip, string orderBy)
        {
            var sb = new StringBuilder(sql.Length + 15);

            sb.Append(sql)
                .Append(orderBy)
                .Append(" LIMIT ");

            if (skip > 0)
            {
                sb.Append(skip)
                    .Append(',');
            }

            return sb.Append(take)
                .ToString();
        }
    }
}
