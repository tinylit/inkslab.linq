# 高级特性

> [← 返回 README](README.md)

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
