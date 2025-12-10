using Inkslab.Linq;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace System
#pragma warning restore IDE0130 // 命名空间与文件夹结构不匹配
{
    /// <summary>
    /// 类型拓展类。
    /// </summary>
    internal static class TypeExtensions
    {
        static readonly Type _queryable_T_Type = typeof(IQueryable<>);
        static readonly Type _enumerable_T_Type = typeof(IEnumerable<>);
        static readonly Type _grouping_T1_T2_Type = typeof(IGrouping<,>);

        /// <summary>
        /// 是否为boolean类型或boolean可空类型。
        /// </summary>
        /// <param name="type">类型入参。</param>
        /// <returns></returns>
        public static bool IsBoolean(this Type type) => type.IsValueType && (type == typeof(bool) || type == typeof(bool?));

        /// <summary>
        /// 是否是查询器的派生类。
        /// </summary>
        /// <param name="type">类型。</param>
        /// <returns></returns>
        public static bool IsQueryable(this Type type) => Types.IQueryable.IsAssignableFrom(type);

        /// <summary>
        /// 是否是<see cref="IQueryable{T}"/>并且<seealso cref="IGrouping{TKey, TElement}"/>
        /// </summary>
        /// <param name="type">类型。</param>
        /// <returns></returns>
        public static bool IsGroupingQueryable(this Type type)
        {
            if (type is null)
            {
                return false;
            }

            while (type.IsQueryable())
            {
                if (!IsGenericType(type, out Type[] typeArguments))
                {
                    goto label_continue;
                }

                if (typeArguments.Length > 1)
                {
                    goto label_continue;
                }

                if (IsGrouping(typeArguments[0]))
                {
                    return true;
                }

            label_continue:
                {
                    type = type.BaseType;
                }
            }

            return false;
        }

        private static bool IsGenericType(Type typeSelf, out Type[] type2Arguments)
        {
            while (typeSelf != null && typeSelf != typeof(object))
            {
                if (typeSelf.IsInterface && typeSelf.IsGenericType && typeSelf.GetGenericTypeDefinition() == _queryable_T_Type)
                {
                    type2Arguments = typeSelf.GetGenericArguments();

                    return true;
                }

                Type[] interfaces = typeSelf.GetInterfaces();

                foreach (Type type2 in interfaces)
                {
                    if (IsGenericType(type2, out type2Arguments))
                    {
                        return true;
                    }
                }

                typeSelf = typeSelf.BaseType;
            }

            type2Arguments = Type.EmptyTypes;

            return false;
        }

        /// <summary>
        /// <see cref="IGrouping{TKey, TElement}"/>
        /// </summary>
        /// <param name="type">类型。</param>
        /// <returns></returns>
        public static bool IsGrouping(this Type type) => type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == _grouping_T1_T2_Type;

        /// <summary>
        /// <see cref="IEnumerable{T}"/>
        /// </summary>
        /// <param name="type">类型。</param>
        /// <returns></returns>
        public static bool IsEnumerable(this Type type) => type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == _enumerable_T_Type;

        private static readonly Type[] _simpleTypes = new Type[]
        {
            Types.String,
            Types.JsonPayload,
            Types.JsonbPayload
        };

        /// <summary>
        /// 细胞类型（最小类型单元）。
        /// </summary>
        /// <param name="type">类型。</param>
        /// <returns></returns>
        public static bool IsCell(this Type type)
        {
            if (type is null)
            {
                return false;
            }

            if (type.IsEnum)
            {
                return true;
            }

            if (type.IsValueType)
            {
                return (Nullable.GetUnderlyingType(type) ?? type).IsSimple();
            }

            if (_simpleTypes.Contains(type))
            {
                return true;
            }

            return type.FullName is "Newtonsoft.Json.Linq.JObject" or "Newtonsoft.Json.Linq.JArray";
        }
    }
}
