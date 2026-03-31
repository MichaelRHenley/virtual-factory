using Virtual_Factory.Models;

namespace Virtual_Factory.Services
{
    public interface IScheduleAdapter
    {
        Task<IReadOnlyList<ScheduleEntry>> GetByEquipmentAsync(string equipmentId);
    }
}
