using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Inkslab.Linq.Tests
{
    /// <summary>
    /// InventoryCommodityTests
    /// </summary>
    public class InventoryCommodityTests
    {
        private readonly IQueryable<InventoryCommodityStock> _stocks;
        private readonly IQueryable<InventoryCommodityStockPreLock> _preLocks;
        private readonly IQueryable<InventoryPreLock> _inventoryPreLockQuery;
        private readonly IQueryable<InventoryPreLockItems> _inventoryCommodityStockPrelockQuery;

        public InventoryCommodityTests(IQueryable<InventoryCommodityStock> stocks, IQueryable<InventoryCommodityStockPreLock> preLocks, IQueryable<InventoryPreLock> inventoryPreLockQuery, IQueryable<InventoryPreLockItems> inventoryCommodityStockPrelockQuery)
        {
            _stocks = stocks;
            _preLocks = preLocks;
            _inventoryPreLockQuery = inventoryPreLockQuery;
            _inventoryCommodityStockPrelockQuery = inventoryCommodityStockPrelockQuery;
        }

        /// <summary>
        /// SQL预览:
        /// SELECT `stock`.`id` AS `Id`, `stock`.`medical_trade_no` AS `MedicalTradeNo`, 
        /// `stock`.`cargo_owner_no` AS `CargoOwnerNo`, `stock`.`warehouse_no` AS `WarehouseNo`, 
        /// `stock`.`erp_product_id` AS `ErpProductId`, `stock`.`stock_qty` AS `StockQty`, 
        /// `stock`.`available_qty` AS `AvailableQty`, `stock`.`lock_qty` AS `LockQty` 
        /// FROM `inventory_commodity_stock_pre_lock` AS `preLock` 
        /// INNER JOIN `inventory_commodity_stock` AS `stock` 
        ///     ON `preLock`.`medical_trade_no` = `stock`.`medical_trade_no` 
        ///     AND `preLock`.`cargo_owner_no` = `stock`.`cargo_owner_no` 
        ///     AND `preLock`.`warehouse_no` = `stock`.`warehouse_no` 
        ///     AND `preLock`.`erp_product_id` = `stock`.`erp_product_id` 
        /// WHERE `preLock`.`order_no` = ?inDto_OrderNo 
        ///     AND `preLock`.`is_valid` 
        ///     AND `preLock`.`lock_type` <= 2 
        ///     AND NOT `preLock`.`is_confirm`
        /// </summary>
        [Fact]
        public async Task TestStockAsync()
        {
            var inDto = new
            {
                OrderNo = "ORDER12345"
            };

            var commodityStocks = await (
               from preLock in _preLocks
               join stock in _stocks on
                   new { preLock.MedicalTradeNo, preLock.CargoOwnerNo, preLock.WarehouseNo, preLock.ErpProductId }
                   equals
                   new { stock.MedicalTradeNo, stock.CargoOwnerNo, stock.WarehouseNo, stock.ErpProductId }
               where preLock.OrderNo == inDto.OrderNo
                     && preLock.IsValid
                     && preLock.LockType <= 2
                     && !preLock.IsConfirm
               select stock
           ).ToListAsync();
        }

        /// <summary>
        /// 应用过滤条件。
        /// </summary>
        private static IQueryable<InventoryCommodityStock> ApplyFilters(IQueryable<InventoryCommodityStock> query, InventoryCommodityStockFilterInDto dto, DateTime start)
        {
            if (!string.IsNullOrEmpty(dto.MedicalTradeNo))
            {
                query = query.Where(x => x.MedicalTradeNo == dto.MedicalTradeNo);
            }

            if (!string.IsNullOrEmpty(dto.CargoOwnerNo))
            {
                query = query.Where(x => x.CargoOwnerNo == dto.CargoOwnerNo);
            }

            if (!string.IsNullOrEmpty(dto.WarehouseNo))
            {
                query = query.Where(x => x.WarehouseNo == dto.WarehouseNo);
            }

            if (!string.IsNullOrEmpty(dto.ErpProductId))
            {
                query = query.Where(x => x.ErpProductId.Contains(dto.ErpProductId));
            }

            if (dto.HasLockOffsetStock == true)
            {
                query = query.Where(x => x.LockOffsetStock > 0);
            }

            // 仓库校准时间异常筛选。
            if (dto.HasSyncTimeAbnormal == true)
            {
                var end = start.AddDays(1);
                query = query.Where(x => x.StorageSyncTime < start || x.StorageSyncTime >= end);
            }
            else if (dto.HasSyncTimeAbnormal == false)
            {
                var end = start.AddDays(1);
                query = query.Where(x => x.StorageSyncTime >= start && x.StorageSyncTime < end);
            }

            if (dto.IsSyncDiff.HasValue)
            {
                query = query.Where(x => x.IsSyncDiff == dto.IsSyncDiff.Value);
            }

            return query;
        }

        [Fact]
        public async Task TestStockOnlyAsync()
        {
            var linq = (from t1 in _inventoryPreLockQuery
                        join t2 in _inventoryCommodityStockPrelockQuery on t1.Id equals t2.InventoryPreLockId
                        where t1.Status <= 1 && t2.LockType < 10
                        select t2);

            var results = await linq.ToListAsync();
        }

        [Fact]
        public async Task TestStockWithPreLockAsync()
        {
            var linq = (from t1 in _inventoryPreLockQuery
                        join t2 in _inventoryCommodityStockPrelockQuery on t1.Id equals t2.InventoryPreLockId
                        where t1.Status <= 1 && t2.LockType < 10
                        select t2)
                                  .GroupBy(t2 => new { t2.CargoOwnerNo, t2.MedicalTradeNo, t2.WarehouseNo, t2.ErpProductId })
                                  .Select(t2 => new
                                  {
                                      CargoOwnerNo = t2.Key.CargoOwnerNo,
                                      MedicalTradeNo = t2.Key.MedicalTradeNo,
                                      WarehouseNo = t2.Key.WarehouseNo,
                                      ErpProductId = t2.Key.ErpProductId,
                                      PreLockStock = t2.Where(ps => ps.LockType == 1).Sum(ps => ps.LockStock),
                                      LockValidityPeriodPreLockStock = t2.Where(ps => ps.LockType == 2).Sum(ps => ps.LockStock)
                                  });

            var results = await linq.ToListAsync();
        }

        [Fact]
        public async Task TestPreLockAsync()
        {
            var dto = new InventoryCommodityStockFilterInDto
            {
                MedicalTradeNo = "MTN12345",
                CargoOwnerNo = "CON12345",
                WarehouseNo = "WHN12345",
                ErpProductId = "EPI12345",
                AvailableStockRange = 100,
                HasLockOffsetStock = true,
                HasSyncTimeAbnormal = false,
                IsSyncDiff = false,
                PageIndex = 1,
                PageSize = 20
            };

            var currentDate = DateTime.Today;

            // 步骤1：先对主表进行筛选，只查询必要字段，避免不必要的JOIN。
            var query = _stocks.AsQueryable();

            // 应用过滤条件。
            query = ApplyFilters(query, dto, currentDate);

            var linq = from x in query
                       join y in (from t1 in _inventoryPreLockQuery
                                  join t2 in _inventoryCommodityStockPrelockQuery on t1.Id equals t2.InventoryPreLockId
                                  where t1.Status <= 1 && t2.LockType < 10
                                  select t2)
                                  .GroupBy(t2 => new { t2.CargoOwnerNo, t2.MedicalTradeNo, t2.WarehouseNo, t2.ErpProductId })
                                  .Select(t2 => new
                                  {
                                      CargoOwnerNo = t2.Key.CargoOwnerNo,
                                      MedicalTradeNo = t2.Key.MedicalTradeNo,
                                      WarehouseNo = t2.Key.WarehouseNo,
                                      ErpProductId = t2.Key.ErpProductId,
                                      PreLockStock = t2.Where(ps => ps.LockType == 1).Sum(ps => ps.LockStock),
                                      LockValidityPeriodPreLockStock = t2.Where(ps => ps.LockType == 2).Sum(ps => ps.LockStock)
                                  })
                                   on new { x.CargoOwnerNo, x.MedicalTradeNo, x.WarehouseNo, x.ErpProductId } equals new { y.CargoOwnerNo, y.MedicalTradeNo, y.WarehouseNo, y.ErpProductId } into prelock
                       from pl in prelock.DefaultIfEmpty()
                       orderby x.Id descending
                       select new
                       {
                           Id = x.Id,
                           MedicalTradeNo = x.MedicalTradeNo,
                           CargoOwnerNo = x.CargoOwnerNo,
                           WarehouseNo = x.WarehouseNo,
                           ErpProductId = x.ErpProductId,
                           ErpProductCode = x.ErpProductCode,
                           Stock = x.Stock,
                           VirtualStock = x.VirtualStock,
                           LockStock = x.LockStock,
                           LockVirtualWarehouseStock = x.LockVirtualWarehouseStock,
                           LockOutboundStock = x.LockOutboundStock,
                           LockOffsetStock = x.LockOffsetStock,
                           LockValidityPeriodAllocationStock = x.LockValidityPeriodAllocationStock,
                           LockValidityPeriodStock = x.LockValidityPeriodStock,
                           LockValidityPeriodLockStock = x.LockValidityPeriodLockStock,
                           SyncTime = x.SyncTime,
                           UpdateTime = x.UpdateTime,
                           PreLockStock = pl.PreLockStock,
                           LockValidityPeriodPreLockStock = pl.LockValidityPeriodPreLockStock,
                           IsSyncDiff = x.IsSyncDiff,
                           StorageSyncTime = x.StorageSyncTime
                       };

            // 排序并分页查询主表数据。
            var results = await linq.ToListAsync(dto.PageIndex, dto.PageSize);
        }

        private class InventoryCommodityStockFilterInDto
        {
            /// <summary>
            /// 医贸编号。
            /// </summary>
            public string MedicalTradeNo { get; set; }

            /// <summary>
            /// 货主编号。
            /// </summary>
            public string CargoOwnerNo { get; set; }

            /// <summary>
            /// 仓库编号。
            /// </summary>
            public string WarehouseNo { get; set; }

            /// <summary>
            /// ERP商品Id。
            /// </summary>
            public string ErpProductId { get; set; }

            /// <summary>
            /// 可用库存范围。
            /// </summary>
            public int? AvailableStockRange { get; set; }

            /// <summary>
            /// 是否存在占用偏移。
            /// </summary>
            public bool? HasLockOffsetStock { get; set; }

            /// <summary>
            /// 校准时间异常（true：查询校准时间不是当天的记录）。
            /// </summary>
            public bool? HasSyncTimeAbnormal { get; set; }

            /// <summary>
            /// 校准库存差异
            /// </summary>
            public bool? IsSyncDiff { get; set; }

            private const int DefaultPageSize = 20;

            private int _pageIndex;
            private int _pageSize;

            /// <summary>
            /// 页码（从1开始）。如果设置的值小于等于0，将自动调整为1。
            /// </summary>
            [Required]
            public int PageIndex
            {
                get => _pageIndex <= 0 ? 1 : _pageIndex;
                set => _pageIndex = value <= 0 ? 1 : value;
            }

            /// <summary>
            /// 每页的数据条数。如果设置的值小于等于0，将使用默认值20。
            /// </summary>
            [Required]
            public int PageSize
            {
                get => _pageSize <= 0 ? DefaultPageSize : _pageSize;
                set => _pageSize = value <= 0 ? DefaultPageSize : value;
            }
        }
    }
}