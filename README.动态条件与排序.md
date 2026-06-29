# 动态条件与排序

> [← 返回 README](README.md)

## Conditions 动态条件

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

## WhereIf 扩展方法

```csharp
var result = await _users
    .WhereIf(!string.IsNullOrEmpty(name), u => u.Name.Contains(name))
    .WhereIf(isAdmin.HasValue, u => u.IsAdministrator == isAdmin.Value)
    .ToListAsync();
```

## Expression 组合

```csharp
Expression<Func<User, bool>> predicate = Conditions.True<User>();

if (!string.IsNullOrEmpty(name))
    predicate = predicate.And(u => u.Name.Contains(name));

if (isAdmin.HasValue)
    predicate = predicate.And(u => u.IsAdministrator == isAdmin.Value);

var result = await _users.Where(predicate).ToListAsync();
```

## Ranks 动态排序

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
