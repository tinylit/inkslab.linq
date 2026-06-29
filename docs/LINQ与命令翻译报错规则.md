# Inkslab.Linq 报错规则与禁令清单

> 本文从源码中穷尽收集了 `Inkslab.Linq`、三个数据库适配器（MySql / SqlServer / PostgreSQL）以及 `Inkslab.Transactions` 中**全部 `throw new ...Exception`（共 364 处）**，按模块归类，明确每条报错的**触发原因（即规则 / 禁令）**与**位置**。
>
> 用途：作为编写 LINQ 查询、实体注解、增删改命令时的「禁令对照表」——凡命中下表条件即会抛错，应在编码阶段规避。

## 异常类型说明

| 异常类型 | 含义 |
|---|---|
| `DSyntaxErrorException` | LINQ 语法 / 写法错误（继承 `System.Data.SyntaxErrorException`），最常见的「用法禁令」类报错 |
| `NotSupportedException` | 当前引擎或框架不支持该类型 / 方法 / 能力 |
| `InvalidOperationException` | 操作时机或对象状态非法（如未指定分区键、实体为空、结果重复消费） |
| `NotImplementedException` | 仅供框架内部占位的成员被错误调用，或未实现的占位方法 |
| `NoElementException` / `MultipleRowsException` | 单行查询结果数不符合预期（继承 `CodeException`） |
| `ArgumentException` / `ArgumentNullException` / `ArgumentOutOfRangeException` | API 入参校验失败 |
| `(无消息)` | 源码中未携带文案，多为防御性断言，正常使用不应触发 |

阅读约定：消息原文保留源码中的中文与插值占位符（如 `{name}`、`{Engine}`）。同一文案在多个数据库引擎分支重复出现的，合并为一行并列出全部行号。

---

## 一、查询脚本结构（`ScriptVisitor`）

LINQ 方法链的**顺序与组合规则**。

| 报错信息 | 触发原因（禁令） | 位置 |
|---|---|---|
| `使用去重函数需先指定查询字段，如：*.Select(x=>{column-part}).Distinct()。` | 调用 `Distinct()` 前必须先 `Select` 指定查询字段 | `ScriptVisitor.cs:302`、`:397` |
| `使用聚合函数需先指定聚合字段，如：*.{name}(x => x.Field) 或 *.Select(x => x.Field).{name}()。` | 调用聚合函数（`Max`/`Min`/`Sum`/`Average`）前必须指定聚合字段 | `ScriptVisitor.cs:307` |
| `使用函数"{name}"时，必须使用排序函数（OrderBy/OrderByDescending）！` | 使用 `Skip`/`Take`/分页等函数前必须先 `OrderBy`/`OrderByDescending` | `ScriptVisitor.cs:325` |
| `单个脚步仅支持指定一次查询，请将.Select(x=>{column-part})放在过滤、排序和分组等函数之后，如：*.OrderBy(x=>{column-part}).Select(x=>{column-part}).Skip({skipSize}).Take({TakeSize})！` | 单个查询脚本只能 `Select` 一次，且必须置于过滤 / 排序 / 分组之后 | `ScriptVisitor.cs:377` |
| `使用默认值函数需先指定查询字段，如：*.Select(x=>{column-part}).DefaultIfEmpty({default-value})。` | 调用 `DefaultIfEmpty()` 前必须先 `Select` 指定查询字段 | `ScriptVisitor.cs:406` |
| `方法"{name}"不被支持！` | 使用了框架无法翻译为 SQL 的 LINQ 方法 | `ScriptVisitor.cs:805` |
| `不支持索引参数！` | 在带索引参数的重载中使用了索引（如 `Select((x,i)=>...)`） | `ScriptVisitor.cs:1125` |
| `请保持查询参数的名称一致性！` | 同一查询链中 lambda 参数命名不一致 | `ScriptVisitor.cs:1145`、`:1206` |
| `(无消息)` `DSyntaxErrorException` | 防御性断言：参数关系解析异常 | `ScriptVisitor.cs:534` |
| `(无消息)` `NotImplementedException` | 防御性断言：未实现的访问分支 | `ScriptVisitor.cs:1375` |

---

## 二、条件与比较（`ConditionVisitor`）

`Where` / `On` 等条件子句的比较规则。

| 报错信息 | 触发原因（禁令） | 位置 |
|---|---|---|
| `不支持参数类型"{left.Type}"与"null"的比较！` | 非标量（`Cell`）类型的左操作数与 `null` 比较 | `ConditionVisitor.cs:327` |
| `不支持参数类型"{left.Type}"与非"null"常量值的比较！` | 非标量类型的左操作数与非 `null` 常量比较 | `ConditionVisitor.cs:330` |
| `不支持参数类型"{right.Type}"与"null"的比较！` | 非标量类型的右操作数与 `null` 比较 | `ConditionVisitor.cs:355` |
| `不支持参数类型"{right.Type}"与非"null"常量值的比较！` | 非标量类型的右操作数与非 `null` 常量比较 | `ConditionVisitor.cs:358` |
| `(无消息)` `NotSupportedException` | 防御性断言：条件表达式中出现不支持的逻辑 / 转义操作 | `ConditionVisitor.cs:720`、`:780` |
| `(无消息)` `ArgumentNullException` | 二元表达式左 / 右操作数为 null | `ConditionVisitor.cs:224`、`:229`、`:297`、`:302` |

