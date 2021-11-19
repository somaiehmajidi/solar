using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SolarCoffee.Data;
using SolarCoffee.Data.Models;
using SolarCoffee.Services.Inventory;
using SolarCoffee.Services.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolarCoffee.Services.Order
{
    public class OrderService : IOrderService
    {
        private readonly SolarDbContext _db;
        private readonly ILogger _ILogger;
        private readonly IProductService _productService;
        private readonly IInventoryService _inventoryService;

        public OrderService(
            SolarDbContext dbContext, 
            ILogger<OrderService> logger,
            IProductService productService,
            IInventoryService inventoryService
            ) {
            _db = dbContext;
            _ILogger = logger;
            _productService = productService;
            _inventoryService = inventoryService;
        }

        public ServiceResponse<bool> GenerateInvoiceForOrder(SalesOrder order)
        {
            foreach(var item in order.SalesOrderItems)
            {
                item.Product = _productService.GetProductById(item.Product.Id);
                var inventoryId = _inventoryService.GetByProductId(item.Product.Id).Id;
                _inventoryService.UpdateUnitsAvailable(inventoryId, -item.Quantity);
            }

            try
            {
                _db.SalesOrders.Add(order);
                _db.SaveChanges();

                return new ServiceResponse<bool>
                {
                    IsSuccess = true,
                    Data = true,
                    Time = DateTime.UtcNow,
                    Message = "Open order created"
                };
            }
            catch (Exception e)
            {
                return new ServiceResponse<bool>
                {
                    IsSuccess = false,
                    Data = false,
                    Time = DateTime.UtcNow,
                    Message = e.StackTrace
                };
            }
        }

        public List<SalesOrder> GetOrders()
        {
            return _db.SalesOrders
                .Include(so => so.Customer)
                    .ThenInclude(customer => customer.PrimaryAddress)
                .Include(so => so.SalesOrderItems)
                    .ThenInclude(item => item.Product)
                .ToList();
        }

        public ServiceResponse<bool> MarkFulfilled(int id)
        {
            var now = DateTime.UtcNow;
            var order = _db.SalesOrders.Find(id);
            order.UpdatedOn = now;
            order.IsPaid = true;

            try {
                _db.SalesOrders.Update(order);
                _db.SaveChanges();

                return new ServiceResponse<bool> {
                    IsSuccess = true,
                    Data = true,
                    Time = now,
                    Message = $"Order {id} closed: Invoice paid in full"
                };
            }
            catch (Exception e) {
                return new ServiceResponse<bool> {
                    IsSuccess = false,
                    Data = false,
                    Time = now,
                    Message = e.StackTrace
                };
            }
        }
    }
}
