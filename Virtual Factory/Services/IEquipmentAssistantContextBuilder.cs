using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public interface IEquipmentAssistantContextBuilder
    {
        Task<EquipmentAssistantContextDto?> BuildAsync(
            string equipmentId,
            CancellationToken cancellationToken = default);
    }
}
