using Virtual_Factory.Models;

namespace Virtual_Factory.Services
{
    public interface IMaterialAdapter
    {
        Task<IReadOnlyList<Material>> GetByEquipmentAsync(string equipmentId);
    }
}
