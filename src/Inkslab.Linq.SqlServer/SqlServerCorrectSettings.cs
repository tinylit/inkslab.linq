#if NET6_0_OR_GREATER
using System.Globalization;
#else
using System.Text;
#endif
using System;

namespace Inkslab.Linq.SqlServer
{
    /// <summary>
    /// SqlServer矫正设置。
    /// </summary>
    public class SqlServerCorrectSettings : IDbCorrectSettings
    {
        /// <inheritdoc/>
        public string Name(string name) => string.Concat("[", name, "]");

        /// <inheritdoc/>
        public string ParamterName(string name) => string.Concat("@", name);

        /// <inheritdoc/>
        public virtual string ToSQL(string sql, int take, int skip, string orderBy)
        {
            if (string.IsNullOrEmpty(sql))
            {
                throw new ArgumentException("SQL 不能为 null 或空字符串。", nameof(sql));
            }

#if NET6_0_OR_GREATER
            if (orderBy?.Length > 0)
            {
                return take > 0
                    ? string.Create(CultureInfo.CurrentCulture, $"{sql} {orderBy} OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY")
                    : string.Create(CultureInfo.CurrentCulture, $"{sql} {orderBy} OFFSET {skip} ROWS");
            }

            return take > 0
                ? string.Create(CultureInfo.CurrentCulture, $"{sql} OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY")
                : string.Create(CultureInfo.CurrentCulture, $"{sql} OFFSET {skip} ROWS");
#else
            var sb = new StringBuilder(sql.Length + (orderBy?.Length ?? 0) + (take > 0 ? 40 : 18));

            sb.Append(sql);

            if (orderBy?.Length > 0)
            {
                sb.Append(' ').Append(orderBy);
            }

            sb.Append(" OFFSET ")
               .Append(skip)
               .Append(" ROWS");

            if (take > 0)
            {
                sb.Append(" FETCH NEXT ")
                    .Append(take)
                    .Append(" ROWS ONLY");
            }

            return sb.ToString();
#endif
        }
    }
}
