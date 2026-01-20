using Inkslab.Linq.Annotations;
using System;
using System.ComponentModel.DataAnnotations;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// 商品库存预占用表
    /// </summary>
    [Table("inventory_commodity_stock_prelock")]
    public class InventoryCommodityStockPreLock
    {
        /// <summary>
        /// 主键。
        /// </summary>
        [Key]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 应用编码。
        /// </summary>
        [Field("application_code")]
        [StringLength(50)]
        public string ApplicationCode { get; set; }

        /// <summary>
        /// 医贸编号。
        /// </summary>
        [Field("medical_trade_no")]
        [StringLength(25)]
        public string MedicalTradeNo { get; set; }

        /// <summary>
        /// 货主编号。
        /// </summary>
        [Field("cargo_owner_no")]
        [StringLength(25)]
        public string CargoOwnerNo { get; set; }

        /// <summary>
        /// 仓库编号。
        /// </summary>
        [Field("warehouse_no")]
        [StringLength(25)]
        public string WarehouseNo { get; set; }

        /// <summary>
        /// 订单号，占用库存的依据。
        /// </summary>
        [Field("order_no")]
        [StringLength(50)]
        public string OrderNo { get; set; }

        /// <summary>
        /// 业务id。
        /// </summary>
        [Field("virtual_business_id")]
        [StringLength(36)]
        public string VirtualBusinessId { get; set; } = "";

        /// <summary>
        /// 占用类型
        /// </summary>
        [Field("lock_type")]
        public int LockType { get; set; }

        /// <summary>
        /// ERP商品ID。
        /// </summary>
        [Field("erp_product_id")]
        [StringLength(15)]
        public string ErpProductId { get; set; }

        /// <summary>
        /// 占用库存。
        /// </summary>
        [Field("lock_stock")]
        public int LockStock { get; set; }

        /// <summary>
        /// 创建数据时为false，操作redis成功后修改为true。
        /// </summary>
        [Field("is_valid")]
        public bool IsValid { get; set; }

        /// <summary>
        /// 确认后才更新库存。
        /// </summary>
        [Field("is_confirm")]
        public bool IsConfirm { get; set; }

        /// <summary>
        /// 更新时间。
        /// </summary>
        [Field("update_time")]
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 更新人。
        /// </summary>
        [Field("update_by")]
        public long UpdateBy { get; set; }

        /// <summary>
        /// 创建时间。
        /// </summary>
        [Field("create_time")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 创建人。
        /// </summary>
        [Field("create_by")]
        public long CreateBy { get; set; }
    }
}