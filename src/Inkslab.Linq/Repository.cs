using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Linq.Abilities;
using Inkslab.Linq.Enums;
using static System.Linq.Expressions.Expression;

namespace Inkslab.Linq
{
    /// <summary>
    /// 仓库。
    /// </summary>
    /// <typeparam name="TEntity">类型。</typeparam>
    public class Repository<TEntity>
        : IRepository<TEntity>,
            IRepositoryDataSharding<TEntity>,
            IRepositoryTimeout<TEntity>,
            IRepositoryCondition<TEntity>,
            IRepositoryIgnore<TEntity>
        where TEntity : class, new()
    {
        private static readonly Type _elementType;
        private static readonly MethodInfo _whereFn;
        private static readonly MethodInfo _updateFn;
        private static readonly MethodInfo _deleteFn;
        private static readonly MethodInfo _insertFn;
        private static readonly MethodInfo _ignoreFn;
        private static readonly MethodInfo _takeWhileFn;
        private static readonly MethodInfo _skipWhileFn;
        private static readonly MethodInfo _deleteWithPredicateFn;
        private static readonly Expression _default;

        static Repository()
        {
            _elementType = typeof(TEntity);

            _default = Constant(new NestedQueryable());

            _whereFn = QueryableMethods.Where.MakeGenericMethod(_elementType);
            _updateFn = QueryableMethods.Update.MakeGenericMethod(_elementType);
            _deleteFn = QueryableMethods.Delete.MakeGenericMethod(_elementType);
            _insertFn = QueryableMethods.Insert.MakeGenericMethod(_elementType);
            _ignoreFn = QueryableMethods.Ignore.MakeGenericMethod(_elementType);

            _takeWhileFn = QueryableMethods.TakeWhile.MakeGenericMethod(_elementType);
            _skipWhileFn = QueryableMethods.SkipWhile.MakeGenericMethod(_elementType);
            _deleteWithPredicateFn = QueryableMethods.DeleteWithPredicate.MakeGenericMethod(
                _elementType
            );
        }

        private readonly Repository<TEntity> _repository;
        private readonly IRepositoryExecutor _executor;
        private readonly IRepositoryRouter<TEntity> _router;
        private readonly RepositoryType _repositoryType = RepositoryType.Normal;
        private readonly Expression _node;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="executor">执行器。</param>
        /// <param name="router">路由器。</param>
        public Repository(IRepositoryExecutor executor, IRepositoryRouter<TEntity> router)
        {
            _executor = executor;
            _router = router;
            _node = _default;
        }

        private Repository(
            IRepositoryExecutor executor,
            IRepositoryRouter<TEntity> router,
            Expression node
        )
        {
            _executor = executor;
            _router = router;
            _node = node;
        }

        private Repository(
            Repository<TEntity> repository,
            IRepositoryExecutor executor,
            IRepositoryRouter<TEntity> router,
            RepositoryType repositoryType
        )
        {
            _repository = repository;
            _executor = executor;
            _router = router;
            _repositoryType = repositoryType;
        }

        /// <summary>
        /// 获取操作节点。
        /// </summary>
        /// <returns>节点。</returns>
        protected virtual Expression OperationNode() =>
            _repositoryType switch
            {
                RepositoryType.Normal => _node,
                RepositoryType.Ignore
                    => Call(null, _ignoreFn, new Expression[1] { _repository.OperationNode() }),
                _ => _repository.OperationNode()
            };

        /// <summary>
        /// 超时时间。
        /// </summary>
        /// <returns></returns>
        protected virtual int? CommandTimeout() =>
            _repositoryType switch
            {
                RepositoryType.Normal => null,
                _ => _repository.CommandTimeout()
            };

        /// <summary>
        /// 忽略。
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsIgnore() =>
            _repositoryType switch
            {
                RepositoryType.Normal => false,
                RepositoryType.Ignore => true,
                _ => _repository.IsIgnore()
            };

        /// <summary>
        /// 获取分片键。
        /// </summary>
        /// <returns></returns>
        protected virtual string ShardingKey() =>
            _repositoryType switch
            {
                RepositoryType.Normal => null,
                _ => _repository.ShardingKey()
            };

        #region Extentions

        /// <inheritdoc/>
        public IRepositoryDataSharding<TEntity> DataSharding(string shardingKey)
        {
            if (string.IsNullOrEmpty(shardingKey))
            {
                throw new ArgumentException(
                    $"'{nameof(shardingKey)}' cannot be null or empty.",
                    nameof(shardingKey)
                );
            }

            return new RepositoryDataSharding(this, _executor, _router, shardingKey);
        }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.Where{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public IRepositoryCondition<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
        {
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return new Repository<TEntity>(
                _executor,
                _router,
                Call(null, _whereFn, new Expression[2] { OperationNode(), Quote(predicate) })
            );
        }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.TakeWhile{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public IRepositoryCondition<TEntity> TakeWhile(Expression<Func<TEntity, bool>> predicate)
        {
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return new Repository<TEntity>(
                _executor,
                _router,
                Call(null, _takeWhileFn, new Expression[2] { OperationNode(), Quote(predicate) })
            );
        }

        /// <summary>
        ///     The <see cref="MethodInfo" /> for <see cref="Queryable.SkipWhile{TSource}(IQueryable{TSource},Expression{Func{TSource,bool}})" />
        /// </summary>
        public IRepositoryCondition<TEntity> SkipWhile(Expression<Func<TEntity, bool>> predicate)
        {
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return new Repository<TEntity>(
                _executor,
                _router,
                Call(null, _skipWhileFn, new Expression[2] { OperationNode(), Quote(predicate) })
            );
        }

        /// <summary>
        /// 获取或设置在终止尝试执行命令并生成错误之前的等待时间。
        /// </summary>
        /// <param name="commandTimeout">超时时间。</param>
        /// <returns></returns>
        public IRepositoryTimeout<TEntity> Timeout(int commandTimeout)
        {
            return new RepositoryTimeout(this, _executor, _router, commandTimeout);
        }

        /// <inheritdoc/>
        public IRepositoryIgnore<TEntity> Ignore()
        {
            return new Repository<TEntity>(this, _executor, _router, RepositoryType.Ignore);
        }

        /// <summary>
        /// 插入数据。
        /// </summary>
        /// <param name="querable">需要插入的数据。</param>
        /// <returns></returns>
        public int Insert(IQueryable<TEntity> querable)
        {
            if (querable is null)
            {
                throw new ArgumentNullException(nameof(querable));
            }

            return _executor.Execute(
                Call(null, _insertFn, new Expression[2] { OperationNode(), querable.Expression })
            );
        }

        /// <summary>
        /// 插入数据。
        /// </summary>
        /// <param name="querable">需要插入的数据。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public Task<int> InsertAsync(
            IQueryable<TEntity> querable,
            CancellationToken cancellationToken = default
        )
        {
            if (querable is null)
            {
                throw new ArgumentNullException(nameof(querable));
            }

            return _executor.ExecuteAsync(
                Call(null, _insertFn, new Expression[2] { OperationNode(), querable.Expression }),
                cancellationToken
            );
        }

        /// <summary>
        /// 删除数据。
        /// </summary>
        /// <returns></returns>
        public int Delete() =>
            _executor.Execute(Call(null, _deleteFn, new Expression[1] { OperationNode() }));

        /// <summary>
        /// 删除数据。
        /// </summary>
        /// <returns></returns>
        public int Delete(Expression<Func<TEntity, bool>> predicate) =>
            _executor.Execute(
                Call(
                    null,
                    _deleteWithPredicateFn,
                    new Expression[2] { OperationNode(), Quote(predicate) }
                )
            );

        /// <summary>
        /// 删除数据。
        /// </summary>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public Task<int> DeleteAsync(CancellationToken cancellationToken = default) =>
            _executor.ExecuteAsync(
                Call(null, _deleteFn, new Expression[1] { OperationNode() }),
                cancellationToken
            );

        /// <summary>
        /// 删除数据。
        /// </summary>
        /// <param name="predicate">删除条件。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public Task<int> DeleteAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default
        ) =>
            _executor.ExecuteAsync(
                Call(
                    null,
                    _deleteWithPredicateFn,
                    new Expression[2] { OperationNode(), Quote(predicate) }
                ),
                cancellationToken
            );

        /// <summary>
        /// 更新数据。
        /// </summary>
        /// <param name="updateSetter">更新的字段和值。</param>
        /// <returns></returns>
        public int Update(Expression<Func<TEntity, TEntity>> updateSetter)
        {
            if (updateSetter is null)
            {
                throw new ArgumentNullException(nameof(updateSetter));
            }

            return _executor.Execute(
                Call(null, _updateFn, new Expression[2] { OperationNode(), Quote(updateSetter) })
            );
        }

        /// <summary>
        /// 更新数据。
        /// </summary>
        /// <param name="updateSetter">更新的字段和值。</param>
        /// <param name="cancellationToken">取消。</param>
        /// <returns></returns>
        public Task<int> UpdateAsync(
            Expression<Func<TEntity, TEntity>> updateSetter,
            CancellationToken cancellationToken = default
        )
        {
            if (updateSetter is null)
            {
                throw new ArgumentNullException(nameof(updateSetter));
            }

            return _executor.ExecuteAsync(
                Call(null, _updateFn, new Expression[2] { OperationNode(), Quote(updateSetter) }),
                cancellationToken
            );
        }

        #endregion

        #region 路由能力
        /// <inheritdoc/>
        public IInsertable<TEntity> Into(TEntity entry)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            return Into(new List<TEntity>(1) { entry });
        }

