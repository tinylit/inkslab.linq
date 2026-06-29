# LINQ 编写规则与禁令清单

> [← 返回 README](README.md)

> **AI 编码依据**：框架在 LINQ → SQL 翻译阶段会对写法做严格校验，命中下列任一条件即抛异常（多为 `DSyntaxErrorException`）。编写查询、增删改命令、实体注解时**必须**遵守本清单，方可保证翻译成功、语法正确。
>
> 完整异常清单（含位置与原文）见 [`docs/LINQ与命令翻译报错规则.md`](docs/LINQ与命令翻译报错规则.md)。

## 一、查询方法链顺序

LINQ 方法的调用顺序受严格约束：

| 规则 | ✅ 正确 | ❌ 错误 |
|------|--------|--------|
| `Distinct()` 前必须先 `Select` 指定字段 | `.Select(x => x.Name).Distinct()` | `.Distinct()` |
| 聚合函数（`Max`/`Min`/`Sum`/`Average`）必须指定聚合字段 | `.Max(x => x.Id)` 或 `.Select(x => x.Id).Max()` | `.Max()` |
| `Skip`/`Take`/分页前必须先 `OrderBy`/`OrderByDescending` | `.OrderBy(x => x.Id).Skip(10).Take(10)` | `.Skip(10).Take(10)` |
| 单个查询脚本**只能 `Select` 一次**，且须置于过滤/排序/分组**之后** | `.Where(...).OrderBy(...).Select(...)` | `.Select(...).Where(...).Select(...)` |
| `DefaultIfEmpty()` 前必须先 `Select` | `.Select(x => x.Name).DefaultIfEmpty("")` | `.DefaultIfEmpty()` |
| 不支持带索引参数的重载 | `.Select(x => x.Name)` | `.Select((x, i) => x.Name)` |
| 同一查询链中 lambda 参数命名须**保持一致** | `from u in users ... where u.Id > 0` | 同链中混用 `u` / `x` 指代同一表 |
| 只能翻译框架支持的 LINQ 方法 | 见[异步查询方法](README.LINQ查询.md#异步查询方法) | 框架未实现的自定义方法 |

## 二、Select 投影规则

| 规则 | 说明 |
|------|------|
| ❌ 不支持基础类型多参数构造 | 禁止 `x => new SimpleType(x.Id, x.Name, 3)` 或 `x => new SimpleType(1, 2, 3)` |
| ❌ 不支持把整个参数对象作为成员 | 禁止 `x => new { x }` 或 `(x, y) => new { x.Id, y }`，必须取**具体字段** |
| ❌ 不支持导航属性 | 只能投影标量字段，不能用对象导航属性 |
| ❌ 访问未映射字段会报 `MissingFieldException` | 投影的属性必须有 `[Field]` 映射（或符合默认映射规则） |
| 列别名要求 | 排名/复杂投影列必须指定别名：`x => new { NewName = x.Edation }` |

## 三、条件比较（`Where` / `On`）

| 规则 | 说明 |
|------|------|
| ✅ 实体/Join 分支可与 `null` 比较（判空） | 来自查询表或 Join 分支的实体参数可 `entity == null` / `entity != null`，翻译为其主键（无主键则所有字段）的 `IS NULL` / `IS NOT NULL`，典型用于左连接 `DefaultIfEmpty()` 判空 |
| ❌ 游离的非标量类型不能与 `null` 比较 | 无法解析为表/Join 分支的复杂类型与 `null` 比较会抛 `DSyntaxErrorException`；标量字段判空请用 `x.Id == null` |
| ❌ 非标量类型不能与非 `null` 常量比较 | 与非 null 常量比较时，左右操作数须为可映射为列的标量值（`IsCell`：值类型/可空值类型/枚举/`string`/`Guid`/Json 等） |

## 四、聚合与分组

| 规则 | 说明 |
|------|------|
| `GROUP BY` 投影列约束 | 投影的列**必须**出现在聚合函数或分组键（`g.Key`）中，否则报「列无效」 |
| ❌ 聚合查询不支持多字段 | 单个聚合函数只能作用于单字段表达式 |
| 仅支持框架已知聚合函数 | 自定义/未识别的聚合函数会被拒绝 |

## 五、排序与排名（`Ranks`）

| 规则 | 说明 |
|------|------|
| 判断条件仅允许常量 | `Ranks.By(...).When(条件)` 中的条件**只能是常量表达式** |
| 排名函数必须在 `Select` 中使用 | 不能用于非排名查询 |
| 排名函数必须含分区或排序条件 | 缺少分区/排序会报错 |
| 排名/投影列必须指定别名 | `x => new { Rn = ... }` |

## 六、Join 与子查询

| 规则 | 说明 |
|------|------|
| 一个查询器有且仅有一次查询结果 | JOIN 的每个子查询输出必须唯一确定（一次 `Select`） |
| `DefaultIfEmpty()`（左连接）须按规范写法 | 见 [Join 查询](README.LINQ查询.md#join-查询) 的 `into ... from ... DefaultIfEmpty()` 模式 |

## 七、字符串与日期方法

| 规则 | 说明 |
|------|------|
| 仅支持框架已翻译的字符串方法 | 见 [字符串函数支持](README.LINQ查询.md#字符串函数支持)；其它方法报「不被支持」 |
| 仅支持已知日期片段 | 仅 `Date`/`Year`/`Month`/`Day`/`Hour`/`Minute`/`Second`/`Millisecond`/`DayOfWeek`/`DayOfYear`/`Ticks`/`TimeOfDay`（见 [DateTime 成员支持](README.LINQ查询.md#datetime-成员支持)） |
| 不同引擎支持度不同 | 某方法/片段在特定引擎不支持时会报「引擎不支持」 |
| ❌ 不支持静态 `ToString` | 不能在表达式中调用静态 `ToString` |

## 八、方法调用通用限制

| 规则 | 说明 |
|------|------|
| ❌ 不能把方法调用结果直接嵌入表达式 | **静态或实例方法**的返回值不能直接作为表达式的一部分；须先求值为变量再使用 |

```csharp
// ❌ 错误：方法结果直接嵌入
var list = await _users.Where(u => u.Name == GetName()).ToListAsync();

// ✅ 正确：先求值为变量
var name = GetName();
var list = await _users.Where(u => u.Name == name).ToListAsync();
```

## 九、增删改命令（Insert / Update / Delete）

| 规则 | 说明 |
|------|------|
| `Update` 必须指定更新字段 | 表达式更新须 `new User { ... }`；实体更新须有可更新列 |
| `Insert` 必须指定插入字段 | 过滤后字段集合不能为空 |
| ❌ 字段不可重复指定 | 同一字段在 `Set`/`Limit`/`Except` 等中只能出现一次 |
| ❌ 不可写入只读字段 | `[DatabaseGenerated]` 等只读字段不能出现在 Insert/Update 列中 |
| 只能指定有效数据库字段 | 未映射的属性不能用于增删改 |
| ❌ 无主键表不支持 `Update`/`Delete` | 实体更新/删除以主键为条件，必须有 `[Key]` |
| `[Version]` 类型受限 | 乐观锁字段仅支持 `int`/`long`/`double`/`DateTime`，其它类型报错 |

## 十、分片表与分页

| 规则 | 说明 |
|------|------|
| 分片表**必须**指定分区键 | `[Table("x_[sharding]")]` 的表操作前须 `.DataSharding(key)` |
| ❌ 普通表不能用分区操作 | 非分片表调用 `DataSharding` 会报「不支持分区操作」 |
| 分片键不能为 null/空 | `DataSharding("")` 非法 |
| 分页页码 ≥ 1，每页条目 ≥ 1 | `ToList(pageIndex, pageSize)` 两参均须 ≥ 1 |
| ❌ `Take`/`Skip` 不能为负 | `Take(n)`/`Skip(n)` 的 `n` 须 ≥ 0 |

## 十一、实体注解配置

| 规则 | 说明 |
|------|------|
| `[Table]` 必填且规范 | 表名不能为 null/空白，不能含首尾空白 |
| `[Field]` 名称规范 | 字段名不能为 null/空白，不能含首尾空白 |
| 实体必须声明字段 | 无任何字段的实体非法 |
| `[Version]` 类型受限 | 仅 `int`/`long`/`double`/`DateTime`（见 [`[Version]` 自动值规则](README.实体与映射.md#version-自动值规则)） |
| `PopulateIdentity()` 前置条件 | 须「单主键 + `[DatabaseGenerated]`」，且引擎支持（见 [自增主键反写](README.仓储操作.md#自增主键反写populateidentity)） |

## 十二、自定义错误消息（`NoElementError`）

| 规则 | 说明 |
|------|------|
| 仅用于单元素终结操作 | `NoElementError` 仅在链以 `Min`/`Max`/`Average`/`Last`/`First`/`Single`/`ElementAt` 结尾时可用 |
| 错误消息不能为空 | 自定义消息须为非空字符串 |
| `DefaultIfEmpty` 默认值类型须兼容 | 默认值类型须能转换为查询结果类型；`null` 不能用于非可空值类型 |

## 十三、单行查询的结果约定（运行期）

下列规则在**执行时**而非翻译时校验，决定应选 `First` 还是 `FirstOrDefault`：

| 终结方法 | 无结果 | 多于一行 |
|----------|--------|---------|
| `First` / `FirstAsync` | ❌ 抛 `NoElementException` | 取首条 |
| `Single` / `SingleAsync` | ❌ 抛 `NoElementException` | ❌ 抛 `MultipleRowsException` |
| `FirstOrDefault` / `SingleOrDefault` | 返回默认值 | `First*` 取首条 / `Single*` 仍抛多行异常 |

- 不确定是否有结果时，用 `*OrDefault` 版本。
- 需要「无结果即报错且消息友好」时，用 `NoElementError("自定义消息")`（约束见[第十二节](#十二自定义错误消息noelementerror)）。

## 十四、多结果集消费约定（`QueryMultiple`）

| 规则 | 说明 |
|------|------|
| 必须**按顺序**读取 | 结果集须与 SQL 中语句顺序一致地 `ReadAsync` |
| 每个结果集**只能读一次** | 乱序或重复消费会抛 `Query results must be consumed in the correct order...` |

```csharp
await using var reader = await _database.QueryMultipleAsync(
    "SELECT * FROM user WHERE id = @id; SELECT COUNT(*) FROM user;", new { id = 1 });

var user = await reader.ReadAsync<User>(RowStyle.FirstOrDefault);  // 第 1 个结果集
var count = await reader.ReadAsync<int>(RowStyle.Single);          // 第 2 个结果集（不可回读第 1 个）
```

## 十五、原生 SQL 参数约束（`IDatabase`）

| 规则 | 说明 |
|------|------|
| ❌ 字面模式不支持集合参数 | 非参数化（字面）写法不能传集合值 |
| `IN` 参数值必须可迭代 | 用于 `IN` 展开的参数值须为可枚举集合，否则抛「参数值不可迭代！」 |
| `{=name}` 占位符须存在 | SQL 中引用的 `{=name}` 参数必须在参数字典中提供，否则抛 `KeyNotFoundException` |
| 批量写入须指定表名 | `WriteToServerAsync(dataTable)` 的 `DataTable.TableName` 必须指定 |

## 十六、依赖注入与连接注册约定

服务注册阶段的硬性规则（启动时命中即抛异常）：

| 规则 | 说明 |
|------|------|
| 引擎须先注册再使用 | 使用某引擎前必须调用对应 `UseMySql()`/`UseSqlServer()`/`UsePostgreSQL()`，否则抛「引擎未注册」 |
| ❌ `UseLinq` 不可重复注册 | 同一构建器上 `UseLinq` 只能调用一次 |
| ❌ 连接类型不可重复注册 | 一个 `IConnectionStrings` 类型只能绑定一个数据库连接 |
| `MappingCapacity > 0` | 该配置项必须大于 0 |
| 连接字符串非空 | `UseLinq(connStr)` / `UseDatabase` 的连接串不能为 null/空 |
