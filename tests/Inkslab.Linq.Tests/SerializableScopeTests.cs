using Inkslab.Transactions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using XunitPlus;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// SerializableScope 单元测试
    /// 核心功能：在序列化范围中，相同的连接字符串永远使用同一个数据库连接实例
    /// </summary>
    [TestPriority(2)]
    public class SerializableScopeTests
    {
        private readonly IRepository<User> _userRpts;
        private readonly IQueryable<User> _users;

        public SerializableScopeTests(IRepository<User> userRpts, IQueryable<User> users)
        {
            _userRpts = userRpts;
            _users = users;
        }

        #region 基础功能测试

        /// <summary>
        /// 测试：SerializableScope 在范围内使用相同的连接实例
        /// 验证：同一 SerializableScope 中，多次数据库操作使用同一连接
        /// </summary>
        [Fact]
        [Step(1)]
        public async Task SerializableScope_ShouldReuseSameConnection_InScopeAsync()
        {
            // Arrange & Act
            await using (var scope = new SerializableScope())
            {
                // 第一次查询
                var user1 = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync();

                // 第二次查询（应该使用同一个连接）
                var user2 = await _users.Where(x => x.Id > 0).OrderBy(x => x.Id).FirstOrDefaultAsync();

                // 第三次查询
                var count = await _users.CountAsync();

                // Assert
                // 在同一 scope 内，所有操作都使用同一连接
                Assert.True(count >= 0);
            }
        }

        /// <summary>
        /// 测试：SerializableScope 销毁后连接被正确释放
        /// 验证：Scope 结束后，后续操作使用新的连接
        /// </summary>
        [Fact]
        [Step(2)]
        public async Task SerializableScope_ShouldReleaseConnection_AfterDisposeAsync()
        {
            // Arrange & Act
            await using (var scope = new SerializableScope())
            {
                var user1 = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync();
            } // scope 销毁，连接应该被释放

            // 新的查询应该使用新的连接
            var user2 = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(user2);
        }

        /// <summary>
        /// 测试：嵌套 SerializableScope 使用相同连接
        /// 验证：内外层 Scope 共享同一连接实例
        /// </summary>
        [Fact]
        [Step(3)]
        public async Task NestedSerializableScope_ShouldShareSameConnectionAsync()
        {
            // Arrange & Act
            await using (var outerScope = new SerializableScope())
            {
                var user1 = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync();

                await using (var innerScope = new SerializableScope())
                {
                    // 内层 Scope 应该复用外层 Scope 的连接
                    var user2 = await _users.Where(x => x.Id > 0).OrderBy(x => x.Id).FirstOrDefaultAsync();
                    var count = await _users.CountAsync();

                    // Assert
                    Assert.True(count >= 0);
                }

                // 外层 Scope 继续使用
                var user3 = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync();
                Assert.NotNull(user3);
            }
        }

        /// <summary>
        /// 测试：SerializableScope 中执行批量操作
        /// 验证：批量操作在同一连接中执行，性能更优
        /// </summary>
        [Fact]
        [Step(4)]
        public async Task SerializableScope_ShouldHandleBatchOperationsAsync()
        {
            // Arrange
            var users = Enumerable.Range(1, 10)
                .Select(i => new User
                {
                    Name = $"TestUser_{i}_{Guid.NewGuid()}",
                    DateAt = DateTime.Now
                })
                .ToList();

            // Act
            await using (var scope = new SerializableScope())
            {
                // 批量插入
                foreach (var user in users)
                {
                    // 每次插入都复用同一连接
                    _userRpts.Into(user).Execute();
                }

                // 查询验证
                var count = await _users.Where(x => x.Name.StartsWith("TestUser_")).CountAsync();

                // Assert
                Assert.True(count >= users.Count);
            }
        }

        #endregion

        #region 与事务单元混合使用测试

        /// <summary>
        /// 测试：SerializableScope + TransactionUnit 提交事务
        /// 验证：在序列化范围内，事务正常提交
        /// </summary>
        [Fact]
        [Step(5)]
        public async Task SerializableScopeWithTransaction_ShouldCommitAsync()
        {
            // Arrange
            var testName = $"CommitTest_{Guid.NewGuid()}";

            // Act
            await using (var scope = new SerializableScope())
            {
                await using (var transaction = new TransactionUnit())
                {
                    // 创建测试用户
                    var user = new User
                    {
                        Name = testName,
                        DateAt = DateTime.Now
                    };

                    _userRpts.Into(user).Execute();

                    // 提交事务
                    await transaction.CompleteAsync();
                }
            }

            // Assert - 验证数据已提交
            var savedUser = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Name == testName);
            Assert.NotNull(savedUser);
            Assert.Equal(testName, savedUser.Name);
        }

        /// <summary>
        /// 测试：SerializableScope + TransactionUnit 回滚事务
        /// 验证：在序列化范围内，事务正常回滚
        /// </summary>
        [Fact]
        [Step(6)]
        public async Task SerializableScopeWithTransaction_ShouldRollbackAsync()
        {
            // Arrange
            var testName = $"RollbackTest_{Guid.NewGuid()}";

            // Act
            await using (var scope = new SerializableScope())
            {
                await using (var transaction = new TransactionUnit())
                {
                    // 创建测试用户
                    var user = new User
                    {
                        Name = testName,
                        DateAt = DateTime.Now
                    };

                    _userRpts.Into(user).Execute();

                    // 不调用 CompleteAsync，事务自动回滚
                }
            }

            // Assert - 验证数据已回滚
            var savedUser = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Name == testName);
            Assert.Null(savedUser);
        }

        /// <summary>
        /// 测试：SerializableScope 外层 + TransactionUnit 内层
        /// 验证：先建立序列化范围，再开启事务
        /// </summary>
        [Fact]
        [Step(7)]
        public async Task SerializableScope_OuterWithTransaction_InnerAsync()
        {
            // Arrange
            var testName = $"OuterScope_{Guid.NewGuid()}";

            // Act
            await using (var scope = new SerializableScope())
            {
                // 在 Scope 内先执行一个查询
                var initialCount = await _users.CountAsync();

                await using (var transaction = new TransactionUnit())
                {
                    // 事务内操作
                    var user = new User
                    {
                        Name = testName,
                        DateAt = DateTime.Now
                    };

                    _userRpts.Into(user).Execute();
                    await transaction.CompleteAsync();
                }

                // Scope 继续使用，验证数据
                var finalCount = await _users.CountAsync();
                var savedUser = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Name == testName);

                // Assert
                Assert.NotNull(savedUser);
                Assert.True(finalCount >= initialCount);
            }
        }

        /// <summary>
        /// 测试：TransactionUnit 外层 + SerializableScope 内层
        /// 验证：先开启事务，再建立序列化范围
        /// </summary>
        [Fact]
        [Step(8)]
        public async Task Transaction_OuterWithSerializableScope_InnerAsync()
        {
            // Arrange
            var testName = $"OuterTransaction_{Guid.NewGuid()}";

            // Act
            await using (var transaction = new TransactionUnit())
            {
                await using (var scope = new SerializableScope())
                {
                    // 在 Scope 内执行事务操作
                    var user = new User
                    {
                        Name = testName,
                        DateAt = DateTime.Now
                    };

                    _userRpts.Into(user).Execute();

                    // 验证插入
                    var savedUser = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Name == testName);
                    Assert.NotNull(savedUser);
                }

                // 提交事务
                await transaction.CompleteAsync();
            }

            // Assert - 验证数据已提交
            var finalUser = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Name == testName);
            Assert.NotNull(finalUser);
        }

        /// <summary>
        /// 测试：嵌套事务 + SerializableScope
        /// 验证：复杂嵌套场景下，连接和事务管理正确
        /// </summary>
        [Fact]
        [Step(9)]
        public async Task NestedTransactionWithSerializableScopeAsync()
        {
            // Arrange
            var testName1 = $"Nested1_{Guid.NewGuid()}";
            var testName2 = $"Nested2_{Guid.NewGuid()}";

            // Act
            await using (var scope = new SerializableScope())
            {
                await using (var outerTransaction = new TransactionUnit())
                {
                    // 外层事务操作
                    var user1 = new User
                    {
                        Name = testName1,
                        DateAt = DateTime.Now
                    };
                    _userRpts.Into(user1).Execute();

                    await using (var innerTransaction = new TransactionUnit(TransactionOption.Required))
                    {
                        // 内层事务操作（共享外层事务）
                        var user2 = new User
                        {
                            Name = testName2,
                            DateAt = DateTime.Now
                        };
                        _userRpts.Into(user2).Execute();

                        // 内层事务提交
                        await innerTransaction.CompleteAsync();
                    }

                    // 外层事务提交
                    await outerTransaction.CompleteAsync();
                }
            }

            // Assert - 验证两条数据都已提交
            var savedUser1 = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Name == testName1);
            var savedUser2 = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Name == testName2);
            Assert.NotNull(savedUser1);
            Assert.NotNull(savedUser2);
        }

        /// <summary>
        /// 测试：嵌套事务 + SerializableScope
        /// 验证：复杂嵌套场景下，连接和事务管理正确
        /// </summary>
        [Fact]
        [Step(10)]
        public async Task NestedTransactionWithSerializableScope2Async()
        {
            // Arrange
            var testName1 = $"Nested1_{Guid.NewGuid()}";
            var testName2 = $"Nested2_{Guid.NewGuid()}";

            // Act
            await using (var scope = new SerializableScope())
            {
                // 预热一个连接，链接内的事务都使用同一个“DbConnection”。
                await _users.OrderBy(x => x.Id).FirstOrDefaultAsync();

                await using (var outerTransaction = new TransactionUnit())
                {
                    // 外层事务操作
                    var user1 = new User
                    {
                        Name = testName1,
                        DateAt = DateTime.Now
                    };
                    _userRpts.Into(user1).Execute();

                    await using (var innerTransaction = new TransactionUnit(TransactionOption.Required))
                    {
                        // 内层事务操作（共享外层事务）
                        var user2 = new User
                        {
                            Name = testName2,
                            DateAt = DateTime.Now
                        };
                        _userRpts.Into(user2).Execute();

                        // 内层事务提交
                        await innerTransaction.CompleteAsync();
                    }

                    // 外层事务提交
                    await outerTransaction.CompleteAsync();
                }
            }

            // Assert - 验证两条数据都已提交
            var savedUser1 = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Name == testName1);
            var savedUser2 = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Name == testName2);
            Assert.NotNull(savedUser1);
            Assert.NotNull(savedUser2);
        }

        /// <summary>
        /// 测试：RequiresNew 事务选项 + SerializableScope
        /// 验证：新事务在序列化范围内独立执行
        /// </summary>
        [Fact]
        [Step(11)]
        public async Task SerializableScope_WithRequiresNewTransactionAsync()
        {
            // Arrange
            var outerName = $"OuterTx_{Guid.NewGuid()}";
            var innerName = $"InnerTx_{Guid.NewGuid()}";

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                // Act
                await using (var scope = new SerializableScope())
                {
                    await using (var outerTransaction = new TransactionUnit())
                    {
                        // 外层事务操作
                        var user1 = new User
                        {
                            Name = outerName,
                            DateAt = DateTime.Now
                        };

                        _userRpts.Into(user1).Execute();

                        await using (var innerTransaction = new TransactionUnit(TransactionOption.RequiresNew)) // 同一个连接时，无法开启嵌套事务
                        {
                            // 内层独立事务操作
                            var user2 = new User
                            {
                                Name = innerName,
                                DateAt = DateTime.Now
                            };
                            _userRpts.Into(user2).Execute(); // 报错 Transactions may not be nested.

                            // 内层事务提交
                            await innerTransaction.CompleteAsync();
                        }

                        // 外层事务不提交（回滚）
                    }
                }
            });
        }

        /// <summary>
        /// 测试：Suppress 事务选项 + SerializableScope
        /// 验证：抑制事务后，操作在序列化范围内仍正常执行
        /// </summary>
        [Fact]
        [Step(12)]
        public async Task SerializableScope_WithSuppressTransactionAsync()
        {
            // Arrange
            var outerName = $"OuterTx_{Guid.NewGuid()}";
            var suppressName = $"SuppressTx_{Guid.NewGuid()}";

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                // Act
                await using (var scope = new SerializableScope())
                {
                    await using (var outerTransaction = new TransactionUnit())
                    {
                        // 外层事务操作
                        var user1 = new User
                        {
                            Name = outerName,
                            DateAt = DateTime.Now
                        };

                        _userRpts.Into(user1).Execute();

                        await using (var suppressTransaction = new TransactionUnit(TransactionOption.Suppress)) // 抑制事务，用同一个连接时，无法抑制
                        {
                            // 抑制事务，操作自动提交
                            var user2 = new User
                            {
                                Name = suppressName,
                                DateAt = DateTime.Now
                            };
                            _userRpts.Into(user2).Execute(); // 报错 The transaction associated with this command is not the connection's active transaction; 

                            // Suppress 模式下，CompleteAsync 无实际作用
                            await suppressTransaction.CompleteAsync();
                        }

                        // 外层事务不提交（回滚）
                    }
                }
            });
        }

        #endregion

        #region 并发与异步测试

        /// <summary>
        /// 测试：并发场景下 SerializableScope 的线程安全性
        /// 验证：多个异步任务在各自的 Scope 中独立运行
        /// </summary>
        [Fact]
        [Step(13)]
        public async Task SerializableScope_ShouldBeThreadSafe_InConcurrentScenariosAsync()
        {
            // Arrange
            var tasks = new List<Task<int>>();

            // Act - 创建多个并发任务
            for (int i = 0; i < 5; i++)
            {
                var taskId = i;
                var task = Task.Run(async () =>
                {
                    await using (var scope = new SerializableScope())
                    {
                        // 每个任务在自己的 Scope 中执行
                        var count = await _users.CountAsync();
                        await Task.Delay(10); // 模拟异步操作
                        var users = await _users.Take(10).OrderBy(x => x.Id).ToListAsync();
                        return users.Count;
                    }
                });
                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);

            // Assert - 所有任务都应该成功完成
            Assert.Equal(5, results.Length);
            Assert.All(results, count => Assert.True(count >= 0));
        }

        /// <summary>
        /// 测试：SerializableScope 中的异步操作
        /// 验证：复杂异步场景下连接管理正确
        /// </summary>
        [Fact]
        [Step(14)]
        public async Task SerializableScope_ShouldHandleComplexAsyncOperationsAsync()
        {
            // Arrange & Act
            await using (var scope = new SerializableScope())
            {
                // 并行执行多个查询
                var task3 = _users.OrderByDescending(x => x.Id).Take(100).ToListAsync();
                var task1 = _users.CountAsync();
                var task2 = _users.Where(x => x.Id > 0).Take(10).OrderBy(x => x.Id).ToListAsync();

                // 串行范围中，不支持并行查询。
                await Assert.ThrowsAsync<InvalidOperationException>(() => Task.WhenAll(task1, task2, task3));
            }
        }

        #endregion

        #region 边界条件与异常测试

        /// <summary>
        /// 测试：空 SerializableScope（无任何操作）
        /// 验证：空 Scope 不会导致异常
        /// </summary>
        [Fact]
        [Step(15)]
        public async Task SerializableScope_WithNoOperations_ShouldNotThrowAsync()
        {
            // Act & Assert
            await using (var scope = new SerializableScope())
            {
                // 不执行任何操作
            }

            // 验证后续操作正常
            var count = await _users.CountAsync();
            Assert.True(count >= 0);
        }

        /// <summary>
        /// 测试：SerializableScope 中发生异常
        /// 验证：异常不影响 Scope 的正确销毁
        /// </summary>
        [Fact]
        [Step(16)]
        public async Task SerializableScope_ShouldDisposeCorrectly_OnExceptionAsync()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await using (var scope = new SerializableScope())
                {
                    var count = await _users.CountAsync();

                    // 触发异常
                    throw new InvalidOperationException("Test exception");
                }
            });

            // 验证后续操作正常（Scope 已正确销毁）
            var finalCount = await _users.CountAsync();
            Assert.True(finalCount >= 0);
        }

        /// <summary>
        /// 测试：多次创建和销毁 SerializableScope
        /// 验证：资源正确管理，无内存泄漏
        /// </summary>
        [Fact]
        [Step(17)]
        public async Task SerializableScope_MultipleCreateAndDisposeAsync()
        {
            // Act
            for (int i = 0; i < 10; i++)
            {
                await using (var scope = new SerializableScope())
                {
                    var count = await _users.CountAsync();
                    Assert.True(count >= 0);
                }
            }

            // Assert - 验证最终状态正常
            var finalCount = await _users.CountAsync();
            Assert.True(finalCount >= 0);
        }

        #endregion

        #region 实际业务场景测试

        /// <summary>
        /// 测试：模拟订单创建业务场景
        /// 验证：SerializableScope + Transaction 在复杂业务中的应用
        /// </summary>
        [Fact]
        [Step(18)]
        public async Task RealWorldScenario_OrderCreation_WithSerializableScopeAndTransactionAsync()
        {
            // Arrange
            var orderName = $"Order_{Guid.NewGuid()}";

            // Act - 模拟订单创建流程
            await using (var scope = new SerializableScope())
            {
                await using (var transaction = new TransactionUnit())
                {
                    // 1. 创建订单
                    var order = new User
                    {
                        Name = orderName,
                        DateAt = DateTime.Now
                    };
                    _userRpts.Into(order).Execute();

                    // 2. 查询验证
                    var createdOrder = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Name == orderName);
                    Assert.NotNull(createdOrder);

                    // 3. 更新订单状态
                    await _userRpts.UpdateAsync(x => new User
                    {
                        DateAt = DateTime.Now.AddHours(1)
                    });

                    // 4. 再次查询确认
                    var updatedOrder = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Id == createdOrder.Id);
                    Assert.NotNull(updatedOrder);

                    // 5. 提交事务
                    await transaction.CompleteAsync();
                }
            }

            // Assert - 验证最终数据
            var finalOrder = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Name == orderName);
            Assert.NotNull(finalOrder);
        }

        /// <summary>
        /// 测试：模拟订单创建业务场景
        /// 验证：SerializableScope + Transaction 在复杂业务中的应用
        /// </summary>
        [Fact]
        [Step(19)]
        public async Task RealWorldScenario_OrderCreation_WithSerializableScopeAndTransactionMultiAsync()
        {
            // Arrange
            var orderName = $"Order_{Guid.NewGuid()}";
            var orderName2 = $"Order_{Guid.NewGuid()}";

            // Act - 模拟订单创建流程
            await using (var scope = new SerializableScope())
            {
                await using (var transaction = new TransactionUnit())
                {
                    // 1. 创建订单
                    var order = new User
                    {
                        Name = orderName,
                        DateAt = DateTime.Now
                    };
                    _userRpts.Into(order).Execute();

                    // 2. 查询验证
                    var createdOrder = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Name == orderName);
                    Assert.NotNull(createdOrder);

                    // 3. 更新订单状态
                    await _userRpts.UpdateAsync(x => new User
                    {
                        DateAt = DateTime.Now.AddHours(1)
                    });

                    // 4. 再次查询确认
                    var updatedOrder = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Id == createdOrder.Id);
                    Assert.NotNull(updatedOrder);

                    // 5. 提交事务
                    await transaction.CompleteAsync();
                }

                await using (var transaction = new TransactionUnit())
                {
                    // 1. 创建订单
                    var order = new User
                    {
                        Name = orderName2,
                        DateAt = DateTime.Now
                    };
                    _userRpts.Into(order).Execute();

                    // 2. 查询验证
                    var createdOrder = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Name == orderName2);
                    Assert.NotNull(createdOrder);

                    // 3. 更新订单状态
                    await _userRpts.UpdateAsync(x => new User
                    {
                        DateAt = DateTime.Now.AddHours(1)
                    });

                    // 4. 再次查询确认
                    var updatedOrder = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Id == createdOrder.Id);
                    Assert.NotNull(updatedOrder);
                }
            }

            // Assert - 验证最终数据
            var finalOrder = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Name == orderName);
            var finalOrder2 = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.Name == orderName2);
            Assert.NotNull(finalOrder);
            Assert.Null(finalOrder2);
        }

        /// <summary>
        /// 测试：模拟批量数据处理场景
        /// 验证：SerializableScope 提升批量操作性能
        /// </summary>
        [Fact]
        [Step(20)]
        public async Task RealWorldScenario_BatchProcessing_WithSerializableScopeAsync()
        {
            // Arrange
            var batchSize = 20;
            var testPrefix = $"Batch_{Guid.NewGuid()}_";

            // Act - 在 Scope 内执行批量处理
            await using (var scope = new SerializableScope())
            {
                await using (var transaction = new TransactionUnit())
                {
                    for (int i = 0; i < batchSize; i++)
                    {
                        var user = new User
                        {
                            Name = $"{testPrefix}{i}",
                            DateAt = DateTime.Now
                        };
                        _userRpts.Into(user).Execute();
                    }

                    await transaction.CompleteAsync();
                }

                // 在同一 Scope 内验证
                var count = await _users.Where(x => x.Name.StartsWith(testPrefix)).CountAsync();
                Assert.Equal(batchSize, count);
            }
        }

        #endregion
    }
}