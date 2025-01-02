using Inkslab.Linq.Annotations;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Inkslab.Linq.Tests
{
    [Table("user")]
    public class User
    {
        [Key]
        [Field("id")]
        [ReadOnly(true)]
        public int Id { get; set; }
        [Field("name")]
        public string Name { get; set; }
        [Version]
        [Field("date")]
        public DateTime DateAt { get; set; }
    }
}
