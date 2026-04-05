using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public interface IAssistantService
    {
        Task<AssistantResponseDto?> GetEquipmentAssistantResponseAsync(
            string equipmentId,
            CancellationToken cancellationToken = default);

        Task<AssistantResponseDto?> AskAsync(
            string? equipmentName,
            CancellationToken cancellationToken = default);

        Task<AssistantResponseDto> BuildContextualAnswerAsync(
            AssistantAskRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
