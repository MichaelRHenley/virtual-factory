using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public interface IEquipmentContextSummaryService
    {
        Task<EquipmentContextSummaryDto?> GetSummaryAsync(
            string equipmentId,
            CancellationToken cancellationToken = default);
    }
}