---

## 三、成员、字段与投影（`BaseVisitor` / `SelectVisitor`）

实体成员访问与投影（`new {...}`）规则。

| 报错信息 | 触发原因（禁令） | 位置 |
|---|---|---|
| `静态的 ToString 方法不支持转换！` | 在表达式中调用静态 `ToString` | `BaseVisitor.cs:237` |
| `不支持数据库引擎"{Engine}"的 ToString() 方法转换！` | 当前引擎不支持 `ToString()` 转换 | `BaseVisitor.cs:290` |
| `不支持基础类型的多参数实例化，如：x => new SimpleType(x.Id,x.Name,3) 或 x => new SimpleType(1, 2, 3) 语法。` | 投影中对基础类型做多参数构造 | `BaseVisitor.cs:597` |
| `不支持基础类型的聚合属性，如：(x,y) => new { x.Id, y } 或 x => new { x } 语法。` | 投影中把整个参数对象作为成员（应改为具体字段） | `BaseVisitor.cs:607` |
| `属性"{parameter.Type.Name}.{node.Member.Name}"不是有效的数据库字段！` `MissingFieldException` | 访问了实体上不存在 / 未映射的字段 | `BaseVisitor.cs:780` |
| `不支持依赖成员"{node.Member.Name}"的解析！` | 访问了带复杂依赖关系的成员 | `BaseVisitor.cs:798` |
| `不支持导航属性！` | 使用了对象导航属性而非标量字段 | `BaseVisitor.cs:812` |
| `不支持查询器常量！` | 把 `IQueryable` 常量直接作为表达式值 | `BaseVisitor.cs:921` |
| `未能分析到表参数！` | lambda 体为参数时无法确定来源 | `BaseVisitor.cs:976` |
| `未能分析到表名称！` | 无法解析表信息 / 表名 | `BaseVisitor.cs:981`、`:1294`、`:1346` |
| `(无消息)` `NotSupportedException` | 防御性断言：成员 / 调用解析失败 | `BaseVisitor.cs:492`、`:497`、`:960`、`:1174` |
| `(无消息)` `NotImplementedException` | 防御性断言：无父访问器时调用 | `BaseVisitor.cs:949`、`:1058` |
| `(无消息)` `NotSupportedException` | 防御性断言：`Reverse` 等遇到不支持的方法 | `SelectVisitor.cs:341` |
| `(无消息)` `ArgumentNullException` | 构造函数 `adapter` / `visitor` 为 null | `BaseVisitor.cs:69`、`:82` |

---

## 四、聚合与分组（`AggregateVisitor` / `AggregateTermVisitor` / `AggregateSelectVisitor`）

| 报错信息 | 触发原因（禁令） | 位置 |
|---|---|---|
| `聚合函数"{node.Method}"不被支持！` | 使用了框架不支持的聚合函数 | `AggregateVisitor.cs:82` |
| `在聚合查询中，聚合函数"{name}"不被识别!` | 聚合查询中出现无法识别的聚合函数名 | `AggregateVisitor.cs:125`、`AggregateTermVisitor.cs:259` |
| `聚合查询不支持多字段！` | 对多字段表达式做聚合 | `AggregateTermVisitor.cs:283` |
| `列"{memberName}"无效，因为该列没有包含在聚合函数或"GROUP BY"子句中！` | `GROUP BY` 查询中投影了既不在聚合函数也不在分组键中的列 | `AggregateSelectVisitor.cs:95` |

---

## 五、排序与排名 / 窗口函数（`OrderByVisitor`）

