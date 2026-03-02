using System;
using System.ComponentModel.DataAnnotations;
using Inkslab.Linq.Annotations;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// 库存预占用表
    /// inventory_pre_lock
    /// </summary>
    [Table("inventory_pre_lock")]
    public class InventoryPreLock
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 应用编码
        /// </summary>
        [Required]
        [Field("business_line_code")]
        [StringLength(25)]
        public string BusinessLineCode { get; set; }

        /// <summary>
        /// 业务单据编号
        /// </summary>
        [Required]
        [Field("business_no")]
        [StringLength(50)]
        public string BusinessNo { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        [Required]
        [Field("status")]
        public int Status { get; set; }

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
