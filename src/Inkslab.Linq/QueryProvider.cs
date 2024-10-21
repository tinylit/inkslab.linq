using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Linq
{
    /// <summary>
    /// 查询供应器。
    /// </summary>
    public abstract class QueryProvider : IQueryProvider
    {
        private static readonly MethodInfo _createFn;
        private static readonly MethodInfo _executeFn;

        static QueryProvider()
        {
            var queryProviderType = typeof(QueryProvider);

            _createFn = queryProviderType.GetMethod(nameof(CreateQuery), 1, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, new Type[] { typeof(Expression) }, null);
            _executeFn = queryProviderType.GetMethod(nameof(Execute), 1, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, new Type[] { typeof(Expression) }, null);
        }

        private readonly IRepositoryExecutor _executor;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="executor">执行器。</param>
        public QueryProvider(IRepositoryExecutor executor)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        private static Type FindGenericType(Type type, Type definition)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == definition)
                {
                    return type;
                }

                if (definition.IsInterface)
                {
                    Type[] interfaces = type.GetInterfaces();

                    foreach (Type interfaceType in interfaces)
                    {
                        Type elementType = FindGenericType(interfaceType, definition);

                        if (elementType is null)
                        {
                            continue;
                        }

                        return elementType;
                    }
                }

                type = type.BaseType;
            }

            return null;
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            Type elementType =
                FindGenericType(expression.Type, typeof(IQueryable<>))
                ?? throw new ArgumentException("无效表达式!", nameof(expression));

            return (IQueryable)_createFn.MakeGenericMethod(elementType).Invoke(this, new object[] { expression });
        }

        /// <summary>
        /// 创建查询器。
        /// </summary>
        /// <typeparam name="TElement">元素类型。</typeparam>
        /// <param name="expression">表达式。</param>
        /// <returns>查询器。</returns>
        protected abstract IQueryable<TElement> CreateQuery<TElement>(Expression expression);

        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression) => CreateQuery<TElement>(expression);

        /// <inheritdoc/>
        object IQueryProvider.Execute(Expression expression)
            => _executeFn.MakeGenericMethod(expression.Type)
                .Invoke(this, new object[1] { expression });

        /// <inheritdoc/>
        TResult IQueryProvider.Execute<TResult>(Expression expression) => Execute<TResult>(expression);

        private TResult Execute<TResult>(Expression expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return _executor.Read<TResult>(expression);
        }
    }
}