| 报错信息 | 触发原因（禁令） | 位置 |
|---|---|---|
| `不支持的排名表达式，表达式的判断条件仅允许使用常量表达式！` | `Ranks` 排名表达式的判断条件中含非常量 | `OrderByVisitor.cs:205`、`:210` |
| `不支持非排名查询的"{node.Method.Name}"函数！` | 在非排名查询上使用了排名专用函数 | `OrderByVisitor.cs:237` |
| `排名函数"{node.Method.Name}"必须包含分区或排序条件！` | 排名函数缺少分区 / 排序条件 | `OrderByVisitor.cs:241` |
| `使用了不支持的排名函数"{node.Method.Name}"！` | 调用了不支持的排名函数 | `OrderByVisitor.cs:242`、`:243`、`:253`、`:254`、`:267` 等多处 |
| `排名函数必须在"Select"中使用！` | 排名函数未放在 `Select` 内 | `OrderByVisitor.cs:244`、`:258` |
| `必须为列指定别名，如：x=> new { NewName = x.Edation }！` | 排名 / 投影列未指定别名 | `OrderByVisitor.cs:245`、`:250`、`:259`、`:262`、`:263` 等多处 |
| `列别名不能为空！` | 列别名为空字符串（在 `:251`–`:400+` 的 switch 分支中大量重复出现） | `OrderByVisitor.cs:251`、`:252`、`:255`–`:400+`（多处） |
| `(无消息)` `NotSupportedException` | 防御性断言：`RankBy` 遇到无法识别的方法 | `OrderByVisitor.cs:240` |
| `(无消息)` `ArgumentNullException`（参数 `rank`） | `OrderBy` / `OrderByDescending` / `ThenBy` / `ThenByDescending` / `DefaultBy` / `DefaultByDescending` 等扩展方法 `rank` 入参为 null | `OrderByVisitor.cs:318`、`:330`、`:342`、`:354`、`:378`、`:393`、`:420`、`:430`、`:440`、`:450` |

---

## 六、Join 与子查询（`JoinVisitor`）

| 报错信息 | 触发原因（禁令） | 位置 |
|---|---|---|
| `一个查询器中，有且仅有一次查询结果！` | JOIN 构建时 SELECT 输出状态不唯一 | `JoinVisitor.cs:145`、`:214` |
| `(无消息)` `DSyntaxErrorException` | 防御性断言：`DefaultIfEmpty` 遇到无法识别的方法调用 | `JoinVisitor.cs:342` |

---

## 七、字符串方法（`ByStringCallVisitor`）

| 报错信息 | 触发原因（禁令） | 位置 |
|---|---|---|
| `字符串的"{node.Method}"方法不被支持！` | 调用了框架未翻译的字符串方法 | `ByStringCallVisitor.cs:579` |
| `(无消息)` `NotSupportedException` | 防御性断言：`String.IndexOf` 等在不支持的参数组合下被调用 | `ByStringCallVisitor.cs:539` |
| `(无消息)` `ArgumentNullException`（参数 `value`） | 字符串方法参数为 null | `ByStringCallVisitor.cs:58` |

---

## 八、日期时间（`CoreVisitor` / `BaseVisitor.DateTime`）

| 报错信息 | 触发原因（禁令） | 位置 |
|---|---|---|
| `日期时间的"{node.Method.Name}"方法不被支持！` | 在 `DateTime` 上调用了当前引擎不支持的方法（MySQL / SqlServer / PostgreSQL / 其他引擎分支） | `CoreVisitor.cs:514`、`:547`、`:588`、`:597` |
| `不支持"{name}"日期片段计算!` | 取用了不支持的日期片段（超出 `Year`/`Month`/`Day`/`Hour`/`Minute`/`Second`/`Millisecond`/`DayOfWeek`/`DayOfYear`/`Ticks`/`TimeOfDay`/`Date`），各引擎分支均有 | `BaseVisitor.DateTime.cs:104`、`:244`、`:348`、`:527`、`:685`、`:807`、`:923` |
| `不支持数据库引擎"{Engine}"的日期片段计算!` | 当前引擎整体不支持日期片段计算 | `BaseVisitor.cs:477` |

---

## 九、方法调用通用限制（`CoreVisitor` / `CoalesceVisitor` / `ConditionalVisitor`）

| 报错信息 | 触发原因（禁令） | 位置 |
|---|---|---|
| `不支持将静态方法（{node.Method.Name}）结果直接作为表达式的一部分！请参考 {ReturnType.Name} {variable} = *[{DeclaringType.Name}.{node.Method.Name}(...args)]; 然后使用 {variable} 替换表达式 *.{node.Method.Name}(...args)！` | 不能把静态方法调用结果直接嵌入表达式，应先求值为变量再使用 | `CoreVisitor.cs:41`、`:72` |
| `不支持将方法（{node.Method.Name}）结果直接作为表达式的一部分！请参考 {ReturnType.Name} {variable} = *[{node.Method.Name}(...args)]; 然后使用 {variable} 替换表达式 *.{node.Method.Name}(...args)！` | 不能把实例方法调用结果直接嵌入表达式，应先求值为变量再使用 | `CoreVisitor.cs:103` |
| `(动态构造的错误消息)` `DSyntaxErrorException` | `Enumerable` 方法调用翻译失败时动态生成的语法错误 | `CoreVisitor.cs:424` |
| `(无消息)` `NotSupportedException` | 防御性断言：遇到无法识别的方法 / 在 `List<T>` 上调用不支持的方法 | `CoreVisitor.cs:172`、`:452` |
| `Node type must be Coalesce.` `InvalidOperationException` | `CoalesceVisitor` 接收到非 `Coalesce` 节点（内部断言） | `CoalesceVisitor.cs:27` |
| `(无消息)` `NotSupportedException` | 防御性断言：`Coalesce`（`??`）在不支持的引擎中被调用 | `CoalesceVisitor.cs:133` |
| `(无消息)` `InvalidOperationException` | `ConditionalVisitor` 接收到非 `Conditional` 节点（内部断言） | `ConditionalVisitor.cs:24` |

