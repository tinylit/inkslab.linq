using System.Collections.Generic;
using System.Text;

namespace Inkslab.Linq.SqlServer
{
    /// <summary>
    /// SqlServer矫正设置。
    /// </summary>
    public class SqlServerCorrectSettings : IDbCorrectSettings
    {
        private readonly IReadOnlyCollection<IFormatter> _formatters;

        /// <summary>
        /// 适配器。
        /// </summary>
        public SqlServerCorrectSettings()
        {
            _formatters = new List<IFormatter>(0);
        }

        /// <summary>
        /// 适配器。
        /// </summary>
        /// <param name="formatters">格式化器。</param>
        public SqlServerCorrectSettings(IEnumerable<IFormatter> formatters)
        {
            _formatters = new List<IFormatter>(formatters);
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<IFormatter> Formatters => _formatters;

        /// <inheritdoc/>
        public string Name(string name) => string.Concat("[", name, "]");

        /// <inheritdoc/>
        public string ParamterName(string name) => string.Concat("@", name);

        /// <inheritdoc/>
        public virtual string ToSQL(string sql, int take, int skip, string orderBy)
        {
            var sb = new StringBuilder(sql.Length + (take > 0 ? 40 : 18));

            sb.Append(sql)
               .Append(orderBy)
               .Append(" OFFSET ")
               .Append(skip)
               .Append(" ROWS");

            if (take > 0)
            {
                sb.Append(" FETCH NEXT ")
                    .Append(take)
                    .Append(" ROWS ONLY");
            }

            return sb.ToString();
        }
    }
}
