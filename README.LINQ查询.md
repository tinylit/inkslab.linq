# LINQ 查询

> [← 返回 README](README.md)

通过注入 `IQueryable<T>` 进行类型安全的 LINQ 查询，支持完整的 LINQ 方法链。

## 基础查询

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

## Join 查询

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

## 子查询

```csharp
// 子查询嵌套
var subQuery = from e in _userExes
               where e.RoleType == 1
               select e.Id;

var linq = from u in _users
           where subQuery.Contains(u.Id)
           select u;
```

## 分组聚合

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

## Union / Concat

```csharp
var query1 = _users.Where(u => u.IsAdministrator).Select(u => new { u.Id, u.Name });
var query2 = _users.Where(u => !u.IsAdministrator).Select(u => new { u.Id, u.Name });

var union = await query1.Union(query2).ToListAsync();       // 去重合并
var concat = await query1.Concat(query2).ToListAsync();     // 直接合并
```

## 异步查询方法

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

## 字符串函数支持

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

## DateTime 成员支持

LINQ 中的 `DateTime` 属性会自动翻译为各数据库的日期函数：

```csharp
var result = await _users
    .Where(u => u.DateAt.Year == 2024 && u.DateAt.Month == 12)
    .OrderBy(u => u.DateAt.Date)
    .ToListAsync();
```

支持的成员：`Date`、`Year`、`Month`、`Day`、`Hour`、`Minute`、`Second`、`Millisecond`、`DayOfWeek`、`DayOfYear`、`Ticks`、`TimeOfDay`。

> **注意**：`Ticks` 在不同数据库中精度不同，建议使用 `Date`/`Year`/`Month` 等进行日期比较，避免直接比较 `Ticks`。

## IQueryable 扩展方法

| 方法 | 命名空间 | 说明 |
|------|----------|------|
| `DataSharding(string key)` | `System.Linq` | 指定分片键 |
| `WhereIf(bool test, predicate)` | `System.Linq` | 条件为真时添加 WHERE |
| `Timeout(int seconds)` | `System.Linq` | 设置命令超时 |
| `NoElementError(string msg)` | `System.Linq` | 无结果时抛出自定义异常 |
| `ToList(int pageIndex, int pageSize)` | `System.Linq` | 分页查询，返回 `PagedList<T>` |
