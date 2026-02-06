using System.Text;

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
            var sb = new StringBuilder(sql.Length + orderBy.Length + (take > 0 ? 40 : 18));

            sb.Append(sql)
               .Append(' ')
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
