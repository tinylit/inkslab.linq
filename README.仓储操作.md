# 仓储操作

> [← 返回 README](README.md)

通过注入 `IRepository<TEntity>` 进行增删改操作。

## 插入

```csharp
// 单条插入
await _userRepository.Into(new User { Name = "张三" }).ExecuteAsync();

// 批量插入
await _userRepository.Into(userList).ExecuteAsync();

// 忽略重复键插入
await _userRepository.Ignore().Into(userList).ExecuteAsync();

// 指定列插入
await _userRepository.Into(user).Limit(x => new { x.Name, x.DateAt }).ExecuteAsync();

// 排除列插入
await _userRepository.Into(user).Except(x => x.DateAt).ExecuteAsync();

// 从查询结果插入
var linq = _users.Where(u => u.Id > 100).Select(u => new User { Name = u.Name });
await _userRepository.InsertAsync(linq);
```

### 自增主键反写（PopulateIdentity）

`PopulateIdentity()` 在插入执行后，将数据库为**每一行**真实生成的自增 ID 回填到对应实体属性上（不做任何「ID 连续」假设）：

```csharp
var users = new List<User>
{
    new User { Name = "张三" },
    new User { Name = "李四" }
};

await _userRepository.Into(users).PopulateIdentity().ExecuteAsync();

// 执行后 users[0].Id、users[1].Id 已被回填为数据库生成的值
```

**前置条件**：实体必须满足「单主键 + `[DatabaseGenerated]`」，否则在调用处 fail-fast 抛 `InvalidOperationException`；引擎或 `Ignore` 组合不支持时抛 `NotSupportedException`。

**引擎支持矩阵**：

| 场景 | 支持的引擎 | 实现方式 |
|------|-----------|----------|
| 反写（非 Ignore） | PostgreSQL、SqlServer、MySQL、SQLite（3.35+）、DB2、Sybase | `RETURNING` / `OUTPUT INSERTED` / `LAST_INSERT_ID()` / `FINAL TABLE` / `@@IDENTITY` |
| `Ignore` + 反写 | PostgreSQL、MySQL、SQLite | 原生 INSERT 忽略并配合 id 返回 |
| 不支持 | Oracle（批量自增反写）、其余引擎的 Ignore 组合 | fail-fast |

**各路径行为**（遵循「能大批量绝不小批量，能小批量绝不单行」）：

- **RETURNING 族**（PG/SqlServer/SQLite/DB2，非 Ignore）：单条多值语句逐行返回，超过 100 行自动拆批，往返 ⌈N/100⌉ 次。
- **标量族**（MySQL/Sybase，含 MySQL Ignore）：无多行 RETURNING，改为单条命令内多组「单行 INSERT + 标量回读」（MySQL Ignore 另读 `ROW_COUNT()` 判定是否被跳过），按参数预算分块，往返 ⌈N/K⌉ 次——绝不逐行往返。
- **RETURNING 族 Ignore**（PG/SQLite）：逐行执行，按 RETURNING 行数判定是否反写，被冲突跳过的实体保持原值。

## 更新

```csharp
// 表达式更新（推荐）
await _userRepository
    .Where(x => x.Id == 1)
    .UpdateAsync(x => new User { Name = "新名称", DateAt = DateTime.Now });

// 实体更新（以主键为条件）
await _userRepository.UpdateTo(user).ExecuteAsync();

// 批量实体更新
await _userRepository.UpdateTo(userList).ExecuteAsync();

// 指定更新列
await _userRepository.UpdateTo(user).Set(x => new { x.Name }).ExecuteAsync();

// 排除更新列
await _userRepository.UpdateTo(user).SetExcept(x => x.DateAt).ExecuteAsync();

// 跳过幂等验证（Version 字段不参与条件）
await _userRepository.UpdateTo(user).SkipIdempotentValid().ExecuteAsync();
```

## 删除

```csharp
// 条件删除
await _userRepository.DeleteAsync(x => x.Id == 1);

// 实体删除（以主键为条件）
await _userRepository.DeleteWith(user).ExecuteAsync();

// 子查询删除
await _userRepository
    .Where(x => _userExes.Where(e => e.RoleType == 2).Select(e => e.Id).Contains(x.Id))
    .DeleteAsync();
```

## 仓储方法链

`IRepository<TEntity>` 支持链式调用，调用顺序为：

```
IRepository<TEntity>
├── DataSharding(key)              // 可选：指定分片键
│   └── Timeout(seconds)           // 可选：命令超时
│       ├── Where(predicate)       // 条件操作
│       │   ├── Delete()           // 条件删除
│       │   └── UpdateAsync(expr)  // 条件更新
│       ├── Ignore()               // 忽略重复键
│       │   └── Into(entries)      // 插入
│       ├── UpdateTo(entries)      // 实体更新
│       └── DeleteWith(entries)    // 实体删除
```
