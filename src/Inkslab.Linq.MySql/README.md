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

## 自增主键反写（PopulateIdentity）

`Into(entries).PopulateIdentity()` 在插入后将数据库生成的自增 ID 回填到实体（要求「单主键 + `[DatabaseGenerated]`」）。

| 能力 | 支持 | 实现方式 |
|------|------|----------|
| 反写（非 Ignore） | ✅ | 标量族：单条命令内多组「单行 INSERT + `LAST_INSERT_ID()` 回读」，按参数预算分块，往返 ⌈N/K⌉ 次（绝不逐行往返） |
| `Ignore` + 反写 | ✅ | 同上，另读 `ROW_COUNT()` 判定该行是否被冲突跳过；被跳过的实体保持原值 |

```csharp
await _userRepository.Into(users).PopulateIdentity().ExecuteAsync();
await _userRepository.Ignore().Into(users).PopulateIdentity().ExecuteAsync();
```

## 连接字符串建议

- 使用 `Charset=utf8mb4` 支持完整 Unicode
- 使用 `AllowLoadLocalInfile=true` 启用批量加载
