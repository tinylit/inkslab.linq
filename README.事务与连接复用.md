# 事务与连接复用

> [← 返回 README](README.md)

## 事务管理

### TransactionUnit

`TransactionUnit` 是基于 `AsyncLocal` 的事务管理器，支持嵌套和跨服务调用：

```csharp
using Inkslab.Transactions;

await using (var transaction = new TransactionUnit())
{
    await _userRepository.UpdateAsync(x => new User { DateAt = DateTime.Now });
    await _userRepository.Into(newUser).ExecuteAsync();

    await transaction.CompleteAsync();  // 显式提交；不调用则自动回滚
}
```

### 构造参数

| TransactionOption | 说明 |
|-------------------|------|
| `Required` | 加入现有事务，无则创建新事务（默认） |
| `RequiresNew` | 始终创建新事务（挂起外层事务） |
| `Suppress` | 不参与事务 |

```csharp
new TransactionUnit()                                          // 默认
new TransactionUnit(TransactionOption.RequiresNew)             // 强制新事务
new TransactionUnit(TransactionOption.Required, IsolationLevel.ReadCommitted)
```

### 嵌套事务

```csharp
await using (var outer = new TransactionUnit())
{
    await _userRepository.UpdateAsync(x => new User { DateAt = DateTime.Now });

    await using (var inner = new TransactionUnit())  // 加入外层事务
    {
        await _userRepository.Into(newUser).ExecuteAsync();
        await inner.CompleteAsync();
    }

    await outer.CompleteAsync();  // 整体提交
}
```

## 连接复用 SerializableScope

`SerializableScope` 在作用域内对相同连接字符串复用同一个数据库连接，适用于批量操作场景：

```csharp
using Inkslab.Linq;

await using (var scope = new SerializableScope())
{
    var user = await _users.FirstOrDefaultAsync();
    var count = await _users.CountAsync();

    for (int i = 0; i < 100; i++)
    {
        await _userRepository
            .Where(x => x.Id == i)
            .UpdateAsync(x => new User { DateAt = DateTime.Now });
    }
}  // 连接自动释放
```

### 与事务配合

```csharp
await using (var scope = new SerializableScope())
{
    await using (var transaction = new TransactionUnit())
    {
        await _userRepository.Into(users).ExecuteAsync();
        await transaction.CompleteAsync();
    }
}
```

- 支持嵌套，内层 Scope 复用外层连接
- 基于 `AsyncLocal` 实现，线程安全
- 在事务环境中，连接由事务管理

## 批量操作

### 通过仓储批量插入

```csharp
int rows = await _userRepository.Timeout(100).Ignore().Into(users).ExecuteAsync();
```

### 通过 DataTable 批量写入

```csharp
var dt = new DataTable("user");
dt.Columns.Add("name", typeof(string));
dt.Columns.Add("date", typeof(DateTime));

for (int i = 0; i < 1000; i++)
    dt.Rows.Add($"User_{i}", DateTime.Now);

int rows = await _database.WriteToServerAsync(dt);
```

### 批量更新 / 删除

```csharp
int rows = await _userRepository.UpdateTo(userList).ExecuteAsync();
int rows = await _userRepository.DeleteWith(userList).ExecuteAsync();
```
