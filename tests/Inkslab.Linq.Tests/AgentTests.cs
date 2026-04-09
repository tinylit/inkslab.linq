#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Inkslab.Linq.Annotations;
using Xunit;

namespace Inkslab.Linq.Tests
{
    #region 实体

    /// <summary>
    /// 平台数据库连接
    /// </summary>
    [Table("database")]
    public class Databases
    {
        /// <summary>
        /// id
        /// </summary>
        [Key]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 平台id
        /// </summary>
        [Field("platform_code")]
        [Required]
        [StringLength(50)]
        public string PlatformCode { get; set; } = default!;

        /// <summary>
        /// 数据库类型
        /// </summary>
        [Field("database_type")]
        public DatabaseEngine DatabaseType { get; set; }

        /// <summary>
        /// 1 ERP 2 WMS 3 ERP中间库 4 WMS中间库
        /// 业务类型
        /// </summary>
        [Field("business_type")]
        public int BusinessType { get; set; }

        /// <summary>
        /// 连接字符串
        /// </summary>
        [Field("connection_strings")]
        [Required]
        [StringLength(255)]
        public string ConnectionStrings { get; set; } = default!;
    }

    /// <summary>
    /// 平台数据库连接SQL语句
    /// </summary>
    [Table("database_commands")]
    public class DatabaseCommands
    {
        /// <summary>
        /// id
        /// </summary>
        [Key]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 平台id
        /// </summary>
        [Field("database_id")]
        public long DatabaseId { get; set; }

        /// <summary>
        /// sql 类型 1 查询 2 执行
        /// </summary>
        [Field("type")]
        public int Type { get; set; }

        /// <summary>
        /// 语句编码
        /// </summary>
        [Field("code")]
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = default!;

        /// <summary>
        /// sql 语句
        /// </summary>
        [Field("tsql")]
        [Required]
        [StringLength(65535)]
        public string Tsql { get; set; } = default!;

        /// <summary>
        /// 生效版本
        /// </summary>
        [Field("version")]
        public long Version { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Field("create_time")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        [Field("create_by")]
        public long CreateBy { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Field("update_time")]
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 更新人
        /// </summary>
        [Field("update_by")]
        public long UpdateBy { get; set; }
    }

    #endregion

    #region DTO