        /// <inheritdoc/>
        public IInsertable<TEntity> Into(IReadOnlyCollection<TEntity> entries)
        {
            if (entries is null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            bool ignore = IsIgnore();
            var shardingKey = ShardingKey();
            var commandTimeout = CommandTimeout();

            return _router.AsInsertable(entries, ignore, shardingKey, commandTimeout);
        }

        /// <inheritdoc/>
        public IInsertable<TEntity> Into(params TEntity[] entries)
        {
            if (entries is null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            return Into((IReadOnlyCollection<TEntity>)entries);
        }

        /// <inheritdoc/>
        public IUpdateable<TEntity> UpdateTo(TEntity entry)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            return UpdateTo(new List<TEntity>(1) { entry });
        }

        /// <inheritdoc/>
        public IUpdateable<TEntity> UpdateTo(IReadOnlyCollection<TEntity> entries)
        {
            if (entries is null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            var shardingKey = ShardingKey();
            var commandTimeout = CommandTimeout();

            return _router.AsUpdateable(entries, shardingKey, commandTimeout);
        }

        /// <inheritdoc/>
        public IUpdateable<TEntity> UpdateTo(params TEntity[] entries)
        {
            if (entries is null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            return UpdateTo((IReadOnlyCollection<TEntity>)entries);
        }

        /// <inheritdoc/>
        public IDeleteable<TEntity> DeleteWith(TEntity entry)
        {
            if (entry is null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            return DeleteWith(new List<TEntity>(1) { entry });
        }

        /// <inheritdoc/>
        public virtual IDeleteable<TEntity> DeleteWith(IReadOnlyCollection<TEntity> entries)
        {
            if (entries is null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            var shardingKey = ShardingKey();
            var commandTimeout = CommandTimeout();

            return _router.AsDeleteable(entries, shardingKey, commandTimeout);
        }

        /// <inheritdoc/>
        public IDeleteable<TEntity> DeleteWith(params TEntity[] entries)
        {
            if (entries is null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            return DeleteWith((IReadOnlyCollection<TEntity>)entries);
        }
        #endregion

        #region 补充。
        private class RepositoryDataSharding
            : Repository<TEntity>,
                IRepositoryDataSharding<TEntity>,
                IRepositoryTimeout<TEntity>,
                IRepositoryCondition<TEntity>,
                IRepositoryIgnore<TEntity>
        {
            private static readonly MethodInfo _dataShardingFn;

            static RepositoryDataSharding() =>
                _dataShardingFn = QueryableMethods.DataSharding.MakeGenericMethod(_elementType);

            private readonly string _shardingKey;

            public RepositoryDataSharding(
                Repository<TEntity> repository,
                IRepositoryExecutor executor,
                IRepositoryRouter<TEntity> router,
                string shardingKey
            )
                : base(repository, executor, router, RepositoryType.DataSharding)
            {
                _shardingKey = shardingKey;
            }

            protected override Expression OperationNode()
            {
                return Call(
                    null,
                    _dataShardingFn,
                    new Expression[2] { base.OperationNode(), Constant(_shardingKey) }
                );
            }

            protected override string ShardingKey() => _shardingKey;

            public void Ready()
            {
                throw new NotImplementedException();
            }

            public Task ReadyAsync(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }

        private class RepositoryTimeout
            : Repository<TEntity>,
                IRepositoryDataSharding<TEntity>,
                IRepositoryTimeout<TEntity>,
                IRepositoryCondition<TEntity>,
                IRepositoryIgnore<TEntity>
        {
            private static readonly MethodInfo _timeoutFn;

            static RepositoryTimeout() =>
                _timeoutFn = QueryableMethods.Timeout.MakeGenericMethod(_elementType);

            private readonly int _commandTimeout;

            public RepositoryTimeout(
                Repository<TEntity> repository,
                IRepositoryExecutor executor,
                IRepositoryRouter<TEntity> router,
                int commandTimeout
            )
                : base(repository, executor, router, RepositoryType.Timeout)
            {
                _commandTimeout = commandTimeout;
            }

            protected override Expression OperationNode()
            {
                return Call(
                    null,
                    _timeoutFn,
                    new Expression[2] { base.OperationNode(), Constant(_commandTimeout) }
                );
            }

            protected override int? CommandTimeout() => _commandTimeout;
        }
        #endregion

        private enum RepositoryType
        {
            Normal,
            DataSharding,
            Timeout,
            Ignore
        }

        private class NestedQueryable : IQueryable<TEntity>
        {
            private readonly Expression _node;

            public NestedQueryable() => _node = Constant(this);

            public Type ElementType => _elementType;

            public Expression Expression => _node;

            public IQueryProvider Provider => throw new NotImplementedException();

            public IEnumerator<TEntity> GetEnumerator()
            {
                yield break;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
