using System.Collections.Generic;
using System.Threading.Tasks;
using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public interface IInventoryAdapter
    {
        Task<List<InventoryItemDto>> GetInventoryForMaterialsAsync(IEnumerable<string> materialIds);
        Task<InventoryItemDto?> GetInventoryByMaterialAsync(string materialId);
    }
}
