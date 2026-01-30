using System.Text;

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
            var sb = new StringBuilder(sql.Length + 15);

            sb.Append(sql)
                .Append(' ')
                .Append(orderBy)
                .Append(" LIMIT ");

            if (skip > 0)
            {
                sb.Append(skip)
                    .Append(" OFFSET ");
            }

            return sb.Append(take)
                .ToString();
        }
    }
}