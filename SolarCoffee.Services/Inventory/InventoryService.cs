using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SolarCoffee.Data;
using SolarCoffee.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolarCoffee.Services.Inventory
{
    public class InventoryService : IInventoryService
    {
        private readonly SolarDbContext _db;
        private readonly ILogger<InventoryService> _logger;
        public InventoryService(SolarDbContext dbContext, ILogger<InventoryService> logger)
        {
            _db = dbContext;
            _logger = logger;
        }

        public ProductInvertory GetByProductId(int productId)
        {
            return _db.ProductInvertories
                .Include(pi => pi.Product)
                .FirstOrDefault(pi => pi.Product.Id == productId);
        }

        public List<ProductInvertory> GetCurrentInventory()
        {
            return _db.ProductInvertories
                 .Include(pi => pi.Product)
                 .Where(pi => !pi.Product.IsArchived)
                 .ToList();
        }

        public List<ProductInvertorySnapshot> GetSnapshotHistory()
        {
            var earliest = DateTime.UtcNow - TimeSpan.FromHours(6);
            return _db.ProductInvertorySnapshots
                .Include(snap => snap.Product)
                .Where(snap => snap.SnapshotTime > earliest && !snap.Product.IsArchived)
                .ToList();
        }

        public ServiceResponse<ProductInvertory> UpdateUnitsAvailable(int id, int adjustment)
        {
            var now = DateTime.UtcNow;

            try
            {
                var inventory = _db.ProductInvertories
                    .Include(inv => inv.Product)
                    .First(inv => inv.Product.Id == id);
                
                inventory.QuantityOnHand += adjustment;

                try {
                    CreateSnapshot(inventory);
                }
                catch(Exception e) {
                    _logger.LogError("Error creating inventory snspshot.");
                    _logger.LogError(e.StackTrace);
                }
                
                _db.SaveChanges();

                return new ServiceResponse<ProductInvertory>
                {
                    IsSuccess = true,
                    Data = inventory,
                    Message = $"Product {id} inventory adjusted",
                    Time = now
                };
            }

            catch(Exception e)
            {
                return new ServiceResponse<ProductInvertory>
                {
                    IsSuccess = false,
                    Data = null,
                    Message = e.StackTrace,
                    Time = now
                };
            }
        }
        
        private void CreateSnapshot(ProductInvertory invertory)
        {
            var now = DateTime.UtcNow;

            var snapshot = new ProductInvertorySnapshot
            {
                SnapshotTime = now,
                Product = invertory.Product,
                QuantityOnHand = invertory.QuantityOnHand
            };

            _db.Add(snapshot);
        }
    }
}
