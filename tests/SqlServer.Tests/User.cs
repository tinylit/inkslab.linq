using System;
using System.ComponentModel.DataAnnotations;
using Inkslab.Linq.Annotations;

namespace SqlServer.Tests
{
    public class User
    {
        [Key]
        [DatabaseGenerated]
        public int Id { get; set; }

        public string Name { get; set; }

        [Version]
        public DateTime DateAt { get; set; }

        /// <summary>
        /// 是否管理员。
        /// </summary>
        public bool IsAdministrator { get; set; }

        /// <summary>
        /// 可空。
        /// </summary>
        public bool? Nullable { get; set; }
    }
}
