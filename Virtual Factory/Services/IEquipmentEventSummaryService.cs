using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services;

public interface IEquipmentEventSummaryService
{
    Task<List<EquipmentEventSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default);
}