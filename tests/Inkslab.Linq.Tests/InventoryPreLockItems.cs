using System;
using System.ComponentModel.DataAnnotations;
using Inkslab.Linq.Annotations;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// 商品库存预占用明细表
    /// inventory_pre_lock_items
    /// </summary>
    [Table("inventory_pre_lock_items")]
    public class InventoryPreLockItems
    {
        /// <summary>
        /// 主键ID（自增）
        /// </summary>
        [Key]
        [Field("id")]
        [DatabaseGenerated]
        public long Id { get; set; }

        /// <summary>
        /// 预锁ID
        /// </summary>
        [Required]
        [Field("inventory_pre_lock_id")]
        public long InventoryPreLockId { get; set; }

        /// <summary>
        /// 医贸编号
        /// </summary>
        [Required]
        [Field("medical_trade_no")]
        [StringLength(25)]
        public string MedicalTradeNo { get; set; }

        /// <summary>
        /// 货主编号
        /// </summary>
        [Required]
        [Field("cargo_owner_no")]
        [StringLength(25)]
        public string CargoOwnerNo { get; set; }

        /// <summary>
        /// 仓库编号
        /// </summary>
        [Required]
        [Field("warehouse_no")]
        [StringLength(25)]
        public string WarehouseNo { get; set; }

        /// <summary>
        /// 业务id
        /// </summary>
        [Required(AllowEmptyStrings = true)]
        [Field("virtual_business_no")]
        [StringLength(100)]
        public string VirtualBusinessNo { get; set; } = "";

        /// <summary>
        /// 虚拟仓库Id
        /// </summary>
        [Required]
        [Field("virtual_warehouse_id")]
        public long VirtualWarehouseId { get; set; }

        /// <summary>
        /// 锁定类型
        /// </summary>
        [Required]
        [Field("lock_type")]
        public int LockType { get; set; }

        /// <summary>
        /// ERP商品ID
        /// </summary>
        [Required]
        [Field("erp_product_id")]
        [StringLength(15)]
        public string ErpProductId { get; set; }

        /// <summary>
        /// 占用库存
        /// </summary>
        [Required]
        [Field("lock_stock")]
        public int LockStock { get; set; }

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
}
