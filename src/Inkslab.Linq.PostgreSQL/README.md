# Inkslab.Linq.PostgreSQL

PostgreSQL 数据库适配器，基于 [Npgsql](https://www.npgsql.org/) 驱动，支持 JSON/JSONB 数据类型和 COPY 批量操作。

## 依赖

| 包 | 版本 |
|----|------|
| `Inkslab.Linq` | 项目引用 |
| `Npgsql` | `8.0.8`（netstandard2.1）/ `9.0.4`（net6.0） |

## 注册

```csharp
services.UsePostgreSQL()
    .UseLinq("Host=localhost;Database=mydb;Username=postgres;Password=password;");
```

扩展方法 `UsePostgreSQL()` 注册 `DatabaseEngine.PostgreSQL` 引擎和 `NpgsqlConnection` 工厂。

## 组件

| 类 | 说明 |
|----|------|
| `PostgreSQLAdapter` | 实现 `IDbAdapter`，注册 PostgreSQL 方言的方法访问器 |
| `PostgreSQLCorrectSettings` | 实现 `IDbCorrectSettings`，标识符用双引号 `"` 包裹，`LIMIT/OFFSET` 分页 |
| `PostgreSQLBulkCopyFactory` | 实现 `IDbConnectionBulkCopyFactory`，使用 `COPY FROM STDIN` 批量写入 |
| `PostgreSQLBulkAssistant` | 批量复制辅助类，支持 `JsonPayload`/`JsonbPayload` 类型写入 |

## JSON/JSONB 支持

使用 `JsonPayload` 或 `JsonbPayload` 类型映射 PostgreSQL 的 `json`/`jsonb` 字段：

```csharp
[Table("user_contents")]
public class UserContents
{
    [Key][Field("id")][DatabaseGenerated]
    public int Id { get; set; }

    [Field("content")]
    public JsonbPayload Content { get; set; }
}
```

- `JsonPayload`：映射 `json` 类型，参数自动添加 `::json` 转换
- `JsonbPayload`：映射 `jsonb` 类型，参数自动添加 `::jsonb` 转换
- 批量操作（`WriteToServerAsync`）完整支持 JSON/JSONB 异步写入

## DateTime 成员映射

PostgreSQL 使用 `EXTRACT` 函数：

| C# 成员 | SQL 函数 |
|---------|----------|
| `Date` | `CAST(DATE_TRUNC('day', date) AS DATE)` |
| `Year` / `Month` / `Day` | `EXTRACT(YEAR/MONTH/DAY FROM date)` |
| `Hour` / `Minute` / `Second` | `EXTRACT(HOUR/MINUTE/SECOND FROM date)` |
| `Millisecond` | `EXTRACT(MILLISECONDS FROM date)::INTEGER % 1000` |
| `DayOfWeek` | `EXTRACT(DOW FROM date)` |
| `DayOfYear` | `EXTRACT(DOY FROM date)` |
| `TimeOfDay` | `date::TIME` |

## 类型映射注意事项

| C# 类型 | PostgreSQL 类型 | 注意 |
|---------|----------------|------|
| `UInt64` | `Numeric` | 需注意精度，建议使用 `decimal` |
| `Byte` / `SByte` | `Smallint` | — |
| `UInt16` | `Integer` | — |
