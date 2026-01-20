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