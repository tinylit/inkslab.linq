using Inkslab.Linq;

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace System.Data
#pragma warning restore IDE0130 // 命名空间与文件夹结构不匹配
{
    /// <summary>
    /// 数据库类型扩展。
    /// </summary>
    public static class DbTypeExtensions
    {
        /// <summary>
        /// 是否为 JSON 类型，<see cref="LookupDb.JsonDbType"/>。
        /// </summary>
        /// <param name="dbType">数据库类型。</param>
        /// <returns>是否为 JSON 类型。</returns>
        public static bool IsJsonType(this DbType dbType)
        {
            return dbType == LookupDb.JsonDbType;
        }

        /// <summary>
        /// 是否为 JSONB 类型，<see cref="LookupDb.JsonbDbType"/>。
        /// </summary>
        /// <param name="dbType">数据库类型。</param>
        /// <returns>是否为 JSONB 类型。</returns>
        public static bool IsJsonbType(this DbType dbType)
        {
            return dbType == LookupDb.JsonbDbType;
        }
    }
}