---

## 十、查询结果类型与默认值（`QueryVisitor`）

| 报错信息 | 触发原因（禁令） | 位置 |
|---|---|---|
| `默认值"null"无法转换为"{conversionType}"类型!` | 把 `null` 作为非可空值类型的默认值 | `QueryVisitor.cs:108` |
| `查询结果类型({conversionType})和指定的默认值类型({_defaultValue.GetType()})无法进行默认转换!` | `DefaultIfEmpty()` 默认值类型与查询结果类型不兼容 | `QueryVisitor.cs:127` |
| `函数"{name}"仅在表达式链以"Min"、"Max"、"Average"、"Last"、"First"、"Single"或"ElementAt"结尾时，可用！` | 错误消息函数仅能用于聚合 / 单元素终结操作 | `QueryVisitor.cs:150` |
| `函数"{name}"错误消息是字符串类型且不能为空！` | 错误消息参数为空 / null | `QueryVisitor.cs:155` |
| `函数"{name}"仅在表达式链最多只能出现一次！` | `DefaultIfEmpty()` 在链中重复出现 | `QueryVisitor.cs:164` |
| `函数"{name}"仅在表达式链以"FirstOrDefault"、"LastOrDefault"、"SingleOrDefault"或"ElementAtOrDefault"结尾时，可用！` | `DefaultIfEmpty()` 用于不兼容的终结操作 | `QueryVisitor.cs:169` |

---

## 十一、增删改命令翻译（`ExecutorVisitor`）

`Update` / `Insert` 表达式翻译。下列「请指定更新字段 / 字段重复 / 只读字段 / 非有效字段 / 版本处理」在 MySQL、SqlServer、PostgreSQL、Oracle 四个引擎分支与 INSERT 投影中重复出现，已合并并列出全部行号。

| 报错信息 | 触发原因（禁令） | 位置 |
|---|---|---|
| `当前数据库引擎{Engine}的方法"{node.Method.Name}"不被支持！` | 当前引擎不支持该增删改操作方法 | `ExecutorVisitor.cs:74` |
| `方法"{name}"不被支持！` | 回流分析遇到未知方法名 | `ExecutorVisitor.cs:123` |
| `请指定更新字段！` | `Update` 未指定任何要更新的字段 | `ExecutorVisitor.cs:350`、`:540`、`:733`、`:921` |
| `不支持"{tableInfo.Name}"表字段"{field}"版本"{version}"处理！` | 乐观锁版本字段类型不被支持 | `ExecutorVisitor.cs:419`、`:609`、`:798`、`:986` |
| `字段"{memberInfo.Name}"重复指定!` / `字段"{name}"重复指定!` | 同一字段被多次指定更新 / 插入 | `ExecutorVisitor.cs:455`、`:647`、`:834`、`:1049`、`:1252`、`:1288` |
| `"{memberInfo.Name}"是只读字段!` / `"{name}"是只读字段!` | 尝试写入只读字段 | `ExecutorVisitor.cs:460`、`:652`、`:839`、`:1054`、`:1245`、`:1281` |
| `"{memberInfo.Name}"不是有效的数据库字段!` / `"{name}"不是有效的数据库字段!` | 指定的属性不映射到任何数据库字段 | `ExecutorVisitor.cs:475`、`:665`、`:866`、`:1067`、`:1273`、`:1295` |
| `未指定插入字段！` | `Insert` 未指定任何字段 | `ExecutorVisitor.cs:1133` |
| `(无消息)` `NotSupportedException` | 防御性断言：`Startup` 接收非 `Call` 表达式 | `ExecutorVisitor.cs:87` |

---

## 十二、仓储命令构建（`RepositoryRouter.*`）

`IRepository<T>` 的插入 / 更新 / 删除命令。

