using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Inkslab.Linq.Annotations;
using Xunit;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;

namespace Inkslab.Linq.Tests
{
    #region 实体
    /// <summary>
    /// 用户表
    /// </summary>
    [Table("my_user")]
    public partial class MyUser
    {
        /// <summary>
        /// id
        /// </summary>
        [Key]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 认证中心用户id
        /// </summary>
        [Field("authentication_user_id")]
        public long AuthenticationUserId { get; set; }

        /// <summary>
        /// 账号
        /// </summary>
        [Field("account")]
        [StringLength(50)]
        public string Account { get; set; } = null!;

        /// <summary>
        /// 姓名
        /// </summary>
        [Field("name")]
        [StringLength(25)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// 手机号
        /// </summary>
        [Field("phone")]
        [StringLength(15)]
        public string Phone { get; set; } = null!;

        /// <summary>
        /// 类型
        ///              1024、后台用户。
        ///              1、咨询客户。
        ///              512、专家。
        /// </summary>
        [Field("types")]
        public int Types { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [Field("is_valid")]
        public bool IsValid { get; set; }

        /// <summary>
        /// 是否删除
        /// </summary>
        [Field("is_delete")]
        public bool IsDelete { get; set; }

        /// <summary>
        /// 禁用时间
        /// </summary>
        [Field("disable_time")]
        [MaxLength(6)]
        public DateTime? DisableTime { get; set; }

        /// <summary>
        /// 创建人id
        /// </summary>
        [Field("create_by")]
        public long CreateBy { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Field("create_time")]
        [MaxLength(6)]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新人id
        /// </summary>
        [Field("update_by")]
        public long? UpdateBy { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Field("update_time")]
        [MaxLength(6)]
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 删除人id
        /// </summary>
        [Field("delete_by")]
        public long? DeleteBy { get; set; }

        /// <summary>
        /// 删除时间
        /// </summary>
        [Field("delete_time")]
        [MaxLength(6)]
        public DateTime? DeleteTime { get; set; }
    }

    /// <summary>
    /// 业务咨询表
    /// </summary>
    [Table("business_consultation_rep")]
    public partial class BusinessConsultationRep
    {
        /// <summary>
        /// 自增主键id
        /// </summary>
        [Key]
        [DatabaseGenerated]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 业务线编码
        /// </summary>
        [Field("business_line_id")]
        public long BusinessLineId { get; set; }

        /// <summary>
        /// 编码
        /// </summary>
        [Field("code")]
        [StringLength(2)]
        public string Code { get; set; } = null!;

        /// <summary>
        /// 业务名称
        /// </summary>
        [Field("name")]
        [StringLength(20)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// 咨询时长
        /// </summary>
        [Field("consult_duration")]
        public int ConsultDuration { get; set; }

        /// <summary>
        /// 退款时长
        /// </summary>
        [Field("refund_duration")]
        public int RefundDuration { get; set; }

        /// <summary>
        /// 招呼语
        /// </summary>
        [Field("call")]
        [StringLength(100)]
        public string Call { get; set; } = null!;

        /// <summary>
        /// 创建人id
        /// </summary>
        [Field("create_by")]
        public long CreateBy { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Field("create_time")]
        [MaxLength(6)]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新人id
        /// </summary>
        [Field("update_by")]
        public long UpdateBy { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Field("update_time")]
        [MaxLength(6)]
        public DateTime UpdateTime { get; set; }
    }

    /// <summary>
    /// 业务科室专家咨询类型关系表
    /// </summary>
    [Table("business_department_consultation_rel")]
    public class BusinessDepartmentConsultationRel
    {
        /// <summary>
        /// 自增主键id
        /// </summary>
        [Key]
        [DatabaseGenerated]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 科室Id
        /// </summary>
        [Field("business_department_id")]
        public long BusinessDepartmentId { get; set; }

        /// <summary>
        /// 专家ID
        /// </summary>
        [Field("specialist_id")]
        public long SpecialistId { get; set; }

        /// <summary>
        /// 业务线ID
        /// </summary>
        [Field("business_line_id")]
        public long BusinessLineId { get; set; }

        /// <summary>
        /// 咨询ID
        /// </summary>
        [Field("business_consultation_id")]
        public long BusinessConsultationId { get; set; }
    }

    /// <summary>
    /// 业务科室专家关系表
    /// </summary>
    [Table("business_department_rel")]
    public partial class BusinessDepartmentRel
    {
        /// <summary>
        /// 自增主键id
        /// </summary>
        [Key]
        [DatabaseGenerated]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 科室Id
        /// </summary>
        [Field("business_department_id")]
        public long BusinessDepartmentId { get; set; }

        /// <summary>
        /// 专家ID
        /// </summary>
        [Field("specialist_id")]
        public long SpecialistId { get; set; }

        /// <summary>
        /// 业务线ID
        /// </summary>
        [Field("business_line_id")]
        public long BusinessLineId { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        [Field("sort")]
        public long Sort { get; set; }
    }


    /// <summary>
    /// 专家费用信息表
    /// </summary>
    [Table("specialist_cost_rep")]
    public partial class SpecialistCostRep
    {
        /// <summary>
        /// 自增主键id
        /// </summary>
        [Key]
        [DatabaseGenerated]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 专家ID
        /// </summary>
        [Field("specialist_id")]
        public long SpecialistId { get; set; }

        /// <summary>
        /// 费用类型
        /// </summary>
        [Field("cost_type")]
        public int CostType { get; set; }

        /// <summary>
        /// 费用
        /// </summary>
        [Field("cost")]
        public decimal Cost { get; set; }

        /// <summary>
        /// 是否支持
        /// </summary>
        [Field("is_support")]
        public bool IsSupport { get; set; }
    }


    /// <summary>
    /// 专家表
    /// </summary>
    [Table("specialist")]
    public partial class Specialist
    {
        /// <summary>
        /// 用户id
        /// </summary>
        [Key]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 头像地址
        /// </summary>
        [Field("head_url")]
        [StringLength(300)]
        public string HeadUrl { get; set; } = null!;

        /// <summary>
        /// 简介
        /// </summary>
        [Field("introduction")]
        [StringLength(500)]
        public string Introduction { get; set; } = null!;

        /// <summary>
        /// 值班时间
        /// </summary>
        [Field("duty_time")]
        [StringLength(200)]
        public string DutyTime { get; set; } = null!;

        /// <summary>
        /// 在线状态
        /// </summary>
        [Field("is_online")]
        public bool IsOnline { get; set; }
    }

    /// <summary>
    /// 订单
    /// </summary>
    [Table("order")]
    public partial class Order
    {
        /// <summary>
        /// id
        /// </summary>
        [Key]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 业务线id
        /// </summary>
        [Field("business_line_id")]
        public long BusinessLineId { get; set; }

        /// <summary>
        /// 订单编号
        /// </summary>
        [Field("code")]
        [StringLength(50)]
        public string Code { get; set; }

        /// <summary>
        /// 用户id
        /// </summary>
        [Field("user_id")]
        public long UserId { get; set; }

        /// <summary>
        /// 客户编号
        /// </summary>
        [Field("customer_code")]
        [StringLength(36)]
        public string CustomerCode { get; set; }

        /// <summary>
        /// 咨询费用
        /// </summary>
        [Field("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// 付费方式
        /// </summary>
        [Field("payment_method")]
        public int PaymentMethod { get; set; }

        /// <summary>
        /// 订单状态
        /// </summary>
        [Field("state")]
        public int State { get; set; }

        /// <summary>
        /// 支付状态
        /// </summary>
        [Field("pay_state")]
        public int PayState { get; set; }

        /// <summary>
        /// 业务线名称
        /// </summary>
        [Field("business_line_name")]
        [StringLength(100)]
        public string BusinessLineName { get; set; }

        /// <summary>
        /// 专家id
        /// </summary>
        [Field("expert_id")]
        public long ExpertId { get; set; }

        /// <summary>
        /// 专家名称
        /// </summary>
        [Field("expert_name")]
        [StringLength(50)]
        public string ExpertName { get; set; }

        /// <summary>
        /// 咨询类型编码
        /// </summary>
        [Field("consult_code")]
        [StringLength(20), NotNull]
        public string ConsultCode { get; set; }

        /// <summary>
        /// 咨询类型名称
        /// </summary>
        [Field("consult_type_name")]
        [StringLength(50)]
        public string ConsultTypeName { get; set; }

        /// <summary>
        /// 咨询方式
        /// </summary>
        [Field("consult_mode")]
        public int ConsultMode { get; set; }

        /// <summary>
        /// 支付超时时间
        /// </summary>
        [Field("pay_timeout")]
        public DateTime PayTimeout { get; set; }

        /// <summary>
        /// 支付成功时间
        /// </summary>
        [Field("pay_success_time")]
        public DateTime? PaySuccessTime { get; set; }

        /// <summary>
        /// 评价星级
        /// </summary>
        [Field("assess_star")]
        public float? AssessStar { get; set; }

        /// <summary>
        /// 是否开启问诊单
        /// </summary>
        [Field("is_enable_inquiry")]
        public bool IsEnableInquiry { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Field("create_time")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        [Field("create_by")]
        public long CreateBy { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Field("update_time")]
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 更新人
        /// </summary>
        [Field("update_by")]
        public long UpdateBy { get; set; }
    }

    /// <summary>
    /// 业务常用语
    /// </summary>
    [Table("business_term_rep")]
    public partial class BusinessTermRep
    {
        /// <summary>
        /// 自增主键id
        /// </summary>
        [Key]
        [DatabaseGenerated]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 业务线编码
        /// </summary>
        [Field("business_line_id")]
        public long BusinessLineId { get; set; }

        /// <summary>
        /// 业务名称
        /// </summary>
        [Field("content")]
        [StringLength(500)]
        public string Content { get; set; } = null!;

        /// <summary>
        /// 创建人id
        /// </summary>
        [Field("create_by")]
        public long CreateBy { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Field("create_time")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新人id
        /// </summary>
        [Field("update_by")]
        public long UpdateBy { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Field("update_time")]
        public DateTime UpdateTime { get; set; }
    }
    /// <summary>
    /// 会话群组人员
    /// </summary>
    [Table("session_group_user")]
    public partial class SessionGroupUser
    {
        /// <summary>
        /// 自增主键id
        /// </summary>
        [Key]
        [DatabaseGenerated]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 业务线id
        /// </summary>
        [Field("business_line_id")]
        public long BusinessLineId { get; set; }

        /// <summary>
        /// 会话id
        /// </summary>
        [Field("session_id")]
        public long SessionId { get; set; }

        /// <summary>
        /// 用户id
        /// </summary>
        [Field("user_id")]
        public long UserId { get; set; }

        /// <summary>
        /// 客户编号
        /// </summary>
        [Field("customer_code")]
        [StringLength(36)]
        public string CustomerCode { get; set; }

        /// <summary>
        /// 是否可以结束会话
        /// </summary>
        [Field("is_close")]
        public bool IsClose { get; set; }
    }
    #endregion

    public class NestedQueriesTests
    {
        private readonly IQueryable<BusinessConsultationRep> _businessConsultationReps;
        private readonly IQueryable<BusinessDepartmentConsultationRel> _businessDepartmentConsultationRels;
        private readonly IQueryable<MyUser> _users;
        private readonly IQueryable<Specialist> _specialists;
        private readonly IQueryable<SpecialistCostRep> _specialistCostReps;
        private readonly IQueryable<BusinessDepartmentRel> _businessDepartmentRels;
        private readonly IQueryable<Order> _orderReps;
        private readonly IQueryable<BusinessTermRep> _businessTermReps;
        private readonly IQueryable<SessionGroupUser> _sessionGroupUsers;

        public NestedQueriesTests(IQueryable<BusinessConsultationRep> businessConsultationReps,
        IQueryable<BusinessDepartmentConsultationRel> businessDepartmentConsultationRels,
        IQueryable<MyUser> users,
        IQueryable<Specialist> specialists,
        IQueryable<SpecialistCostRep> specialistCostReps,
        IQueryable<BusinessDepartmentRel> businessDepartmentRels,
        IQueryable<Order> orderReps,
        IQueryable<BusinessTermRep> businessTermReps,
        IQueryable<SessionGroupUser> sessionGroupUsers)
        {
            _specialistCostReps = specialistCostReps;
            _businessDepartmentRels = businessDepartmentRels;
            _orderReps = orderReps;
            _businessTermReps = businessTermReps;
            _sessionGroupUsers = sessionGroupUsers;
            _users = users;
            _specialists = specialists;
            _businessDepartmentConsultationRels = businessDepartmentConsultationRels;
            _businessConsultationReps = businessConsultationReps;
        }

        [Fact]
        public async Task TestAsync()
        {
            var id = 9000000000000000;

            var specialistQuery = from a in _businessDepartmentConsultationRels
                                  join b in _users on a.SpecialistId equals b.Id // a join b
                                  where a.Id == id
                                  select new
                                  {
                                      BusinessDepartmentId = a.BusinessDepartmentId,
                                      SpecialistId = a.SpecialistId,
                                      BusinessLineId = a.BusinessLineId,
                                      SpecialistName = b.Name,
                                  };


            var consultationQuery = from a in _businessDepartmentConsultationRels
                                    join b in _businessConsultationReps on a.BusinessConsultationId equals b.Id into ab // a left join b                                  
                                    from b in ab.DefaultIfEmpty()
                                    join c in specialistQuery on new { a.BusinessDepartmentId, a.SpecialistId, a.BusinessLineId } equals new { c.BusinessDepartmentId, c.SpecialistId, c.BusinessLineId }
                                    select new
                                    {
                                        a.BusinessDepartmentId,
                                        a.SpecialistId,
                                        a.BusinessLineId,
                                        a.BusinessConsultationId,
                                        c.SpecialistName,
                                        ConsultationName = b.Name
                                    };

            var consultations = await consultationQuery.ToListAsync();
        }

        [Fact]
        public async Task Test2Async()
        {
            var param = new
            {
                Name = "测",
                Phone = "18980861011",
                IsValid = null as bool?,
                BusinessDepartmentId = 9000000000000000
            };

            Expression<Func<MyUser, bool>> userExpression = x => !x.IsDelete;

            // 姓名
            if (!string.IsNullOrWhiteSpace(param.Name))
            {
                userExpression = userExpression.And(x => x.Name.Contains(param.Name));
            }

            // 手机号
            if (!string.IsNullOrWhiteSpace(param.Phone))
            {
                userExpression = userExpression.And(x => x.Phone.Contains(param.Phone));
            }

            // 状态
            if (param.IsValid.HasValue)
            {
                userExpression = userExpression.And(x => x.IsValid == param.IsValid);
            }

            var specialistQuery = from a in _businessDepartmentRels
                                  join b in _specialists on a.SpecialistId equals b.Id // a join b     
                                  join c in _users.Where(userExpression) on a.SpecialistId equals c.Id // a join c
                                  join d in _specialistCostReps.Where(x => x.CostType == 1) on a.SpecialistId equals d.SpecialistId into ad // a left join d
                                  from d in ad.DefaultIfEmpty()
                                  where a.BusinessDepartmentId == param.BusinessDepartmentId
                                  orderby c.CreateTime descending
                                  select new
                                  {
                                      Id = a.Id,
                                      BusinessDepartmentId = a.BusinessDepartmentId,
                                      SpecialistId = a.SpecialistId,
                                      BusinessLineId = a.BusinessLineId,
                                      HeadUrl = b.HeadUrl,
                                      Name = c.Name,
                                      Phone = c.Phone,
                                      IsValid = c.IsValid,
                                      Cost = d == null ? 0 : d.Cost
                                  };

            var results = await specialistQuery.CountAsync();
        }

        [Fact]
        public async Task Test3Async()
        {
            var businessDepartmentIds = new List<long> { 9000000000000000 };
            var specialistIds = new List<long> { 9000000000000000 };
            var businessLineIds = new List<long> { 9000000000000000 };

            var consultationQuery = from a in _businessDepartmentConsultationRels
                                    join b in _businessConsultationReps on a.BusinessConsultationId equals b.Id into ab // a left join b                                  
                                    from be in ab.DefaultIfEmpty()
                                    where (businessDepartmentIds.Contains(a.BusinessDepartmentId) && specialistIds.Contains(a.SpecialistId) && businessLineIds.Contains(a.BusinessLineId))
                                    select new
                                    {
                                        a.BusinessDepartmentId,
                                        a.SpecialistId,
                                        a.BusinessLineId,
                                        a.BusinessConsultationId,
                                        ConsultationName = be.Name
                                    };

            var consultations = await consultationQuery.ToListAsync();
        }

        [Fact]
        public async Task Test4Async()
        {
            var businessConsultationCount = await _businessConsultationReps.CountAsync(x => x.BusinessLineId == 9000000000000000);
        }

        [Fact]
        public async Task Test5Async()
        {
            var id = 9000000000000000;

            var specialistQuery = from a in _businessDepartmentConsultationRels
                                  join b in _users on a.SpecialistId equals b.Id // a join b
                                  where a.Id == id
                                  orderby a.Id descending
                                  select new
                                  {
                                      BusinessDepartmentId = a.BusinessDepartmentId,
                                      SpecialistId = a.SpecialistId,
                                      BusinessLineId = a.BusinessLineId,
                                      SpecialistName = b.Name,
                                  };


            var consultationQuery = from a in _businessDepartmentConsultationRels
                                    join b in _businessConsultationReps on a.BusinessConsultationId equals b.Id into ab // a left join b                                  
                                    from b in ab.DefaultIfEmpty()
                                    join c in specialistQuery.Skip(5).Take(5) on new { a.BusinessDepartmentId, a.SpecialistId, a.BusinessLineId } equals new { c.BusinessDepartmentId, c.SpecialistId, c.BusinessLineId }
                                    select new
                                    {
                                        a.BusinessDepartmentId,
                                        a.SpecialistId,
                                        a.BusinessLineId,
                                        a.BusinessConsultationId,
                                        c.SpecialistName,
                                        ConsultationName = b.Name
                                    };

            var consultations = await consultationQuery.ToListAsync();
        }

        [Fact]
        public async Task Test6Async()
        {
            var query = from a in _orderReps
                        join b in _businessTermReps on a.BusinessLineId equals b.BusinessLineId
                        where a.Id == 9000000000000000
                        orderby b.CreateTime descending
                        select new
                        {
                            Content = b.Content,
                            Id = b.Id
                        };
            var list = await query.ToListAsync();
        }

        [Fact]
        public async Task Test7Async()
        {
            var orderIds = new List<long> { 9000000000000000 };

            var results = await (from gu in _sessionGroupUsers.Where(t => orderIds.Contains(t.SessionId))
                                 join s in _specialists on gu.UserId equals s.Id
                                 select s.Id)
                 .Distinct()
                 .ToListAsync();
        }
    }
}