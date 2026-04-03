using System.Collections.Generic;
using System.Threading.Tasks;
using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public interface IBomAdapter
    {
        Task<List<BomItemDto>> GetBomBySkuAsync(string sku);
    }
}
