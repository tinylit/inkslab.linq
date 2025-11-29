using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Inkslab.Linq
{
    /// <summary>
    /// 类型。
    /// </summary>
    internal static class Types
    {
        /// <summary>
        /// <see cref="System.Linq.Enumerable"/>
        /// </summary>
        public static readonly Type Enumerable = typeof(Enumerable);

        /// <summary>
        /// <see cref="Conditions"/>
        /// </summary>
        public static readonly Type Conditions = typeof(Conditions);

        /// <summary>
        /// <see cref="IRank{T}"/>
        /// </summary>
        public static readonly Type Ranks = typeof(Ranks);

        /// <summary>
        /// <see cref="System.Linq.Queryable"/>
        /// </summary>
        public static readonly Type Queryable = typeof(Queryable);

        /// <summary>
        /// <see cref="System.Linq.IQueryable"/>
        /// </summary>
        public static readonly Type IQueryable = typeof(IQueryable);

        /// <summary>
        /// <see cref="IQueryable{T}"/>
        /// </summary>
        public static readonly Type IQueryableGeneric = typeof(IQueryable<>);

        /// <summary>
        /// <see cref="System.Linq.QueryableExtentions"/>
        /// </summary>
        public static readonly Type QueryableExtentions = typeof(QueryableExtentions);

        /// <summary>
        /// <see cref="bool"/>
        /// </summary>
        public static readonly Type Boolean = typeof(bool);

        /// <summary>
        /// <see cref="int"/>
        /// </summary>
        public static readonly Type Int32 = typeof(int);

        /// <summary>
        /// <see cref="char"/>
        /// </summary>
        public static readonly Type Char = typeof(char);

        /// <summary>
        /// <see cref="string"/>
        /// </summary>
        public static readonly Type String = typeof(string);

        /// <summary>
        /// <see cref="System.Text.Json.Nodes.JsonObject"/>
        /// </summary>
        public static readonly Type JsonObject = typeof(System.Text.Json.Nodes.JsonObject);

                /// <summary>
        /// <see cref="System.Text.Json.Nodes.JsonArray"/>
        /// </summary>
        public static readonly Type JsonArray = typeof(System.Text.Json.Nodes.JsonArray);

        /// <summary>
        /// <see cref="System.Text.Json.JsonDocument"/>
        /// </summary>
        public static readonly Type JsonDocument = typeof(System.Text.Json.JsonDocument);

        /// <summary>
        /// <see cref="Linq.JsonPayload"/>
        /// </summary>
        public static readonly Type JsonPayload = typeof(JsonPayload);

        /// <summary>
        /// <see cref="Linq.JsonbPayload"/>
        /// </summary>
        public static readonly Type JsonbPayload = typeof(JsonbPayload);

        /// <summary>
        /// <see cref="System.Guid"/>
        /// </summary>
        public static readonly Type Guid = typeof(Guid);

        /// <summary>
        /// <see cref="System.Version"/>
        /// </summary>
        public static readonly Type Version = typeof(Version);

        /// <summary>
        /// <see cref="System.DateTime"/>
        /// </summary>
        public static readonly Type DateTime = typeof(DateTime);

        /// <summary>
        /// <see cref="System.DateTimeOffset"/>
        /// </summary>
        public static readonly Type DateTimeOffset = typeof(DateTimeOffset);

        /// <summary>
        /// <see cref="IEnumerable{T}"/>
        /// </summary>
        public static readonly Type IEnumerable = typeof(IEnumerable);

        /// <summary>
        /// <see cref="System.Collections.IEnumerable"/>
        /// </summary>
        public static readonly Type IEnumerableGeneric = typeof(IEnumerable<>);

        /// <summary>
        /// <see cref="object"/>
        /// </summary>
        public static readonly Type Object = typeof(object);
    }
}
