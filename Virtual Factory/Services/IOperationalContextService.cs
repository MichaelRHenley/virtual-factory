using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public interface IOperationalContextService
    {
        /// <summary>
        /// Returns the merged operational context for the given equipment, or
        /// <c>null</c> if the equipment is unknown (no live telemetry and no event history).
        /// </summary>
        Task<EquipmentContextDto?> GetContextAsync(
            string equipmentId,
            CancellationToken cancellationToken = default);
    }
}
