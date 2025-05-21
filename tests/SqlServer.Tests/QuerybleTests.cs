using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Inkslab.Linq;
using Inkslab.Linq.Annotations;
using Xunit;

namespace SqlServer.Tests
{
    public class QuerybleTests
    {
        #region 实体。
        /// <summary>
        /// 活动表
        /// </summary>
        [Table("Activity")]
        public partial class Activity
        {
            /// <summary>
            /// 主键
            /// </summary>
            [Key]
            public long Id { get; set; }
            /// <summary>
            /// 名称
            /// </summary>
            [Field("Name")]
            public string Name { get; set; }
            /// <summary>
            /// 活动类型 1：特价。2：满减。3：满赠。4：满返。5：秒杀。6：效期。7：换购。8：聚划算。9：试用品
            /// </summary>
            public int Type { get; set; }
            /// <summary>
            /// 终端类型：
            ///              0：不限。
            ///              1：客户端。
            ///              128：业务员端。
            /// </summary>
            public int TerminalType { get; set; }
            /// <summary>
            /// 优惠类型
            ///                    0：无。
            /// 
            ///                    特价或效期：
            ///                    1：直降。
            ///                    2：折扣。
            /// 
            ///                    满减：
            ///                    11：金额满减。
            ///                    12：金额每满减。
            ///                    13：数量满减。
            ///                    14：数量每满减
            /// 
            ///                    满赠：
            ///                    21：金额满赠。
            ///                    22：金额每满赠。
            ///                    23：数量满赠。
            ///                    24：数量每满赠。
            /// 
            ///                    满返。
            ///                    31：金额满返。
            ///                    32：金额每满返。
            ///                    33：数量满返
            ///                    34：数量每满返
            /// 
            ///                    换购。
            ///                    41：金额满换。
            ///                    42：金额每满换。
            ///                    43：数量满换。
            ///                    44：数量每满换。
            /// </summary>
            public int DiscountType { get; set; }
            /// <summary>
            /// 0：全部
            ///              1：诊所
            ///              2：卫生室/卫生站
            ///              4：卫生院
            ///              8：单体药店
            ///              16：连锁药店
            ///              32：民营医院
            ///              64：公立医院
            ///              128：连锁总部
            ///              256：批发公司
            /// </summary>
            public int CustomerTypes { get; set; }
            /// <summary>
            /// 是否限制区域，选全国时，为不限区域。
            /// </summary>
            public bool IsLimitArea { get; set; }
            /// <summary>
            /// 客户规则限制类型 0：不限；1：核心客户；2：指定客户；3：动态客户。
            /// </summary>
            public int RestrictionType { get; set; }
            /// <summary>
            /// 是否整单活动
            /// </summary>
            public bool IsOrderActivity { get; set; }

            /// <summary>
            /// 是否增长活动
            /// </summary>
            public bool IsGrowth { get; set; }
            /// <summary>
            /// 是否未购进活动
            /// </summary>
            public bool IsNotPurchased { get; set; }
            /// <summary>
            /// 是否指定商品，限制商品时，仅活动商品表的商品可参与活动
            /// </summary>
            public bool IsLimitCommodity { get; set; }
            /// <summary>
            /// 有无倒计时提醒
            /// </summary>
            public bool HasCountdownReminder { get; set; }
            /// <summary>
            /// 无子活动时，子活动id和活动id相同。
            /// </summary>
            public bool HasChildren { get; set; }

            /// <summary>
            /// 是否必买换购品
            /// </summary>
            public bool IsMustTradeIn { get; set; }
            /// <summary>
            /// 开始时间
            /// </summary>
            public DateTime StartTime { get; set; }
            /// <summary>
            /// 结束时间
            /// </summary>
            public DateTime EndTime { get; set; }
            /// <summary>
            /// 倒计时开始时间
            /// </summary>
            public DateTime CountdownStartTime { get; set; }
            /// <summary>
            /// 倒计时结束时间
            /// </summary>
            public DateTime CountdownEndTime { get; set; }
            /// <summary>
            /// 状态：
            ///              0：准备。
            ///              1：准备完成。
            ///              2：计算。
            ///              3：计算完成。
            /// </summary>
            public int ReadyStatus { get; set; }
            /// <summary>
            /// 计算完成方可启用。
            /// </summary>
            public bool IsEnable { get; set; }
            /// <summary>
            /// 特价/秒杀 品种数限制
            /// </summary>
            public long PurchaseLimit { get; set; }
            /// <summary>
            /// 创建人
            /// </summary>
            public long CreateBy { get; set; }
            /// <summary>
            /// 创建时间
            /// </summary>
            public DateTime CreateTime { get; set; }
        }

        /// <summary>
        /// 客户活动表:按照客户id分表
        /// </summary>
        [Table("CustomerActivity[sharding_key]")]
        public class CustomerActivity
        {
            /// <summary>
            /// ID
            /// </summary>
            public long Id { get; set; }
            /// <summary>
            /// 事业部ID
            /// </summary>
            public long DivisionId { get; set; }
            /// <summary>
            /// 客户ID
            /// </summary>
            public long CustomerId { get; set; }
            /// <summary>
            /// 活动ID
            /// </summary>
            public long ActivityId { get; set; }
            /// <summary>
            /// 终端类型
            /// </summary>
            public int TerminalType { get; set; }
        }

        #endregion
        private readonly IQueryable<Activity> _activities;
        private readonly IQueryable<CustomerActivity> _customerActivities;

        public QuerybleTests(IQueryable<Activity> activities, IQueryable<CustomerActivity> customerActivities)
        {
            _activities = activities;
            _customerActivities = customerActivities;
        }

        [Fact]
        public async Task Test1Async()
        {
            var now = DateTime.Now;
            var activityIds = new List<long> { 7330139853482360915 };

            var activities = await _activities
                        .Where(s => activityIds.Contains(s.Id) && s.EndTime > now && s.IsEnable)
                        .OrderByDescending(s => s.Id)
                        .ToListAsync();
        }

        [Fact]
        public async Task Test2Async()
        {
            long divisionId = 1;
            long customerId = 1;
            long activityId = 1;
            var terminalTypes = new List<int> { 0, 1 };

            var flag = await _customerActivities.DataSharding("000")
                    .Where(s => s.CustomerId == customerId && s.ActivityId == activityId && s.DivisionId == divisionId && terminalTypes.Contains(s.TerminalType))
                    .AnyAsync();
        }
    }
}