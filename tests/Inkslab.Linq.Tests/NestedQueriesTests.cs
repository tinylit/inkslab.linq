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
    /// 系统用户表
    /// </summary>
    [Table("system_user")]
    public partial class SystemUser
    {
        /// <summary>
        /// id
        /// </summary>
        [Key]
        [DatabaseGenerated]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 用户id
        /// </summary>
        [Field("user_id")]
        public long UserId { get; set; }

        /// <summary>
        /// 部门id
        /// </summary>
        [Field("department_id")]
        public long DepartmentId { get; set; }

        /// <summary>
        /// 岗位id
        /// </summary>
        [Field("post_id")]
        public long PostId { get; set; }
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
    /// <summary>
    /// 订单人员信息
    /// </summary>
    [Table("order_user_info")]
    public partial class OrderUserInfo
    {
        /// <summary>
        /// id(订单id)
        /// </summary>
        [Key]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 客户(门店)名称
        /// </summary>
        [Field("customer_name")]
        [StringLength(200)]
        public string CustomerName { get; set; }

        /// <summary>
        /// 客户来源
        /// </summary>
        [Field("customer_source")]
        [StringLength(50)]
        public string CustomerSource { get; set; }

        /// <summary>
        /// 咨询人名字
        /// </summary>
        [Field("consult_user_name")]
        [StringLength(50)]
        public string ConsultUserName { get; set; }

        /// <summary>
        /// 咨询人电话
        /// </summary>
        [Field("consult_user_phone")]
        [StringLength(11)]
        public string ConsultUserPhone { get; set; }
    }
    /// <summary>
    /// 会话
    /// </summary>
    [Table("session")]
    public partial class Session
    {
        /// <summary>
        /// id(订单id)
        /// </summary>
        [Key]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 咨询开始时间
        /// </summary>
        [Field("consult_start_time")]
        public DateTime? ConsultStartTime { get; set; }

        /// <summary>
        /// 咨询结束时间
        /// </summary>
        [Field("consult_end_time")]
        public DateTime? ConsultEndTime { get; set; }

        /// <summary>
        /// 是否已回复
        /// </summary>
        [Field("is_replied")]
        public bool IsReplied { get; set; }

        /// <summary>
        /// 第一次回复时间
        /// </summary>
        [Field("first_replie_time")]
        public DateTime? FirstReplieTime { get; set; }

        /// <summary>
        /// 是否会诊
        /// </summary>
        [Field("is_consultation")]
        public bool IsConsultation { get; set; }

        /// <summary>
        /// 是否转诊
        /// </summary>
        [Field("is_referral")]
        public bool IsReferral { get; set; }

        /// <summary>
        /// 未回复退款时间
        /// </summary>
        [Field("refund_max_time")]
        public DateTime RefundMaxTime { get; set; }

        /// <summary>
        /// 是否结束
        /// </summary>
        [Field("is_finish")]
        public bool IsFinish { get; set; }

        /// <summary>
        /// 强制结束时间
        /// </summary>
        [Field("force_finish_time")]
        public DateTime ForceFinishTime { get; set; }

        /// <summary>
        /// 是否接通
        /// </summary>
        [Field("is_connected")]
        public bool IsConnected { get; set; }

        /// <summary>
        /// 接受咨询的专家id
        /// </summary>
        [Field("chat_expert_id")]
        public long ChatExpertId { get; set; }

        /// <summary>
        /// 接受咨询的专家名称
        /// </summary>
        [Field("chat_expert_name")]
        [StringLength(50)]
        public string ChatExpertName { get; set; }

        /// <summary>
        /// 是否启用，支付成功后才启用
        /// </summary>
        [Field("is_enable")]
        public bool IsEnable { get; set; }
    }
    /// <summary>
    /// 问诊单
    /// </summary>
    [Table("inquiry")]
    public class Inquiry
    {
        /// <summary>
        /// id
        /// </summary>
        [Key]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 问诊单模版id
        /// </summary>
        [Field("inquiry_template_id")]
        public long InquiryTemplateId { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        [Field("name")]
        [StringLength(20)]
        public string Name { get; set; }

        /// <summary>
        /// 编号
        /// </summary>
        [Field("code")]
        [StringLength(50)]
        public string Code { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        [Field("gender")]
        public int Gender { get; set; }

        /// <summary>
        /// 年龄
        /// </summary>
        [Field("age")]
        public int Age { get; set; }

        /// <summary>
        /// 电话
        /// </summary>
        [Field("phone")]
        [StringLength(11)]
        public string Phone { get; set; }

        /// <summary>
        /// 主诉症状
        /// </summary>
        [Field("description")]
        [StringLength(200)]
        public string Description { get; set; }

        /// <summary>
        /// 舌质
        /// </summary>
        [Field("tongue_nature")]
        public int TongueNature { get; set; }

        /// <summary>
        /// 舌苔
        /// </summary>
        [Field("tongue_coating")]
        public int TongueCoating { get; set; }

        /// <summary>
        /// 苔色
        /// </summary>
        [Field("tongue_color")]
        public int TongueColor { get; set; }

        /// <summary>
        /// 体征
        /// </summary>
        [Field("physical_signs")]
        public int? PhysicalSigns { get; set; }

        /// <summary>
        /// 咳嗽
        /// </summary>
        [Field("cough")]
        public int? Cough { get; set; }

        /// <summary>
        /// 月经
        /// </summary>
        [Field("menstruation")]
        public int? Menstruation { get; set; }

        /// <summary>
        /// 订单id
        /// </summary>
        [Field("order_id")]
        public long OrderId { get; set; }

        /// <summary>
        /// 患者id。
        /// </summary>
        [Field("patient_id")]
        public long PatientId { get; set; }

        /// <summary>
        /// 是否可编辑
        /// </summary>
        [Field("is_edit")]
        public bool IsEdit { get; set; } = false;

        /// <summary>
        /// 是否开方
        /// </summary>
        [Field("is_prescribe")]
        public bool IsPrescribe { get; set; } = false;

        /// <summary>
        /// 创建人id
        /// </summary>
        [Field("create_by")]
        public long CreateBy { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Field("create_time")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否妊娠期
        /// </summary>
        [Field("is_pregnancy")]
        public bool? IsPregnancy { get; set; }

        /// <summary>
        /// 是否月经期
        /// </summary>
        [Field("is_menstruation")]
        public bool? IsMenstruation { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        [Field("version")]
        public int Version { get; set; } = 0;
    }

    /// <summary>
    /// 手机-极光关系表
    /// </summary>
    [Table("jpush_phone_rel")]
    public class JPushPhoneRel
    {
        /// <summary>
        /// 主键编号
        /// </summary>
        [Key]
        [Field("id")]
        [DatabaseGenerated]
        public long Id { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        [Field("phone")]
        [StringLength(11)]
        public string Phone { get; set; }

        /// <summary>
        /// 极光主键id
        /// </summary>
        [Field("jpush_id")]
        public long JPushId { get; set; }

        /// <summary>
        /// 业务线code
        /// </summary>
        [Field("business_line_code")]
        [StringLength(20)]
        public string BusinessLineCode { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Field("create_time")]
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 极光表
    /// </summary>
    [Table("jpush")]
    public class JPush
    {
        /// <summary>
        /// 主键编号
        /// </summary>
        [Key]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 极光id
        /// </summary>
        [Field("registration_id")]
        [StringLength(36)]
        public string RegistrationId { get; set; }


        /// <summary>
        /// 手机号
        /// </summary>
        [Field("phone")]
        [StringLength(11)]
        public string Phone { get; set; }


        /// <summary>
        /// 应用id
        /// </summary>
        [Field("appid")]
        [StringLength(36)]
        public string Appid { get; set; }


        /// <summary>
        /// 设备id
        /// </summary>
        [Field("device_id")]
        [StringLength(36)]
        public string DeviceId { get; set; }

        /// <summary>
        /// 平台id
        /// </summary>
        [Field("platform_id")]

        public long PlatformId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Field("create_time")]
        public DateTime CreateTime { get; set; }
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
        private readonly IQueryable<Order> _orders;
        private readonly IQueryable<BusinessTermRep> _businessTermReps;
        private readonly IQueryable<SessionGroupUser> _sessionGroupUsers;
        private readonly IQueryable<SystemUser> _systemUsers;
        private readonly IQueryable<OrderUserInfo> _orderUserInfos;
        private readonly IQueryable<Inquiry> _inquiries;
        private readonly IQueryable<Session> _sessions;
        private readonly IQueryable<JPushPhoneRel> _jPushPhoneRels;
        private readonly IQueryable<JPush> _jPushes;

        public NestedQueriesTests(IQueryable<BusinessConsultationRep> businessConsultationReps,
        IQueryable<BusinessDepartmentConsultationRel> businessDepartmentConsultationRels,
        IQueryable<MyUser> users,
        IQueryable<Specialist> specialists,
        IQueryable<SpecialistCostRep> specialistCostReps,
        IQueryable<BusinessDepartmentRel> businessDepartmentRels,
        IQueryable<Order> orders,
        IQueryable<BusinessTermRep> businessTermReps,
        IQueryable<SessionGroupUser> sessionGroupUsers,
        IQueryable<SystemUser> systemUsers,
        IQueryable<OrderUserInfo> orderUserInfos,
        IQueryable<Inquiry> inquiries,
        IQueryable<Session> sessions,
        IQueryable<JPushPhoneRel> jPushPhoneRels,
        IQueryable<JPush> jPushes)
        {
            _specialistCostReps = specialistCostReps;
            _businessDepartmentRels = businessDepartmentRels;
            _orders = orders;
            _businessTermReps = businessTermReps;
            _sessionGroupUsers = sessionGroupUsers;
            _systemUsers = systemUsers;
            _orderUserInfos = orderUserInfos;
            _inquiries = inquiries;
            _sessions = sessions;
            _jPushPhoneRels = jPushPhoneRels;
            _jPushes = jPushes;
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
            var query = from a in _orders
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

        [Fact]
        public async Task Test8Async()
        {
            var id = 9000000000000000;

            await _systemUsers.Where(s => s.UserId == id).AnyAsync();
        }

        [Fact]
        public async Task Test9Async()
        {
            var id = 9000000000000000;

            await _systemUsers.AllAsync(s => s.UserId == id);
        }

        [Fact]
        public async Task Test10Async()
        {
            int patientId = 1;
            string patientName = null;
            string patientPhone = "18980861011";

            var query = from o in _orders
                        join ou in _orderUserInfos on o.Id equals ou.Id
                        join s in _sessions on o.Id equals s.Id
                        where (patientId <= 0 || _inquiries.Any(i => i.OrderId == o.Id && i.PatientId == patientId))
                        && (string.IsNullOrWhiteSpace(patientName) || _inquiries.Any(inquiry => inquiry.OrderId == o.Id && inquiry.Name.Contains(patientName)))
                        && (string.IsNullOrWhiteSpace(patientPhone) || _inquiries.Any(inquiry => inquiry.OrderId == o.Id && inquiry.Phone.Contains(patientPhone)))
                        orderby o.CreateTime descending
                        select new
                        {
                            Id = o.Id,
                            Code = o.Code,
                            BusinessLineName = o.BusinessLineName,
                            CustomerName = ou.CustomerName,
                            ConsultUserName = ou.ConsultUserName,
                            ConsultUserPhone = ou.ConsultUserPhone,
                            ConsultMode = o.ConsultMode,
                            ConsultCode = o.ConsultCode,
                            ExpertName = s.ChatExpertName,
                            Amount = o.Amount,
                            CreateTime = o.CreateTime,
                            State = o.State,
                            ConsultType = o.ConsultTypeName,
                            ConsultStartTime = s.ConsultStartTime,
                            ConsultEndTime = s.ConsultEndTime,
                            FirstReplieTime = s.FirstReplieTime,
                            RefundMaxTime = s.RefundMaxTime,
                            ForceFinishTime = s.ForceFinishTime,
                        };


            var entities = await query.ToListAsync();
        }

        [Fact]
        public async Task Test11Async()
        {
            int patientId = 1;
            string patientName = null;
            string patientPhone = "18980861011";

            var dto = new
            {
                PatientId = patientId,
                PatientName = patientName,
                PatientPhone = patientPhone
            };

            var query = from o in _orders
                        join ou in _orderUserInfos on o.Id equals ou.Id
                        join s in _sessions on o.Id equals s.Id
                        where (patientId <= 0 || _inquiries.Any(i => i.OrderId == o.Id && i.PatientId == patientId))
                        && (string.IsNullOrWhiteSpace(dto.PatientName) || _inquiries.Any(inquiry => inquiry.OrderId == o.Id && inquiry.Name.Contains(dto.PatientName)))
                        && (string.IsNullOrWhiteSpace(dto.PatientPhone) || _inquiries.Any(inquiry => inquiry.OrderId == o.Id && inquiry.Phone.Contains(dto.PatientPhone)))
                        orderby o.CreateTime descending
                        select new
                        {
                            Id = o.Id,
                            Code = o.Code,
                            BusinessLineName = o.BusinessLineName,
                            CustomerName = ou.CustomerName,
                            ConsultUserName = ou.ConsultUserName,
                            ConsultUserPhone = ou.ConsultUserPhone,
                            ConsultMode = o.ConsultMode,
                            ConsultCode = o.ConsultCode,
                            ExpertName = s.ChatExpertName,
                            Amount = o.Amount,
                            CreateTime = o.CreateTime,
                            State = o.State,
                            ConsultType = o.ConsultTypeName,
                            ConsultStartTime = s.ConsultStartTime,
                            ConsultEndTime = s.ConsultEndTime,
                            FirstReplieTime = s.FirstReplieTime,
                            RefundMaxTime = s.RefundMaxTime,
                            ForceFinishTime = s.ForceFinishTime,
                        };


            var entities = await query.ToListAsync();
        }

        [Fact]
        public async Task Test12Async()
        {
            int patientId = 1;
            string patientName = null;
            string patientPhone = "18980861011";

            var dto = new
            {
                PatientId = patientId,
                PatientName = patientName,
                PatientPhone = patientPhone
            };

            var query = from o in _orders
                        join ou in _orderUserInfos on o.Id equals ou.Id
                        join s in _sessions on o.Id equals s.Id
                        where Conditions.If(patientId > 0, _inquiries.Any(i => i.OrderId == o.Id && i.PatientId == patientId))
                        && Conditions.If(!string.IsNullOrWhiteSpace(dto.PatientName), !_inquiries.Any(inquiry => inquiry.OrderId == o.Id && inquiry.Name.Contains(dto.PatientName)))
                        && Conditions.If(!string.IsNullOrWhiteSpace(dto.PatientPhone), !_inquiries.Any(inquiry => inquiry.OrderId == o.Id && !inquiry.Phone.Contains(dto.PatientPhone)))
                        orderby o.CreateTime descending
                        select new
                        {
                            Id = o.Id,
                            Code = o.Code,
                            BusinessLineName = o.BusinessLineName,
                            CustomerName = ou.CustomerName,
                            ConsultUserName = ou.ConsultUserName,
                            ConsultUserPhone = ou.ConsultUserPhone,
                            ConsultMode = o.ConsultMode,
                            ConsultCode = o.ConsultCode,
                            ExpertName = s.ChatExpertName,
                            Amount = o.Amount,
                            CreateTime = o.CreateTime,
                            State = o.State,
                            ConsultType = o.ConsultTypeName,
                            ConsultStartTime = s.ConsultStartTime,
                            ConsultEndTime = s.ConsultEndTime,
                            FirstReplieTime = s.FirstReplieTime,
                            RefundMaxTime = s.RefundMaxTime,
                            ForceFinishTime = s.ForceFinishTime,
                        };


            var entities = await query.ToListAsync();
        }

        [Fact]
        public async Task Test13Async()
        {
            var results = await (from rel in _jPushPhoneRels
                                 join aurora in _jPushes on rel.JPushId equals aurora.Id
                                 where rel.Phone == "18980861011"
                                 group aurora by aurora.DeviceId into g
                                 orderby g.Max(rel => rel.CreateTime)
                                 select new
                                 {
                                     DeviceId = g.Key
                                 }).ToListAsync(1, 10);
        }

        [Fact]
        public async Task Test14Async()
        {
            var results = await (from rel in _jPushPhoneRels
                                 join aurora in _jPushes on rel.JPushId equals aurora.Id
                                 where rel.Phone == "18980861011"
                                 group aurora by aurora.DeviceId into g
                                 orderby g.Max(rel => rel.CreateTime) - g.Min(rel => rel.CreateTime)
                                 select new
                                 {
                                     DeviceId = g.Key
                                 }).ToListAsync(1, 10);
        }
    }
}