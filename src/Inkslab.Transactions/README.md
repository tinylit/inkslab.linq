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

内部事务实现，通过 `AsyncLocal` 在异步上下文中传递。

| 成员 | 说明 |
|------|------|
| `CommitAsync()` | 异步提交 |
| `Rollback()` / `RollbackAsync()` | 回滚 |

### TransactionStatus

`Active`、`Committed`、`Aborted`、`InDoubt`
