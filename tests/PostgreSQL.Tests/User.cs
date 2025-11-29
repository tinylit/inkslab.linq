using System;
using System.ComponentModel.DataAnnotations;
using Inkslab.Linq.Annotations;

namespace PostgreSQL.Tests
{
    /// <summary>
    /// 用户实体类
    /// </summary>
    [Table("users")]
    public class User
    {
        /// <summary>
        /// 用户主键ID（自增）
        /// </summary>
        [Key]
        [Field("id")]
        public int Id { get; set; }

        /// <summary>
        /// 用户名称
        /// </summary>
        [Field("name")]
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// 用户邮箱（唯一约束）
        /// </summary>
        [Field("email")]
        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string Email { get; set; }

        /// <summary>
        /// 用户年龄
        /// </summary>
        [Field("age")]
        public int? Age { get; set; }

        /// <summary>
        /// 用户薪资
        /// </summary>
        [Field("salary")]
        public decimal? Salary { get; set; }

        /// <summary>
        /// 是否激活（默认为true）
        /// </summary>
        [Field("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 创建时间（默认为当前时间戳）
        /// </summary>
        [Field("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
