using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public interface IEquipmentAvailabilityService
    {
        Task<List<EquipmentAvailabilityDto>> GetAvailabilityAsync(int hours = 24);
        Task<List<EquipmentStateAvailabilityDto>> GetStateAvailabilityAsync(int hours = 24);
        Task<EquipmentStateAvailabilityDto?> GetStateAvailabilityAsync(string equipmentId, int hours = 24);
    }
}