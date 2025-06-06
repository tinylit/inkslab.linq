using System;
using System.ComponentModel.DataAnnotations;
using Inkslab.Linq.Annotations;

namespace Inkslab.Linq.Tests
{
    [Table("user")]
    public class User
    {
        [Key]
        [Field("id")]
        [DatabaseGenerated]
        public int Id { get; set; }

        [Field("name")]
        public string Name { get; set; }

        [Version]
        [Field("date")]
        public DateTime DateAt { get; set; }

        /// <summary>
        /// 是否管理员。
        /// </summary>
        [Field("is_administrator")]
        public bool IsAdministrator { get; set; }

        /// <summary>
        /// 可空。
        /// </summary>
        [Field("nullable")]
        public bool? Nullable { get; set; }
    }

    [Table("user_[sharding]")]
    public class UserSharding : User { }
}
