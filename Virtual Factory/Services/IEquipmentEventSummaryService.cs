using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public interface IEquipmentEventSummaryService
    {
        Task<List<EquipmentEventSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default);

        Task<List<EquipmentEventTimelineItemDto>> GetTimelineAsync(
            string equipmentId,
            int hours = 24,
            CancellationToken cancellationToken = default);
    }
}