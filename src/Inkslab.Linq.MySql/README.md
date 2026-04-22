# Inkslab.Linq.MySql

MySQL 数据库适配器，基于 [MySqlConnector](https://mysqlconnector.net/) 驱动。

## 依赖

| 包 | 版本 |
|----|------|
| `Inkslab.Linq` | 项目引用 |
| `MySqlConnector` | `2.4.0` |

## 注册

```csharp
services.UseMySql()
    .UseLinq("server=localhost;uid=root;pwd=password;database=mydb;Charset=utf8mb4;");
```

扩展方法 `UseMySql()` 注册 `DatabaseEngine.MySQL` 引擎和 `MySqlConnection` 工厂。

## 组件

| 类 | 说明 |
|----|------|
| `MySqlAdapter` | 实现 `IDbAdapter`，注册 MySQL 方言的方法访问器和字段名包装规则 |
| `MySqlCorrectSettings` | 实现 `IDbCorrectSettings`，MySQL 标识符用反引号 `` ` `` 包裹，`LIMIT offset, count` 分页 |
| `MySqlBulkCopyFactory` | 实现 `IDbConnectionBulkCopyFactory`，MySQL 批量复制（`LOAD DATA LOCAL INFILE`） |
| `MySqlBulkAssistant` | 批量复制辅助类 |

## 连接字符串建议

- 使用 `Charset=utf8mb4` 支持完整 Unicode
- 使用 `AllowLoadLocalInfile=true` 启用批量加载
