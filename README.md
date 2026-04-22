# Inkslab.Linq

高性能 .NET LINQ-to-SQL ORM 框架，支持 MySQL、SQL Server、PostgreSQL 多数据库引擎，提供类型安全的查询构建、仓储模式、事务管理和批量操作能力。

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Version](https://img.shields.io/badge/version-1.2.75-green.svg)](Directory.Build.props)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%20Standard%202.1-purple.svg)](Directory.Build.props)
[![GitHub Issues](https://img.shields.io/github/issues-raw/tinylit/inkslab.linq)](../../issues)

---

## 目录

- [快速开始](#快速开始)
- [项目架构](#项目架构)
- [实体定义](#实体定义)
- [LINQ 查询](#linq-查询)
- [仓储操作](#仓储操作)
- [动态条件与排序](#动态条件与排序)
- [原生 SQL 与 IDatabase](#原生-sql-与-idatabase)
- [事务管理](#事务管理)
- [连接复用 SerializableScope](#连接复用-serializablescope)
- [批量操作](#批量操作)
- [JSON/JSONB 支持](#jsonjsonb-支持)
- [分片表](#分片表)
- [多数据库配置](#多数据库配置)
- [SQL 分析器 LinqAnalyzer](#sql-分析器-linqanalyzer)
- [API 速查表](#api-速查表)
- [测试](#测试)
- [开发与构建](#开发与构建)
- [许可证](#许可证)

---

## 快速开始

### 安装 NuGet 包

```bash
# 核心库（必选）
dotnet add package Inkslab.Linq

# 数据库适配器（选其一或多个）
dotnet add package Inkslab.Linq.MySql
dotnet add package Inkslab.Linq.SqlServer
dotnet add package Inkslab.Linq.PostgreSQL

# 事务管理（可选）
dotnet add package Inkslab.Transactions
```

### 注册服务

```csharp
using Microsoft.Extensions.DependencyInjection;

// MySQL
services.UseMySql()
    .UseLinq("server=localhost;uid=root;pwd=password;database=mydb;Charset=utf8mb4;");

// SQL Server
services.UseSqlServer()
    .UseLinq("Server=localhost;Database=MyDB;Trusted_Connection=true;");

// PostgreSQL
services.UsePostgreSQL()
    .UseLinq("Host=localhost;Database=mydb;Username=postgres;Password=password;");
```

注册完成后，通过依赖注入获取以下服务：

| 服务类型 | 说明 |
|----------|------|
| `IQueryable<TEntity>` | LINQ 查询入口 |
| `IRepository<TEntity>` | 仓储操作（增删改） |
| `IDatabase` | 原生 SQL 执行 |
| `IDatabaseFactory` | 动态创建数据库实例 |

---

## 项目架构

### 解决方案结构

```
inkslab.linq.sln
├── src/
│   ├── Inkslab.Linq/                # 核心抽象层与 LINQ-to-SQL 翻译引擎
│   ├── Inkslab.Linq.MySql/          # MySQL 适配器（MySqlConnector）
│   ├── Inkslab.Linq.SqlServer/      # SQL Server 适配器（Microsoft.Data.SqlClient）
│   ├── Inkslab.Linq.PostgreSQL/     # PostgreSQL 适配器（Npgsql）
│   └── Inkslab.Transactions/        # 事务管理（TransactionUnit）
└── tests/
    ├── Inkslab.Linq.Tests/          # 核心测试（MySQL）
    ├── SqlServer.Tests/             # SQL Server 测试
    ├── PostgreSQL.Tests/            # PostgreSQL 测试
    └── Combination.Tests/           # 多数据库组合测试
```

### 核心组件关系

```
IQueryable<T> ──→ QueryProvider ──→ LINQ Expression Tree ──→ SQL
                                                              │
IRepository<T> ──→ RepositoryExecutor ──→ IDbAdapter ─────────┤
                                              │               │
IDatabase<T> ──→ DatabaseExecutor ────────────┘               │
                       │                                      ▼
                 IDbConnectionPipeline ──→ DbConnection ──→ 数据库
```

### 数据库引擎枚举

```csharp
public enum DatabaseEngine
{
    SQLite = 1, MySQL = 2, SqlServer = 3, PostgreSQL = 4,
    Oracle = 5, DB2 = 6, Sybase = 7
}
```

---

## 实体定义

使用注解将 C# 类映射到数据库表：

```csharp
using System.ComponentModel.DataAnnotations;
using Inkslab.Linq.Annotations;

[Table("user")]                              // 映射表名
public class User
{
    [Key]                                    // 主键
    [Field("id")]                            // 映射字段名
    [DatabaseGenerated]                      // 数据库自增/生成
    public int Id { get; set; }

    [Field("name")]
    public string Name { get; set; }

    [Version]                                // 乐观锁版本字段
    [Field("date")]
    public DateTime DateAt { get; set; }

    [Field("is_administrator")]
    public bool IsAdministrator { get; set; }

    [Field("nullable")]
    public bool? Nullable { get; set; }
}
```

### 注解速查

| 注解 | 目标 | 说明 |
|------|------|------|
| `[Table("table_name")]` | 类 | 指定数据库表名，可选设置 `Schema` |
| `[Field("column_name")]` | 属性 | 指定数据库字段名 |
| `[Key]` | 属性 | 标记为主键（`Update`/`Delete` 以主键为条件） |
| `[DatabaseGenerated]` | 属性 | 标记为数据库生成字段（`Insert` 时忽略） |
| `[Version]` | 属性 | 乐观锁版本控制 |

#### `[Version]` 自动值规则

| 属性类型 | 自动值策略 |
|----------|-----------|
| `int` | 自增 +1 |
| `long` | `DateTime.Now.Ticks` |
| `double` | UTC 时间戳 |
| `DateTime` | `DateTime.Now` |

### 分片表定义

表名中使用 `[sharding]` 占位符，运行时通过 `DataSharding(key)` 替换：

```csharp
[Table("user_[sharding]")]         // 如 DataSharding("2024") → "user_2024"
public class UserSharding : User { }
```

### Fluent API 配置（IConfig）

除注解外，还支持 Fluent API 方式配置实体映射：

```csharp
public interface IConfig<TTable>
{
    IConfigTable Table(string name);
    IConfigCol Field(string name);
}
```

---

## LINQ 查询

通过注入 `IQueryable<T>` 进行类型安全的 LINQ 查询，支持完整的 LINQ 方法链。

### 基础查询

```csharp
public class UserService
{
    private readonly IQueryable<User> _users;

    public UserService(IQueryable<User> users) => _users = users;

    // Where + OrderBy + 分页
    public async Task<List<User>> GetActiveUsersAsync(int page, int size)
    {
        return await _users
            .Where(u => u.IsAdministrator)
            .OrderByDescending(u => u.DateAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();
    }

    // 投影查询
    public async Task<List<object>> GetUserNamesAsync()
    {
        return await _users
            .Where(u => u.Id > 100)
            .Select(u => new { u.Id, u.Name, DateStr = u.DateAt.ToString() })
            .ToListAsync();
    }
}
```

### Join 查询

```csharp
// Inner Join
var linq = from u in _users
           join e in _userExes on u.Id equals e.Id
           where u.Id > 100
           select new { u.Name, e.RoleType };

// Left Join（DefaultIfEmpty）
var linq = from u in _users
           join e in _userExes on u.Id equals e.Id into ue
           from e in ue.DefaultIfEmpty()
           select new { u.Name, RoleType = (int?)e.RoleType };
```

### 子查询

```csharp
// 子查询嵌套
var subQuery = from e in _userExes
               where e.RoleType == 1
               select e.Id;

var linq = from u in _users
           where subQuery.Contains(u.Id)
           select u;
```

### 分组聚合

```csharp
var linq = from u in _users
           group new { u.Name, u.DateAt } by new { u.Id, u.Name } into g
           where g.Count() > 1
           orderby g.Key.Id descending
           select new
           {
               g.Key.Id,
               g.Key.Name,
               Total = g.Count(),
               ActiveCount = g.Count(x => x.DateAt > DateTime.Now)
           };
```

### Union / Concat

```csharp
var query1 = _users.Where(u => u.IsAdministrator).Select(u => new { u.Id, u.Name });
var query2 = _users.Where(u => !u.IsAdministrator).Select(u => new { u.Id, u.Name });

var union = await query1.Union(query2).ToListAsync();       // 去重合并
var concat = await query1.Concat(query2).ToListAsync();     // 直接合并
```

### 异步查询方法

所有查询方法均有异步版本，由 `QueryableAsync` 静态类提供：

| 方法 | 说明 |
|------|------|
| `ToListAsync()` | 查询列表 |
| `FirstAsync()` / `FirstOrDefaultAsync()` | 首条记录 |
| `SingleAsync()` / `SingleOrDefaultAsync()` | 唯一记录 |
| `LastAsync()` / `LastOrDefaultAsync()` | 末条记录 |
| `CountAsync()` / `LongCountAsync()` | 计数 |
| `AnyAsync()` / `AllAsync()` | 存在性判断 |
| `MinAsync()` / `MaxAsync()` | 极值 |
| `SumAsync()` / `AverageAsync()` | 聚合 |

### 字符串函数支持

LINQ 中的字符串方法会自动翻译为对应数据库函数：

```csharp
var result = await _users
    .Where(u => u.Name.Contains("test"))      // LIKE '%test%'
    .Select(u => new
    {
        Sub = u.Name.Substring(0, 5),          // SUBSTRING
        Replaced = u.Name.Replace("a", "b"),   // REPLACE
        Index = u.Name.IndexOf("test"),         // LOCATE / CHARINDEX
        IsEmpty = string.IsNullOrEmpty(u.Name)  // IS NULL OR = ''
    })
    .ToListAsync();
```

### DateTime 成员支持

LINQ 中的 `DateTime` 属性会自动翻译为各数据库的日期函数：

```csharp
var result = await _users
    .Where(u => u.DateAt.Year == 2024 && u.DateAt.Month == 12)
    .OrderBy(u => u.DateAt.Date)
    .ToListAsync();
```

支持的成员：`Date`、`Year`、`Month`、`Day`、`Hour`、`Minute`、`Second`、`Millisecond`、`DayOfWeek`、`DayOfYear`、`Ticks`、`TimeOfDay`。

> **注意**：`Ticks` 在不同数据库中精度不同，建议使用 `Date`/`Year`/`Month` 等进行日期比较，避免直接比较 `Ticks`。

### IQueryable 扩展方法

| 方法 | 命名空间 | 说明 |
|------|----------|------|
| `DataSharding(string key)` | `System.Linq` | 指定分片键 |
| `WhereIf(bool test, predicate)` | `System.Linq` | 条件为真时添加 WHERE |
| `Timeout(int seconds)` | `System.Linq` | 设置命令超时 |
| `NoElementError(string msg)` | `System.Linq` | 无结果时抛出自定义异常 |
| `ToList(int pageIndex, int pageSize)` | `System.Linq` | 分页查询，返回 `PagedList<T>` |

---

## 仓储操作

通过注入 `IRepository<TEntity>` 进行增删改操作。

### 插入

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

### 更新

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

### 删除

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

### 仓储方法链

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

---

## 动态条件与排序

### Conditions 动态条件

`Conditions` 静态类用于在 LINQ 表达式中构建动态 WHERE 条件（编译时展开，非运行时拼接）：

```csharp
// Conditions.If：条件为真时追加 WHERE
var linq = from u in _users
           where Conditions.If(!string.IsNullOrEmpty(name), u.Name.Contains(name))
              && Conditions.If(roleType.HasValue,
                    _userExes.Where(e => e.RoleType == roleType.Value)
                             .Select(e => e.Id).Contains(u.Id))
           select u;

// Conditions.Conditional：三元条件
var linq = from u in _users
           where Conditions.Conditional(useStrict, u.Id == targetId, u.Id > 0)
           select u;
```

**Conditions API**：

| 方法 | 说明 |
|------|------|
| `If(bool test, bool ifTrue)` | 条件为真时应用 `ifTrue` |
| `If<T>(T source, bool test, Func<T, bool> ifTrue)` | 带上下文的条件 |
| `Conditional(bool test, bool ifTrue, bool ifFalse)` | 三元条件 |
| `IsTrue<T>(T source, Func<T, bool> predicate)` | 总是应用条件 |
| `True<T>()` / `False<T>()` | 返回恒真/恒假表达式 |
| `And(expr, expr)` | 表达式 AND 组合 |
| `Or(expr, expr)` | 表达式 OR 组合 |
| `Fragment<T>(expr)` | 条件片段（用于 IsTrue） |

### WhereIf 扩展方法

```csharp
var result = await _users
    .WhereIf(!string.IsNullOrEmpty(name), u => u.Name.Contains(name))
    .WhereIf(isAdmin.HasValue, u => u.IsAdministrator == isAdmin.Value)
    .ToListAsync();
```

### Expression 组合

```csharp
Expression<Func<User, bool>> predicate = Conditions.True<User>();

if (!string.IsNullOrEmpty(name))
    predicate = predicate.And(u => u.Name.Contains(name));

if (isAdmin.HasValue)
    predicate = predicate.And(u => u.IsAdministrator == isAdmin.Value);

var result = await _users.Where(predicate).ToListAsync();
```

### Ranks 动态排序

`Ranks.By` 用于在 `orderby` 子句中实现运行时动态排序：

```csharp
var linq = from u in _users
           orderby u.DateAt,
                   Ranks.By(u, rank => rank
                       .When(sortType < 100)
                           .OrderBy(x => x.DateAt)
                           .ThenByDescending(x => x.Id)
                       .DefaultByDescending(x => x.DateAt)
                           .DefaultBy(x => x.Id))
           select u;
```

排序链式接口：`IRank<T>` → `.When(bool)` → `IOrderBy<T>` → `.OrderBy()/.OrderByDescending()` → `IThenBy<T>` → `.ThenBy()/.ThenByDescending()/.DefaultBy()/.DefaultByDescending()`

---

## 原生 SQL 与 IDatabase

通过注入 `IDatabase` 执行原生 SQL 和存储过程。

### 查询

```csharp
// 单条查询
var user = await _database.FirstOrDefaultAsync<User>(
    "SELECT * FROM user WHERE id = @id", new { id = 1 });

// 列表查询
var users = await _database.QueryAsync<User>(
    "SELECT * FROM user WHERE id > @id ORDER BY id LIMIT 10", new { id = 100 });

// 标量查询
var count = await _database.SingleAsync<int>(
    "SELECT COUNT(*) FROM user WHERE is_administrator = @flag", new { flag = true });
```

### 执行

```csharp
var affected = await _database.ExecuteAsync(
    "UPDATE user SET name = @name WHERE id = @id", new { name = "新名称", id = 1 });
```

### 多结果集（QueryMultiple）

```csharp
await using var reader = await _database.QueryMultipleAsync(
    "SELECT * FROM user WHERE id = @id; SELECT COUNT(*) FROM user;", new { id = 1 });

var user = await reader.ReadAsync<User>(RowStyle.FirstOrDefault);
var count = await reader.ReadAsync<int>(RowStyle.Single);
```

### 存储过程

```csharp
var outParam = new DynamicParameter
{
    Direction = ParameterDirection.Output,
    DbType = DbType.String,
    Size = 50
};

var parameters = new Dictionary<string, object>
{
    ["@UserId"] = 1,
    ["@UserName"] = outParam
};

var result = await _database.QueryAsync<User>("GetUserInfo", parameters);
var userName = outParam.Value as string;
```

### ExecuteMultiple 批量命令

```csharp
await _database.ExecuteMultipleAsync(async executor =>
{
    await executor.ExecuteAsync("UPDATE user SET name = @name WHERE id = @id",
        new { name = "A", id = 1 });
    await executor.ExecuteAsync("UPDATE user SET name = @name WHERE id = @id",
        new { name = "B", id = 2 });
});
```

### DynamicParameter

用于存储过程输出参数和特殊数据类型（如 JSON）：

| 属性 | 类型 | 说明 |
|------|------|------|
| `Value` | `object` | 参数值（输出参数执行后读取） |
| `Direction` | `ParameterDirection` | `Input` / `Output` / `InputOutput` / `ReturnValue` |
| `DbType` | `DbType` | 数据库类型 |
| `Size` | `int` | 参数大小（字符串/二进制输出参数必须指定） |
| `Precision` | `byte` | 数值精度 |
| `Scale` | `byte` | 小数位数 |

---

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

---

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

---

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

---

## JSON/JSONB 支持

框架提供 `JsonPayload` 和 `JsonbPayload` 类型用于 PostgreSQL 的 JSON/JSONB 字段：

```csharp
[Table("user_contents")]
public class UserContents
{
    [Key][Field("id")][DatabaseGenerated]
    public int Id { get; set; }

    [Field("content")]
    public JsonbPayload Content { get; set; }
}

// 插入
await _repository.Into(new UserContents
{
    Content = new JsonbPayload("{\"name\":\"test\"}")
}).ExecuteAsync();

// 通过 DynamicParameter 指定 JSONB 类型
var param = new DynamicParameter
{
    Value = "{\"name\":\"test\"}",
    DbType = LookupDb.JsonbDbType,
    Direction = ParameterDirection.Input
};
```

| 类型 | DbType | 用途 |
|------|--------|------|
| `JsonPayload` | `LookupDb.JsonDbType` | PostgreSQL JSON 字段 |
| `JsonbPayload` | `LookupDb.JsonbDbType` | PostgreSQL JSONB 字段 |

支持在 LINQ 查询和批量操作（`WriteToServerAsync`）中使用。

---

## 分片表

```csharp
[Table("user_[sharding]")]
public class UserSharding : User { }

// 查询
var users = await _userShardings
    .DataSharding("2024")
    .Where(x => x.Id > 100)
    .ToListAsync();

// 仓储操作
await _userShardingRepository
    .DataSharding("2024")
    .Into(userList)
    .ExecuteAsync();
```

---

## 多数据库配置

### 定义连接字符串类

```csharp
public class PromotionConnectionStrings : IConnectionStrings
{
    public string Strings => "Server=localhost;Database=Promotion;Trusted_Connection=true;";
}
```

### 注册多数据库

```csharp
services.UseMySql()
    .UseLinq("server=localhost;uid=root;pwd=password;database=main;");

services.UseSqlServer()
    .UseDatabase<PromotionConnectionStrings>();
```

### 使用

```csharp
public class MyService
{
    private readonly IQueryable<User> _users;
    private readonly IDatabase<PromotionConnectionStrings> _promDb;

    public MyService(IQueryable<User> users, IDatabase<PromotionConnectionStrings> promDb)
    {
        _users = users;
        _promDb = promDb;
    }
}
```

### IDatabaseFactory 动态创建

```csharp
services.AddDatabaseFactory();

var db = _databaseFactory.Create(DatabaseEngine.MySQL, connectionString);
var users = await db.QueryAsync<User>("SELECT * FROM user LIMIT 10");
```

---

## SQL 分析器 LinqAnalyzer

无需数据库连接即可将 LINQ 表达式翻译为 SQL，适用于单元测试和 SQL 预览：

```csharp
using Inkslab.Linq;

var users = LinqAnalyzer.From<User>(DatabaseEngine.MySQL, mySqlAdapter);

// 翻译为 CommandSql
CommandSql<List<User>> sql = users.Where(u => u.Id > 100).ToSql();

// 翻译为 SQL 字符串
string sqlText = users.Where(u => u.Id > 100).ToSqlString();

// 复杂查询翻译
string sqlText = users.ToSqlString(q => q.Where(u => u.Id > 100).Count());
```

---

## API 速查表

### IRepository&lt;TEntity&gt; 接口继承链

```
IRepository<TEntity>
  ├── DataSharding(string shardingKey) → IRepositoryDataSharding<TEntity>
  │     └── Timeout(int commandTimeout) → IRepositoryTimeout<TEntity>
  │           ├── Where(predicate) → IRepositoryCondition<TEntity>
  │           │     ├── TakeWhile(predicate)
  │           │     ├── SkipWhile(predicate)
  │           │     ├── Delete() / DeleteAsync()
  │           │     └── (链式 Where)
  │           ├── Ignore() → IRepositoryIgnore<TEntity>
  │           │     ├── Into(entries) → IInsertable<TEntity>
  │           │     │     ├── Limit(columns) / Except(columns) → ICommandExecutor
  │           │     │     ├── Execute() / ExecuteAsync()
  │           │     │     └── (直接执行)
  │           │     ├── Insert(IQueryable) / InsertAsync(IQueryable)
  │           │     └── (直接插入)
  │           ├── UpdateTo(entries) → IUpdateable<TEntity>
  │           │     ├── Set(columns) / SetExcept(columns) → IUpdateableOfSet<TEntity>
  │           │     │     └── SkipIdempotentValid() → ICommandExecutor
  │           │     └── Execute() / ExecuteAsync()
  │           └── DeleteWith(entries) → IDeleteable<TEntity>
  │                 ├── SkipIdempotentValid() → ICommandExecutor
  │                 └── Execute() / ExecuteAsync()
```

### IDatabase 方法一览

| 方法 | 说明 |
|------|------|
| `Single<T>` / `SingleAsync<T>` | 查询唯一值（无结果抛异常） |
| `SingleOrDefault<T>` / `SingleOrDefaultAsync<T>` | 查询唯一值或默认值 |
| `First<T>` / `FirstAsync<T>` | 查询首条（无结果抛异常） |
| `FirstOrDefault<T>` / `FirstOrDefaultAsync<T>` | 查询首条或默认值 |
| `Query<T>` / `QueryAsync<T>` | 查询列表 |
| `QueryMultiple` / `QueryMultipleAsync` | 多结果集查询 |
| `Execute` / `ExecuteAsync` | 执行非查询命令 |
| `WriteToServer` / `WriteToServerAsync` | 批量写入 |
| `ExecuteMultiple` / `ExecuteMultipleAsync` | 批量命令执行 |

### CommandSql 对象

```csharp
public class CommandSql
{
    public string Text { get; }                                    // SQL 文本
    public CommandType CommandType { get; }                        // Text / StoredProcedure
    public IReadOnlyDictionary<string, object> Parameters { get; } // 参数字典
    public int? Timeout { get; set; }                              // 超时（秒）
}
// StoredProcedureCommandSql : CommandSql — 存储过程
// CommandSql<T> : CommandSql — 带 RowStyle、DefaultValue、NoElementError
```

### RowStyle 枚举

| 值 | 说明 |
|----|------|
| `First` | 首条（无结果抛异常） |
| `FirstOrDefault` | 首条或默认值 |
| `Single` | 唯一（多条或无结果抛异常） |
| `SingleOrDefault` | 唯一或默认值 |

### 异常体系

| 异常类 | 基类 | 说明 |
|--------|------|------|
| `DSyntaxErrorException` | `SyntaxErrorException` | SQL 语法错误 |
| `NoElementException` | `CodeException` | 查询无结果（errorCode=1） |

---

## 测试

项目使用 [xunitPlus](https://github.com/tinylit/xunitPlus) 测试框架，支持构造函数依赖注入。

### 测试 Startup

```csharp
public class Startup : XunitPlus.Startup
{
    public Startup(Type serviceType) : base(serviceType) { }

    public override void ConfigureServices(IServiceCollection services, HostBuilderContext context)
    {
        services.UseMySql()
            .UseLinq("server=localhost;uid=root;pwd=password;database=test_db;Charset=utf8mb4;");
        services.AddDatabaseFactory();
        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));
        base.ConfigureServices(services, context);
    }
}
```

### 测试示例

```csharp
public class UserTests
{
    private readonly IRepository<User> _userRepository;
    private readonly IQueryable<User> _users;

    public UserTests(IRepository<User> userRepository, IQueryable<User> users)
    {
        _userRepository = userRepository;
        _users = users;
    }

    [Fact]
    public async Task QueryUsers()
    {
        var users = await _users.Where(u => u.Id > 0).Take(10).ToListAsync();
        Assert.NotNull(users);
    }
}
```

### 运行测试

```bash
dotnet test
dotnet test tests/Inkslab.Linq.Tests/
```

---

## 开发与构建

### 环境要求

- .NET 6.0 SDK 或更高版本
- C# 9.0（`LangVersion=9.0`，`strict` 模式）

### 构建

```bash
dotnet build
```

### 打包

```powershell
.\build.ps1
# 输出：.nupkgs/
# Inkslab.Transactions, Inkslab.Linq, Inkslab.Linq.MySql,
# Inkslab.Linq.SqlServer, Inkslab.Linq.PostgreSQL
```

### 包依赖

| 包 | 版本 |
|----|------|
| `Inkslab` | `[1.2.23, 2.0.0)` |
| `Microsoft.Extensions.Logging.Abstractions` | `3.1.0` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `3.1.0` |
| `Microsoft.Extensions.ObjectPool` | `6.0.36` |

---

## 许可证

[MIT License](LICENSE) - Copyright (c) 2023 Yuanli He