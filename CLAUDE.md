# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run a specific test project
dotnet test tests/Inkslab.Linq.Tests/
dotnet test tests/SqlServer.Tests/
dotnet test tests/PostgreSQL.Tests/
dotnet test tests/Combination.Tests/

# Run a single test by name
dotnet test --filter "FullyQualifiedName~TestClassName.MethodName"

# Pack NuGet packages (outputs to .nupkgs/)
.\build.ps1
```

## Architecture

**Inkslab.Linq** is a LINQ-to-SQL ORM framework targeting .NET 6.0 and .NET Standard 2.1, with C# 9.0 (`strict` mode). It translates LINQ expression trees into database-specific SQL at runtime.

### Project layout

```
src/
  Inkslab.Linq/          — Core: expression tree translation engine, interfaces, DI wiring
  Inkslab.Linq.MySql/    — MySQL adapter (MySqlConnector)
  Inkslab.Linq.SqlServer/— SQL Server adapter (Microsoft.Data.SqlClient)
  Inkslab.Linq.PostgreSQL/— PostgreSQL adapter (Npgsql)
  Inkslab.Transactions/  — AsyncLocal-based transaction scope (TransactionUnit)
tests/
  Inkslab.Linq.Tests/    — Core tests against MySQL
  SqlServer.Tests/       — SQL Server-specific tests
  PostgreSQL.Tests/      — PostgreSQL-specific tests
  Combination.Tests/     — Multi-database composition tests
```

### Core translation pipeline

```
IQueryable<T>  →  QueryProvider  →  LINQ Expression Tree  →  QueryVisitor/SelectVisitor
                                                                    ↓
                                                              SqlWriter (SQL text)
                                                                    ↓
IRepository<T> →  RepositoryExecutor →  IDbAdapter  →  IDbConnectionPipeline  →  DbConnection
IDatabase      →  DatabaseExecutor   ↗
```

Key types:

| Type | Role |
|------|------|
| `IDbAdapter` | Per-database-engine: holds `IDbCorrectSettings` (quoting, pagination) and `IMethodVisitor` map (LINQ method → SQL function) |
| `IDbCorrectSettings` | Abstracts name quoting (`Name()`), parameter prefix (`ParamterName()`), and `LIMIT`/`OFFSET` syntax (`ToSQL()`) |
| `QueryVisitor` / `SelectVisitor` | Walk expression trees and write SQL via `SqlWriter` |
| `DbStrictAdapter` | Wraps `IDbAdapter`; used as constructor arg to visitor hierarchy |
| `DatabaseLinqBuilder` | Fluent DI builder returned by `UseMySql()` / `UseSqlServer()` / `UsePostgreSQL()` |
| `SerializableScope` | AsyncLocal connection-reuse scope (reduces connection churn in loops) |
| `TransactionUnit` | AsyncLocal transaction scope; must call `CompleteAsync()` to commit |
| `LinqAnalyzer` | Translates LINQ to `CommandSql` without a real connection—used in unit tests |
| `LookupDb` | Provides `JsonDbType` / `JsonbDbType` constants and ANSI string helpers for parameter binding |

### Adding a new database adapter

1. Implement `IDbAdapter` → provide `IDbCorrectSettings` + `IMethodVisitor` registry.
2. Register via an extension method that calls `services.AddDbAdapter<TAdapter>(engine).UseLinq(connStr)`.
3. Override `IDbConnectionFactory` to return the driver's `DbConnection`.

### Entity annotations

```csharp
[Table("table_name")]        // required
[Field("col_name")]          // maps property to column
[Key]                        // primary key (used by Update/Delete)
[DatabaseGenerated]          // skip on INSERT
[Version]                    // optimistic-lock; auto-increments based on type
```

Sharding tables use `[Table("user_[sharding]")]` and are resolved at query time via `.DataSharding(key)`.

### Test framework

Tests use **xunitPlus** (constructor-injected DI). Every test project contains a `Startup : XunitPlus.Startup` that registers the database adapter and connection string. The test runner builds the DI container and injects `IQueryable<T>`, `IRepository<T>`, `IDatabase`, etc. directly into test constructors.

Tests that require a real database (`RepositoryTests`, `TransactionTests`, etc.) need a live connection string in `Startup.cs`. Tests that only translate LINQ to SQL (`LinqAnalyzerTests`, `CommandSqlTests`, `ConditionsTests`, etc.) use `LinqAnalyzer` and need no database.
