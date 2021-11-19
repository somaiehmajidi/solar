using SolarCoffee.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolarCoffee.Services.Inventory
{
    public interface IInventoryService
    {
        List<ProductInvertory> GetCurrentInventory();
        ServiceResponse<ProductInvertory> UpdateUnitsAvailable(int id, int adjustment);
        ProductInvertory GetByProductId(int productId);
        List<ProductInvertorySnapshot> GetSnapshotHistory();
    }
}