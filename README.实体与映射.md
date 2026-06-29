# 实体与映射

> [← 返回 README](README.md)

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

## 注解速查

| 注解 | 目标 | 说明 |
|------|------|------|
| `[Table("table_name")]` | 类 | 指定数据库表名，可选设置 `Schema` |
| `[Field("column_name")]` | 属性 | 指定数据库字段名 |
| `[Key]` | 属性 | 标记为主键（`Update`/`Delete` 以主键为条件） |
| `[DatabaseGenerated]` | 属性 | 标记为数据库生成字段（`Insert` 时忽略） |
| `[Version]` | 属性 | 乐观锁版本控制 |

### `[Version]` 自动值规则

| 属性类型 | 自动值策略 |
|----------|-----------|
| `int` | 自增 +1 |
| `long` | `DateTime.Now.Ticks` |
| `double` | UTC 时间戳 |
| `DateTime` | `DateTime.Now` |

## 列值类型映射（数据库 → 实体）

查询结果回填实体时，框架将数据库列值转换为实体属性类型。**核心原则：在不丢失数值的前提下，支持「小类型 → 大类型」。**

### 数值类型

- **字段类型与属性类型一致**：直接读取，无额外开销。
- **无损小转大**：自动转换，例如
  - 整数加宽：`sbyte`/`byte`/`short`/`ushort` → `int`，`int`/`uint` → `long`，`uint` → `ulong` 等；
  - 浮点/定点加宽：`float` → `double`，`int`/`long`/`float`/`double` → `decimal`，`decimal` → `double`/`float`。
- **反向「大转小」**（如 `long` → `int`）：执行运行时**区间校验**，值在范围内则转换，**超出范围抛出友好异常（绝不静默截断）**。

### 任意类型 → `string`

实体属性为 `string` 而数据库字段为其它类型（`int`、`long`、`decimal`、`bool`、`DateTime`、`Guid` 等）时，框架自动调用其 `ToString()` 完成转换。

### 枚举（`enum`）

枚举属性同时支持「数字来源」与「字符串来源」（可空枚举 `enum?` 同样适用）：

- **数字来源**：按枚举底层类型（`int`/`long`/…）读取数据库数值，并支持上述无损小转大（如字段 `tinyint` → 底层 `int`）。
- **字符串来源**：使用 `Enum.Parse(..., ignoreCase: true)`，同时兼容
  - **枚举名称**（如 `"Green"`，忽略大小写）；
  - **数字文本**（如 `"3"`）。

> 写入方向（实体 → SQL 参数）：枚举按其底层数值类型绑定参数（见 `LookupDb`）。

## 分片表定义

表名中使用 `[sharding]` 占位符，运行时通过 `DataSharding(key)` 替换：

```csharp
[Table("user_[sharding]")]         // 如 DataSharding("2024") → "user_2024"
public class UserSharding : User { }
```

## Fluent API 配置（IConfig）

除注解外，还支持 Fluent API 方式配置实体映射：

```csharp
public interface IConfig<TTable>
{
    IConfigTable Table(string name);
    IConfigCol Field(string name);
}
```
