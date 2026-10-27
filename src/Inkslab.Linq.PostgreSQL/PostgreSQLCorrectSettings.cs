#if NET6_0_OR_GREATER
using System.Globalization;
#else
using System.Text;
#endif
using System;

namespace Inkslab.Linq.PostgreSQL
{
    /// <summary>
    /// PostgreSQL 矫正配置。
    /// </summary>
    public class PostgreSQLCorrectSettings : IDbCorrectSettings
    {
        /// <inheritdoc/>
        public string Name(string name) => string.Concat("\"", name, "\"");

        /// <inheritdoc/>
        public string ParamterName(string name) => string.Concat("@", name);

        /// <inheritdoc/>
        public string ToSQL(string sql, int take, int skip, string orderBy)
        {
            if (string.IsNullOrEmpty(sql))
            {
                throw new ArgumentException("SQL 不能为 null 或空字符串。", nameof(sql));
            }

#if NET6_0_OR_GREATER
            if (orderBy?.Length > 0)
            {
                return skip > 0
                    ? string.Create(CultureInfo.CurrentCulture, $"{sql} {orderBy} LIMIT {take} OFFSET {skip}")
                    : string.Create(CultureInfo.CurrentCulture, $"{sql} {orderBy} LIMIT {take}");
            }

            return skip > 0
                ? string.Create(CultureInfo.CurrentCulture, $"{sql} LIMIT {take} OFFSET {skip}")
                : string.Create(CultureInfo.CurrentCulture, $"{sql} LIMIT {take}");
#else
            var sb = new StringBuilder(sql.Length + (orderBy?.Length ?? 0) + 25);

            sb.Append(sql);

            if (orderBy?.Length > 0)
            {
                sb.Append(' ').Append(orderBy);
            }

            sb.Append(" LIMIT ").Append(take);

            if (skip > 0)
            {
                sb.Append(" OFFSET ")
                    .Append(skip);
            }

            return sb.ToString();
#endif
        }
    }
}
