-- Inkslab.Linq.Tests 实际数据库测试用建表脚本（MySQL 5.7+ / 8.x）。
-- 数据库名称取自 Startup.cs；请在 framework 数据库中执行本文件。
-- 如数据库尚不存在，可先执行：
-- CREATE DATABASE IF NOT EXISTS `framework` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
-- MySQL 客户端示例：USE framework; SOURCE E:/Frameworks/inkslab.linq/tests/Inkslab.Linq.Tests/schema.mysql.sql;
--
-- 根据实体的 [Table] / [Field] / [Key] / [DatabaseGenerated] / 长度标注推导。
-- 未声明长度的普通字符串使用 VARCHAR(255)，published.head/content 使用 LONGTEXT；
-- 大长度字符串使用 TEXT / MEDIUMTEXT，decimal 未声明精度时使用 DECIMAL(28,8)。
-- 值类型按可空性设置 NULL；字符串未标 [Required] / [NotNull] 时允许 NULL。
-- 日期统一 DATETIME(6)；[Version] 由 ORM 维护，不添加 ON UPDATE 或触发器。
-- user.is_administrator 默认 0，以支持 InsertLinq / ShardingInsertLinq 只插入 name、date。
-- 仅创建缺失表，不调整已有表结构；不插入测试数据；脚本末尾重建 GetUserInfo 存储过程。
-- DatabaseTests.CreateAndExecuteStoredProcedureWithOutputParametersAsync 另需 id = 100 且姓名非空的用户数据。
-- 查询、聚合和部分断言另需测试数据；本文件并非完整的测试环境初始化脚本。
--
-- 不建表的测试实体：
-- AbilityPopulateIdentityTests / AbilityUpdateValidationTests 使用伪执行器；
-- KnownIssueRedLightTests / TypeMappingBoundaryTests 只测试翻译、映射及异常。
-- MySqlBulkAssistantTests 自行创建并清理随机命名的测试表。
-- user_[sharding] 的实际数据库测试使用分片键 2025，建为 user_2025。
-- 其余分片键（001 / 002 / 2024）仅用于翻译或异常测试。
-- 未声明外键和额外唯一约束，避免对测试中的独立插入、删除施加额外限制。

SET NAMES utf8mb4;

-- User.cs: User
CREATE TABLE IF NOT EXISTS `user` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `name` VARCHAR(255) NULL,
    `date` DATETIME(6) NOT NULL,
    `is_administrator` TINYINT(1) NOT NULL DEFAULT 0,
    `nullable` TINYINT(1) NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- User.cs: UserSharding 继承 User 的字段与主键。
CREATE TABLE IF NOT EXISTS `user_2025` LIKE `user`;

