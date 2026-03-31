using Virtual_Factory.Models;

namespace Virtual_Factory.Services
{
    public interface IWorkOrderAdapter
    {
        Task<IReadOnlyList<WorkOrder>> GetByEquipmentAsync(string equipmentId);
    }
}
