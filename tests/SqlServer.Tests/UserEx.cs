using Inkslab.Linq.Annotations;
using System;
using System.ComponentModel.DataAnnotations;

namespace SqlServer.Tests
{
    public class UserEx
    {
        [Key]
        public int Id { get; set; }
        [Field("Role")]
        public int RoleType { get; set; }
        public int Age { get; set; }
        [Field("Date")]
        public DateTime DateAt { get; set; }
    }
}
