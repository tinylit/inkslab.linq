# API 速查表

> [← 返回 README](README.md)

## IRepository&lt;TEntity&gt; 接口继承链

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
  │           │     │     ├── PopulateIdentity() → IInsertable<TEntity>  // 自增主键反写
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

## IDatabase 方法一览

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

## CommandSql 对象

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

## RowStyle 枚举

| 值 | 说明 |
|----|------|
| `First` | 首条（无结果抛异常） |
| `FirstOrDefault` | 首条或默认值 |
| `Single` | 唯一（多条或无结果抛异常） |
| `SingleOrDefault` | 唯一或默认值 |

## 异常体系

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
