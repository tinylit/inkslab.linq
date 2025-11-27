using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Inkslab.Linq;
using Inkslab.Linq.Annotations;
using Newtonsoft.Json.Linq;

namespace PostgreSQL.Tests
{

    [Table("user_contents")]
    public class UserContentsOfJsonDocument
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
        public JsonDocument Content { get; set; }
    }

    [Table("user_contents")]
    public class UserContentsOfJObject
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
        public JObject Content { get; set; }
    }

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