| 报错信息 | 触发原因（禁令） | 位置 |
|---|---|---|
| `不支持表达式({columns})!` | 字段选择表达式节点类型不被支持 | `RepositoryRouter.cs:94` |
| `分区表"{_instance.Name}"的操作，必须指定分区键！` | 对分区（分片）表操作但未指定分区键 | `RepositoryRouter.cs:115`、`:153`、`:191` |
| `普通表"{_instance.Name}"不支持分区操作！` | 对普通表指定了分区键 | `RepositoryRouter.cs:118`、`:156`、`:194` |
| `不支持无主键表的更新操作！` | 对无主键表执行更新 | `RepositoryRouter.cs:146` |
| `不支持无主键表的删除操作！` | 对无主键表执行删除 | `RepositoryRouter.cs:184` |
| `列"{entry.ColumnName}"的类型"{entry.ColumnType}"不支持批处理！` | 列类型无法用于批量处理 | `RepositoryRouter.cs:378` |
| `未指定操作字段！` | 字段过滤后无可操作字段 | `RepositoryRouter.Command.cs:206` |
| `实体"{_elementType.Name}"不存在「单主键 + [DatabaseGenerated]」配置，无法启用自增主键回填。` | 启用自增回填但实体未配置单主键 + `[DatabaseGenerated]` | `RepositoryRouter.InsertCommand.cs:181` |
| `启用自增主键回填时，数据库引擎"{Engine}"不支持 Ignore 模式（仅支持 PostgreSQL / MySQL / SQLite）。` | 该引擎不支持 Ignore 模式下的自增回填 | `RepositoryRouter.InsertCommand.cs:197` |
| `启用自增主键回填时，数据库引擎"{Engine}"不支持（仅支持 PostgreSQL / SqlServer / MySQL / SQLite / DB2 / Sybase；Oracle 不支持批量自增主键回填）。` | 该引擎不支持自增主键回填 | `RepositoryRouter.InsertCommand.cs:209` |
| `自增主键回填：实体数（{_entities.Count}）与返回的 ID 数（{ids.Count}）不一致。` | 回填时返回 ID 数与实体数不匹配 | `RepositoryRouter.InsertCommand.cs:228` |
| `实体不能为空！` | 命令构建时实体为 null | `RepositoryRouter.InsertCommand.cs:289`、`:383`、`:548`、`:650`；`UpdateableCommand.cs:357`、`:452`；`DeleteableCommand.cs:72`、`:150`、`:406` |
| `未指定插入字段！` | 插入字段集合为空 | `RepositoryRouter.InsertCommand.cs:574` |
| `未指定更新字段！` | 更新字段集合为空 | `RepositoryRouter.UpdateableCommand.cs:411` |
| `数据库引擎"{Engine}"不支持批量更新！` | 该引擎不支持批量更新 / 删除 | `RepositoryRouter.UpdateableCommand.cs:168`、`DeleteableCommand.cs:295` |
| `自增主键反写：{engine} 单行 INSERT 未能读取到有效的自增 ID。` | 单行插入未读到有效自增 ID（≤0 或缺失） | `RepositoryRouter.Insertable.cs:280` |
| `(无消息)` `NotSupportedException` | 防御性断言：`VersionKind` 枚举值未处理 | `RepositoryRouter.UpdateableCommand.cs:269`、`:534` |
| `(无消息)` `NotImplementedException` | 防御性断言：常量值类型未处理 | `RepositoryRouter.cs:80` |
| `(无消息)` `ArgumentNullException` | `columns` / `entries` / `source` 入参为 null | `RepositoryRouter.cs:68`、`:108`、`:141`、`:179`；`Command.cs:62`、`:162`、`:184`；`Insertable.cs:54`、`:461`；`Updateable.cs:143`、`:164` |
| `(无消息)` `ArgumentOutOfRangeException` | `offset` / `count` 越界 | `RepositoryRouter.Command.cs:67` |

---

## 十三、实体与注解配置（`TableAnalyzer` / `DefaultTableAnalyzer` / `Annotations`）

| 报错信息 | 触发原因（禁令） | 位置 |
|---|---|---|
| `该表不支持分片!` | 对未声明分片的表调用 `Fragment()` | `TableAnalyzer.cs:83` |
| `请指定表名称！` | 实体配置未指定表名（等价于缺少 `[Table]`） | `TableAnalyzer.cs:268`、`:335` |
| `请声明字段！` | 实体未声明任何字段 | `TableAnalyzer.cs:273`、`:340` |
| `"{_tableType}"类型中，不包含该属性！` | `Field()` 指定的属性不属于目标类型 | `TableAnalyzer.cs:118` |
| `"{nameof(name)}"不能为 null 或空。` | `Field()` / `Table()` 的名称参数为空 | `TableAnalyzer.cs:113`、`:136`、`:166`、`:198` |
| `'{nameof(shardingKey)}' cannot be null or empty.` | 分片键为 null 或空 | `TableAnalyzer.cs:72` |
| `不支持 {propertyType} 类型的属性 {propertyInfo.Name} 进行版本控制！` | `[Version]` 标注在不支持的数据类型上 | `DefaultTableAnalyzer.cs:93` |
| `参数 {nameof(name)} 不能为 null 或空白。` | `[Table]` 表名为 null / 空白 | `Annotations/TableAttribute.cs:20` |
| `参数 {nameof(name)} 不能以空白字符开头或结尾。` | `[Table]` 表名含首尾空白 | `Annotations/TableAttribute.cs:25` |
| `"{nameof(name)}"不能为 null 或空白。` | `[Field]` 字段名为 null / 空白 | `Annotations/FieldAttribute.cs:19` |
| `"{nameof(name)}"不能以空白字符开头或结尾。` | `[Field]` 字段名含首尾空白 | `Annotations/FieldAttribute.cs:24` |
| `(无消息)` `NotSupportedException` | 防御性断言：表分析结果为 null / 不支持的字段表达式 | `TableAnalyzer.cs:190`、`:264`、`:322` |
| `(无消息)` `ArgumentNullException` | `propertyInfo` / `tableType` / `config` / `memberCol` 入参为 null | `TableAnalyzer.cs:108`、`:161`、`:257`、`:415`、`:438`、`:443`；`DefaultTableAnalyzer.cs:19` |

