using System.Collections.Generic;
using System.Threading.Tasks;
using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public interface IEquipmentEventAdapter
    {
        Task<List<EquipmentEventDto>> GetRecentEventsAsync(string equipmentId, int hours);
        Task<EquipmentEventDto?> GetLastAlarmAsync(string equipmentId);
        Task<EquipmentEventDto?> GetLastStopAsync(string equipmentId);
    }
}
