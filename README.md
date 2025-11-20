# Inkslab.Linq

一个高性能的 .NET LINQ 扩展库，提供强大的数据库查询能力和事务管理功能。

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Version](https://img.shields.io/badge/version-1.2.46-green.svg)](.nupkgs/)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%20Standard%202.1-purple.svg)](Directory.Build.props)
[![GitHub](https://img.shields.io/github/license/tinylit/inkslab.linq.svg)](LICENSE)
[![GitHub issues](https://img.shields.io/github/issues-raw/tinylit/inkslab.linq)](../../issues)

## 🚀 快速开始

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

### 3. 复杂查询支持

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
└── TransactionUnit                     # 事务单元

Inkslab.Linq.SqlServer/                 # SQL Server 实现
├── SqlServerAdapter                    # SQL Server 适配器
├── SqlServerBulkCopyFactory           # 批量复制工厂
└── SqlServerLinqServiceCollectionExtensions

Inkslab.Linq.MySql/                     # MySQL 实现
├── MySqlAdapter                        # MySQL 适配器
├── MySqlBulkCopyFactory               # 批量复制工厂
└── MySqlLinqServiceCollectionExtensions

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

public void ConfigureServices(IServiceCollection services)
{
    // MySQL 主数据库
    services.UseMySql()
        .UseLinq("server=localhost;uid=root;pwd=password;database=main;");

    // SQL Server 专用数据库
    services.UseSqlServer()
        .UseDatabase<SqlServerPromotionConnectionStrings>();

    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
}

// 使用特定数据库
public class CombinationService
{
    private readonly IQueryable<User> _users; // 使用主数据库
    private readonly IDatabase<SqlServerPromotionConnectionStrings> _promotionDb; // 使用专用数据库

    public CombinationService(
        IQueryable<User> users, 
        IDatabase<SqlServerPromotionConnectionStrings> promotionDb)
    {
        _users = users;
        _promotionDb = promotionDb;
    }

    public async Task<object> GetDataAsync()
    {
        // 主数据库查询
        var users = await _users.Where(x => x.Id == 100).ToListAsync();

        // 专用数据库原生 SQL 查询
        var promotions = await _promotionDb.QueryAsync<(long Id, string Name)>(
            "SELECT Id, Name FROM Activity WHERE Id > @id ORDER BY CreateTime DESC OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY", 
            new { id = 100 });

        return new { users, promotions };
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

### 5. 存储过程调用

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
| Inkslab.Linq | 1.2.45 | 核心库，提供基础抽象和接口 |
| Inkslab.Linq.SqlServer | 1.2.45 | SQL Server 数据库支持 |
| Inkslab.Linq.MySql | 1.2.45 | MySQL 数据库支持 |
| Inkslab.Transactions | 1.2.45 | 事务管理组件 |

### 包依赖关系

```xml
<!-- 项目文件示例 -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- 核心包 -->
    <PackageReference Include="Inkslab.Linq" Version="1.2.45" />
    
    <!-- 根据需要选择数据库支持 -->
    <PackageReference Include="Inkslab.Linq.SqlServer" Version="1.2.45" />
    <!-- 或者 -->
    <PackageReference Include="Inkslab.Linq.MySql" Version="1.2.45" />
    <PackageReference Include="MySqlConnector" Version="2.4.0" />
    
    <!-- 事务支持 -->
    <PackageReference Include="Inkslab.Transactions" Version="1.2.45" />
  </ItemGroup>
</Project>
```

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