---

## 十四、SQL 写入器（`SqlWriter`）

| 报错信息 | 触发原因（禁令） | 位置 |
|---|---|---|
| `未处理的关键字: {keyword}` `NotImplementedException` | `SqlKeyword` 枚举值未被处理 | `SqlWriter.cs:184` |
| `Take 参数不能为负数!` | `Take(n)` 传入负数 | `SqlWriter.cs:642` |
| `参数不能小于0。` | `Skip(n)` 传入负数 | `SqlWriter.cs:672` |
| `"{nameof(value)}"不能为 null 或空。` | 写入 null / 空字符串 | `SqlWriter.cs:957` |
| `(无消息)` `IndexOutOfRangeException` | `ElementAt` 等索引为负 / 越界 | `SqlWriter.cs:647`、`:654`、`:686`、`:691` |
| `(无消息)` `NotSupportedException` | 防御性断言：`SqlOperator` 枚举值未处理 | `SqlWriter.cs:630` |
| `(无消息)` `ArgumentNullException` | 构造函数 `settings` 为 null | `SqlWriter.cs:416` |

---

## 十五、数据库执行与结果消费（`Database` / `DatabaseExecutor` / `DatabaseExecutor.Async`）

| 报错信息 | 触发原因 | 位置 |
|---|---|---|
| `The input sequence contains no elements.` `NoElementException` | 单行查询（`First`/`Single` 等）结果为空 | `DatabaseExecutor.cs:169`、`:617`；`DatabaseExecutor.Async.cs:168` |
| `(自定义文案 commandSql.NoElementError)` `NoElementException` | 查询为空且指定了自定义空结果错误消息 | `DatabaseExecutor.cs:166`；`DatabaseExecutor.Async.cs:165` |
| `The input sequence contains more than one element.` `MultipleRowsException` | `Single` 类查询返回多于一行 | `DatabaseExecutor.cs:609` |
| `Query results must be consumed in the correct order, and each result can only be consumed once` | 多结果集被乱序或重复消费 | `DatabaseExecutor.cs:643`、`:679`；`DatabaseExecutor.Async.cs:631`、`:668` |
| `请通过"DataTable.TableName"指定目标表名称！` | 批量操作的 `DataTable.TableName` 未指定 | `DatabaseExecutor.cs:363`、`:479`、`:572`；`DatabaseExecutor.Async.cs:308`、`:475`、`:572` |
| `不支持集合参数！` | 字面（非参数化）模式下传入集合参数 | `Database.cs:485` |
| `参数值不可迭代！` | `IN` 参数值无法迭代 | `Database.cs:368` |
| `(无消息)` `KeyNotFoundException` | SQL 中 `{=name}` 参数在字典中不存在 | `Database.cs:323` |
| `(无消息)` `InvalidOperationException` | 防御性断言：参数处理分支异常 | `Database.cs:578` |
| `(无消息)` `ArgumentNullException` | `multipleAction` / `dataTable` / `dt` 入参为 null | `DatabaseExecutor.cs:335`、`:358`、`:474`、`:567`；`DatabaseExecutor.Async.cs:267`、`:303`、`:470`、`:567` |

---

## 十六、依赖注入与连接配置（`DatabaseFactory` / `DatabaseLinqBuilder` / `DbConnectionPipeline` / `InkslabLinqServiceCollectionExtensions` / `DbStrictAdapter` / `DefaultConnections` / `CommandSql`）

