#if NET6_0_OR_GREATER
using System.Globalization;
#else
using System.Text;
#endif
using System;

namespace Inkslab.Linq.MySql
{
    /// <summary>
    /// MySQL 矫正配置。
    /// </summary>
    public class MySqlCorrectSettings : IDbCorrectSettings
    {
        /// <inheritdoc/>
        public string Name(string name) => string.Concat("`", name, "`");
        /// <inheritdoc/>
        public string ParamterName(string name) => string.Concat("?", name);
        /// <inheritdoc/>
        public virtual string ToSQL(string sql, int take, int skip, string orderBy)
        {
            if (string.IsNullOrEmpty(sql))
            {
                throw new ArgumentException("SQL 不能为 null 或空字符串。", nameof(sql));
            }

#if NET6_0_OR_GREATER
            // 插值处理器直接生成结果字符串，避免 StringBuilder 及其缓冲区分配。
            if (orderBy?.Length > 0)
            {
                return skip > 0
                    ? string.Create(CultureInfo.CurrentCulture, $"{sql} {orderBy} LIMIT {skip},{take}")
                    : string.Create(CultureInfo.CurrentCulture, $"{sql} {orderBy} LIMIT {take}");
            }

            return skip > 0
                ? string.Create(CultureInfo.CurrentCulture, $"{sql} LIMIT {skip},{take}")
                : string.Create(CultureInfo.CurrentCulture, $"{sql} LIMIT {take}");
#else
            var sb = new StringBuilder(sql.Length + (orderBy?.Length ?? 0) + 18);

            sb.Append(sql);

            if (orderBy?.Length > 0)
            {
                sb.Append(' ').Append(orderBy);
            }

            sb.Append(" LIMIT ");

            if (skip > 0)
            {
                sb.Append(skip)
                    .Append(',');
            }

            return sb.Append(take)
                .ToString();
#endif
        }
    }
}
