using System;
using System.ComponentModel.DataAnnotations;
using Inkslab.Linq.Annotations;

namespace Inkslab.Linq.Tests
{

    /// <summary>
    /// 发布。
    /// </summary>
    [Table("published")]
    public class Published
    {
        /// <summary>
        /// id
        /// </summary>
        [Key]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 路由键
        /// </summary>
        [Field("routing_key")]
        public string RoutingKey { get; set; }

        /// <summary>
        /// 交换机。
        /// </summary>
        [Field("exchange_name")]
        public string ExchangeName { get; set; }

        /// <summary>
        /// 交换机类型。
        /// </summary>
        [Field("exchange_type")]
        public int ExchangeType { get; set; }

        /// <summary>
        /// 状态。
        /// </summary>
        [Field("status")]
        public int Status { get; set; }

        /// <summary>
        /// 消费者数量。
        /// </summary>
        [Field("number_of_consumers")]
        public int NumberOfConsumers { get; set; }

        /// <summary>
        /// 成功消费者数量。
        /// </summary>
        [Field("number_of_successful_consumers")]
        public int NumberOfSuccessfulConsumers { get; set; }

        /// <summary>
        /// 消息头。
        /// </summary>
        [Field("head")]
        public string Head { get; set; }

        /// <summary>
        /// 内容。
        /// </summary>
        [Field("content")]
        public string Content { get; set; }

        /// <summary>
        /// 添加时间。
        /// </summary>
        [Field("added")]
        public DateTime Added { get; set; }

        /// <summary>
        /// 交付时间。
        /// </summary>
        [Field("deliver_time")]
        public DateTime DeliverTime { get; set; }

        /// <summary>
        /// 过期时间。
        /// </summary>
        [Field("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// 版本。
        /// </summary>
        [Version]
        [Field("version")]
        public long Version { get; set; }
    }
}