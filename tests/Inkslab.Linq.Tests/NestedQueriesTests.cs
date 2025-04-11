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
    /// 问诊单关联患者信息表
    /// </summary>
    [Table("inquiry_rep")]
    public class InquiryRep
    {
        /// <summary>
        /// 问诊单Id
        /// </summary>
        [Key]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 患者id
        /// </summary>
        [Field("patient_id")]
        public long PatientId { get; set; }

        /// <summary>
        /// 订单id
        /// </summary>
        [Field("order_id")]
        public long OrderId { get; set; }
    }
    /// <summary>
	/// 问诊单图片
	/// </summary>
	[Table("inquiry_image")]
    public class InquiryImage
    {
        /// <summary>
        /// id
        /// </summary>
        [Key]
        [Field("id")]
        [DatabaseGenerated]
        public long Id { get; set; }

        /// <summary>
        /// 问诊单id
        /// </summary>
        [Field("inquiry_id")]
        public long InquiryId { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        [Field("url")]
        [StringLength(300)]
        public string Url { get; set; }

        /// <summary>
        /// 类型：1为舌面图，2为舌下图
        /// </summary>
        [Field("type")]
        public int Type { get; set; }
    }
    /// <summary>
    /// 处方建议单表
    /// </summary>
    [Table("prescription_suggestion")]
    public class PrescriptionSuggestion
    {
        /// <summary>
        /// 主键id
        /// </summary>
        [Key]
        [Field("id")]
        public long Id { get; set; }

        /// <summary>
        /// 处方建议单编码
        /// </summary>
        [Field("code")]
        [StringLength(21)]
        public string Code { get; set; }

        /// <summary>
        /// 咨询人id
        /// </summary>
        [Field("consultant_id")]
        public long ConsultantId { get; set; }

        /// <summary>
        /// 问诊单id
        /// </summary>
        [Field("consultant_sheet_id")]
        public long ConsultationSheetId { get; set; }

        /// <summary>
        /// 诊断信息
        /// </summary>
        [Field("diagnostic_information")]
        [StringLength(20)]
        public string DiagnosticInformation { get; set; }

        /// <summary>
        /// 医嘱
        /// </summary>
        [Field("doctor_advice")]
        [StringLength(400)]
        public string DoctorAdvice { get; set; } = String.Empty;

        /// <summary>
        /// 总贴数
        /// </summary>
        [Field("total_dose")]
        public int TotalDose { get; set; }

        /// <summary>
        /// 每天几帖
        /// </summary>
        [Field("daily_dose")]
        public int DailyDose { get; set; }

        /// <summary>
        /// 每幅分几次服用
        /// </summary>
        [Field("per_dose")]
        public int PerDose { get; set; }

        /// <summary>
        /// 使用次数
        /// </summary>
        [Field("usedNumber")]
        public int UsedNumber { get; set; } = 0;

        /// <summary>
        /// 药材味数
        /// </summary>
        [Field("commodity_type_count")]
        public int CommodityTypeCount { get; set; }

        /// <summary>
        /// 处方内容
        /// </summary>
        [Field("prescription_content")]
        [StringLength(1000)]
        public string PrescriptionContent { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        [Field("create_by")]
        public long CreateBy { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Field("create_time")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改人
        /// </summary>
        [Field("update_by")]
        public long UpdateBy { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [Field("update_time")]
        public DateTime UpdateTime { get; set; }
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
        private readonly IQueryable<SystemUser> _systemUsers;
        private readonly IQueryable<Order> _orders;
        private readonly IQueryable<Inquiry> _inquiries;
        private readonly IQueryable<InquiryRep> _inquiryReps;
        private readonly IQueryable<InquiryImage> _inquiryImages;
        private readonly IQueryable<PrescriptionSuggestion> _prescriptionSuggestions;

        public NestedQueriesTests(IQueryable<BusinessConsultationRep> businessConsultationReps,
        IQueryable<BusinessDepartmentConsultationRel> businessDepartmentConsultationRels,
        IQueryable<MyUser> users,
        IQueryable<Specialist> specialists,
        IQueryable<SpecialistCostRep> specialistCostReps,
        IQueryable<BusinessDepartmentRel> businessDepartmentRels,
        IQueryable<Order> orderReps,
        IQueryable<BusinessTermRep> businessTermReps,
        IQueryable<SessionGroupUser> sessionGroupUsers,
        IQueryable<SystemUser> systemUsers,
        IQueryable<Order> orders,
        IQueryable<Inquiry> inquiries,
        IQueryable<InquiryRep> inquiryReps,
        IQueryable<InquiryImage> inquiryImages,
        IQueryable<PrescriptionSuggestion> prescriptionSuggestions)
        {
            _specialistCostReps = specialistCostReps;
            _businessDepartmentRels = businessDepartmentRels;
            _orderReps = orderReps;
            _businessTermReps = businessTermReps;
            _sessionGroupUsers = sessionGroupUsers;
            _systemUsers = systemUsers;
            _orders = orders;
            _inquiries = inquiries;
            _inquiryReps = inquiryReps;
            _inquiryImages = inquiryImages;
            _prescriptionSuggestions = prescriptionSuggestions;
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
            var query = await (from a in _inquiryReps.Where(a => a.PatientId == 1 && a.OrderId != 1)
                               join c in _inquiries on a.Id equals c.Id
                               join e in _orders on a.OrderId equals e.Id
                               join g in _prescriptionSuggestions on c.Id equals g.ConsultationSheetId into leftg
                               from lg in leftg.DefaultIfEmpty()
                               join d in _inquiryImages
                                   .GroupBy(img => img.InquiryId)
                                   .Select(g => new
                                   {
                                       inquiry_id = g.Key,
                                       upUrl = g.Where(x => x.Type == 1).Select(x => x.Url).Max(),
                                       downUrl = g.Where(x => x.Type == 2).Select(x => x.Url).Max()
                                   }) on c.Id equals d.inquiry_id into leftd
                               from ld in leftd.DefaultIfEmpty()
                               orderby e.CreateTime descending, a.Id descending
                               select new
                               {
                                   SessionId = a.OrderId,
                                   SublingualImg = ld.downUrl,
                                   LingualImg = ld != null ? ld.upUrl : null,
                                   Description = c.Description,
                                   DiagnosticInformation = lg != null ? lg.DiagnosticInformation : null,
                                   InquiryId = a.Id,
                                   OrderId = a.OrderId,
                                   OrderCode = e.Code,
                                   CreateTime = e.CreateTime
                               }).ToListAsync(1, 10);
        }
    }
}