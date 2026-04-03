using Virtual_Factory.Dtos;
using Virtual_Factory.Services;

namespace Virtual_Factory.Endpoints
{
    public static class AssistantEndpoints
    {
        public static IEndpointRouteBuilder MapAssistantEndpoints(
            this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/assistant/equipment/{equipmentId}", async (
                string equipmentId,
                IAssistantService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.GetEquipmentAssistantResponseAsync(
                    equipmentId, cancellationToken);

                return result is null ? Results.NotFound() : Results.Ok(result);
            });

            app.MapPost("/api/assistant/ask", async (
                AssistantAskRequestDto body,
                IAssistantService service,
                CancellationToken cancellationToken) =>
            {
                var result = await service.AskAsync(body.EquipmentName, cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            });

            return app;
        }
    }
}