| 报错信息 | 触发原因 | 位置 |
|---|---|---|
| `数据库引擎"{engine}"未注册，请先调用对应的 Use{engine}() 扩展方法。` | 使用了未注册的数据库引擎 | `DatabaseFactory.cs:50` |
| `未找到匹配的数据库引擎：{databaseStrings.Engine}, 请检查连接字符串或工厂配置。` `KeyNotFoundException` | 连接字符串 / 工厂配置中无对应引擎 | `DefaultConnections.cs:33` |
| `当前方法"{nameof(UseLinq)}"已注册，请勿重复注册！` | `UseLinq` 重复注册 | `DatabaseLinqBuilder.cs:85` |
| `一个类型只能用于一个数据库连接，数据库连接（{serviceType.Name}）不能重复注册！` | 同一连接类型被重复注册 | `DatabaseLinqBuilder.cs:114`、`:144` |
| `MappingCapacity 必须大于 0。` | `MappingCapacity` 配置 ≤ 0 | `DatabaseLinqBuilder.cs:58` |
| `"{nameof(connectionStrings)}"不能为 null 或空。` / `"{nameof(connectionString)}"不能为 null 或空。` | 连接字符串为空 | `DatabaseLinqBuilder.cs:78`；`DatabaseFactory.cs:45`；`InkslabLinqServiceCollectionExtensions.cs:35` |
| `数据库链接无效!` `ArgumentException` | `databaseStrings` 为 null | `DbConnectionPipeline.cs:41` |
| `未找到合适的批量复制工厂。` | 当前引擎无匹配的批量复制工厂 | `DbConnectionPipeline.cs:78`、`:96` |
| `"{nameof(text)}"不能是 Null 或为空。` | `CommandSql` 的 SQL 文本为空 | `CommandSql.cs:29` |
| `(无消息)` `AbandonedMutexException` | 数据库连接创建失败 | `InkslabLinqServiceCollectionExtensions.cs:58` |
| `(无消息)` `ArgumentNullException` | `configure` / `connectionStrings` / `factory` / `services` / `connection` / `transaction` / `executor` / `adapter.Settings` / `adapter.Visitors` 等入参为 null | `DatabaseLinqBuilder.cs:49`、`:137`；`DatabaseFactory.cs:27`、`:31`；`DbConnectionPipeline.cs:75`、`:93`、`:199`、`:200`、`:313`、`:436`、`:465`；`InkslabLinqServiceCollectionExtensions.cs:53`、`:82`、`:122`；`DbStrictAdapter.cs:20`、`:21` |

---

## 十七、查询入口与扩展方法（`QueryProvider` / `Queryable` / `QueryableAsync` / `QueryableMethods` / `Extensions` / `Conditions` / `Ranks`）

| 报错信息 | 触发原因（禁令） | 位置 |
|---|---|---|
| `只能在条件表达式（如：where、on等）中使用！` `NotImplementedException` | `Conditions` 的占位方法在条件表达式之外被直接调用 | `Conditions.cs:18`、`:26`、`:36`、`:45`、`:56` |
| `只能在排序表达式中使用！` `NotImplementedException` | `Ranks` 的占位方法在排序表达式之外被直接调用 | `Ranks.cs:23` |
| `无效表达式!` `ArgumentException` | 传入的表达式不是有效的 `IQueryable` 类型表达式 | `QueryProvider.cs:76`；`LinqAnalyzer.cs:203` |
| `不支持的表达式类型：{node.NodeType}` | 表达式扩展遇到不支持的节点类型 | `Extensions/ExpressionExtensions.cs:249` |
| `页码不能小于"1"。` | 分页页码 < 1 | `Extensions/QueryableExtentions.cs:136`；`QueryableAsync.cs:1969` |
| `每页条目数不能小于"1"。` | 分页每页条目数 < 1 | `Extensions/QueryableExtentions.cs:141`；`QueryableAsync.cs:1974` |
| `"{nameof(errMsg)}"不能为 null 或空。` | 自定义错误消息为空 | `Extensions/QueryableExtentions.cs:115` |
| `'{nameof(shardingKey)}' cannot be null or empty.` | 分片键为空 | `Extensions/QueryableExtentions.cs:29`；`Repository.cs:152` |
| `(无消息)` `ArgumentOutOfRangeException` | `commandTimeout` 超出范围 | `Extensions/QueryableExtentions.cs:83` |
| `(无消息)` `NotSupportedException` | 防御性断言：表达式扩展遇到不支持的 lambda / 类型 | `Extensions/ExpressionExtensions.cs:40`、`:136`、`:162` |
| `(无消息)` `NotImplementedException` | 占位 / 未实现的扩展方法被调用 | `Extensions/QueryableExtentions.cs:177`、`:193`、`:215`、`:231`、`:253`；`Repository.cs:511`、`:516`、`:576` |
| `(无消息)` `ArgumentNullException` | `source` / `expression` / `predicate` / `keySelector` / `elementSelector` / `action` / `executor` / `node` / 各实体 / `querable` / `updateSet(ter)` 等入参为 null | `QueryProvider.cs:32`、`:71`、`:103`；`Queryable.cs:47`、`:110`；`QueryableAsync.cs:1915`、`:1964`、`:2182`、`:2187`、`:2192`、`:2267`、`:2295`、`:2300`、`:2340`；`QueryableMethods.cs:318`、`:333`、`:349`、`:364`、`:380`、`:395`、`:410`、`:425`；`Extensions/QueryableExtentions.cs:24`、`:51`、`:61`、`:78`、`:110`、`:169`、`:174`、`:190`、`:207`、`:212`、`:228`、`:245`、`:250`；`Repository.cs:168`、`:185`、`:202`、`:237`、`:258`、`:326`、`:347`、`:364`、`:375`、`:393`、`:404`、`:415`、`:429`、`:440`、`:451`、`:465` |
| `(无消息)` `InvalidOperationException` | 防御性断言：异步执行内部状态异常 | `QueryableAsync.cs:2344`、`:2381` |

