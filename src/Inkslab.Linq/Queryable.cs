using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Inkslab.Linq
{
    /// <summary>
    /// 实体。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Queryable<T>
        : QueryProvider,
            IOrderedQueryable<T>,
            IOrderedQueryable,
            IQueryable<T>,
            IQueryable,
            IAsyncQueryProvider,
            IQueryProvider,
            IAsyncEnumerable<T>,
            IEnumerable<T>,
            IEnumerable
    {
        private readonly IRepositoryExecutor _executor;
        private readonly Expression _node;
        private readonly bool _alwaysAccessDatabase = true;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="executor">执行器。</param>
        public Queryable(IRepositoryExecutor executor)
            : base(executor)
        {
            _executor = executor;
            _node = Expression.Constant(this);
        }

        private Queryable(IRepositoryExecutor executor, Expression node)
            : base(executor)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            _alwaysAccessDatabase = false;
            _executor = executor;
            _node = node;
        }

        private IAsyncEnumerable<T> _asyncEnumerable;

        /// <inheritdoc/>
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            if (_alwaysAccessDatabase)
            {
                _asyncEnumerable = _executor.QueryAsync<T>(_node);
            }
            else
            {
                _asyncEnumerable ??= _executor.QueryAsync<T>(_node);
            }

            return _asyncEnumerable.GetAsyncEnumerator(cancellationToken);
        }

        private IEnumerable<T> enumerable;

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            if (_alwaysAccessDatabase)
            {
                enumerable = _executor.Query<T>(_node);
            }
            else
            {
                enumerable ??= _executor.Query<T>(_node);
            }

            return enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #region Linq
        Type IQueryable.ElementType => typeof(T);

        Expression IQueryable.Expression => _node;

        IQueryProvider IQueryable.Provider => this;

        /// <inheritdoc/>
        protected override IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
            new Queryable<TElement>(_executor, expression);

        /// <inheritdoc/>
        Task<TResult> IAsyncQueryProvider.ExecuteAsync<TResult>(
            Expression expression,
            CancellationToken cancellationToken
        )
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return _executor.ReadAsync<TResult>(expression, cancellationToken);
        }

        #endregion
    }
}
