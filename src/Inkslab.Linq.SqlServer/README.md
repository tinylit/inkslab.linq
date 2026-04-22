# Inkslab.Linq.SqlServer

SQL Server 数据库适配器，基于 [Microsoft.Data.SqlClient](https://github.com/dotnet/SqlClient) 驱动。

## 依赖

| 包 | 版本 |
|----|------|
| `Inkslab.Linq` | 项目引用 |
| `Microsoft.Data.SqlClient` | `5.2.2` |

## 注册

```csharp
services.UseSqlServer()
    .UseLinq("Server=localhost;Database=MyDB;Trusted_Connection=true;");
```

扩展方法 `UseSqlServer()` 注册 `DatabaseEngine.SqlServer` 引擎和 `SqlConnection` 工厂。

## 组件

| 类 | 说明 |
|----|------|
| `SqlServerAdapter` | 实现 `IDbAdapter`，注册 SQL Server 方言的方法访问器 |
| `SqlServerCorrectSettings` | 实现 `IDbCorrectSettings`，标识符用 `[]` 包裹，`OFFSET/FETCH` 分页 |
| `SqlServerBulkCopyFactory` | 实现 `IDbConnectionBulkCopyFactory`，使用 `SqlBulkCopy` 批量复制 |
| `SqlServerBulkAssistant` | 批量复制辅助类 |

## DateTime 成员映射

SQL Server 使用 `DATEPART` 和 `DATEDIFF` 系列函数：

| C# 成员 | SQL 函数 |
|---------|----------|
| `Date` | `CAST(date AS DATE)` |
| `Year` / `Month` / `Day` | `DATEPART(YEAR/MONTH/DAY, date)` |
| `Hour` / `Minute` / `Second` | `DATEPART(HOUR/MINUTE/SECOND, date)` |
| `Millisecond` | `DATEPART(MILLISECOND, date)` |
| `DayOfWeek` | `DATEPART(WEEKDAY, date)` |
| `DayOfYear` | `DATEPART(DAYOFYEAR, date)` |
| `TimeOfDay` | `CAST(date AS TIME)` |
| `Ticks` | `DATEDIFF(NANOSECOND, '1900-01-01', date) / 100` |
