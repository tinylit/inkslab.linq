using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Inkslab.Linq
{
    /// <summary>
    /// LINQ 查询分析器（仅供查询分析使用）。
    /// <para>
    /// 无需数据库连接，直接基于数据库适配器构建 <see cref="IQueryable{T}"/>，
    /// 书写 LINQ 表达式后通过 <see cref="ToSql{T}(IQueryable{T})"/> 获取 <see cref="CommandSql{TElement}"/>，
    /// 或通过 <see cref="ToSqlString{T}(IQueryable{T})"/> 获取内联参数的 SQL 字符串。
    /// </para>
    /// <para>
    /// 为避免被用于实际的数据库操作，<see cref="LinqAnalyzer"/> 仅提供查询分析能力，
    /// 不暴露 Insert/Update/Delete 等执行类 SQL 的生成入口。
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// var adapter = new DbStrictAdapter(DatabaseEngine.MySQL, new MySqlAdapter());
    /// var users = LinqAnalyzer.From&lt;User&gt;(adapter);
    /// var cmd = users.Where(x =&gt; x.Id == 1).ToSql();
    /// var preview = users.Where(x =&gt; x.Id == 1).ToSqlString();
    /// </code>
    /// </example>
    public static class LinqAnalyzer
    {
        /// <summary>
        /// 基于数据库引擎与适配器构建一个仅用于查询分析的 <see cref="IQueryable{T}"/>。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="engine">数据库引擎。</param>
        /// <param name="adapter">数据库适配器。</param>
        /// <returns>仅用于查询分析的查询器。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="adapter"/> 为 null。</exception>
        public static IQueryable<T> From<T>(DatabaseEngine engine, IDbAdapter adapter)
        {
            if (adapter is null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }

            return From<T>(new DbStrictAdapter(engine, adapter));
        }

        /// <summary>
        /// 基于严格适配器构建一个仅用于查询分析的 <see cref="IQueryable{T}"/>。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="adapter">严格适配器。</param>
        /// <returns>仅用于查询分析的查询器。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="adapter"/> 为 null。</exception>
        public static IQueryable<T> From<T>(DbStrictAdapter adapter)
        {
            if (adapter is null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }

            var provider = new AnalyzerQueryProvider(adapter);

            return new AnalyzerQueryable<T>(provider);
        }

        /// <summary>
        /// 将 <see cref="IQueryable{T}"/>（由 <see cref="From{T}(DbStrictAdapter)"/>
        /// 创建）翻译为查询 SQL。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <returns>查询 SQL。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 null。</exception>
        /// <exception cref="NotSupportedException"><paramref name="source"/> 不是通过 <see cref="LinqAnalyzer"/> 创建的。</exception>
        public static CommandSql ToSql<T>(this IQueryable<T> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var adapter = GetAdapter(source.Provider);

            using (var visitor = new QueryVisitor(adapter))
            {
                visitor.Startup(source.Expression);

                return visitor.ToSQL();
            }
        }

        /// <summary>
        /// 分析一个以终止操作（如 <c>Sum</c>、<c>Count</c>、<c>First</c>、<c>Min</c>、<c>Max</c>、<c>Single</c> 等）
        /// 结尾的查询，并翻译为查询 SQL。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <typeparam name="TResult">终止操作返回类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <param name="query">以终止操作结尾的查询表达式。</param>
        /// <returns>查询 SQL。</returns>
        /// <example>
        /// <code>
        /// var cmd = users.ToSql(q =&gt; q.Where(x =&gt; x.Id &gt; 0).Count());
        /// var first = users.ToSql(q =&gt; q.OrderBy(x =&gt; x.Id).First());
        /// </code>
        /// </example>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 或 <paramref name="query"/> 为 null。</exception>
        /// <exception cref="NotSupportedException"><paramref name="source"/> 不是通过 <see cref="LinqAnalyzer"/> 创建的。</exception>
        public static CommandSql ToSql<T, TResult>(this IQueryable<T> source, Expression<Func<IQueryable<T>, TResult>> query)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var adapter = GetAdapter(source.Provider);
            var expression = ReplaceParameter(query.Body, query.Parameters[0], source.Expression);

            using (var visitor = new QueryVisitor(adapter))
            {
                visitor.Startup(expression);

                return visitor.ToSQL();
            }
        }

        /// <summary>
        /// 将 <see cref="IQueryable{T}"/> 翻译为内联参数后的 SQL 字符串（便于日志/调试）。
        /// <para>
        /// 仅用于查询分析和 SQL 预览，不提供执行能力。
        /// </para>
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <returns>内联参数后的 SQL 字符串。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 为 null。</exception>
        /// <exception cref="NotSupportedException"><paramref name="source"/> 不是通过 <see cref="LinqAnalyzer"/> 创建的。</exception>
        public static string ToSqlString<T>(this IQueryable<T> source) => source.ToSql().ToString();

        /// <summary>
        /// 将以终止操作结尾的查询翻译为内联参数后的 SQL 字符串（便于日志/调试）。
        /// </summary>
        /// <typeparam name="T">元素类型。</typeparam>
        /// <typeparam name="TResult">终止操作返回类型。</typeparam>
        /// <param name="source">查询器。</param>
        /// <param name="query">以终止操作结尾的查询表达式。</param>
        /// <returns>内联参数后的 SQL 字符串。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> 或 <paramref name="query"/> 为 null。</exception>
        public static string ToSqlString<T, TResult>(this IQueryable<T> source, Expression<Func<IQueryable<T>, TResult>> query)
            => source.ToSql(query).ToString();

        private static Expression ReplaceParameter(Expression body, ParameterExpression parameter, Expression replacement)
            => new ParameterReplacer(parameter, replacement).Visit(body);

        private static DbStrictAdapter GetAdapter(IQueryProvider provider)
        {
            if (provider is AnalyzerQueryProvider analyzerProvider)
            {
                return analyzerProvider.Adapter;
            }

            throw new NotSupportedException($"当前 {nameof(IQueryable)} 并非由 {nameof(LinqAnalyzer)} 创建，无法分析 SQL。");
        }

        #region 内嵌类。
        private sealed class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _source;
            private readonly Expression _replacement;

            public ParameterReplacer(ParameterExpression source, Expression replacement)
            {
                _source = source;
                _replacement = replacement;
            }

            protected override Expression VisitParameter(ParameterExpression node)
                => node == _source ? _replacement : base.VisitParameter(node);
        }

        private sealed class AnalyzerQueryProvider : IQueryProvider
        {
            public AnalyzerQueryProvider(DbStrictAdapter adapter) => Adapter = adapter;

            public DbStrictAdapter Adapter { get; }

            public IQueryable CreateQuery(Expression expression)
            {
                if (expression is null)
                {
                    throw new ArgumentNullException(nameof(expression));
                }

                Type elementType = FindGenericType(expression.Type, typeof(IQueryable<>))
                    ?? throw new ArgumentException("无效表达式!", nameof(expression));

                Type queryableType = typeof(AnalyzerQueryable<>).MakeGenericType(elementType);

                return (IQueryable)Activator.CreateInstance(queryableType, this, expression);
            }

            public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            {
                if (expression is null)
                {
                    throw new ArgumentNullException(nameof(expression));
                }

                return new AnalyzerQueryable<TElement>(this, expression);
            }

            public object Execute(Expression expression) => throw AnalyzeOnly();

            public TResult Execute<TResult>(Expression expression) => throw AnalyzeOnly();

            private static NotSupportedException AnalyzeOnly()
                => new NotSupportedException($"{nameof(LinqAnalyzer)} 仅用于查询分析，不支持执行，请使用 ToSql() 获取 SQL。");

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
                        foreach (var iface in type.GetInterfaces())
                        {
                            var matched = FindGenericType(iface, definition);

                            if (matched != null)
                            {
                                return matched;
                            }
                        }
                    }

                    type = type.BaseType;
                }

                return null;
            }
        }

        private sealed class AnalyzerQueryable<T> : IOrderedQueryable<T>, IAsyncEnumerable<T>
        {
            private readonly AnalyzerQueryProvider _provider;
            private readonly Expression _expression;

            public AnalyzerQueryable(AnalyzerQueryProvider provider)
            {
                _provider = provider;
                _expression = Expression.Constant(this);
            }

            public AnalyzerQueryable(AnalyzerQueryProvider provider, Expression expression)
            {
                _provider = provider;
                _expression = expression ?? throw new ArgumentNullException(nameof(expression));
            }

            public Type ElementType => typeof(T);

            public Expression Expression => _expression;

            public IQueryProvider Provider => _provider;

            public IEnumerator<T> GetEnumerator() => throw AnalyzeOnly();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                => throw AnalyzeOnly();

            private static NotSupportedException AnalyzeOnly()
                => new NotSupportedException($"{nameof(LinqAnalyzer)} 创建的查询器仅支持查询分析，不支持枚举，请使用 ToSql() 获取 SQL。");
        }
        #endregion
    }
}
