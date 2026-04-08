using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Inkslab.Linq.Annotations;
using Inkslab.Transactions;
using Xunit;
using XunitPlus;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// 仓储测试类。
    /// </summary>
    /// <remarks>
    /// 使用 "xunitPlus" 作为测试框架，以支持按步骤执行测试。
    /// </remarks>
    [TestPriority(1)]
    public class RepositoryTests
    {

        /// <summary>
        /// 渠道配置表。
        /// </summary>
        [Table("channel_config")]
        public class ChannelConfig
        {
            /// <summary>
            /// 主键ID。
            /// </summary>
            [Key]
            [Field("id")]
            public long Id { get; set; }

            /// <summary>
            /// 渠道类型(短信、极光推送、站内信)。
            /// </summary>
            [Field("types")]
            public int Types { get; set; }

            /// <summary>
            /// 对外请求地址。
            /// </summary>
            [Field("request_address")]
            [MaxLength(200)]
            public string RequestAddress { get; set; }

            /// <summary>
            /// 备注。
            /// </summary>
            [Field("remark")]
            [MaxLength(16171)]
            public string Remark { get; set; }

            /// <summary>
            /// 是否启用。
            /// </summary>
            [Field("is_valid")]
            public bool IsValid { get; set; }

            /// <summary>
            /// 创建人。
            /// </summary>
            [Field("create_by")]
            public long CreateBy { get; set; }

            /// <summary>
            /// 创建时间。
            /// </summary>
            [Field("create_time")]
            public DateTime CreateTime { get; set; }

            /// <summary>
            /// 修改人。
            /// </summary>
            [Field("update_by")]
            public long? UpdateBy { get; set; }

            /// <summary>
            /// 修改时间。
            /// </summary>
            [Field("update_time")]
            public DateTime? UpdateTime { get; set; }
        }

        /// <summary>
        /// 商品批次商品库存表
        /// inventory_commodity_batch_item_stock
        /// </summary>
        [Table("inventory_commodity_batch_item_stock")]
        public class InventoryCommodityBatchItemStock
        {
            /// <summary>
            /// 主键ID
            /// </summary>
            [Key]
            [Field("id")]
            [DatabaseGenerated]
            public long Id { get; set; }

            /// <summary>
            /// 医贸编号
            /// </summary>
            [Required]
            [Field("medical_trade_no")]
            [StringLength(25)]
            public string MedicalTradeNo { get; set; } = default!;

            /// <summary>
            /// ERP商品ID
            /// </summary>
            [Required]
            [Field("erp_product_id")]
            [StringLength(36)]
            public string ErpProductId { get; set; } = default!;

            /// <summary>
            /// ERP商品Code
            /// </summary>
            [Required(AllowEmptyStrings = true)]
            [Field("erp_product_code")]
            [StringLength(20)]
            public string ErpProductCode { get; set; } = default!;

            /// <summary>
            /// 货主编码
            /// </summary>
            [Required]
            [Field("cargo_owner_no")]
            [StringLength(25)]
            public string CargoOwnerNo { get; set; } = default!;

            /// <summary>
            /// 仓库编号
            /// </summary>
            [Required]
            [Field("warehouse_no")]
            [StringLength(25)]
            public string WarehouseNo { get; set; } = default!;

            /// <summary>
            /// 批次
            /// </summary>
            [Required]
            [Field("batch_item_no")]
            [StringLength(50)]
            public string BatchItemNo { get; set; } = default!;

            /// <summary>
            /// 批号
            /// </summary>
            [Required]
            [Field("lot_item_no")]
            [StringLength(50)]
            public string LotItemNo { get; set; } = default!;

            /// <summary>
            /// 实际库存
            /// </summary>
            [Required]
            [Field("stock")]
            public int Stock { get; set; }

            /// <summary>
            /// 出库占用库存数
            /// </summary>
            [Required]
            [Field("lock_outbound_stock")]
            public int LockOutboundStock { get; set; }

            /// <summary>
            /// 校准时间
            /// </summary>
            [Required]
            [Field("sync_time")]
            public DateTime SyncTime { get; set; }

            /// <summary>
            /// 校准差异
            /// </summary>
            [Field("is_sync_diff")]
            public bool IsSyncDiff { get; set; }


            /// <summary>
            /// 仓储校准时间
            /// </summary>
            [Field("storage_sync_time")]
            public DateTime StorageSyncTime { get; set; }

            /// <summary>
            /// 库存状态（有效库/无效库）
            /// </summary>
            [Required]
            [Field("status")]
            public int Status { get; set; }

            /// <summary>
            /// 版本号
            /// </summary>
            [Required]
            [Field("version")]
            public int Version { get; set; }

            /// <summary>
            /// 件装量
            /// </summary>
            [Required]
            [Field("packing_quantity")]
            [StringLength(10)]
            public string PackingQuantity { get; set; } = default!;

            /// <summary>
            /// 有效期
            /// </summary>
            [Required]
            [Field("validity_period")]
            public DateTime ValidityPeriod { get; set; }

            /// <summary>
            /// 生产日期
            /// </summary>
            [Required]
            [Field("production_date")]
            public DateTime ProductionDate { get; set; }

            /// <summary>
            /// 入库时间
            /// </summary>
            [Required]
            [Field("in_warehouse_time")]
            public DateTime InWarehouseTime { get; set; }

            /// <summary>
            /// 更新时间
            /// </summary>
            [Required]
            [Field("update_time")]
            public DateTime UpdateTime { get; set; }

            /// <summary>
            /// 更新人
            /// </summary>
            [Required]
            [Field("update_by")]
            public long UpdateBy { get; set; }

            /// <summary>
            /// 创建时间
            /// </summary>
            [Required]
            [Field("create_time")]
            public DateTime CreateTime { get; set; }

            /// <summary>
            /// 创建人
            /// </summary>
            [Required]
            [Field("create_by")]
            public long CreateBy { get; set; }
        }


        private readonly IRepository<User> _userRpo;
        private readonly IRepository<UserEx> _userExRpo;
        private readonly IRepository<InventoryCommodityBatchItemStock> _inventoryCommodityBatchItemStockRepository;
        private readonly IRepository<UserSharding> _userShardingRpo;
        private readonly IQueryable<User> _users;
        private readonly IQueryable<UserEx> _userExes;
        private readonly IRepository<ChannelConfig> _channelConfigRpo;

        public RepositoryTests(
            IRepository<User> userRpo,
            IRepository<UserSharding> userShardingRpo,
            IQueryable<User> users,
            IQueryable<UserEx> userExes,
            IRepository<ChannelConfig> channelConfigRpo,
            IRepository<UserEx> userExRpo,
            IRepository<InventoryCommodityBatchItemStock> inventoryCommodityBatchItemStockRepository)
        {
            _userRpo = userRpo;
            _userShardingRpo = userShardingRpo;
            _users = users;
            _userExes = userExes;
            _channelConfigRpo = channelConfigRpo;
            _userExRpo = userExRpo;
            _inventoryCommodityBatchItemStockRepository = inventoryCommodityBatchItemStockRepository;
        }

        /// <summary>
        /// 插入。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// INSERT IGNORE INTO `user`(`name`, `date`) SELECT `x`.`name`, '2026-01-30 16:31:13.142' FROM `user` AS `x` INNER JOIN `user_ex` AS `y` ON `x`.`id` = `y`.`id` ORDER BY `x`.`id` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void InsertLinq()
        {
            var linq =
                from x in _users
                join y in _userExes on x.Id equals y.Id
                orderby x.Id descending
                select new User { Name = x.Name, DateAt = DateTime.Now };

            using (var tran = new TransactionUnit())
            {
                _userRpo.Timeout(10).Ignore().Insert(linq);
            }
        }

        /// <summary>
        /// 分片插入。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// INSERT IGNORE INTO `user_2025`(`name`, `date`) SELECT `x`.`name`, NOW() FROM `user` AS `x` INNER JOIN `user_ex` AS `y` ON `x`.`id` = `y`.`id` ORDER BY `x`.`id` DESC
        /// </code>
        /// </remarks>
        [Fact]
        public void ShardingInsertLinq()
        {
            var linq =
                from x in _users
                join y in _userExes on x.Id equals y.Id
                orderby x.Id descending
                select new UserSharding { Name = x.Name, DateAt = DateTime.Now };

            using (var tran = new TransactionUnit())
            {
                _userShardingRpo.DataSharding("2025").Timeout(10).Ignore().Insert(linq);
            }
        }

        /// <summary>
        /// 更新所有。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// UPDATE `user` AS `x` SET `x`.date = NOW()
        /// </code>
        /// </remarks>
        [Fact]
        public void UpdateAll()
        {
            _userRpo.Update(x => new User { DateAt = DateTime.Now });
        }

        /// <summary>
        /// 条件更新。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// UPDATE `user` AS `x` SET `x`.date = NOW(), `x`.nullable = IFNULL(1, 0) WHERE EXISTS(SELECT `id`, `role`, `age`, `date` FROM `user_ex` AS `y` WHERE `y`.`role` = 2 AND `x`.`id` = `y`.`id`)
        /// </code>
        /// </remarks>
        [Fact]
        public void UpdateLinq()
        {
            bool? nullable = true;

            _userRpo
                .Timeout(500)
                .Where(x => _userExes.Where(y => y.RoleType == 2).Any(y => x.Id == y.Id))
                .Update(x => new User { DateAt = DateTime.Now, Nullable = nullable ?? false });
        }

        /// <summary>
        /// 条件更新。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// UPDATE `user` AS `x` SET `x`.date = NOW(), `x`.nullable = IFNULL(1, 0) WHERE EXISTS(SELECT `id`, `role`, `age`, `date` FROM `user_ex` AS `y` WHERE `y`.`role` = 2 AND `x`.`id` = `y`.`id`)
        /// </code>
        /// </remarks>
        [Fact]
        public void UpdateNullLinq()
        {
            bool? nullable = null;

            _userRpo
                .Timeout(500)
                .Where(x => _userExes.Where(y => y.RoleType == 2).Any(y => x.Id == y.Id))
                .Update(x => new User { DateAt = DateTime.Now, Nullable = nullable ?? false });
        }

        /// <summary>
        /// 条件更新。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// UPDATE `user` AS `x` SET `x`.date = NOW(), `x`.nullable = IFNULL(1, 0) WHERE EXISTS(SELECT `id`, `role`, `age`, `date` FROM `user_ex` AS `y` WHERE `y`.`role` = 2 AND `x`.`id` = `y`.`id`)
        /// </code>
        /// </remarks>
        [Fact]
        public void UpdateAddNullLinq()
        {
            int? roleType = null;

            _userExRpo
                .Timeout(500)
                .Where(x => x.RoleType == 2)
                .Update(x => new UserEx { DateAt = DateTime.Now, RoleType = x.RoleType + (roleType ?? 0), Age = 18 });
        }

        /// <summary>
        /// 删除（子查询）。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// DELETE FROM `user` AS `x` WHERE `x`.`id` IN(SELECT `y`.`id` FROM `user_ex` AS `y` WHERE `y`.`role` = 2)
        /// </code>
        /// </remarks>
        [Fact]
        public void DeleteByLinq()
        {
            _userRpo
                .Where(x => _userExes.Where(y => y.RoleType == 2).Select(y => y.Id).Contains(x.Id))
                .Delete();
        }

        /// <summary>
        /// 删除所有。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// DELETE FROM `user` AS `g`
        /// </code>
        /// </remarks>
        [Fact]
        public void DeleteAll()
        {
            _userRpo.DeleteWith();
        }

        /// <summary>
        /// 按条件删除。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// DELETE FROM `user` AS `x` WHERE `x`.`id` &lt; 100
        /// </code>
        /// </remarks>
        [Fact]
        public void DeleteByLinqWhere()
        {
            _userRpo.Delete(x => x.Id < 100);
        }

        /// <summary>
        /// 批量插入。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// INSERT IGNORE INTO `user`(`name`,`date`,`is_administrator`,`nullable`)VALUES('测试：000',NOW(),False,null),('测试：001',NOW(),False,null),...
        /// </code>
        /// </remarks>
        [Fact]
        public void Insert()
        {
            int length = 50;

            var users = new List<User>(length);

            for (int i = 0; i < length; i++)
            {
                users.Add(new User { Name = $"测试：{i:000}", DateAt = DateTime.Now });
            }
            using (var tran = new TransactionUnit())
            {
                int rows = _userRpo.Ignore().Into(users).Execute();

                Assert.True(rows == length, $"预期插入 {length} 行，实际插入 {rows} 行。");
            }
        }

        /// <summary>
        /// 分片批量插入。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// INSERT IGNORE INTO `user_2025`(`name`,`date`,`is_administrator`,`nullable`)VALUES('测试：000',NOW(),False,null),('测试：001',NOW(),False,null),...
        /// </code>
        /// </remarks>
        [Fact]
        public void ShardingInsert()
        {
            int length = 50;

            var users = new List<UserSharding>(length);

            for (int i = 0; i < length; i++)
            {
                users.Add(new UserSharding { Name = $"测试：{i:000}", DateAt = DateTime.Now });
            }
            using (var tran = new TransactionUnit())
            {
                int rows = _userShardingRpo.DataSharding("2025").Ignore().Into(users).Execute();

                Assert.True(rows == length, $"预期插入 {length} 行，实际插入 {rows} 行。");
            }
        }

        /// <summary>
        /// 批量更新。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// UPDATE `user` SET `name`='测试：000',`date`=NOW(),`is_administrator`=False,`nullable`=null WHERE `id`=256 AND `date`='2026-01-30 16:30:00.412';UPDATE `user` SET ...
        /// </code>
        /// </remarks>
        [Fact]
        public void Update()
        {
            int length = 50;

            var users = new List<User>(length);

            for (int i = 0; i < length; i++)
            {
                users.Add(
                    new User
                    {
                        Id = i + 256,
                        Name = $"测试：{i:000}",
                        DateAt = DateTime.Now.AddMinutes(i)
                    }
                );
            }

            int rows = _userRpo.UpdateTo(users).Execute();
        }

        /// <summary>
        /// 批量删除。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// DELETE FROM `user` WHERE `id`=256 AND `date`='2026-01-30 16:29:59.800';DELETE FROM `user` WHERE `id`=257 AND `date`='2026-01-30 16:30:59.800';...
        /// </code>
        /// </remarks>
        [Fact]
        public void Delete()
        {
            int length = 50;

            var users = new List<User>(length);

            for (int i = 0; i < length; i++)
            {
                users.Add(
                    new User
                    {
                        Id = i + 256,
                        Name = $"测试：{i:000}",
                        DateAt = DateTime.Now.AddMinutes(i)
                    }
                );
            }

            int rows = _userRpo.DeleteWith(users).Execute();
        }

        /// <summary>
        /// 大批量插入。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// INSERT IGNORE INTO `user`(`name`,`date`,`is_administrator`,`nullable`)VALUES('测试：000',NOW(),False,null),('测试：001',NOW(),False,null),...
        /// </code>
        /// </remarks>
        [Fact]
        public void InsertBulk()
        {
            int length = 200;

            var users = new List<User>(length);

            for (int i = 0; i < length; i++)
            {
                users.Add(new User { Name = $"测试：{i:000}", DateAt = DateTime.Now });
            }

            using (var tran = new TransactionUnit())
            {
                int rows = _userRpo.Timeout(100).Ignore().Into(users).Execute();

                Assert.True(rows == length, $"预期插入 {length} 行，实际插入 {rows} 行。");
            }
        }

        /// <summary>
        /// 大批量更新。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// UPDATE `user` SET `name`='测试：000',`date`=NOW(),`is_administrator`=False,`nullable`=null WHERE `id`=1 AND `date`='...';UPDATE `user` SET ...
        /// </code>
        /// </remarks>
        [Fact]
        public void UpdateBulk()
        {
            int length = 200;

            var users = new List<User>(length);

            for (int i = 0; i < length; i++)
            {
                users.Add(
                    new User
                    {
                        Id = i + 1,
                        Name = $"测试：{i:000}",
                        DateAt = DateTime.Now.AddMinutes(i)
                    }
                );
            }

            int rows = _userRpo.UpdateTo(users).Execute();
        }

        /// <summary>
        /// 大批量删除。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// DELETE FROM `user` WHERE `id`=1 AND `date`='2026-01-30 16:29:59.800';DELETE FROM `user` WHERE `id`=2 AND `date`='2026-01-30 16:30:59.800';...
        /// </code>
        /// </remarks>
        [Fact]
        public void DeleteBulk()
        {
            int length = 200;

            var users = new List<User>(length);

            for (int i = 0; i < length; i++)
            {
                users.Add(
                    new User
                    {
                        Id = i + 1,
                        Name = $"测试：{i:000}",
                        DateAt = DateTime.Now.AddMinutes(i)
                    }
                );
            }

            int rows = _userRpo.DeleteWith(users).Execute();
        }

        /// <summary>
        /// 更新带Trim函数。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// UPDATE `channel_config` AS `x` SET `x`.types = 2, `x`.request_address = TRIM('  http://example.com/api  '), `x`.remark = 'Updated via LINQ', `x`.update_by = 1000000000000000000, `x`.update_time = NOW() WHERE `x`.`id` = 1
        /// </code>
        /// </remarks>
        [Fact]
        public void UpdateTrim()
        {
            var inDto = new
            {
                Id = 1L,
                Types = 2,
                RequestAddress = "  http://example.com/api  ",
                Remark = "Updated via LINQ"
            };

            _channelConfigRpo
               .Where(x => x.Id == inDto.Id)
               .Update(x => new ChannelConfig
               {
                   Types = inDto.Types,
                   RequestAddress = inDto.RequestAddress.Trim(),
                   Remark = inDto.Remark,
                   UpdateBy = 1000000000000000000L,
                   UpdateTime = DateTime.Now
               });
        }

        /// <summary>
        /// 异步条件删除。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// DELETE FROM `user` AS `x` WHERE `x`.`id` &lt; 100
        /// </code>
        /// </remarks>
        [Fact]
        public async Task DeleteAsync()
        {
            await _userRpo.Where(x => x.Id < 100).DeleteAsync();
        }

        /// <summary>
        /// 异步删除所有。
        /// </summary>
        /// <remarks>
        /// 生成SQL预览:
        /// <code>
        /// DELETE FROM `user` AS `g`
        /// </code>
        /// </remarks>
        [Fact]
        public async Task DeleteAllAsync()
        {
            using (var tran = new TransactionUnit())
            {
                await _userRpo.DeleteAsync();
            }
        }

        /// <summary>
        /// 异步条件更新。
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task UpdateAsync()
        {
            var existsOut = new
            {
                Id = 1,
                Stock = 10,
                ValidityPeriod = new DateTime?(DateTime.Now.AddDays(-5)),
                PackingQuantity = new int?(20),
                ProductionDate = new DateTime?(DateTime.Now.AddDays(-10)),
                InWarehouseTime = new DateTime?(DateTime.Now.AddDays(-15)),
                LockOutboundStock = 2
            };

            var saveIn = new
            {
                Id = 1,
                Stock = new int?(5),
                ValidityPeriod = new DateTime?(DateTime.Now.AddDays(-5)),
                PackingQuantity = new int?(15),
                ProductionDate = new DateTime?(DateTime.Now.AddDays(-5)),
                InWarehouseTime = new DateTime?(DateTime.Now.AddDays(-10)),
                SyncTime = new DateTime?(DateTime.Now),
                StorageSyncTime = new DateTime?(DateTime.Now),
                LockOutboundStock = new int?(2),
                IsSyncDiff = true
            };

            bool? isSyncDiff = saveIn.IsSyncDiff;

            int diffStock = saveIn.Stock == null ? 0 : saveIn.Stock.Value - existsOut.Stock;
            int diffLockOutboundStock = saveIn.LockOutboundStock == null ? 0 : saveIn.LockOutboundStock.Value - existsOut.LockOutboundStock;
            // 判断是否需要更新额外字段（当库存更新后大于0且有效期变小时）
            var forceUpdateExtraFields = existsOut.Stock + (saveIn.Stock ?? 0) > 0
                                         && saveIn.ValidityPeriod.HasValue
                                         && saveIn.ValidityPeriod < existsOut.ValidityPeriod;

            await using (var transaction = new TransactionUnit())
            {
                await _inventoryCommodityBatchItemStockRepository
                   .Where(m => m.Id == saveIn.Id)
                   .UpdateAsync(m => new()
                   {
                       Stock = m.Stock + diffStock,
                       LockOutboundStock = m.LockOutboundStock + diffLockOutboundStock,
                       Version = m.Version + 1,
                       SyncTime = saveIn.SyncTime ?? m.SyncTime,
                       StorageSyncTime = saveIn.StorageSyncTime ?? m.StorageSyncTime,
                       IsSyncDiff = isSyncDiff ?? m.IsSyncDiff,
                       PackingQuantity = forceUpdateExtraFields && saveIn.PackingQuantity.HasValue ? saveIn.PackingQuantity.ToString() : m.PackingQuantity,
                       ValidityPeriod = forceUpdateExtraFields && saveIn.ValidityPeriod.HasValue ? saveIn.ValidityPeriod.Value : m.ValidityPeriod,
                       ProductionDate = forceUpdateExtraFields && saveIn.ProductionDate.HasValue ? saveIn.ProductionDate.Value : m.ProductionDate,
                       InWarehouseTime = forceUpdateExtraFields && saveIn.InWarehouseTime.HasValue ? saveIn.InWarehouseTime.Value : m.InWarehouseTime,
                   });


                await transaction.CompleteAsync();
            }
        }

        /// <summary>
        /// 异步条件更新。
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Update2Async()
        {
            var existsOut = new
            {
                Id = 1,
                Stock = 10,
                ValidityPeriod = new DateTime?(DateTime.Now.AddDays(-5)),
                PackingQuantity = new int?(20),
                ProductionDate = new DateTime?(DateTime.Now.AddDays(-10)),
                InWarehouseTime = new DateTime?(DateTime.Now.AddDays(-15)),
                LockOutboundStock = 2
            };

            var saveIn = new
            {
                Id = 1,
                Stock = new int?(5),
                ValidityPeriod = new DateTime?(DateTime.Now.AddDays(-5)),
                PackingQuantity = new int?(15),
                ProductionDate = new DateTime?(DateTime.Now.AddDays(-5)),
                InWarehouseTime = new DateTime?(DateTime.Now.AddDays(-10)),
                SyncTime = new DateTime?(DateTime.Now),
                StorageSyncTime = new DateTime?(DateTime.Now),
                LockOutboundStock = new int?(2),
                IsSyncDiff = true
            };

            bool? isSyncDiff = null;

            int diffStock = saveIn.Stock == null ? 0 : saveIn.Stock.Value - existsOut.Stock;
            int diffLockOutboundStock = saveIn.LockOutboundStock == null ? 0 : saveIn.LockOutboundStock.Value - existsOut.LockOutboundStock;
            // 判断是否需要更新额外字段（当库存更新后大于0且有效期变小时）
            var forceUpdateExtraFields = existsOut.Stock + (saveIn.Stock ?? 0) > 0
                                         && saveIn.ValidityPeriod.HasValue
                                         && saveIn.ValidityPeriod < existsOut.ValidityPeriod;

            await using (var transaction = new TransactionUnit())
            {
                await _inventoryCommodityBatchItemStockRepository
                   .Where(m => m.Id == saveIn.Id)
                   .UpdateAsync(m => new()
                   {
                       Stock = m.Stock + diffStock,
                       LockOutboundStock = m.LockOutboundStock + diffLockOutboundStock,
                       Version = m.Version + 1,
                       SyncTime = saveIn.SyncTime ?? m.SyncTime,
                       StorageSyncTime = saveIn.StorageSyncTime ?? m.StorageSyncTime,
                       IsSyncDiff = isSyncDiff ?? m.IsSyncDiff,
                       PackingQuantity = forceUpdateExtraFields && saveIn.PackingQuantity.HasValue ? saveIn.PackingQuantity.ToString() : m.PackingQuantity,
                       ValidityPeriod = forceUpdateExtraFields && saveIn.ValidityPeriod.HasValue ? saveIn.ValidityPeriod.Value : m.ValidityPeriod,
                       ProductionDate = forceUpdateExtraFields && saveIn.ProductionDate.HasValue ? saveIn.ProductionDate.Value : m.ProductionDate,
                       InWarehouseTime = forceUpdateExtraFields && saveIn.InWarehouseTime.HasValue ? saveIn.InWarehouseTime.Value : m.InWarehouseTime,
                   });


                await transaction.CompleteAsync();
            }
        }

        /// <summary>
        /// 异步条件更新。
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Update3Async()
        {
            var existsOut = new
            {
                Id = 117040,
                Stock = 123123,
                ValidityPeriod = new DateTime?(new DateTime(2027, 4, 14)),
                LockOutboundStock = 0,
                Version = 85
            };

            var saveIn = new
            {
                Id = 117040,
                Stock = new int?(123123),
                ValidityPeriod = new DateTime?(new DateTime(2027, 4, 14)),
                PackingQuantity = new int?(0),
                ProductionDate = new DateTime?(new DateTime(2027, 4, 15)),
                InWarehouseTime = new DateTime?(new DateTime(2024, 12, 5)),
                SyncTime = new DateTime?(new DateTime(2026, 4, 8)),
                StorageSyncTime = new DateTime?(new DateTime(2026, 4, 8)),
                LockOutboundStock = new int?()
            };

            int diffStock = saveIn.Stock == null ? 0 : saveIn.Stock.Value - existsOut.Stock;
            int diffLockOutboundStock = saveIn.LockOutboundStock == null ? 0 : saveIn.LockOutboundStock.Value - existsOut.LockOutboundStock;
            // 判断是否需要更新额外字段（当库存更新后大于0且有效期变小时）
            /*             var forceUpdateExtraFields = existsOut.Stock + (saveIn.Stock ?? 0) > 0
                                                    && saveIn.ValidityPeriod.HasValue
                                                    && saveIn.ValidityPeriod < existsOut.ValidityPeriod; */
            var forceUpdateExtraFields = true;

            bool? isSyncDiff = null;
            if (saveIn.SyncTime.HasValue)
            {
                isSyncDiff = diffStock != 0 || diffLockOutboundStock != 0;
            }

            int? version = existsOut.Version;
            var syncTime = saveIn.SyncTime;
            var predicate = BuildBatchItemUpdatePredicate(existsOut.Id, version, saveIn.SyncTime);

            await using (var transaction = new TransactionUnit())
            {
                await _inventoryCommodityBatchItemStockRepository
                   .Where(predicate)
                   .UpdateAsync(m => new()
                   {
                       Stock = m.Stock + diffStock,
                       LockOutboundStock = m.LockOutboundStock + diffLockOutboundStock,
                       Version = m.Version + 1,
                       SyncTime = saveIn.SyncTime ?? m.SyncTime,
                       StorageSyncTime = saveIn.StorageSyncTime ?? m.StorageSyncTime,
                       IsSyncDiff = isSyncDiff ?? m.IsSyncDiff,
                       PackingQuantity = forceUpdateExtraFields && saveIn.PackingQuantity.HasValue ? saveIn.PackingQuantity.ToString() : m.PackingQuantity,
                       ValidityPeriod = forceUpdateExtraFields && saveIn.ValidityPeriod.HasValue ? saveIn.ValidityPeriod.Value : m.ValidityPeriod,
                       ProductionDate = forceUpdateExtraFields && saveIn.ProductionDate.HasValue ? saveIn.ProductionDate.Value : m.ProductionDate,
                       InWarehouseTime = forceUpdateExtraFields && saveIn.InWarehouseTime.HasValue ? saveIn.InWarehouseTime.Value : m.InWarehouseTime,
                   });


                await transaction.CompleteAsync();
            }
        }

        /// <summary>
        /// 构建更新条件表达式
        /// </summary>
        /// <param name="id">记录ID</param>
        /// <param name="version">版本号（null表示不检查版本）</param>
        /// <param name="syncTime">校准时间（null表示不检查校准时间）</param>
        /// <returns>更新条件表达式</returns>
        private static System.Linq.Expressions.Expression<Func<InventoryCommodityBatchItemStock, bool>> BuildBatchItemUpdatePredicate(
            long id,
            long? version,
            DateTime? syncTime)
        {
            System.Linq.Expressions.Expression<Func<InventoryCommodityBatchItemStock, bool>> predicate = m => m.Id == id;

            // 如果指定了版本号，添加版本检查
            if (version.HasValue)
            {
                predicate = predicate.And(m => m.Version == version.Value);
            }

            // 如果指定了校准时间，添加校准时间检查
            if (syncTime.HasValue)
            {
                predicate = predicate.And(m => syncTime >= m.SyncTime);
            }

            return predicate;
        }
    }
}
