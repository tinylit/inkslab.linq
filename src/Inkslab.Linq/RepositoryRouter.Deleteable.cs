using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Linq.Abilities;
using Microsoft.Extensions.Logging;

namespace Inkslab.Linq
{
    public partial class RepositoryRouter<TEntity>
        where TEntity : class, new()
    {
        private class Deleteable : IDeleteable<TEntity>
        {
            private readonly IDatabaseExecutor _executor;
            private readonly IDatabaseStrings _databaseStrings;

            private readonly DeleteableCommand _command;

            public Deleteable(
                IDatabaseExecutor executor,
                IDatabaseStrings databaseStrings,
                DbStrictAdapter adapter,
                IReadOnlyCollection<TEntity> entities,
                int? commandTimeout,
                string shardingKey,
                ILogger logger
            )
            {
                _executor = executor;
                _databaseStrings = databaseStrings;

                _command = new DeleteableCommand(
                    entities,
                    adapter,
                    shardingKey,
                    commandTimeout,
                    logger
                );
            }

            public int Execute()
            {
                _command.CheckValid();

                if (_command.IsEmpty)
                {
                    return 0;
                }

                if (!_command.RequiredBulk)
                {
                    var commandSql = _command.GetCommandSql();

                    return _executor.Execute(_databaseStrings, commandSql);
                }

                int influenceSkipRows = 0;

                var (createSql, dt, updateSql, updateArgs, dropSql) = _command.Combination();

                return _executor.ExecuteMultiple(
                        _databaseStrings,
                        executor =>
                        {
                            influenceSkipRows += executor.Execute(createSql);

                            try
                            {
                                influenceSkipRows += executor.WriteToServer(dt);

                                executor.Execute(new CommandSql(updateSql, updateArgs));
                            }
                            finally
                            {
                                influenceSkipRows += executor.Execute(dropSql);
                            }
                        },
                        _command.CommandTimeout
                    ) - influenceSkipRows;
            }

            public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
            {
                _command.CheckValid();

                if (_command.IsEmpty)
                {
                    return 0;
                }

                if (!_command.RequiredBulk)
                {
                    var commandSql = _command.GetCommandSql();

                    return await _executor.ExecuteAsync(
                        _databaseStrings,
                        commandSql,
                        cancellationToken
                    );
                }

                int influenceSkipRows = 0;

                var (createSql, dt, updateSql, updateArgs, dropSql) = _command.Combination();

                return await _executor.ExecuteMultipleAsync(
                        _databaseStrings,
                        async executor =>
                        {
                            influenceSkipRows += await executor.ExecuteAsync(createSql);

                            try
                            {
                                influenceSkipRows += await executor.WriteToServerAsync(dt);

                                await executor.ExecuteAsync(new CommandSql(updateSql, updateArgs));
                            }
                            finally
                            {
                                influenceSkipRows += await executor.ExecuteAsync(dropSql);
                            }
                        },
                        _command.CommandTimeout,
                        cancellationToken
                    ) - influenceSkipRows;
            }

            public ICommandExecutor SkipIdempotentValid()
            {
                _command.SkipIdempotentValid = true;

                return this;
            }
        }
    }
}
