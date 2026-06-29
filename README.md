# Inkslab.Linq

高性能 .NET LINQ-to-SQL ORM 框架，支持 MySQL、SQL Server、PostgreSQL 多数据库引擎，提供类型安全的查询构建、仓储模式、事务管理和批量操作能力。

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Version](https://img.shields.io/badge/version-1.2.78-green.svg)](Directory.Build.props)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%20Standard%202.1-purple.svg)](Directory.Build.props)
[![GitHub Issues](https://img.shields.io/github/issues-raw/tinylit/inkslab.linq)](../../issues)

---

## 目录

- [快速开始](#快速开始)
- [项目架构](#项目架构)
- [📚 模块文档](#-模块文档)
- [许可证](#许可证)

---

## 快速开始

### 安装 NuGet 包

```bash
# 核心库（必选）
dotnet add package Inkslab.Linq

# 数据库适配器（选其一或多个）
dotnet add package Inkslab.Linq.MySql
dotnet add package Inkslab.Linq.SqlServer
dotnet add package Inkslab.Linq.PostgreSQL

# 事务管理（可选）
dotnet add package Inkslab.Transactions
```

### 注册服务

```csharp
using Microsoft.Extensions.DependencyInjection;

// MySQL
services.UseMySql()
    .UseLinq("server=localhost;uid=root;pwd=password;database=mydb;Charset=utf8mb4;");

// SQL Server
services.UseSqlServer()
    .UseLinq("Server=localhost;Database=MyDB;Trusted_Connection=true;");

// PostgreSQL
services.UsePostgreSQL()
    .UseLinq("Host=localhost;Database=mydb;Username=postgres;Password=password;");
```

注册完成后，通过依赖注入获取以下服务：

| 服务类型 | 说明 |
|----------|------|
| `IQueryable<TEntity>` | LINQ 查询入口 |
| `IRepository<TEntity>` | 仓储操作（增删改） |
| `IDatabase` | 原生 SQL 执行 |
| `IDatabaseFactory` | 动态创建数据库实例 |

---

## 项目架构

### 解决方案结构

```
inkslab.linq.sln
├── src/
│   ├── Inkslab.Linq/                # 核心抽象层与 LINQ-to-SQL 翻译引擎
│   ├── Inkslab.Linq.MySql/          # MySQL 适配器（MySqlConnector）
│   ├── Inkslab.Linq.SqlServer/      # SQL Server 适配器（Microsoft.Data.SqlClient）
│   ├── Inkslab.Linq.PostgreSQL/     # PostgreSQL 适配器（Npgsql）
│   └── Inkslab.Transactions/        # 事务管理（TransactionUnit）
└── tests/
    ├── Inkslab.Linq.Tests/          # 核心测试（MySQL）
    ├── SqlServer.Tests/             # SQL Server 测试
    ├── PostgreSQL.Tests/            # PostgreSQL 测试
    └── Combination.Tests/           # 多数据库组合测试
```

### 核心组件关系

```
IQueryable<T> ──→ QueryProvider ──→ LINQ Expression Tree ──→ SQL
                                                              │
IRepository<T> ──→ RepositoryExecutor ──→ IDbAdapter ─────────┤
                                              │               │
IDatabase<T> ──→ DatabaseExecutor ────────────┘               │
                       │                                      ▼
                 IDbConnectionPipeline ──→ DbConnection ──→ 数据库
```

### 数据库引擎枚举

```csharp
public enum DatabaseEngine
{
    SQLite = 1, MySQL = 2, SqlServer = 3, PostgreSQL = 4,
    Oracle = 5, DB2 = 6, Sybase = 7
}
```

---

## 📚 模块文档

完整使用文档按模块拆分如下，按需查阅：

| 模块 | 内容 |
|------|------|
| [实体与映射](README.实体与映射.md) | 实体注解、`[Version]` 规则、列值类型映射、分片表定义、Fluent API |
| [LINQ 查询](README.LINQ查询.md) | 基础/Join/子查询/分组/Union、异步方法、字符串与日期函数、IQueryable 扩展 |
| [仓储操作](README.仓储操作.md) | 插入与 PopulateIdentity 反写、更新、删除、仓储方法链 |
| [动态条件与排序](README.动态条件与排序.md) | Conditions、WhereIf、Expression 组合、Ranks 动态排序 |
| [原生 SQL 与 IDatabase](README.原生SQL与IDatabase.md) | 查询/执行/多结果集/存储过程/ExecuteMultiple/DynamicParameter |
| [事务与连接复用](README.事务与连接复用.md) | TransactionUnit 事务、SerializableScope、批量操作 |
| [高级特性](README.高级特性.md) | JSON/JSONB、分片表、多数据库配置、LinqAnalyzer |
| [LINQ 编写规则与禁令清单](README.LINQ编写规则.md) | LINQ → SQL 翻译的严格校验规则（AI 编码依据） |
| [API 速查表](README.API速查表.md) | 接口继承链、IDatabase 方法、CommandSql、异常体系、测试与构建 |

> 完整异常清单（含源码位置与原文）见 [`docs/LINQ与命令翻译报错规则.md`](docs/LINQ与命令翻译报错规则.md)。

---

## 许可证

[MIT License](LICENSE) - Copyright (c) 2023 Yuanli He
