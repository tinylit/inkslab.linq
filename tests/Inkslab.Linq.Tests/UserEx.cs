using Inkslab.Linq.Annotations;
using System;
using System.ComponentModel.DataAnnotations;

namespace Inkslab.Linq.Tests
{
    [Table("user_ex")]
    public class UserEx
    {
        [Key]
        [Field("id")]
        public int Id { get; set; }
        [Field("role")]
        public int RoleType { get; set; }
        [Field("age")]
        public int Age { get; set; }
        [Field("date")]
        public DateTime DateAt { get; set; }
    }
}