-- UserEx.cs: UserEx
CREATE TABLE IF NOT EXISTS `user_ex` (
    `id` INT NOT NULL,
    `role` INT NOT NULL,
    `age` INT NOT NULL,
    `date` DATETIME(6) NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Published.cs: Published
CREATE TABLE IF NOT EXISTS `published` (
    `id` BIGINT NOT NULL,
    `routing_key` VARCHAR(255) NULL,
    `exchange_name` VARCHAR(255) NULL,
    `exchange_type` INT NOT NULL,
    `status` INT NOT NULL,
    `number_of_consumers` INT NOT NULL,
    `number_of_successful_consumers` INT NOT NULL,
    `head` LONGTEXT NULL,
    `content` LONGTEXT NULL,
    `added` DATETIME(6) NOT NULL,
    `deliver_time` DATETIME(6) NOT NULL,
    `expires_at` DATETIME(6) NULL,
    `version` BIGINT NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- RepositoryTests.cs: ChannelConfig
CREATE TABLE IF NOT EXISTS `channel_config` (
    `id` BIGINT NOT NULL,
    `types` INT NOT NULL,
    `request_address` VARCHAR(200) NULL,
    `remark` TEXT NULL,
    `is_valid` TINYINT(1) NOT NULL,
    `create_by` BIGINT NOT NULL,
    `create_time` DATETIME(6) NOT NULL,
    `update_by` BIGINT NULL,
    `update_time` DATETIME(6) NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- InventoryCommodityStock.cs: InventoryCommodityStock
CREATE TABLE IF NOT EXISTS `inventory_commodity_stock` (
    `id` BIGINT NOT NULL,
    `medical_trade_no` VARCHAR(25) NOT NULL,
    `cargo_owner_no` VARCHAR(25) NOT NULL,
    `warehouse_no` VARCHAR(25) NOT NULL,
    `commodity_link_id` VARCHAR(67) NOT NULL,
    `erp_product_id` VARCHAR(15) NOT NULL,
    `erp_product_code` VARCHAR(20) NOT NULL,
    `stock` INT NOT NULL,
    `virtual_stock` INT NOT NULL,
    `lock_stock` INT NOT NULL,
    `lock_virtual_warehouse_stock` INT NOT NULL,
    `lock_outbound_stock` INT NOT NULL,
    `lock_offset_stock` INT NOT NULL,
    `lock_validity_period_stock` INT NOT NULL,
    `lock_validity_period_lock_stock` INT NOT NULL,
    `lock_validity_period_allocation_stock` INT NOT NULL,
    `sync_time` DATETIME(6) NOT NULL,
    `is_sync_diff` TINYINT(1) NOT NULL,
    `storage_sync_time` DATETIME(6) NOT NULL,
    `version` BIGINT NOT NULL,
    `update_time` DATETIME(6) NOT NULL,
    `update_by` BIGINT NOT NULL,
    `create_time` DATETIME(6) NOT NULL,
    `create_by` BIGINT NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- InventoryCommodityStockPreLock.cs: InventoryCommodityStockPreLock
CREATE TABLE IF NOT EXISTS `inventory_commodity_stock_prelock` (
    `id` BIGINT NOT NULL,
    `application_code` VARCHAR(50) NULL,
    `medical_trade_no` VARCHAR(25) NULL,
    `cargo_owner_no` VARCHAR(25) NULL,
    `warehouse_no` VARCHAR(25) NULL,
    `order_no` VARCHAR(50) NULL,
    `virtual_business_id` VARCHAR(36) NULL,
    `lock_type` INT NOT NULL,
    `erp_product_id` VARCHAR(15) NULL,
    `lock_stock` INT NOT NULL,
    `is_valid` TINYINT(1) NOT NULL,
    `is_confirm` TINYINT(1) NOT NULL,
    `update_time` DATETIME(6) NOT NULL,
    `update_by` BIGINT NOT NULL,
    `create_time` DATETIME(6) NOT NULL,
    `create_by` BIGINT NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- InventoryPreLock.cs: InventoryPreLock
CREATE TABLE IF NOT EXISTS `inventory_pre_lock` (
    `id` BIGINT NOT NULL,
    `business_line_code` VARCHAR(25) NOT NULL,
    `business_no` VARCHAR(50) NOT NULL,
    `status` INT NOT NULL,
    `update_time` DATETIME(6) NOT NULL,
    `update_by` BIGINT NOT NULL,
    `create_time` DATETIME(6) NOT NULL,
    `create_by` BIGINT NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- InventoryPreLockItems.cs: InventoryPreLockItems
CREATE TABLE IF NOT EXISTS `inventory_pre_lock_items` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `inventory_pre_lock_id` BIGINT NOT NULL,
    `medical_trade_no` VARCHAR(25) NOT NULL,
    `cargo_owner_no` VARCHAR(25) NOT NULL,
    `warehouse_no` VARCHAR(25) NOT NULL,
    `virtual_business_no` VARCHAR(100) NOT NULL,
    `virtual_warehouse_id` BIGINT NOT NULL,
    `lock_type` INT NOT NULL,
    `erp_product_id` VARCHAR(15) NOT NULL,
    `lock_stock` INT NOT NULL,
    `update_time` DATETIME(6) NOT NULL,
    `update_by` BIGINT NOT NULL,
    `create_time` DATETIME(6) NOT NULL,
    `create_by` BIGINT NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- MyRepositoryTests.cs: InventoryCommodityLotItemStock
CREATE TABLE IF NOT EXISTS `inventory_commodity_lot_item_stock` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `medical_trade_no` VARCHAR(25) NOT NULL,
    `erp_product_id` VARCHAR(15) NOT NULL,
    `erp_product_code` VARCHAR(20) NOT NULL,
    `cargo_owner_no` VARCHAR(25) NOT NULL,
    `warehouse_no` VARCHAR(25) NOT NULL,
    `lot_item_no` VARCHAR(50) NOT NULL,
    `stock` INT NOT NULL,
    `lock_outbound_stock` INT NOT NULL,
    `sync_time` DATETIME(6) NOT NULL,
    `status` INT NOT NULL,
    `version` INT NOT NULL,
    `packing_quantity` VARCHAR(10) NOT NULL,
    `validity_period` DATETIME(6) NOT NULL,
    `production_date` DATETIME(6) NOT NULL,
    `in_warehouse_time` DATETIME(6) NOT NULL,
    `update_time` DATETIME(6) NOT NULL,
    `update_by` BIGINT NOT NULL,
    `create_time` DATETIME(6) NOT NULL,
    `create_by` BIGINT NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- RepositoryTests.cs: InventoryCommodityBatchItemStock
CREATE TABLE IF NOT EXISTS `inventory_commodity_batch_item_stock` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `medical_trade_no` VARCHAR(25) NOT NULL,
    `erp_product_id` VARCHAR(36) NOT NULL,
    `erp_product_code` VARCHAR(20) NOT NULL,
    `cargo_owner_no` VARCHAR(25) NOT NULL,
    `warehouse_no` VARCHAR(25) NOT NULL,
    `batch_item_no` VARCHAR(50) NOT NULL,
    `lot_item_no` VARCHAR(50) NOT NULL,
    `stock` INT NOT NULL,
    `lock_outbound_stock` INT NOT NULL,
    `sync_time` DATETIME(6) NOT NULL,
    `is_sync_diff` TINYINT(1) NOT NULL,
    `storage_sync_time` DATETIME(6) NOT NULL,
    `status` INT NOT NULL,
    `version` INT NOT NULL,
    `packing_quantity` VARCHAR(10) NOT NULL,
    `validity_period` DATETIME(6) NOT NULL,
    `production_date` DATETIME(6) NOT NULL,
    `in_warehouse_time` DATETIME(6) NOT NULL,
    `update_time` DATETIME(6) NOT NULL,
    `update_by` BIGINT NOT NULL,
    `create_time` DATETIME(6) NOT NULL,
    `create_by` BIGINT NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- AgentTests.cs: Databases
CREATE TABLE IF NOT EXISTS `database` (
    `id` BIGINT NOT NULL,
    `platform_code` VARCHAR(50) NOT NULL,
    `database_type` INT NOT NULL,
    `business_type` INT NOT NULL,
    `connection_strings` VARCHAR(255) NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- AgentTests.cs: DatabaseCommands
CREATE TABLE IF NOT EXISTS `database_commands` (
    `id` BIGINT NOT NULL,
    `database_id` BIGINT NOT NULL,
    `type` INT NOT NULL,
    `code` VARCHAR(50) NOT NULL,
    `tsql` MEDIUMTEXT NOT NULL,
    `version` BIGINT NOT NULL,
    `create_time` DATETIME(6) NOT NULL,
    `create_by` BIGINT NOT NULL,
    `update_time` DATETIME(6) NOT NULL,
    `update_by` BIGINT NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- NestedQueriesTests.cs: MyUser
CREATE TABLE IF NOT EXISTS `my_user` (
    `id` BIGINT NOT NULL,
    `authentication_user_id` BIGINT NOT NULL,
    `account` VARCHAR(50) NULL,
    `name` VARCHAR(25) NULL,
    `phone` VARCHAR(15) NULL,
    `types` INT NOT NULL,
    `is_valid` TINYINT(1) NOT NULL,
    `is_delete` TINYINT(1) NOT NULL,
    `disable_time` DATETIME(6) NULL,
    `create_by` BIGINT NOT NULL,
    `create_time` DATETIME(6) NOT NULL,
    `update_by` BIGINT NULL,
    `update_time` DATETIME(6) NULL,
    `delete_by` BIGINT NULL,
    `delete_time` DATETIME(6) NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- NestedQueriesTests.cs: SystemUser
CREATE TABLE IF NOT EXISTS `system_user` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `user_id` BIGINT NOT NULL,
    `department_id` BIGINT NOT NULL,
    `post_id` BIGINT NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- NestedQueriesTests.cs: BusinessConsultationRep
CREATE TABLE IF NOT EXISTS `business_consultation_rep` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `business_line_id` BIGINT NOT NULL,
    `code` VARCHAR(2) NULL,
    `name` VARCHAR(20) NULL,
    `consult_duration` INT NOT NULL,
    `refund_duration` INT NOT NULL,
    `call` VARCHAR(100) NULL,
    `create_by` BIGINT NOT NULL,
    `create_time` DATETIME(6) NOT NULL,
    `update_by` BIGINT NOT NULL,
    `update_time` DATETIME(6) NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- NestedQueriesTests.cs: BusinessDepartmentConsultationRel
CREATE TABLE IF NOT EXISTS `business_department_consultation_rel` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `business_department_id` BIGINT NOT NULL,
    `specialist_id` BIGINT NOT NULL,
    `business_line_id` BIGINT NOT NULL,
    `business_consultation_id` BIGINT NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- NestedQueriesTests.cs: BusinessDepartmentRel
CREATE TABLE IF NOT EXISTS `business_department_rel` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `business_department_id` BIGINT NOT NULL,
    `specialist_id` BIGINT NOT NULL,
    `business_line_id` BIGINT NOT NULL,
    `sort` BIGINT NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- NestedQueriesTests.cs: SpecialistCostRep
CREATE TABLE IF NOT EXISTS `specialist_cost_rep` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `specialist_id` BIGINT NOT NULL,
    `cost_type` INT NOT NULL,
    `cost` DECIMAL(28,8) NOT NULL,
    `is_support` TINYINT(1) NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- NestedQueriesTests.cs: Specialist
CREATE TABLE IF NOT EXISTS `specialist` (
    `id` BIGINT NOT NULL,
    `head_url` VARCHAR(300) NULL,
    `introduction` VARCHAR(500) NULL,
    `duty_time` VARCHAR(200) NULL,
    `is_online` TINYINT(1) NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- NestedQueriesTests.cs: Order
CREATE TABLE IF NOT EXISTS `order` (
    `id` BIGINT NOT NULL,
    `business_line_id` BIGINT NOT NULL,
    `code` VARCHAR(50) NULL,
    `user_id` BIGINT NOT NULL,
    `customer_code` VARCHAR(36) NULL,
    `amount` DECIMAL(28,8) NOT NULL,
    `payment_method` INT NOT NULL,
    `state` INT NOT NULL,
    `pay_state` INT NOT NULL,
    `business_line_name` VARCHAR(100) NULL,
    `expert_id` BIGINT NOT NULL,
    `expert_name` VARCHAR(50) NULL,
    `consult_code` VARCHAR(20) NOT NULL,
    `consult_type_name` VARCHAR(50) NULL,
    `consult_mode` INT NOT NULL,
    `pay_timeout` DATETIME(6) NOT NULL,
    `pay_success_time` DATETIME(6) NULL,
    `assess_star` FLOAT NULL,
    `is_enable_inquiry` TINYINT(1) NOT NULL,
    `create_time` DATETIME(6) NOT NULL,
    `create_by` BIGINT NOT NULL,
    `update_time` DATETIME(6) NOT NULL,
    `update_by` BIGINT NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- NestedQueriesTests.cs: BusinessTermRep
CREATE TABLE IF NOT EXISTS `business_term_rep` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `business_line_id` BIGINT NOT NULL,
    `content` VARCHAR(500) NULL,
    `create_by` BIGINT NOT NULL,
    `create_time` DATETIME(6) NOT NULL,
    `update_by` BIGINT NOT NULL,
    `update_time` DATETIME(6) NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- NestedQueriesTests.cs: SessionGroupUser
CREATE TABLE IF NOT EXISTS `session_group_user` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `business_line_id` BIGINT NOT NULL,
    `session_id` BIGINT NOT NULL,
    `user_id` BIGINT NOT NULL,
    `customer_code` VARCHAR(36) NULL,
    `is_close` TINYINT(1) NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- NestedQueriesTests.cs: OrderUserInfo
CREATE TABLE IF NOT EXISTS `order_user_info` (
    `id` BIGINT NOT NULL,
    `customer_name` VARCHAR(200) NULL,
    `customer_source` VARCHAR(50) NULL,
    `consult_user_name` VARCHAR(50) NULL,
    `consult_user_phone` VARCHAR(11) NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- NestedQueriesTests.cs: Session
CREATE TABLE IF NOT EXISTS `session` (
    `id` BIGINT NOT NULL,
    `consult_start_time` DATETIME(6) NULL,
    `consult_end_time` DATETIME(6) NULL,
    `is_replied` TINYINT(1) NOT NULL,
    `first_replie_time` DATETIME(6) NULL,
    `is_consultation` TINYINT(1) NOT NULL,
    `is_referral` TINYINT(1) NOT NULL,
    `refund_max_time` DATETIME(6) NOT NULL,
    `is_finish` TINYINT(1) NOT NULL,
    `force_finish_time` DATETIME(6) NOT NULL,
    `is_connected` TINYINT(1) NOT NULL,
    `chat_expert_id` BIGINT NOT NULL,
    `chat_expert_name` VARCHAR(50) NULL,
    `is_enable` TINYINT(1) NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- NestedQueriesTests.cs: Inquiry
CREATE TABLE IF NOT EXISTS `inquiry` (
    `id` BIGINT NOT NULL,
    `inquiry_template_id` BIGINT NOT NULL,
    `name` VARCHAR(20) NULL,
    `code` VARCHAR(50) NULL,
    `gender` INT NOT NULL,
    `age` INT NOT NULL,
    `phone` VARCHAR(11) NULL,
    `description` VARCHAR(200) NULL,
    `tongue_nature` INT NOT NULL,
    `tongue_coating` INT NOT NULL,
    `tongue_color` INT NOT NULL,
    `physical_signs` INT NULL,
    `cough` INT NULL,
    `menstruation` INT NULL,
    `order_id` BIGINT NOT NULL,
    `patient_id` BIGINT NOT NULL,
    `is_edit` TINYINT(1) NOT NULL,
    `is_prescribe` TINYINT(1) NOT NULL,
    `create_by` BIGINT NOT NULL,
    `create_time` DATETIME(6) NOT NULL,
    `is_pregnancy` TINYINT(1) NULL,
    `is_menstruation` TINYINT(1) NULL,
    `version` INT NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- NestedQueriesTests.cs: JPushPhoneRel
CREATE TABLE IF NOT EXISTS `jpush_phone_rel` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `phone` VARCHAR(11) NULL,
    `jpush_id` BIGINT NOT NULL,
    `business_line_code` VARCHAR(20) NULL,
    `create_time` DATETIME(6) NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- NestedQueriesTests.cs: JPush
CREATE TABLE IF NOT EXISTS `jpush` (
    `id` BIGINT NOT NULL,
    `registration_id` VARCHAR(36) NULL,
    `phone` VARCHAR(11) NULL,
    `appid` VARCHAR(36) NULL,
    `device_id` VARCHAR(36) NULL,
    `platform_id` BIGINT NOT NULL,
    `create_time` DATETIME(6) NOT NULL,
    PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- DatabaseTests.CreateAndExecuteStoredProcedureWithOutputParametersAsync
-- 参数名称、方向、类型与测试中的 DynamicParameter 配置保持一致。
-- 测试固定传入 UserId = 100；请预先准备对应用户，name 非 NULL 且不超过 50 字符。
-- 示例数据（需要时手动执行）：
-- INSERT INTO `user` (`id`, `name`, `date`) VALUES (100, '存储过程测试用户', NOW(6));
-- DELIMITER 是 MySQL 客户端指令；通过驱动执行时，需移除 DELIMITER 行并单独提交 CREATE PROCEDURE。
DROP PROCEDURE IF EXISTS `GetUserInfo`;

DELIMITER $$
CREATE PROCEDURE `GetUserInfo` (
    IN UserId INT,
    OUT UserName VARCHAR(50),
    OUT UserCount INT
)
READS SQL DATA
BEGIN
    SELECT u.`name` INTO UserName
    FROM `user` AS u
    WHERE u.`id` = UserId;

    SELECT COUNT(*) INTO UserCount FROM `user`;

    SELECT u.* FROM `user` AS u WHERE u.`id` = UserId;
END$$
DELIMITER ;

-- 手动验证（返回用户结果集，并读取两个输出参数）：
-- CALL `GetUserInfo`(100, @UserName, @UserCount);
-- SELECT @UserName AS UserName, @UserCount AS UserCount;


