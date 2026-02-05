using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Inkslab.Linq.Annotations;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// 仓储测试
    /// </summary>
    public class MyRepositoryTests
    {
        /// <summary>
        /// 商品批号批号库存表
        /// inventory_commodity_lot_item
        /// </summary>
        [Table("inventory_commodity_lot_item_stock")]
        public class InventoryCommodityLotItemStock
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
            [StringLength(15)]
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

        private readonly IRepository<InventoryCommodityLotItemStock> _inventoryCommodityLotItemStockRepository;

        public MyRepositoryTests(IRepository<InventoryCommodityLotItemStock> inventoryCommodityLotItemStockRepository)
        {
            _inventoryCommodityLotItemStockRepository = inventoryCommodityLotItemStockRepository;
        }

        /// <summary>
        /// 测试：增量更新库存（UpdateAsync）
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task TestAsync()
        {
            var shouldUpdateExtraFields = false;

            var saveIn = new
            {
                Id = 97473,
                MedicalTradeNo = "MTN123",
                ErpProductId = "EPI456",
                ErpProductCode = "EPC789",
                CargoOwnerNo = "CON321",
                WarehouseNo = "WN654",
                LotItemNo = "LI987",
                Stock = (int?)100,
                LockOutboundStock = (int?)20,
                SyncTime = (DateTime?)null,
                Version = 1,
                PackingQuantity = (int?)null,
                ValidityPeriod = (DateTime?)null,
                ProductionDate = (DateTime?)null,
                InWarehouseTime = (DateTime?)null,
                UpdateTime = DateTime.UtcNow,
                UpdateBy = 12345,
                CreateTime = DateTime.UtcNow.AddDays(-30),
                CreateBy = 12345
            };

            // 增量更新库存
            await _inventoryCommodityLotItemStockRepository
               .Where(m => m.Id == saveIn.Id)
                   .UpdateAsync(m => new()
                   {
                       Stock = m.Stock + (saveIn.Stock ?? 0),
                       LockOutboundStock = m.LockOutboundStock + (saveIn.LockOutboundStock ?? 0),
                       PackingQuantity = shouldUpdateExtraFields && saveIn.PackingQuantity.HasValue ? saveIn.PackingQuantity.ToString() : m.PackingQuantity,
                       ValidityPeriod = shouldUpdateExtraFields && saveIn.ValidityPeriod.HasValue ? saveIn.ValidityPeriod.Value : m.ValidityPeriod,
                       ProductionDate = shouldUpdateExtraFields && saveIn.ProductionDate.HasValue ? saveIn.ProductionDate.Value : m.ProductionDate,
                       InWarehouseTime = shouldUpdateExtraFields && saveIn.InWarehouseTime.HasValue ? saveIn.InWarehouseTime.Value : m.InWarehouseTime,
                       Version = m.Version + 1,
                       SyncTime = saveIn.SyncTime ?? m.SyncTime
                   });
        }
    }
}