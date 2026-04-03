using System.Collections.Generic;
using System.Threading.Tasks;
using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public interface ISkuRunProfileProvider
    {
        Task<List<SkuRunProfileDto>> GetProfilesForSkuAsync(string sku, string equipmentId);
        Task<SkuRunProfileDto?> GetProfileAsync(string sku, string equipmentId, string signalName);
    }
}
