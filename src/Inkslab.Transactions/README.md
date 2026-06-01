# Inkslab.Transactions

事务管理组件，提供基于 `AsyncLocal` 的分布式事务单元，支持嵌套事务和多种隔离级别。

## 依赖

无外部依赖（纯 .NET SDK 项目）。

## 核心类型

### TransactionUnit

事务单元，实现 `IAsyncDisposable` 和 `IDisposable`。作用域内的所有数据库操作自动纳入事务。

```csharp
using Inkslab.Transactions;

await using (var transaction = new TransactionUnit())
{
    // 数据库操作...
    await transaction.CompleteAsync();  // 显式提交；不调用则自动回滚
}
```

#### 构造参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `option` | `TransactionOption` | `Required` | 事务传播行为 |
| `isolationLevel` | `IsolationLevel` | `Unspecified` | 事务隔离级别 |

### TransactionOption

| 值 | 说明 |
|----|------|
| `Required` | 加入现有事务，无则创建新事务 |
| `RequiresNew` | 始终创建新事务（挂起外层事务） |
| `Suppress` | 不参与事务 |

### IsolationLevel

| 值 | 说明 |
|----|------|
| `Unspecified` | 使用数据库默认 |
| `ReadUncommitted` | 读未提交 |
| `ReadCommitted` | 读已提交 |
| `RepeatableRead` | 可重复读 |
| `Serializable` | 串行化 |
| `Snapshot` | 快照（SQL Server） |
| `Chaos` | 不隔离 |

### Transaction

公开事务实现（`public sealed class`，实现 `ITransaction`），通过 `AsyncLocal` 在异步上下文中传递。一般无需直接 `new`，由 `TransactionUnit` 管理其生命周期。

| 成员 | 说明 |
|------|------|
| `static Transaction Current` | 当前异步上下文中的环境事务（无则为 `null`） |
| `static void Delegation(IDelivery delivery)` | 注册交付：存在事务则在提交成功后触发 `Done()`，无事务则立即 `Done()` |
| `void RegisterDelivery(IDelivery delivery)` | 向本事务注册交付（提交成功后触发） |
| `void EnlistTransaction(ITransaction transaction)` | 签署子事务 |
| `Task CommitAsync(CancellationToken)` | 异步提交（失败时回滚已签署子事务） |
| `void Rollback()` / `Task RollbackAsync(CancellationToken)` | 回滚 |
| `event TransactionCompletedEventHandler TransactionCompleted` | 提交或回滚完成后触发 |
| `Guid TransactionId` | 事务唯一标识（父子事务一致） |
| `IsolationLevel IsolationLevel` | 隔离级别 |
| `TransactionStatus Status` | 事务状态 |

### IDelivery 与提交后交付

`IDelivery` 用于「仅当事务提交成功后才执行」的副作用（如发送消息、写 outbox）：当前存在事务时延迟到提交成功，无事务时立即执行。

```csharp
public interface IDelivery
{
    void Done();   // 事务提交成功（或无事务）时被调用
}

// 在事务作用域内登记交付
Transaction.Delegation(new SendMailDelivery(mail));

await using (var transaction = new TransactionUnit())
{
    await _userRepository.Into(newUser).ExecuteAsync();
    Transaction.Delegation(new SendMailDelivery(mail));  // 提交成功后才真正发送
    await transaction.CompleteAsync();
}
```

### ITransaction

事务契约（`IAsyncDisposable` + `IDisposable`），定义 `CommitAsync`、`Rollback`、`RollbackAsync`，供自定义事务参与者（通过 `EnlistTransaction` 签署）实现。

### TransactionStatus

`Active`、`Committed`、`Aborted`、`InDoubt`
