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

        Task<DateTime?> GetLastStoppedAsync(
            string equipmentId,
            CancellationToken cancellationToken = default);

        Task<DateTime?> GetLastAlarmAsync(
            string equipmentId,
            CancellationToken cancellationToken = default);
    }
}