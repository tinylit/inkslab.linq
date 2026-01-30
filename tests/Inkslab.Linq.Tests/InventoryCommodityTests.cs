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

        public InventoryCommodityTests(IQueryable<InventoryCommodityStock> stocks, IQueryable<InventoryCommodityStockPreLock> preLocks)
        {
            _stocks = stocks;
            _preLocks = preLocks;
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
    }
}