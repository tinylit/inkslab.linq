using Inkslab.Linq.Annotations;
using System;
using System.ComponentModel.DataAnnotations;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// 商品锁库表
    /// inventory_commodity_stock
    /// </summary>
    [Table("inventory_commodity_stock")]
    public class InventoryCommodityStock
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 医贸编号
        /// </summary>
        [Required]
        [Field("medical_trade_no")]
        [StringLength(25)]
        public string MedicalTradeNo { get; set; } = default!;

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
        /// ERP组合Id 格式：{医贸编号}-{货主编码}-{erp商品ID}
        /// </summary>
        [Required]
        [Field("commodity_link_id")]
        [StringLength(67)]
        public string CommodityLinkId { get; set; } = default!;

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
        [Required]
        [Field("erp_product_code")]
        [StringLength(20)]
        public string ErpProductCode { get; set; } = default!;

        /// <summary>
        /// 实际库存
        /// </summary>
        [Field("stock")]
        public int Stock { get; set; }

        /// <summary>
        /// 虚拟库存
        /// </summary>
        [Field("virtual_stock")]
        public int VirtualStock { get; set; }

        /// <summary>
        /// 占用库存
        /// </summary>
        [Field("lock_stock")]
        public int LockStock { get; set; }

        /// <summary>
        /// 虚拟仓库占用数量
        /// </summary>
        [Field("lock_virtual_warehouse_stock")]
        public int LockVirtualWarehouseStock { get; set; }

        /// <summary>
        /// 出库占用库存数
        /// </summary>
        [Field("lock_outbound_stock")]
        public int LockOutboundStock { get; set; }

        /// <summary>
        /// 占用偏移
        /// </summary>
        [Field("lock_offset_stock")]
        public int LockOffsetStock { get; set; }

        /// <summary>
        /// 效期库存
        /// </summary>
        [Field("lock_validity_period_stock")]
        public int LockValidityPeriodStock { get; set; }

        /// <summary>
        /// 效期占用库存
        /// </summary>
        [Field("lock_validity_period_lock_stock")]
        public int LockValidityPeriodLockStock { get; set; }

        /// <summary>
        /// 效期分配占用：给某一个效期活动 单独划分出 库存时的总数量
        /// </summary>
        [Field("lock_validity_period_allocation_stock")]
        public int LockValidityPeriodAllocationStock { get; set; }

        /// <summary>
        /// 校准时间
        /// </summary>
        [Field("sync_time")]
        public DateTime SyncTime { get; set; }

        /// <summary>
        /// 版本号（乐观锁）
        /// </summary>
        [Field("version")]
        public int Version { get; set; }

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
        /// 普通商品可用库存
        /// </summary>
        /// <returns></returns>
        public int NormalAvailableStock()
        {
            return Stock + VirtualStock - LockStock - LockVirtualWarehouseStock - LockOutboundStock - LockValidityPeriodAllocationStock;
        }

        /// <summary>
        /// 效期商品可用库存
        /// </summary>
        /// <returns></returns>
        public int PeriodAvailableStock()
        {
            return LockValidityPeriodStock - LockValidityPeriodLockStock;
        }
    }
}