using System.ComponentModel.DataAnnotations;
using Inkslab.Linq;
using Inkslab.Linq.Annotations;

namespace PostgreSQL.Tests
{
    [Table("user_contents")]
    public class UserContentsOfJsonbPayload
    {
        /// <summary>
        /// 主键ID。
        /// </summary>
        [Key]
        [Field("id")]
        [DatabaseGenerated]
        public int Id { get; set; }

        /// <summary>
        /// 内容。
        /// </summary>
        [Field("content")]
        public JsonbPayload Content { get; set; }
    }
}