    public class DatabaseLinkCommandInDto
    {
        public string? Code { get; set; }
        public string? MedicalTradeNo { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class DatabaseLinkCommandOutDto
    {
        public string Tsql { get; set; } = default!;
        public long Version { get; set; }
        public string? ConnectionString { get; set; }
        public string? MedicalTradeNo { get; set; }
        public int BusinessType { get; set; }
        public DatabaseEngine DataBaseType { get; set; }
    }

    #endregion

    /// <summary>
    /// 平台数据库连接SQL语句查询测试（复现SQL解析语法错误）。
    /// </summary>
    public class AgentTests
    {
        private readonly IQueryable<Databases> _databasesQueryable;
        private readonly IQueryable<DatabaseCommands> _databaseCommandsQueryable;

        public AgentTests(
            IQueryable<Databases> databasesQueryable,
            IQueryable<DatabaseCommands> databaseCommandsQueryable)
        {
            _databasesQueryable = databasesQueryable;
            _databaseCommandsQueryable = databaseCommandsQueryable;
        }

        /// <summary>
        /// 测试：带Code和MedicalTradeNo条件，子查询Contains + 分页。
        /// SQL预览:
        /// SELECT `s`.`tsql` AS `Tsql`, `s`.`version` AS `Version`
        /// FROM `database_commands` AS `s`
        /// WHERE `s`.`code` = ?inDto_Code
        ///   AND `s`.`database_id` IN (SELECT `s1`.`id` FROM `database` AS `s1` WHERE `s1`.`medical_trade_no` = ?inDto_MedicalTradeNo)
        /// ORDER BY `s`.`id` DESC
        /// LIMIT ?pageSize OFFSET ?offset
        /// </summary>
        [Fact]
        public async Task DatabaseLinkCommand_WithCodeAndMedicalTradeNo_ShouldParseSqlAsync()
        {
            var inDto = new DatabaseLinkCommandInDto
            {
                Code = "TEST_CODE",
                MedicalTradeNo = "MT001",
                PageIndex = 1,
                PageSize = 20
            };

            var databaseQuery = _databasesQueryable.AsQueryable();
            var databaseCommandQuery = _databaseCommandsQueryable.AsQueryable();

            databaseCommandQuery = databaseCommandQuery.Where(x => x.Code == inDto.Code);
            databaseQuery = databaseQuery.Where(x => x.PlatformCode == inDto.MedicalTradeNo);

            var databaseIdsQuery = databaseQuery.Select(s => s.Id);

            var database = await databaseQuery
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            var result = await databaseCommandQuery
                .Where(x => databaseIdsQuery.Contains(x.DatabaseId))
                .OrderByDescending(s => s.Id)
                .Select(s => new DatabaseLinkCommandOutDto
                {
                    Tsql = s.Tsql,
                    Version = s.Version
                })
                .ToListAsync(inDto.PageIndex, inDto.PageSize);
        }

        /// <summary>
        /// 测试：仅MedicalTradeNo条件（无Code），子查询Contains + 分页。
        /// </summary>
        [Fact]
        public async Task DatabaseLinkCommand_WithMedicalTradeNoOnly_ShouldParseSqlAsync()
        {
            var inDto = new DatabaseLinkCommandInDto
            {
                MedicalTradeNo = "MT002",
                PageIndex = 1,
                PageSize = 10
            };

            var databaseQuery = _databasesQueryable.AsQueryable();
            var databaseCommandQuery = _databaseCommandsQueryable.AsQueryable();

            databaseQuery = databaseQuery.Where(x => x.PlatformCode == inDto.MedicalTradeNo);

            var databaseIdsQuery = databaseQuery.Select(s => s.Id);

            var database = await databaseQuery
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            var result = await databaseCommandQuery
                .Where(x => databaseIdsQuery.Contains(x.DatabaseId))
                .OrderByDescending(s => s.Id)
                .Select(s => new DatabaseLinkCommandOutDto
                {
                    Tsql = s.Tsql,
                    Version = s.Version
                })
                .ToListAsync(inDto.PageIndex, inDto.PageSize);
        }

        /// <summary>
        /// 测试：仅Code条件（无MedicalTradeNo），无数据库过滤的子查询Contains + 分页。
        /// </summary>
        [Fact]
        public async Task DatabaseLinkCommand_WithCodeOnly_ShouldParseSqlAsync()
        {
            var inDto = new DatabaseLinkCommandInDto
            {
                Code = "TEST_CODE",
                PageIndex = 1,
                PageSize = 20
            };

            var databaseQuery = _databasesQueryable.AsQueryable();
            var databaseCommandQuery = _databaseCommandsQueryable.AsQueryable();

            databaseCommandQuery = databaseCommandQuery.Where(x => x.Code == inDto.Code);

            var databaseIdsQuery = databaseQuery.Select(s => s.Id);

            var database = await databaseQuery
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            var result = await databaseCommandQuery
                .Where(x => databaseIdsQuery.Contains(x.DatabaseId))
                .OrderByDescending(s => s.Id)
                .Select(s => new DatabaseLinkCommandOutDto
                {
                    Tsql = s.Tsql,
                    Version = s.Version
                })
                .ToListAsync(inDto.PageIndex, inDto.PageSize);
        }

        /// <summary>
        /// 测试：无任何过滤条件，全量子查询Contains + 分页。
        /// </summary>
        [Fact]
        public async Task DatabaseLinkCommand_WithNoFilter_ShouldParseSqlAsync()
        {
            var inDto = new DatabaseLinkCommandInDto
            {
                PageIndex = 1,
                PageSize = 20
            };

            var databaseQuery = _databasesQueryable.AsQueryable();
            var databaseCommandQuery = _databaseCommandsQueryable.AsQueryable();

            var databaseIdsQuery = databaseQuery.Select(s => s.Id);

            var database = await databaseQuery
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            var result = await databaseCommandQuery
                .Where(x => databaseIdsQuery.Contains(x.DatabaseId))
                .OrderByDescending(s => s.Id)
                .Select(s => new DatabaseLinkCommandOutDto
                {
                    Tsql = s.Tsql,
                    Version = s.Version
                })
                .ToListAsync(inDto.PageIndex, inDto.PageSize);
        }
    }
}
