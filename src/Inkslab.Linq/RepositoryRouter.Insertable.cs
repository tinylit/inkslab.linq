using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Inkslab.Linq.Abilities;
using Inkslab.Linq.Enums;

namespace Inkslab.Linq
{
    public partial class RepositoryRouter<TEntity>
        where TEntity : class, new()
    {
        private class Insertable : IInsertable<TEntity>
        {
            private readonly IDatabaseExecutor _executor;
            private readonly IDatabaseStrings _databaseStrings;

            private readonly InsertCommand _command;

            public Insertable(
                IDatabaseExecutor executor,
                IDatabaseStrings databaseStrings,
                DbStrictAdapter adapter,
                IReadOnlyList<TEntity> entities,
                int? commandTimeout,
                string shardingKey,
                bool ignore
            )
            {
                _executor = executor;
                _databaseStrings = databaseStrings;

                _command = new InsertCommand(
                    entities,
                    adapter,
                    ignore,
                    shardingKey,
                    commandTimeout
                );
            }

            public ICommandExecutor Except(string[] columns)
            {
                _command.Except(columns);

                return this;
            }

            public ICommandExecutor Except<TColumn>(Expression<Func<TEntity, TColumn>> columns)
            {
                if (columns is null)
                {
                    throw new ArgumentNullException(nameof(columns));
                }

                return Except(AnalysisFields(columns));
            }

            //! 反写拆批阈值与 Command.RequiredBulk（>100）对齐：每批不超过 100 行，
            //! 保证生成的多值 INSERT 不再触发 bulk 工具路径。
            private const int WRITEBACK_BATCH_SIZE = 100;

            //! RETURNING 族（PostgreSQL/SqlServer/SQLite/DB2）反写原语：一次 INSERT...RETURNING/OUTPUT/FINAL TABLE
            //! 逐行返回真实 id，按位置 zip 反写。命令以自身实体集合（普通路径 = 全部实体；Bulk 拆批路径 = 窗口子命令）
            //! 生成 SQL 并回填；要求返回的 id 数与实体数严格相等（由 PopulateIdentities 校验）。
            //! 标量族（MySQL/Sybase）无多行 RETURNING，不走此方法——见 ExecutePerRowScalarWriteback。
            private int ExecuteOneShotIdWriteback(InsertCommand command)
            {
                var commandSql = command.GetCommandSql();

                var ids = _executor.Query<long>(_databaseStrings, commandSql);
                command.PopulateIdentities(ids);
                return ids.Count;
            }

            private async Task<int> ExecuteOneShotIdWritebackAsync(
                InsertCommand command,
                CancellationToken cancellationToken)
            {
                var commandSql = command.GetCommandSql();

                var ids = new List<long>(command.Entities.Count);
                await foreach (var id in _executor
                    .QueryAsync<long>(_databaseStrings, commandSql)
                    .WithCancellation(cancellationToken)
                    .ConfigureAwait(false))
                {
                    ids.Add(id);
                }

                command.PopulateIdentities(ids);
                return ids.Count;
            }

            private int ExecuteBulkWithIdWriteback()
            {
                int count = _command.Entities.Count;
                int total = 0;
                for (int offset = 0; offset < count; offset += WRITEBACK_BATCH_SIZE)
                {
                    int size = Math.Min(WRITEBACK_BATCH_SIZE, count - offset);
                    total += ExecuteOneShotIdWriteback(_command.Slice(offset, size));
                }
                return total;
            }

            private async Task<int> ExecuteBulkWithIdWritebackAsync(CancellationToken cancellationToken)
            {
                int count = _command.Entities.Count;
                int total = 0;
                for (int offset = 0; offset < count; offset += WRITEBACK_BATCH_SIZE)
                {
                    int size = Math.Min(WRITEBACK_BATCH_SIZE, count - offset);
                    total += await ExecuteOneShotIdWritebackAsync(
                        _command.Slice(offset, size),
                        cancellationToken
                    ).ConfigureAwait(false);
                }
                return total;
            }

            //! RETURNING 族（PostgreSQL/SQLite）Ignore 路径逐行降级：N 次往返，每行据 RETURNING 行数判定。
            //! 成功插入返回 1 行 id 并反写；冲突被跳过返回 0 行，实体保持原值不变。
            //! PostgreSQL = ON CONFLICT DO NOTHING RETURNING；SQLite = INSERT OR IGNORE ... RETURNING。
            private int ExecuteReturningIgnoreWriteback()
            {
                int count = _command.Entities.Count;
                int success = 0;

                for (int i = 0; i < count; i++)
                {
                    var command = _command.Slice(i, 1);
                    var commandSql = command.GetCommandSql();

                    var ids = _executor.Query<long>(_databaseStrings, commandSql);
                    if (ids.Count == 1)
                    {
                        command.PopulateIdentities(ids);
                        success++;
                    }
                }

                return success;
            }

            private async Task<int> ExecuteReturningIgnoreWritebackAsync(CancellationToken cancellationToken)
            {
                int count = _command.Entities.Count;
                int success = 0;

                for (int i = 0; i < count; i++)
                {
                    var command = _command.Slice(i, 1);
                    var commandSql = command.GetCommandSql();

                    var ids = new List<long>(1);
                    await foreach (var id in _executor
                        .QueryAsync<long>(_databaseStrings, commandSql)
                        .WithCancellation(cancellationToken)
                        .ConfigureAwait(false))
                    {
                        ids.Add(id);
                    }

                    if (ids.Count == 1)
                    {
                        command.PopulateIdentities(ids);
                        success++;
                    }
                }

                return success;
            }

            //! 标量族（MySQL LAST_INSERT_ID() / Sybase @@IDENTITY）多语句分批精确反写：
            //! 这两个引擎无多行 RETURNING，但把多组「单行 INSERT + 标量 SELECT」放进一条命令，单次往返取回
            //! 2N（ignore 为 3N）个有序结果集——每个标量紧跟自己那条单行 INSERT，逐行精确，零连续/零行序假设。
            //! 分块上限 = min(WRITEBACK_BATCH_SIZE, 参数预算)，N 次往返压缩为 ⌈N/K⌉，永不退回单行。
            private int ExecuteScalarBatchedWriteback()
            {
                int count = _command.Entities.Count;
                int chunkSize = ScalarChunkSize();
                bool ignore = _command.Ignore;
                int success = 0;

                for (int offset = 0; offset < count; offset += chunkSize)
                {
                    int size = Math.Min(chunkSize, count - offset);
                    var chunk = _command.Slice(offset, size);
                    var commandSql = chunk.GetScalarPopulateIdentityCommandSql();

                    using var reader = _executor.QueryMultiple(_databaseStrings, commandSql);

                    for (int j = 0; j < size; j++)
                    {
                        reader.Read<long>(); //! 跳过 INSERT 结果集

                        if (ignore)
                        {
                            var rowCount = reader.Read<long>();   //! ROW_COUNT()：1=插入/0=被忽略
                            var ids = reader.Read<long>();        //! LAST_INSERT_ID()
                            if (rowCount.Count > 0 && rowCount[0] == 1)
                            {
                                chunk.PopulateIdentityAt(j, ReadScalarId(ids, chunk.Engine));
                                success++;
                            }
                        }
                        else
                        {
                            var ids = reader.Read<long>();        //! LAST_INSERT_ID()/@@IDENTITY
                            chunk.PopulateIdentityAt(j, ReadScalarId(ids, chunk.Engine));
                            success++;
                        }
                    }
                }

                return success;
            }

            private async Task<int> ExecuteScalarBatchedWritebackAsync(CancellationToken cancellationToken)
            {
                int count = _command.Entities.Count;
                int chunkSize = ScalarChunkSize();
                bool ignore = _command.Ignore;
                int success = 0;

                for (int offset = 0; offset < count; offset += chunkSize)
                {
                    int size = Math.Min(chunkSize, count - offset);
                    var chunk = _command.Slice(offset, size);
                    var commandSql = chunk.GetScalarPopulateIdentityCommandSql();

                    await using var reader = await _executor
                        .QueryMultipleAsync(_databaseStrings, commandSql, cancellationToken)
                        .ConfigureAwait(false);

                    for (int j = 0; j < size; j++)
                    {
                        await reader.ReadAsync<long>(cancellationToken).ConfigureAwait(false); //! 跳过 INSERT 结果集

                        if (ignore)
                        {
                            var rowCount = await reader.ReadAsync<long>(cancellationToken).ConfigureAwait(false);
                            var ids = await reader.ReadAsync<long>(cancellationToken).ConfigureAwait(false);
                            if (rowCount.Count > 0 && rowCount[0] == 1)
                            {
                                chunk.PopulateIdentityAt(j, ReadScalarId(ids, chunk.Engine));
                                success++;
                            }
                        }
                        else
                        {
                            var ids = await reader.ReadAsync<long>(cancellationToken).ConfigureAwait(false);
                            chunk.PopulateIdentityAt(j, ReadScalarId(ids, chunk.Engine));
                            success++;
                        }
                    }
                }

                return success;
            }

            //! 分块大小：参数预算（MAX_PARAMETERS_COUNT / 每行参数数）内尽量取大批，且不超过 WRITEBACK_BATCH_SIZE。
            private int ScalarChunkSize()
            {
                int perRow = _command.CountParameterPerRow();
                int byParams = perRow <= 0
                    ? Command.MAX_PARAMETERS_COUNT
                    : Command.MAX_PARAMETERS_COUNT / perRow;

                return Math.Max(1, Math.Min(WRITEBACK_BATCH_SIZE, byParams));
            }

            //! 成功插入的行其标量必须 >0；为 0 或缺失视为反常（非 ignore 行必到此校验，ignore 仅在 ROW_COUNT=1 时校验）。
            private static long ReadScalarId(IList<long> ids, DatabaseEngine engine)
            {
                if (ids.Count == 0 || ids[0] <= 0)
                {
                    throw new InvalidOperationException(
                        $"自增主键反写：{engine} 单行 INSERT 未能读取到有效的自增 ID。"
                    );
                }
                return ids[0];
            }

            public int Execute()
            {
                _command.CheckValid();

                if (_command.IsEmpty)
                {
                    return 0;
                }

                if (_command.PopulateIdentityEnabled)
                {
                    //! 标量族（MySQL/Sybase）：无多行 RETURNING，多语句分批精确反写（覆盖 ignore 与非 ignore）。
                    if (_command.Engine is DatabaseEngine.MySQL or DatabaseEngine.Sybase)
                    {
                        return ExecuteScalarBatchedWriteback();
                    }

                    //! RETURNING 族（PostgreSQL/SqlServer/SQLite/DB2）：逐行/多行精确返回。
                    if (_command.Ignore)
                    {
                        return ExecuteReturningIgnoreWriteback();
                    }

                    if (_command.RequiredBulk)
                    {
                        return ExecuteBulkWithIdWriteback();
                    }

                    return ExecuteOneShotIdWriteback(_command);
                }

                //! 未开启反写：保持原批量/单语句行为，不触发任何反写 IO。
                if (!_command.RequiredBulk)
                {
                    var commandSql = _command.GetCommandSql();

                    return _executor.Execute(_databaseStrings, commandSql);
                }

                if (_command.Ignore)
                {
                    int influenceSkipRows = 0;

                    var (createSql, dt, insertSql, insertArgs, dropSql) =
                        _command.IgnoreCombination();

                    return _executor.ExecuteMultiple(
                            _databaseStrings,
                            executor =>
                            {
                                influenceSkipRows += executor.Execute(createSql);

                                try
                                {
                                    influenceSkipRows += executor.WriteToServer(dt);

                                    executor.Execute(new CommandSql(insertSql, insertArgs));
                                }
                                finally
                                {
                                    influenceSkipRows += executor.Execute(dropSql);
                                }
                            },
                            _command.CommandTimeout
                        ) - influenceSkipRows;
                }
                else
                {
                    var dt = _command.Combination();

                    return _executor.WriteToServer(
                        _databaseStrings,
                        dt,
                        _command.CommandTimeout
                    );
                }
            }

            public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
            {
                _command.CheckValid();

                if (_command.IsEmpty)
                {
                    return 0;
                }

                if (_command.PopulateIdentityEnabled)
                {
                    //! 标量族（MySQL/Sybase）：无多行 RETURNING，多语句分批精确反写（覆盖 ignore 与非 ignore）。
                    if (_command.Engine is DatabaseEngine.MySQL or DatabaseEngine.Sybase)
                    {
                        return await ExecuteScalarBatchedWritebackAsync(cancellationToken).ConfigureAwait(false);
                    }

                    //! RETURNING 族（PostgreSQL/SqlServer/SQLite/DB2）：逐行/多行精确返回。
                    if (_command.Ignore)
                    {
                        return await ExecuteReturningIgnoreWritebackAsync(cancellationToken).ConfigureAwait(false);
                    }

                    if (_command.RequiredBulk)
                    {
                        return await ExecuteBulkWithIdWritebackAsync(cancellationToken).ConfigureAwait(false);
                    }

                    return await ExecuteOneShotIdWritebackAsync(_command, cancellationToken).ConfigureAwait(false);
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

                if (_command.Ignore)
                {
                    int influenceSkipRows = 0;

                    var (createSql, dt, insertSql, insertArgs, dropSql) =
                        _command.IgnoreCombination();

                    return await _executor.ExecuteMultipleAsync(
                            _databaseStrings,
                            async executor =>
                            {
                                influenceSkipRows += await executor.ExecuteAsync(createSql);

                                try
                                {
                                    influenceSkipRows += await executor.WriteToServerAsync(dt);

                                    await executor.ExecuteAsync(
                                        new CommandSql(insertSql, insertArgs)
                                    );
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
                else
                {
                    var dt = _command.Combination();

                    return await _executor.WriteToServerAsync(
                        _databaseStrings,
                        dt,
                        _command.CommandTimeout,
                        cancellationToken
                    );
                }
            }

            public ICommandExecutor Limit(string[] columns)
            {
                _command.Limit(columns);

                return this;
            }

            public ICommandExecutor Limit<TColumn>(Expression<Func<TEntity, TColumn>> columns)
            {
                if (columns is null)
                {
                    throw new ArgumentNullException(nameof(columns));
                }

                return Limit(AnalysisFields(columns));
            }

            public IInsertable<TEntity> PopulateIdentity()
            {
                _command.EnablePopulateIdentity();

                return this;
            }
        }
    }
}
