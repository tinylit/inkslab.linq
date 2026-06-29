# 原生 SQL 与 IDatabase

> [← 返回 README](README.md)

通过注入 `IDatabase` 执行原生 SQL 和存储过程。

## 查询

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

## 执行

```csharp
var affected = await _database.ExecuteAsync(
    "UPDATE user SET name = @name WHERE id = @id", new { name = "新名称", id = 1 });
```

## 多结果集（QueryMultiple）

```csharp
await using var reader = await _database.QueryMultipleAsync(
    "SELECT * FROM user WHERE id = @id; SELECT COUNT(*) FROM user;", new { id = 1 });

var user = await reader.ReadAsync<User>(RowStyle.FirstOrDefault);
var count = await reader.ReadAsync<int>(RowStyle.Single);
```

## 存储过程

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

## ExecuteMultiple 批量命令

```csharp
await _database.ExecuteMultipleAsync(async executor =>
{
    await executor.ExecuteAsync("UPDATE user SET name = @name WHERE id = @id",
        new { name = "A", id = 1 });
    await executor.ExecuteAsync("UPDATE user SET name = @name WHERE id = @id",
        new { name = "B", id = 2 });
});
```

## DynamicParameter

用于存储过程输出参数和特殊数据类型（如 JSON）：

| 属性 | 类型 | 说明 |
|------|------|------|
| `Value` | `object` | 参数值（输出参数执行后读取） |
| `Direction` | `ParameterDirection` | `Input` / `Output` / `InputOutput` / `ReturnValue` |
| `DbType` | `DbType` | 数据库类型 |
| `Size` | `int` | 参数大小（字符串/二进制输出参数必须指定） |
| `Precision` | `byte` | 数值精度 |
| `Scale` | `byte` | 小数位数 |
