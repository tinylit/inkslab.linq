using System;
using System.ComponentModel.DataAnnotations;
using Inkslab.Linq;
using Inkslab.Linq.Annotations;

namespace PostgreSQL.Tests
{
    /// <summary>
    ///     已发送消息。
    /// </summary>
    [Table("delivery_[yyyyMM]_snt")]
    public class DeliverySnt
    {
        /// <summary>
        ///     主键。
        /// </summary>
        [Key]
        [Field("id")]
        [DatabaseGenerated]
        public long Id { get; set; }

        /// <summary>
        ///     模板标识。
        /// </summary>
        [Field("delivery_id")]
        public long DeliveryId { get; set; }

        /// <summary>
        ///     数据源标识。
        /// </summary>
        [Field("data_source_id")]
        public long DataSourceId { get; set; }

        /// <summary>
        ///     持续时间（毫秒）。
        /// </summary>
        [Field("duration")]
        public long Duration { get; set; }

        /// <summary>
        ///     内容。
        /// </summary>
        [Field("request_content")]
        public JsonbPayload RequestContent { get; set; }

        /// <summary>
        ///     响应内容。
        /// </summary>
        [Field("response_content")]
        public string ResponseContent { get; set; }

        /// <summary>
        ///     添加时间。
        /// </summary>
        [Field("added")]
        public DateTime Added { get; set; }
    }
}