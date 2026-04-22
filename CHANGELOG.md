# 更新日志

本文件记录 Inkslab.Linq 各版本的主要变更。

---

## v1.2.75 (当前版本)

> 版本号参见 [Directory.Build.props](Directory.Build.props)

---

## v1.2.57

### 新增

- **SerializableScope 串行化范围**：相同连接字符串在作用域内复用同一连接实例，减少连接创建/销毁开销，基于 `AsyncLocal` 实现线程安全
- **ToString() 方法支持**：LINQ 查询中支持 `ToString()` 字符串转换
- **MySQL UTF8MB4**：连接字符串完整支持 `utf8mb4` 字符集
- **空节点处理增强**：优化条件判断和节点处理的健壮性
- **IsPlainVariable 默认参数**：简化调用方式

### 变更

- **PostgreSQL 批量操作增强**：
  - `WriteValueAsync` 支持 `JsonPayload` 和 `JsonbPayload`
  - 增强 `TypeCode` → PostgreSQL 类型映射（`UInt64` → `Numeric`，`Byte`/`SByte` → `Smallint`，`UInt16` → `Integer`）
  - 完善标识符转义，支持表名/列名中的特殊字符
- **事务连接管理**：`TransactionLink` 代理自动开启已关闭的连接
- **JSON/JSONB**：移除 PostgreSQL ENUM 强约束，完整支持 JSON 文档/对象/数组的插入和更新

---

## 版本策略

所有 NuGet 包使用统一版本号，由 `Directory.Build.props` 中的 `<Version>` 控制：

- `Inkslab.Linq`
- `Inkslab.Linq.MySql`
- `Inkslab.Linq.SqlServer`
- `Inkslab.Linq.PostgreSQL`
- `Inkslab.Transactions`