---

## 十八、SQL 分析器（`LinqAnalyzer`）— 单元测试用

| 报错信息 | 触发原因 | 位置 |
|---|---|---|
| `当前 IQueryable 并非由 LinqAnalyzer 创建，无法分析 SQL。` | 把非 `LinqAnalyzer` 创建的 `IQueryable` 传入分析器 | `LinqAnalyzer.cs:170` |
| `无效表达式!` | `CreateQuery` 的表达式不是有效的 `IQueryable` 表达式 | `LinqAnalyzer.cs:203` |
| `(无消息)` `ArgumentNullException` | `adapter` / `source` / `query` / `expression` 入参为 null | `LinqAnalyzer.cs:44`、`:60`、`:82`、`:116`、`:121`、`:199`、`:214`、`:270` |

---

## 十九、自定义格式化（`AdapterFormatter`）

| 报错信息 | 触发原因 | 位置 |
|---|---|---|
| `仅支持类型System.Text.RegularExpressions.Match、System.Text.RegularExpressions.Group、System.String、System.Boolean类型的映射。` | 自定义 Formatter 方法含不支持的参数类型 | `AdapterFormatter.cs:103` |
| `(无消息)` `NotSupportedException` | 无法解析匹配且 `UnsolvedThrowError` 为 true | `AdapterFormatter.cs:216` |
| `(无消息)` `ArgumentNullException` | 构造函数 `regex` 为 null | `AdapterFormatter.cs:187` |

---

## 二十、事务（`Inkslab.Transactions`）

| 报错信息 | 触发原因（禁令） | 位置 |
|---|---|---|
| `事务已完成，无法注册新的交付。` | 事务完成后再 `RegisterDelivery` | `Transaction.cs:121` |
| `事务已完成，无法签署新的子事务。` | 事务完成后再 `EnlistTransaction` | `Transaction.cs:168` |
| `一个或多个交付执行失败。` `AggregateException` | `CommitAsync` 时多个交付执行抛错 | `Transaction.cs:244` |
| `回滚过程中发生一个或多个错误。` `AggregateException` | 回滚（异步 / 同步）时多个事务抛错 | `Transaction.cs:300`、`:347` |
| `事务释放过程中发生一个或多个错误。` `AggregateException` | `Dispose`/`DisposeAsync` 时多个事务抛错 | `Transaction.cs:408`、`:474` |
| `(无消息)` `InvalidOperationException` | 对已完成事务执行 `Commit`/`Rollback` 等操作 | `Transaction.cs:192`、`:269`、`:316`；`TransactionUnit.cs:86` |
| `(无消息)` `ObjectDisposedException` | 操作已释放的事务对象 | `Transaction.cs:116`、`:163`、`:187`、`:264`、`:311`；`TransactionUnit.cs:81` |
| `(无消息)` `ArgumentOutOfRangeException` | `isolationLevel` / `transactionOption` 不是有效枚举值 | `Transaction.cs:34`；`TransactionUnit.cs:48`、`:53` |
| `(无消息)` `ArgumentNullException` | `delivery` / `transaction` 入参为 null | `Transaction.cs:88`、`:111`、`:158`；`TransactionEventArgs.cs:16` |

---

## 二十一、批量复制（数据库适配器）

> MySql / SqlServer 适配器目录内无自定义 `throw`，相关校验由 `DatabaseExecutor` 统一处理；PostgreSQL 适配器有独立的批量复制实现。

| 报错信息 | 触发原因 | 位置 |
|---|---|---|
| `PostgreSQL批量复制失败: {ex.Message}` `InvalidOperationException` | `COPY` / 批量 INSERT 执行过程中底层抛错被包装 | `PostgreSQLBulkAssistant.cs:83`、`:129`、`:441`、`:483` |
| `请通过"DataTable.TableName"指定目标表名称！` `ArgumentException` | 批量复制的 `DataTable.TableName` 未指定 | `PostgreSQLBulkAssistant.cs:147` |
| `类型 {dataType.Name} 不是简单类型` `ArgumentException` | 批量复制字段值不是受支持的简单类型 | `PostgreSQLBulkAssistant.cs:611` |
| `(无消息)` `ArgumentNullException` | `dt` 入参为 null | `PostgreSQLBulkAssistant.cs:142` |
