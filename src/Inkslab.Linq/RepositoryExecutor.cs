using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Inkslab.Linq
{
    /// <summary>
    /// 仓库执行器。
    /// </summary>
    public class RepositoryExecutor : IRepositoryExecutor
    {
        private readonly DbStrictAdapter _adapter;
        private readonly IDatabaseExecutor _executor;
        private readonly IDatabaseStrings _databaseStrings;
        private readonly ILogger<RepositoryExecutor> _logger;

        /// <summary>
        /// 仓库执行器。
        /// </summary>
        /// <param name="adapter">适配器。</param>
        /// <param name="executor">执行器。</param>
        /// <param name="databaseStrings">链接字符串。</param>
        /// <param name="logger">日志。</param>
        public RepositoryExecutor(IDatabaseExecutor executor, IDatabaseStrings databaseStrings, DbStrictAdapter adapter, ILogger<RepositoryExecutor> logger)
        {
            _adapter = adapter;
            _executor = executor;
            _databaseStrings = databaseStrings;
            _logger = logger;
        }

        /// <inheritdoc/>
        public int Execute(Expression expression)
        {
            using (var visitor = new ExecutorVisitor(_adapter))
            {
                visitor.Startup(expression);

                var commandSql = visitor.ToSQL();

                return _executor.Execute(_databaseStrings, commandSql);
            }
        }

        /// <inheritdoc/>
        public Task<int> ExecuteAsync(Expression expression, CancellationToken cancellationToken = default)
        {
            using (var visitor = new ExecutorVisitor(_adapter))
            {
                try
                {
                    visitor.Startup(expression);
                }
                catch (System.Exception e)
                {
                    _logger.LogError(e, "执行器分析表达式异常！");

                    throw;
                }

                var commandSql = visitor.ToSQL();

                return _executor.ExecuteAsync(_databaseStrings, commandSql, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<T> Query<T>(Expression expression)
        {
            using (var visitor = new QueryVisitor(_adapter))
            {
                try
                {
                    visitor.Startup(expression);
                }
                catch (System.Exception e)
                {
                    _logger.LogError(e, "查询器分析表达式异常！");

                    throw;
                }

                var commandSql = visitor.ToSQL<T>();

                return _executor.Query<T>(_databaseStrings, commandSql);
            }
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<T> QueryAsync<T>(Expression expression)
        {
            using (var visitor = new QueryVisitor(_adapter))
            {
                try
                {
                    visitor.Startup(expression);
                }
                catch (System.Exception e)
                {
                    _logger.LogError(e, "查询器分析表达式异常！");

                    throw;
                }

                var commandSql = visitor.ToSQL<T>();

                return _executor.QueryAsync<T>(_databaseStrings, commandSql);
            }
        }

        /// <inheritdoc/>
        public T Read<T>(Expression expression)
        {
            using (var visitor = new QueryVisitor(_adapter))
            {
                try
                {
                    visitor.Startup(expression);
                }
                catch (System.Exception e)
                {
                    _logger.LogError(e, "查询器分析表达式异常！");

                    throw;
                }

                var commandSql = visitor.ToSQL<T>();

                return _executor.Read(_databaseStrings, commandSql);
            }
        }

        /// <inheritdoc/>
        public Task<T> ReadAsync<T>(Expression expression, CancellationToken cancellationToken = default)
        {
            using (var visitor = new QueryVisitor(_adapter))
            {
                try
                {
                    visitor.Startup(expression);
                }
                catch (System.Exception e)
                {
                    _logger.LogError(e, "查询器分析表达式异常！");

                    throw;
                }

                var commandSql = visitor.ToSQL<T>();

                return _executor.ReadAsync(_databaseStrings, commandSql, cancellationToken);
            }
        }
    }
}