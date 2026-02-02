# Inkslab.Linq

一个高性能的 .NET LINQ 扩展库，提供强大的数据库查询能力和事务管理功能。

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Version](https://img.shields.io/badge/version-1.2.55-green.svg)](.nupkgs/)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%20Standard%202.1-purple.svg)](Directory.Build.props)
[![GitHub](https://img.shields.io/github/license/tinylit/inkslab.linq.svg)](LICENSE)
[![GitHub issues](https://img.shields.io/github/issues-raw/tinylit/inkslab.linq)](../../issues)

## � 最近更新 (v1.2.57)

### ✨ 新增功能

#### 1. **SerializableScope 串行化范围支持** 🔥
- **核心功能**：在串行化范围内，相同连接字符串永远使用同一个数据库连接实例
- **性能优化**：减少连接创建和销毁开销，显著提升批量操作性能
- **事务兼容**：与 `TransactionUnit` 完美配合，支持嵌套使用
- **资源管理**：自动管理连接生命周期，支持同步和异步释放
- **线程安全**：基于 `AsyncLocal` 实现，支持异步上下文传递

**使用场景**：
- 批量数据操作（批量插入、更新、删除）
- 需要复用数据库连接的高频操作
- 与事务单元配合使用，确保连接一致性
- 减少连接池压力，提升应用性能

#### 2. 空节点处理逻辑增强
- 添加了对空节点（null nodes）的处理逻辑，提升代码健壮性
- 优化了条件判断和节点处理流程

#### 3. IsPlainVariable 方法优化
- 为 `IsPlainVariable` 方法添加了默认参数值
- 简化了调用方式，提高开发效率

#### 4. 字符串处理与条件判断优化
- 优化了字符串处理和条件判断逻辑
- 简化了代码复杂度，增强代码可读性

#### 5. MySQL 连接字符串 UTF8MB4 支持
- 调整 MySQL 连接字符串以完全支持 `utf8mb4` 字符集
- 移除不必要的代码，优化连接性能
- **注意**：如果使用中文或其他多字节字符，请确保数据库和表使用 `utf8mb4` 字符集

#### 6. ToString() 方法支持
- 新增 `ToString()` 方法支持，改进 LINQ 查询中的字符串转换
- 支持对象字段直接转换为字符串表示

### 🔧 重要变更

#### PostgreSQL 批量操作增强
- **异步写入方法完善**：`WriteValueAsync` 方法新增对 `JsonPayload` 和 `JsonbPayload` 类型的完整支持
- **类型映射改进**：增强了 `TypeCode` 到 PostgreSQL 数据类型的映射规则
  - `UInt64` → `Numeric` 的转换需注意精度（需进行 decimal 显式转换）
  - `Byte`/`SByte` → `Smallint` 的转换
  - `UInt16` → `Integer` 的转换
- **标识符转义**：完善了特殊字符处理，支持表名和列名中的特殊字符自动转义

#### 事务连接管理
- `TransactionLink` 代理类支持更完善的事务处理
- 自动开启已关闭的连接以确保事务可用
- 批量复制在事务上下文中正确执行

#### JSON/JSONB 数据类型
- 完整支持 `JsonPayload` 和 `JsonbPayload` 在批量操作中的使用
- 在 LINQ 查询中支持 JSON 文档、JSON 对象、JSON 数组的插入和更新
- 移除了 `PostgreSQL ENUM` 的强约束，提供更灵活的数据存储方式

---

## �🚀 快速开始

### 安装

通过 NuGet 包管理器安装：

```bash
# 核心库
dotnet add package Inkslab.Linq

# SQL Server 支持
dotnet add package Inkslab.Linq.SqlServer

# MySQL 支持  
dotnet add package Inkslab.Linq.MySql

# PostgreSQL 支持
dotnet add package Inkslab.Linq.PostgreSQL

# 事务管理
dotnet add package Inkslab.Transactions
```

或通过 Package Manager Console：

```powershell
# 核心库
Install-Package Inkslab.Linq

# SQL Server 支持
Install-Package Inkslab.Linq.SqlServer

# MySQL 支持
Install-Package Inkslab.Linq.MySql

# PostgreSQL 支持
Install-Package Inkslab.Linq.PostgreSQL

# 事务管理
Install-Package Inkslab.Transactions
```

### 基础配置

在 `Startup.cs` 或 `Program.cs` 中配置服务：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// MySQL 配置
services.UseMySql()
    .UseLinq("server=localhost;uid=root;pwd=password;database=mydb;");

// 或 SQL Server 配置
services.UseSqlServer()
    .UseLinq("Server=localhost;Database=MyDB;Trusted_Connection=true;");

// 或 PostgreSQL 配置（支持 JSON/JSONB 和批量操作）
services.UsePostgreSQL()
    .UseLinq("Host=localhost;Database=mydb;Username=postgres;Password=password;");

// 添加日志（可选）
services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
```

### 实体定义

```csharp
using System.ComponentModel.DataAnnotations;
using Inkslab.Linq.Annotations;

[Table("users")]
public class User
{
    [Key]
    [Field("id")]
    [DatabaseGenerated]
    public long Id { get; set; }

    [Field("name")]
    [StringLength(50)]
    public string Name { get; set; }

    [Field("email")]
    [StringLength(100)]
    public string Email { get; set; }

    [Field("is_active")]
    public bool IsActive { get; set; }

    [Field("created_at")]
    public DateTime CreatedAt { get; set; }
}
```

## 📖 核心功能

### 1. 仓储模式 (Repository Pattern)

```csharp
using Inkslab.Linq;

public class UserEx
{
    public long Id { get; set; }
    public int RoleType { get; set; }
}

public class UserService
{
    private readonly IRepository<User> _userRepository;
    private readonly IQueryable<User> _users;
    private readonly IQueryable<UserEx> _userExes;

    public UserService(
        IRepository<User> userRepository, 
        IQueryable<User> users,
        IQueryable<UserEx> userExes)
    {
        _userRepository = userRepository;
        _users = users;
        _userExes = userExes;
    }

    // 查询用户
    public async Task<List<User>> GetActiveUsersAsync()
    {
        return await _users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    // 更新用户 - 全表更新
    public async Task UpdateAllUsersAsync()
    {
        await _userRepository.UpdateAsync(x => new User
        {
            CreatedAt = DateTime.Now
        });
    }

    // 更新用户 - 条件更新
    public async Task UpdateUserAsync(long userId, string name)
    {
        bool? nullable = true;
        await _userRepository
            .Timeout(500)
            .Where(x => x.Id == userId)
            .UpdateAsync(x => new User
            {
                Name = name,
                CreatedAt = DateTime.Now,
                IsActive = nullable ?? false
            });
    }

    // 批量插入
    public async Task<int> BatchInsertAsync(List<User> users)
    {
        return await _userRepository.Ignore().Into(users).ExecuteAsync();
    }

    // 使用 LINQ 查询结果插入
    public async Task InsertFromQueryAsync()
    {
        var linq = from x in _users
                   join y in _userExes on x.Id equals y.Id
                   orderby x.Id descending
                   select new User { Name = x.Name, CreatedAt = DateTime.Now };

        await _userRepository.Timeout(10).Ignore().InsertAsync(linq);
    }

    // 条件删除
    public async Task DeleteUserAsync(long userId)
    {
        await _userRepository.DeleteAsync(x => x.Id == userId);
    }

    // 使用子查询删除
    public async Task DeleteUsersWithSubQueryAsync()
    {
        await _userRepository
            .Where(x => _userExes.Where(y => y.RoleType == 2).Select(y => y.Id).Contains(x.Id))
            .DeleteAsync();
    }
}
```

### 2. 事务管理

使用 `TransactionUnit` 进行事务控制：

```csharp
using Inkslab.Transactions;

// 自动回滚事务
public async Task ProcessDataAsync()
{
    await using (var transaction = new TransactionUnit())
    {
        await _userRepository.UpdateAsync(x => new User
        {
            CreatedAt = DateTime.Now
        });
        
        // 发生异常时事务自动回滚
        // 正常执行完毕也会回滚（未调用 CompleteAsync）
    }
}

// 手动提交事务
public async Task SaveDataAsync()
{
    await using (var transaction = new TransactionUnit())
    {
        await _userRepository.UpdateAsync(x => new User
        {
            CreatedAt = DateTime.Now
        });
        
        // 显式提交事务
        await transaction.CompleteAsync();
    }
}

// 原生 SQL 事务支持
public async Task ExecuteSqlAsync()
{
    await using (var transaction = new TransactionUnit())
    {
        string sql = "SELECT * FROM users WHERE id = @id";
        var user = await _database.FirstOrDefaultAsync<User>(sql, new { id = 1 });
        
        await transaction.CompleteAsync();
    }
}
```

### 3. 串行化范围 (SerializableScope)

`SerializableScope` 提供了连接复用机制，在同一范围内，相同连接字符串将使用同一个数据库连接实例，显著提升批量操作性能。

#### 核心特性

- **连接复用**：相同连接字符串自动复用数据库连接
- **自动管理**：连接生命周期由 Scope 自动管理
- **事务兼容**：与 `TransactionUnit` 完美配合
- **嵌套支持**：支持 Scope 嵌套，内外层共享连接
- **线程安全**：基于 `AsyncLocal` 实现，支持异步上下文

#### 基本用法

```csharp
using Inkslab.Linq;

// 基本用法：在范围内复用连接
public async Task BasicSerializableScopeAsync()
{
    await using (var scope = new SerializableScope())
    {
        // 第一次查询 - 创建新连接
        var user1 = await _users.FirstOrDefaultAsync();
        
        // 第二次查询 - 复用同一连接
        var user2 = await _users.Where(x => x.Id > 100).FirstOrDefaultAsync();
        
        // 第三次查询 - 仍然复用同一连接
        var count = await _users.CountAsync();
        
        // 批量操作 - 所有操作使用同一连接，性能更优
        for (int i = 0; i < 100; i++)
        {
            await _userRepository.UpdateAsync(x => new User { CreatedAt = DateTime.Now });
        }
    } // Scope 结束，连接自动释放
}

// 批量插入场景
public async Task BatchInsertWithScopeAsync()
{
    var users = Enumerable.Range(1, 1000)
        .Select(i => new User
        {
            Name = $"User_{i}",
            Email = $"user{i}@example.com",
            CreatedAt = DateTime.Now
        })
        .ToList();

    await using (var scope = new SerializableScope())
    {
        // 批量插入，复用连接，性能提升显著
        foreach (var user in users)
        {
            await _userRepository.Into(user).ExecuteAsync();
        }
        
        // 验证插入结果
        var insertedCount = await _users.CountAsync();
        Console.WriteLine($"已插入 {insertedCount} 条记录");
    }
}

// 嵌套 Scope - 内外层共享连接
public async Task NestedSerializableScopeAsync()
{
    await using (var outerScope = new SerializableScope())
    {
        var user1 = await _users.FirstOrDefaultAsync();
        
        await using (var innerScope = new SerializableScope())
        {
            // 内层 Scope 复用外层 Scope 的连接
            var user2 = await _users.Where(x => x.Id > 0).FirstOrDefaultAsync();
            var count = await _users.CountAsync();
        }
        
        // 外层 Scope 继续使用相同连接
        var user3 = await _users.OrderBy(x => x.Id).FirstOrDefaultAsync();
    }
}
```

#### 与事务单元配合使用

`SerializableScope` 与 `TransactionUnit` 可以灵活组合，支持多种使用模式：

```csharp
// 模式 1：SerializableScope 外层 + TransactionUnit 内层（推荐）
public async Task ScopeOuterTransactionInnerAsync()
{
    await using (var scope = new SerializableScope())
    {
        // 先建立连接复用范围
        var initialCount = await _users.CountAsync();
        
        await using (var transaction = new TransactionUnit())
        {
            // 事务内操作，复用 Scope 的连接
            var user = new User
            {
                Name = "测试用户",
                Email = "test@example.com",
                CreatedAt = DateTime.Now
            };
            
            await _userRepository.Into(user).ExecuteAsync();
            
            // 提交事务
            await transaction.CompleteAsync();
        }
        
        // Scope 继续使用，验证数据
        var finalCount = await _users.CountAsync();
        Assert.True(finalCount > initialCount);
    }
}

// 模式 2：TransactionUnit 外层 + SerializableScope 内层
public async Task TransactionOuterScopeInnerAsync()
{
    await using (var transaction = new TransactionUnit())
    {
        await using (var scope = new SerializableScope())
        {
            // 在事务和 Scope 内执行操作
            var user = new User
            {
                Name = "测试用户",
                Email = "test@example.com",
                CreatedAt = DateTime.Now
            };
            
            await _userRepository.Into(user).ExecuteAsync();
            
            // 验证插入
            var savedUser = await _users
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(x => x.Name == "测试用户");
            
            Assert.NotNull(savedUser);
        }
        
        // 提交事务
        await transaction.CompleteAsync();
    }
}

// 模式 3：同时使用 - 批量事务操作
public async Task BatchTransactionWithScopeAsync()
{
    await using (var scope = new SerializableScope())
    {
        await using (var transaction = new TransactionUnit())
        {
            // 批量更新
            for (int i = 1; i <= 100; i++)
            {
                await _userRepository
                    .Where(x => x.Id == i)
                    .UpdateAsync(x => new User
                    {
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    });
            }
            
            // 批量插入
            var newUsers = Enumerable.Range(1, 50)
                .Select(i => new User
                {
                    Name = $"BatchUser_{i}",
                    Email = $"batch{i}@example.com",
                    CreatedAt = DateTime.Now
                })
                .ToList();
            
            await _userRepository.Ignore().Into(newUsers).ExecuteAsync();
            
            // 提交事务
            await transaction.CompleteAsync();
        }
    }
}
```

#### 性能对比

```csharp
// ❌ 不使用 SerializableScope - 每次操作创建新连接
public async Task WithoutScopeAsync()
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    for (int i = 0; i < 100; i++)
    {
        // 每次都创建和释放连接
        await _userRepository
            .Where(x => x.Id == i)
            .UpdateAsync(x => new User { CreatedAt = DateTime.Now });
    }
    
    stopwatch.Stop();
    Console.WriteLine($"不使用 Scope: {stopwatch.ElapsedMilliseconds} ms");
}

// ✅ 使用 SerializableScope - 复用连接
public async Task WithScopeAsync()
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    await using (var scope = new SerializableScope())
    {
        for (int i = 0; i < 100; i++)
        {
            // 复用同一连接，性能提升 50%-80%
            await _userRepository
                .Where(x => x.Id == i)
                .UpdateAsync(x => new User { CreatedAt = DateTime.Now });
        }
    }
    
    stopwatch.Stop();
    Console.WriteLine($"使用 Scope: {stopwatch.ElapsedMilliseconds} ms");
}
```

#### 使用建议

**适用场景**：
- ✅ 批量插入、更新、删除操作
- ✅ 高频数据库操作（如循环中的查询）
- ✅ 需要在多个操作间共享连接的场景
- ✅ 与事务配合使用，确保连接一致性
- ✅ 减少连接池压力，提升应用性能

**注意事项**：
- 🔸 Scope 生命周期内连接保持打开状态，需及时释放
- 🔸 嵌套使用时，内层 Scope 会复用外层连接
- 🔸 与事务混合使用时，事务优先级更高
- 🔸 在事务环境中（`TransactionUnit` 或 `System.Transactions.Transaction`），连接由事务管理
- 🔸 建议使用 `await using` 确保资源正确释放

### 4. 复杂查询支持

支持嵌套查询、多表关联等复杂场景：

```csharp
// 嵌套查询示例
public async Task<List<UserConsultationDto>> GetUserConsultationsAsync(long id)
{
    // 子查询
    var specialistQuery = from a in _businessDepartmentConsultationRels
                          join b in _users on a.SpecialistId equals b.Id
                          where a.Id == id
                          select new
                          {
                              BusinessDepartmentId = a.BusinessDepartmentId,
                              SpecialistId = a.SpecialistId,
                              BusinessLineId = a.BusinessLineId,
                              SpecialistName = b.Name,
                          };

    // 主查询使用子查询结果
    var consultationQuery = from a in _businessDepartmentConsultationRels
                            join b in _businessConsultationReps on a.BusinessConsultationId equals b.Id into ab
                            from b in ab.DefaultIfEmpty() // Left Join
                            join c in specialistQuery on new { a.BusinessDepartmentId, a.SpecialistId, a.BusinessLineId } 
                                equals new { c.BusinessDepartmentId, c.SpecialistId, c.BusinessLineId }
                            select new UserConsultationDto
                            {
                                BusinessDepartmentId = a.BusinessDepartmentId,
                                SpecialistId = a.SpecialistId,
                                BusinessLineId = a.BusinessLineId,
                                BusinessConsultationId = a.BusinessConsultationId,
                                SpecialistName = c.SpecialistName,
                                ConsultationName = b.Name
                            };

    return await consultationQuery.ToListAsync();
}

// 分组聚合查询
public async Task<List<UserStatsDto>> GetUserStatsAsync()
{
    var now = DateTime.Now;

    var linq = from x in _users
               where x.Id == 100
               group new { x.Name, x.CreatedAt } by new { x.Id, x.Name } into g
               where g.Count() > 1
               orderby g.Key descending
               select new UserStatsDto
               {
                   Id = g.Key.Id,
                   Name = g.Key.Name,
                   Total = g.Count(),
                   ActiveCount = g.Count(x => x.CreatedAt > now),
                   RecentCount = g.Where(x => x.CreatedAt > now).Count()
               };

    return await linq.ToListAsync();
}
```

### 4. 分片表支持

```csharp
// 分片表实体
[Table("user_[sharding]")]
public class UserSharding
{
    [Key]
    [Field("id")]
    [DatabaseGenerated]
    public long Id { get; set; }

    [Field("name")]
    public string Name { get; set; }

    [Version]
    [Field("created_at")]
    public DateTime CreatedAt { get; set; }
}

// 分片表操作
public class ShardingUserService
{
    private readonly IRepository<UserSharding> _userShardingRepository;
    private readonly IQueryable<UserSharding> _userShardings;

    // 按年份分片查询
    public async Task<List<UserSharding>> GetUsersByYearAsync(string year)
    {
        return await _userShardings
            .DataSharding(year)
            .Where(x => x.Id > 100)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    // 分片表插入
    public async Task<int> InsertToShardAsync(string year, List<UserSharding> users)
    {
        return await _userShardingRepository
            .DataSharding(year)
            .Ignore()
            .Into(users)
            .ExecuteAsync();
    }
}
```

### 5. 动态条件查询

```csharp
using System.Linq.Expressions;
using Inkslab.Linq; // 添加 And 扩展方法的命名空间

public class UserSearchDto
{
    public string Name { get; set; }
    public string Email { get; set; }
    public bool? IsActive { get; set; }
}

public async Task<List<User>> SearchUsersAsync(UserSearchDto searchDto)
{
    // 方法一：使用 Conditions 帮助类
    var linq = from u in _users
               where Conditions.If(!string.IsNullOrEmpty(searchDto.Name), u.Name.Contains(searchDto.Name))
               && Conditions.If(!string.IsNullOrEmpty(searchDto.Email), u.Email.Contains(searchDto.Email))
               && Conditions.If(searchDto.IsActive.HasValue, u.IsActive == searchDto.IsActive.Value)
               orderby u.CreatedAt descending
               select u;

    return await linq.ToListAsync();
}

// 方法二：动态 Expression 构建（扩展方法）
public async Task<List<User>> SearchUsersWithExpressionAsync(UserSearchDto searchDto)
{
    Expression<Func<User, bool>> predicate = x => true;

    if (!string.IsNullOrEmpty(searchDto.Name))
    {
        predicate = predicate.And(x => x.Name.Contains(searchDto.Name));
    }

    if (!string.IsNullOrEmpty(searchDto.Email))
    {
        predicate = predicate.And(x => x.Email.Contains(searchDto.Email));
    }

    if (searchDto.IsActive.HasValue)
    {
        predicate = predicate.And(x => x.IsActive == searchDto.IsActive.Value);
    }

    return await _users.Where(predicate).OrderBy(x => x.CreatedAt).ToListAsync();
}
```

### 6. 自定义排序

```csharp
public async Task<List<User>> GetUsersWithCustomSortAsync(int sortType)
{
    var linq = from x in _users
               where x.IsActive
               orderby x.CreatedAt, 
                       x.Name, 
                       Ranks.By(x, c => c.When(sortType < 100)
                                        .OrderBy(y => y.CreatedAt)
                                        .ThenByDescending(y => y.Id)
                                        .DefaultByDescending(t => t.CreatedAt)
                                        .DefaultBy(t => t.Id))
               select x;

    return await linq.ToListAsync();
}
```

## 🏗️ 项目架构

### 核心组件

```
Inkslab.Linq/                          # 核心抽象层
├── IRepository<T>                      # 仓储接口
├── IDatabase                           # 数据库接口  
├── IQueryable<T>                       # 查询接口
├── DynamicParameter                    # 动态参数（用于输出参数和JSON类型）
├── TransactionUnit                     # 事务单元
└── SerializableScope                   # 串行化范围（连接复用）

Inkslab.Linq.SqlServer/                 # SQL Server 实现
├── SqlServerAdapter                    # SQL Server 适配器
├── SqlServerBulkCopyFactory           # 批量复制工厂
└── SqlServerLinqServiceCollectionExtensions

Inkslab.Linq.MySql/                     # MySQL 实现
├── MySqlAdapter                        # MySQL 适配器
├── MySqlBulkCopyFactory               # 批量复制工厂
└── MySqlLinqServiceCollectionExtensions

Inkslab.Linq.PostgreSQL/                # PostgreSQL 实现
├── PostgreSQLAdapter                   # PostgreSQL 适配器
├── PostgreSQLBulkAssistant            # PostgreSQL 批量复制（基于 COPY 命令）
├── PostgreSQLBulkCopyFactory          # 批量复制工厂
├── JsonPayload / JsonbPayload          # JSON 数据类型支持
└── PostgreSQLLinqServiceCollectionExtensions

Inkslab.Transactions/                   # 事务管理
└── TransactionUnit                     # 事务单元实现
```

### 多数据库配置

```csharp
// 自定义连接字符串提供者
public class SqlServerPromotionConnectionStrings : IConnectionStrings
{
    public string Strings { get; } = "Server=localhost;Database=Promotion;Trusted_Connection=true;";
}

public class PostgreSQLAnalyticsConnectionStrings : IConnectionStrings
{
    public string Strings { get; } = "Host=localhost;Database=analytics;Username=postgres;Password=password;";
}

public void ConfigureServices(IServiceCollection services)
{
    // MySQL 主数据库
    services.UseMySql()
        .UseLinq("server=localhost;uid=root;pwd=password;database=main;");

    // SQL Server 专用数据库
    services.UseSqlServer()
        .UseDatabase<SqlServerPromotionConnectionStrings>();

    // PostgreSQL 分析数据库（支持 JSON/JSONB 和批量操作）
    services.UsePostgreSQL()
        .UseDatabase<PostgreSQLAnalyticsConnectionStrings>();

    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
}

// 使用特定数据库
public class CombinationService
{
    private readonly IQueryable<User> _users; // 使用主数据库
    private readonly IDatabase<SqlServerPromotionConnectionStrings> _promotionDb; // 使用专用 SQL Server 数据库
    private readonly IDatabase<PostgreSQLAnalyticsConnectionStrings> _analyticsDb; // 使用专用 PostgreSQL 数据库

    public CombinationService(
        IQueryable<User> users, 
        IDatabase<SqlServerPromotionConnectionStrings> promotionDb,
        IDatabase<PostgreSQLAnalyticsConnectionStrings> analyticsDb)
    {
        _users = users;
        _promotionDb = promotionDb;
        _analyticsDb = analyticsDb;
    }

    public async Task<object> GetDataAsync()
    {
        // 主数据库查询
        var users = await _users.Where(x => x.Id == 100).ToListAsync();

        // 专用 SQL Server 数据库原生 SQL 查询
        var promotions = await _promotionDb.QueryAsync<(long Id, string Name)>(
            "SELECT Id, Name FROM Activity WHERE Id > @id ORDER BY CreateTime DESC OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY", 
            new { id = 100 });

        // 专用 PostgreSQL 数据库 JSON 查询
        var jsonData = await _analyticsDb.QueryAsync<UserContentsOfJsonbPayload>(
            "SELECT * FROM user_contents WHERE id = @id",
            new { id = 1 });

        return new { users, promotions, jsonData };
    }
}
```

## 🧪 测试

项目使用 XunitPlus 测试框架，支持自动依赖注入：

```csharp
public class UserServiceTests
{
    private readonly IRepository<User> _userRepository;
    private readonly IQueryable<User> _users;
    private readonly IDatabase _database;

    // XunitPlus 自动依赖注入
    public UserServiceTests(
        IRepository<User> userRepository,
        IQueryable<User> users,
        IDatabase database)
    {
        _userRepository = userRepository;
        _users = users;
        _database = database;
    }

    [Fact]
    public async Task Should_Insert_Users_Successfully()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Name = "测试用户1", Email = "test1@example.com", IsActive = true },
            new User { Name = "测试用户2", Email = "test2@example.com", IsActive = true }
        };

        // Act
        var result = await _userRepository.Ignore().Into(users).ExecuteAsync();

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public async Task Should_Query_Users_With_Complex_Conditions()
    {
        // Act
        var result = await _users
            .Where(u => u.IsActive && u.Name.Contains("测试"))
            .OrderBy(u => u.CreatedAt)
            .Take(10)
            .ToListAsync();

        // Assert
        Assert.NotNull(result);
    }
}
```

### 测试配置

创建测试项目的 `Startup.cs`：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class Startup : XunitPlus.Startup
{
    public Startup(Type serviceType) : base(serviceType) { }

    public override void ConfigureServices(IServiceCollection services, HostBuilderContext context)
    {
        // 配置测试数据库
        services.UseMySql()
            .UseLinq("server=localhost;uid=root;pwd=password;database=test_db;");

        services.AddLogging(builder => builder.AddDebug().SetMinimumLevel(LogLevel.Debug));

        base.ConfigureServices(services, context);
    }
}
```

### 运行测试

```bash
# 构建项目
dotnet build

# 运行所有测试
dotnet test

# 运行特定测试项目
dotnet test tests/Inkslab.Linq.Tests/

# 使用 VS Code 调试
# 按 F5 启动调试，支持断点调试和单元测试
```

## 🔧 高级特性

### 1. 批量操作

```csharp
// 批量插入
var users = new List<User>();
for (int i = 0; i < 1000; i++)
{
    users.Add(new User { Name = $"用户{i}", Email = $"user{i}@example.com" });
}
int insertedRows = await _userRepository.Timeout(100).Ignore().Into(users).ExecuteAsync();

// 批量更新
var usersToUpdate = await _users.Where(x => x.IsActive).ToListAsync();
foreach (var user in usersToUpdate)
{
    user.CreatedAt = DateTime.Now;
}
int updatedRows = await _userRepository.UpdateTo(usersToUpdate).ExecuteAsync();

// 批量删除
int deletedRows = await _userRepository.DeleteWith(usersToUpdate).ExecuteAsync();
```

### 2. 字符串函数支持

```csharp
var result = await _users
    .Where(x => x.Id == 100)
    .Select(x => new
    {
        x.Id,
        x.Name,
        NameSubstring = x.Name.Substring(2, 5),
        IsEmpty = string.IsNullOrEmpty(x.Name),
        ReplacedName = x.Name.Replace("测试", "TEST"),
        IndexOfTest = x.Name.IndexOf("测试", 2)
    })
    .ToListAsync();
```

### 3. 布尔字段操作

```csharp
var linq = from x in _users
           where x.IsActive                          // 直接使用布尔字段
           && (x.IsDeleted ?? false)                 // 可空布尔字段
           && (x.IsVerified.HasValue)                // 检查是否有值
           && (x.IsActive ^ x.IsDeleted.Value)       // 异或操作
           orderby x.CreatedAt descending
           select x;
```

### 4. Union 和 Concat 操作

```csharp
var activeUsers = from x in _users
                  where x.IsActive
                  select new { x.Id, x.Name, Type = "Active" };

var inactiveUsers = from x in _users
                    where !x.IsActive
                    select new { x.Id, x.Name, Type = "Inactive" };

// Union - 去重合并
var unionResult = await activeUsers.Union(inactiveUsers).ToListAsync();

// Concat - 直接合并
var concatResult = await activeUsers.Concat(inactiveUsers)
    .Select(x => string.Concat(x.Name, "-", x.Type)
    .Distinct()
    .ToListAsync();
```

### 5. DateTime 日期时间处理

框架为各数据库提供了全面的日期时间成员访问支持，自动转换 C# DateTime 属性为对应的 SQL 函数调用。

#### 支持的日期时间成员

框架支持以下 DateTime 成员的 LINQ 查询：

```csharp
public class DateTimeMembers
{
    public DateTime Date { get; set; }          // 日期部分（去掉时间）
    public int Year { get; set; }               // 年份
    public int Month { get; set; }              // 月份
    public int Day { get; set; }                // 日期
    public int Hour { get; set; }               // 小时
    public int Minute { get; set; }             // 分钟
    public int Second { get; set; }             // 秒
    public int Millisecond { get; set; }        // 毫秒
    public DayOfWeek DayOfWeek { get; set; }    // 星期几
    public int DayOfYear { get; set; }          // 年中第几天
    public long Ticks { get; set; }             // 从 0001-01-01 00:00:00 至今的 100 纳秒单位数
    public TimeSpan TimeOfDay { get; set; }     // 一天中的时间部分
}
```

#### 使用示例

```csharp
public async Task QueryByDateTimeAsync()
{
    var now = DateTime.Now;
    var startOfYear = new DateTime(DateTime.Now.Year, 1, 1);

    // 按年份分组
    var byYear = await _users
        .Where(x => x.CreatedAt.Year == DateTime.Now.Year)
        .OrderByDescending(x => x.CreatedAt)
        .ToListAsync();

    // 按月份筛选
    var thisMonth = await _users
        .Where(x => x.CreatedAt.Month == 12 && x.CreatedAt.Day > 10)
        .ToListAsync();

    // 按时间范围筛选（只比较日期部分）
    var todayUsers = await _users
        .Where(x => x.CreatedAt.Date == DateTime.Now.Date)
        .OrderBy(x => x.CreatedAt.Hour)
        .ToListAsync();

    // 按小时分组统计
    var byHour = await _users
        .GroupBy(x => x.CreatedAt.Hour)
        .Select(g => new
        {
            Hour = g.Key,
            Count = g.Count(),
            Users = g.ToList()
        })
        .ToListAsync();

    // 按星期几统计
    var byDayOfWeek = await _users
        .GroupBy(x => x.CreatedAt.DayOfWeek)
        .Select(g => new
        {
            DayOfWeek = g.Key,
            Count = g.Count()
        })
        .OrderBy(x => x.DayOfWeek)
        .ToListAsync();
}
```

#### 各数据库的实现细节

##### MySQL 日期处理
```csharp
// MySQL 使用以下函数：
// Year(date)      - YEAR 函数
// Month(date)     - MONTH 函数
// Day(date)       - DAY 函数
// Date(date)      - DATE 函数（提取日期部分）
// Hour(date)      - HOUR 函数
// Minute(date)    - MINUTE 函数
// Second(date)    - SECOND 函数
// TimeOfDay(date) - TIME 函数
// DayOfWeek(date) - DAYOFWEEK 函数（1=周日，7=周六）
// DayOfYear(date) - DAYOFYEAR 函数
// Ticks(date)     - TIMESTAMPDIFF(MICROSECOND, '0001-01-01', date) * 10
```

##### PostgreSQL 日期处理
```csharp
// PostgreSQL 使用以下函数：
// Year(date)      - EXTRACT(YEAR FROM date)
// Month(date)     - EXTRACT(MONTH FROM date)
// Day(date)       - EXTRACT(DAY FROM date)
// Date(date)      - CAST(DATE_TRUNC('day', date) AS DATE)
// Hour(date)      - EXTRACT(HOUR FROM date)
// Minute(date)    - EXTRACT(MINUTE FROM date)
// Second(date)    - EXTRACT(SECOND FROM date)
// TimeOfDay(date) - date::TIME
// DayOfWeek(date) - EXTRACT(DOW FROM date)（0=周日，6=周六）
// DayOfYear(date) - EXTRACT(DOY FROM date)
// Millisecond(date) - EXTRACT(MILLISECONDS FROM date)::INTEGER % 1000
// Ticks(date)     - 参见下文"Ticks 精度问题"
```

##### SQL Server 日期处理
```csharp
// SQL Server 使用 DATEPART 和 DATEDIFF 函数：
// Year(date)      - DATEPART(YEAR, date)
// Month(date)     - DATEPART(MONTH, date)
// Day(date)       - DATEPART(DAY, date)
// Date(date)      - CAST(date AS DATE)
// Hour(date)      - DATEPART(HOUR, date)
// Minute(date)    - DATEPART(MINUTE, date)
// Second(date)    - DATEPART(SECOND, date)
// Millisecond(date) - DATEPART(MILLISECOND, date)
// TimeOfDay(date) - CAST(date AS TIME)
// DayOfWeek(date) - DATEPART(WEEKDAY, date)（1=周日，7=周六）
// DayOfYear(date) - DATEPART(DAYOFYEAR, date)
```

#### DateTime.Ticks 精度问题 ⚠️

**DateTime.Ticks 是 .NET 中最容易出现精度问题的属性**。框架在不同数据库中的实现方式各不相同，需要特别注意：

##### PostgreSQL Ticks 计算

PostgreSQL 中 Ticks 的计算公式：
```sql
EXTRACT(EPOCH FROM date AT TIME ZONE 'UTC') * 10000000 + 
EXTRACT(MICROSECONDS FROM date) * 10 + 
621355968000000000
```

**精度问题：**
- PostgreSQL `EPOCH` 是以秒为单位的浮点数，精度为微秒级
- 转换后可能存在舍入误差，特别是在微秒位置
- 不同的时区处理可能导致计算结果偏差

##### MySQL Ticks 计算

MySQL 中 Ticks 的计算公式：
```sql
TIMESTAMPDIFF(MICROSECOND, '0001-01-01', date) * 10
```

**精度问题：**
- MySQL `TIMESTAMPDIFF` 返回的是微秒差值（INTEGER）
- 乘以 10 得到 100 纳秒单位（Ticks）
- 日期超出 MySQL 支持范围时会返回错误结果
- MySQL 的日期范围：'1000-01-01' 至 '9999-12-31'，但 .NET DateTime 范围是 0001-01-01 至 9999-12-31

##### SQL Server Ticks 计算

SQL Server 中 Ticks 的计算公式：
```sql
DATEDIFF(NANOSECOND, '1900-01-01', date) / 100
```

**精度问题：**
- SQL Server `DATEDIFF` 支持纳秒级精度（精确到 100 纳秒）
- SQL Server 内部时间精度最高为 100 纳秒
- 转换时可能因 SQL Server 本身的精度限制而丢失信息

#### 使用建议

```csharp
// ✅ 推荐做法
public async Task SafeDateTimeUsageAsync()
{
    // 1. 使用日期部分而非 Ticks
    var todayUsers = await _users
        .Where(x => x.CreatedAt.Date == DateTime.Now.Date)
        .ToListAsync();

    // 2. 使用年、月、日、小时等具体成员
    var recentUsers = await _users
        .Where(x => x.CreatedAt.Year == 2024 && x.CreatedAt.Month == 12)
        .ToListAsync();

    // 3. 日期范围查询使用 Date 类型
    var startDate = new DateTime(2024, 1, 1);
    var endDate = new DateTime(2024, 12, 31);
    var yearData = await _users
        .Where(x => x.CreatedAt.Date >= startDate.Date 
            && x.CreatedAt.Date <= endDate.Date)
        .ToListAsync();
}

// ⚠️ 需要谨慎的做法
public async Task CarefulDateTimeUsageAsync()
{
    // 不要直接比较 Ticks，可能因精度问题产生意外结果
    // 错误示例：
    // var users = _users.Where(x => x.CreatedAt.Ticks > someValue).ToListAsync();

    // 改为比较日期时间本身：
    var referenceTime = DateTime.Now.AddHours(-1);
    var users = await _users
        .Where(x => x.CreatedAt > referenceTime)
        .ToListAsync();

    // 或者在应用层进行 Ticks 比较后验证
    var result = await _users
        .Where(x => x.CreatedAt.Year == 2024)
        .ToListAsync();
    
    var filtered = result
        .Where(x => x.CreatedAt.Ticks > someTicksValue)
        .ToList();
}
```

#### 精度对比表

| 数据库 | 最高精度 | Ticks 计算来源 | 潜在问题 |
|------|--------|-------------|--------|
| SQL Server | 100 纳秒 | DATEDIFF(NANOSECOND) / 100 | 精度较好，但受 SQL Server 限制 |
| PostgreSQL | 1 微秒 | EXTRACT(EPOCH) + EXTRACT(MICROSECONDS) | 浮点舍入，时区处理复杂 |
| MySQL | 1 微秒 | TIMESTAMPDIFF(MICROSECOND) * 10 | 日期范围限制，溢出风险 |
| SQLite | 毫秒级 | 基于 julianday() 函数 | 精度较低，可能丢失微秒信息 |
| Oracle | 1 微秒 | 基于 SYSTIMESTAMP 运算 | 复杂计算，舍入误差 |

---

### 6. PostgreSQL JSON 数据类型支持

Inkslab.Linq 为 PostgreSQL 提供了完整的 JSON/JSONB 数据类型支持，支持多种 JSON 表示方式，摒弃了 **`PostgreSQL ENUM`** 强约束。

#### 支持的 JSON 类型

框架支持以下 JSON 类型的自动转换和处理：

1. **`JsonPayload`** - 框架内置类型，用于存储原始JSON字符串
2. **`JsonbPayload`** - 框架内置类型，专门用于PostgreSQL JSONB类型

#### 实体定义

```csharp
using System.ComponentModel.DataAnnotations;
using Inkslab.Linq;
using Inkslab.Linq.Annotations;

// 使用 JsonbPayload（推荐用于 JSONB）
[Table("user_contents")]
public class UserContentsOfJsonbPayload
{
    [Key]
    [Field("id")]
    [DatabaseGenerated]
    public int Id { get; set; }

    [Field("content")]
    public JsonbPayload Content { get; set; }
}
```

#### 插入 JSON 数据

```csharp
public class UserService
{
    private readonly IRepository<UserContentsOfJsonbPayload> _repositoryOfJsonbPayload;
    private readonly IDatabase _database;

    public UserService(
        IRepository<UserContentsOfJsonbPayload> repositoryOfJsonbPayload,
        IDatabase database)
    {
        _repositoryOfJsonbPayload = repositoryOfJsonbPayload;
        _database = database;
    }

    // 使用 JsonbPayload 插入
    public async Task InsertWithJsonbPayloadAsync()
    {
        var data = new UserContentsOfJsonbPayload
        {
            Content = new JsonbPayload("{\"name\":\"inkslab\",\"age\":35}")
        };
        
        await _repositoryOfJsonbPayload.Into(data).ExecuteAsync();
    }

    // 直接指定 JSONB 类型
    public async Task InsertDirectJsonbAsync()
    {
        string sql = "INSERT INTO \"user_contents\"(\"content\") VALUES(@content::jsonb) RETURNING id;";
        
        var id = await _database.SingleAsync<long?>(sql, new { 
            content = "{\"name\":\"测试\"}" 
        });
    }

    // 使用 DynamicParameter 指定 JSONB 类型
    public async Task InsertWithDynamicParameterAsync()
    {
        string sql = "INSERT INTO \"user_contents\"(\"content\") VALUES(@content) RETURNING id;";
        
        var id = await _database.SingleAsync<long?>(sql, new
        {
            content = new DynamicParameter
            {
                Value = "{\"name\":\"测试2\"}",
                DbType = LookupDb.JsonbDbType,
                Direction = ParameterDirection.Input
            }
        });
    }
}
```

#### 查询 JSON 数据

```csharp
public async Task QueryJsonDataAsync()
{
    var queryable = _queryableOfJsonbPayload;

    // 查询所有 JSON 数据
    var allData = await queryable
        .OrderByDescending(x => x.Id)
        .ToListAsync();

    // 获取最新的 JSON 记录
    var latest = await queryable
        .OrderByDescending(x => x.Id)
        .FirstOrDefaultAsync();

    // 使用原生 SQL 查询 JSON 字段
    string sql = "SELECT * FROM \"user_contents\" WHERE id = @id";
    var result = await _database.FirstOrDefaultAsync<UserContentsOfJsonbPayload>(sql, new { id = 1 });
}
```

#### 注意事项

1. **自动类型转换**：框架会自动识别 JSON 参数并应用 `::json` 或 `::jsonb` 强制转换
2. **JsonPayload 推荐**：对于 PostgreSQL JSON 字段，推荐使用 `JsonPayload`
3. **JsonbPayload 推荐**：对于 PostgreSQL JSONB 字段，推荐使用 `JsonbPayload`
4. **批量操作支持**：在 `WriteToServerAsync` 中，框架现已完整支持 JSON/JSONB 数据的异步写入

#### LINQ 查询中使用 JSON

```csharp
// 插入 JSON 数据（通过 LINQ）
public async Task InsertJsonViaLinqAsync()
{
    var jsonData = new UserContentsOfJsonbPayload
    {
        Content = new JsonbPayload("{\"name\":\"test\",\"age\":25}")
    };
    
    await _repositoryOfJsonbPayload.Into(jsonData).ExecuteAsync();
}

// 更新 JSON 数据
public async Task UpdateJsonDataAsync(int id, JsonbPayload newContent)
{
    await _repositoryOfJsonbPayload
        .Where(x => x.Id == id)
        .UpdateAsync(x => new UserContentsOfJsonbPayload
        {
            Content = newContent
        });
}

// 查询并处理 JSON 数据
public async Task<List<UserContentsOfJsonbPayload>> QueryJsonDataAsync()
{
    return await _queryableOfJsonbPayload
        .Where(x => x.Id > 100)
        .OrderByDescending(x => x.Id)
        .Take(100)
        .ToListAsync();
}
```

### 7. PostgreSQL 批量操作

PostgreSQL 通过 `COPY` 命令支持高效的批量数据导入，框架提供了便捷的批量操作接口。

#### 基本批量插入

```csharp
using System.Data;
using Inkslab.Linq.PostgreSQL;

public class PostgreSQLBulkExample
{
    private readonly IDatabase _database;

    public PostgreSQLBulkExample(IDatabase database)
    {
        _database = database;
    }

    // 使用仓储进行批量插入
    public async Task<int> BulkInsertAsync(List<User> users)
    {
        return await _userRepository
            .Timeout(100)
            .Ignore()
            .Into(users)
            .ExecuteAsync();
    }

    // 使用数据表进行批量插入
    public async Task<int> BulkInsertWithDataTableAsync()
    {
        var dataTable = new DataTable("users");

        // 定义列结构
        dataTable.Columns.Add("name", typeof(string));
        dataTable.Columns.Add("email", typeof(string));
        dataTable.Columns.Add("age", typeof(int));
        dataTable.Columns.Add("salary", typeof(decimal));
        dataTable.Columns.Add("is_active", typeof(bool));
        dataTable.Columns.Add("created_at", typeof(DateTime));

        // 添加测试数据（示例：1000行）
        var random = new Random();
        var now = DateTime.Now;

        for (int i = 1; i <= 1000; i++)
        {
            dataTable.Rows.Add(
                $"用户{i:D4}",
                $"user{i:D4}@example.com",
                random.Next(18, 65),
                Math.Round((decimal)(random.NextDouble() * 50000 + 30000), 2),
                random.Next(2) == 1,
                now.AddMinutes(-random.Next(0, 525600))
            );
        }

        // 执行批量插入
        return await _database.WriteToServerAsync(connectionStrings, dataTable);
    }
}
```

#### 批量插入 JSON 数据

```csharp
public async Task BulkInsertJsonDataAsync()
{
    var dataTable = new DataTable("user_contents");

    // 定义列结构
    dataTable.Columns.Add("content", typeof(JsonbPayload));

    // 添加 JSON 数据
    var random = new Random();
    var now = DateTime.Now;

    for (int i = 1; i <= 1000; i++)
    {
        dataTable.Rows.Add(
            new JsonbPayload($@"{{
                ""id"": {i},
                ""name"": ""用户{i:D4}"",
                ""age"": {random.Next(18, 65)},
                ""salary"": {Math.Round((decimal)(random.NextDouble() * 50000 + 30000), 2)},
                ""is_active"": {(random.Next(2) == 1).ToString().ToLower()},
                ""created_at"": ""{now:o}""
            }}")
        );
    }

    // 执行批量插入
    int rowsAffected = await _database.WriteToServerAsync(connectionStrings, dataTable);
    Console.WriteLine($"成功插入 {rowsAffected} 行 JSON 数据");
}
```

#### 事务内批量操作

```csharp
public async Task BulkInsertInTransactionAsync()
{
    await using (var transaction = new TransactionUnit())
    {
        // 准备数据
        var dataTable = new DataTable("users");
        dataTable.Columns.Add("name", typeof(string));
        dataTable.Columns.Add("email", typeof(string));

        // 添加数据...
        dataTable.Rows.Add("张三", "zhangsan@test.com");
        dataTable.Rows.Add("李四", "lisi@test.com");

        // 批量插入
        int rowsAffected = await _database.WriteToServerAsync(connectionStrings, dataTable);

        Console.WriteLine($"插入 {rowsAffected} 行数据");

        // 提交事务
        await transaction.CompleteAsync();
    }
}
```

#### 处理特殊字符

```csharp
public async Task HandleSpecialCharactersAsync()
{
    // 当表名或列名包含特殊字符时，框架会自动处理
    var dataTable = new DataTable("user data");  // 表名包含空格
    dataTable.Columns.Add("full name", typeof(string));        // 列名包含空格
    dataTable.Columns.Add("e-mail address", typeof(string));   // 列名包含连字符
    dataTable.Columns.Add("salary/month", typeof(decimal));    // 列名包含斜杠

    // 添加数据
    dataTable.Rows.Add("张三", "zhangsan@company.com", 8500.50m);

    // 自动处理特殊字符，生成: COPY "user data" ("full name", "e-mail address", "salary/month") FROM STDIN
    int rowsAffected = await _database.WriteToServerAsync(connectionStrings, dataTable);
}
```

#### 性能优化建议

- **批量大小**：建议每批插入 1000-10000 行，平衡内存和性能
- **超时设置**：对于大批量操作，使用 `.Timeout(seconds)` 设置合理的超时时间
- **事务处理**：大型批量操作建议在事务内执行，便于失败时回滚
- **性能指标**：框架支持获取插入速率（行/秒）用于性能监控

### 8. 存储过程调用

框架支持调用带有输入参数、输出参数和返回值的存储过程。

#### 创建存储过程示例（MySQL）

```sql
DROP PROCEDURE IF EXISTS GetUserInfo;

DELIMITER $$
CREATE PROCEDURE GetUserInfo(
    IN UserId INT,
    OUT UserName VARCHAR(50),
    OUT UserCount INT
)
BEGIN
    SELECT name INTO UserName FROM `user` WHERE id = UserId;
    SELECT COUNT(*) INTO UserCount FROM `user`;
    SELECT * FROM `user` WHERE id = UserId;
END$$
DELIMITER ;
```

#### 调用存储过程

```csharp
using Inkslab.Linq;
using System.Data;

public class UserService
{
    private readonly IDatabase _database;

    public UserService(IDatabase database)
    {
        _database = database;
    }

    // 调用带输出参数的存储过程
    public async Task<User> GetUserInfoAsync(int userId)
    {
        // 定义输出参数
        var userNameParam = new DynamicParameter
        {
            Direction = ParameterDirection.Output,
            DbType = DbType.String,
            Size = 50  // 输出参数需要指定大小
        };

        var userCountParam = new DynamicParameter
        {
            Direction = ParameterDirection.Output,
            DbType = DbType.Int32
        };

        // 构建参数字典
        var parameters = new Dictionary<string, object>
        {
            ["@UserId"] = userId,           // 输入参数
            ["@UserName"] = userNameParam,  // 输出参数
            ["@UserCount"] = userCountParam // 输出参数
        };

        // 执行存储过程并获取查询结果
        var result = await _database.QueryAsync<User>("GetUserInfo", parameters);

        // 读取输出参数的值
        var userName = userNameParam.Value as string;
        var userCount = Convert.ToInt32(userCountParam.Value);

        Console.WriteLine($"用户名: {userName}, 总用户数: {userCount}");

        return result.FirstOrDefault();
    }

    // 调用存储过程（仅执行，不返回结果集）
    public async Task<int> UpdateUserStatusAsync(int userId, bool isActive)
    {
        var parameters = new Dictionary<string, object>
        {
            ["@UserId"] = userId,
            ["@IsActive"] = isActive
        };

        return await _database.ExecuteAsync("UpdateUserStatus", parameters);
    }

    // 调用带返回值的存储过程
    public async Task<int> DeleteInactiveUsersAsync()
    {
        // 定义返回值参数
        var returnValueParam = new DynamicParameter
        {
            Direction = ParameterDirection.ReturnValue,
            DbType = DbType.Int32
        };

        var parameters = new Dictionary<string, object>
        {
            ["@ReturnValue"] = returnValueParam
        };

        await _database.ExecuteAsync("DeleteInactiveUsers", parameters);

        // 获取存储过程返回值
        return Convert.ToInt32(returnValueParam.Value);
    }
}
```

#### DynamicParameter 参数说明

`DynamicParameter` 用于定义输出参数和返回值参数，支持以下属性：

```csharp
public class DynamicParameter
{
    // 参数方向：Input, Output, InputOutput, ReturnValue
    public ParameterDirection Direction { get; set; }
    
    // 数据库类型
    public DbType DbType { get; set; }
    
    // 参数大小（字符串/二进制类型必须指定）
    public int Size { get; set; }
    
    // 数值精度
    public byte Precision { get; set; }
    
    // 数值小数位数
    public byte Scale { get; set; }
    
    // 参数值（输出参数执行后可从此属性读取返回值）
    public object Value { get; set; }
}
```

#### 参数方向说明

| 参数方向 | 说明 | 使用场景 |
|---------|------|---------|
| `ParameterDirection.Input` | 输入参数（默认） | 传递数据到存储过程 |
| `ParameterDirection.Output` | 输出参数 | 从存储过程获取返回数据 |
| `ParameterDirection.InputOutput` | 输入输出参数 | 既传入数据又接收返回数据 |
| `ParameterDirection.ReturnValue` | 返回值 | 获取存储过程的 RETURN 值 |

#### 注意事项

1. **输出参数必须指定大小**：对于字符串类型的输出参数，必须设置 `Size` 属性
2. **参数名称**：参数名称建议使用 `@` 前缀（如 `@UserId`）
3. **读取输出值**：输出参数的值在存储过程执行后通过 `Value` 属性获取
4. **类型转换**：输出参数的 `Value` 可能为 `DBNull`，使用前需进行类型检查和转换

## 📦 NuGet 包信息

| 包名 | 版本 | 描述 |
|------|------|------|
| Inkslab.Linq | 1.2.57 | 核心库，提供基础抽象和接口 |
| Inkslab.Linq.SqlServer | 1.2.57 | SQL Server 数据库支持 |
| Inkslab.Linq.MySql | 1.2.57 | MySQL 数据库支持 |
| Inkslab.Linq.PostgreSQL | 1.2.57 | PostgreSQL 数据库支持，包含 JSON/JSONB 和批量操作 |
| Inkslab.Transactions | 1.2.57 | 事务管理组件 |

### 包依赖关系

```xml
<!-- 项目文件示例 -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- 核心包 -->
    <PackageReference Include="Inkslab.Linq" Version="1.2.57" />
    
    <!-- 根据需要选择数据库支持（可选择其中一个或多个） -->
    <!-- SQL Server 支持 -->
    <PackageReference Include="Inkslab.Linq.SqlServer" Version="1.2.57" />
    
    <!-- MySQL 支持 -->
    <PackageReference Include="Inkslab.Linq.MySql" Version="1.2.57" />
    <PackageReference Include="MySqlConnector" Version="2.4.0" />
    
    <!-- PostgreSQL 支持（包含 JSON/JSONB 和 COPY 批量操作） -->
    <PackageReference Include="Inkslab.Linq.PostgreSQL" Version="1.2.57" />
    <PackageReference Include="Npgsql" Version="8.0.8" />
    
    <!-- 事务支持 -->
    <PackageReference Include="Inkslab.Transactions" Version="1.2.57" />
  </ItemGroup>
</Project>
```

## ⚠️ 重要注意事项

#### PostgreSQL 用户注意事项
- `UInt64` 值转换为 `Numeric` 类型时，可能涉及精度问题
- **建议**：在处理超大整数时，使用 `decimal` 类型而非 `ulong` 类型
- 表名或列名包含特殊字符时，框架会自动进行引号包裹和转义

#### 事务处理建议
- `TransactionUnit` 会自动开启已关闭的数据库连接
- **建议**：在大型批量操作中使用事务，确保数据一致性

### 类型转换注意事项

框架内置的类型转换通过表达式树动态编译，使用 LFU 缓存优化性能：

| 转换场景 | 推荐做法 | 风险防控 |
|---------|--------|--------|
| 无符号→有符号 | 考虑 `MaxValue` 范围 | 使用 `checked` 防止溢出 |
| 高精度→低精度 | 使用 `decimal` 中间值 | 考虑精度丢失 |
| 空值处理 | 总是检查 `HasValue` | 避免隐式转换 |
| 字符串→数值 | 用 `TryParse` 验证 | 不要依赖强制转换 |

---

## 🛠️ 开发环境

### 要求

- .NET 6.0 或更高版本
- Visual Studio 2022 或 VS Code
- SQL Server 或 MySQL（用于开发和测试）

### 构建脚本

使用 PowerShell 构建脚本：

```powershell
.\build.ps1
```

### VS Code 配置

项目已配置 VS Code 开发环境：

```json
// .vscode/tasks.json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "build",
            "command": "dotnet",
            "type": "process",
            "args": ["build"],
            "problemMatcher": "$msCompile"
        },
        {
            "label": "test",
            "command": "dotnet",
            "type": "process", 
            "args": ["test"],
            "group": "test"
        }
    ]
}
```

## 📝 许可证

本项目使用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 🤝 贡献

欢迎提交 Issues 和 Pull Requests！

### 贡献指南

1. Fork 本仓库
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开 Pull Request

### 开发规范

- 遵循 C# 编码规范
- 编写单元测试
- 更新相关文档
- 确保所有测试通过

## 📞 支持

- 🌟 [GitHub Issues](https://github.com/tinylit/inkslab.linq/issues)
- 📧 技术支持：TINYLIT 团队  
- 📖 [项目文档](https://github.com/tinylit/inkslab.linq)
- 💬 [讨论区](https://github.com/tinylit/inkslab.linq/discussions)

## 🔗 相关链接

- [官方网站](https://github.com/tinylit/inkslab.linq)
- [API 文档](https://github.com/tinylit/inkslab.linq/wiki)
- [示例项目](https://github.com/tinylit/inkslab.linq/tree/main/tests)
- [更新日志](https://github.com/tinylit/inkslab.linq/releases)

---
