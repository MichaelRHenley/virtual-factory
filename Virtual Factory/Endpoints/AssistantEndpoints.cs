using Virtual_Factory.Dtos;
using Virtual_Factory.Services;

namespace Virtual_Factory.Endpoints
{
    public static class AssistantEndpoints
    {
        public static IEndpointRouteBuilder MapAssistantEndpoints(
            this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/assistant/context/{equipmentId}", async (
                string equipmentId,
                IEquipmentAssistantContextBuilder builder,
                CancellationToken cancellationToken) =>
            {
                var ctx = await builder.BuildAsync(equipmentId, cancellationToken);

                if (ctx is null)
                {
                    return Results.NotFound();
                }

                var response = new
                {
                    equipmentId = ctx.EquipmentId,
                    inputSummary = ctx.InputSummary,
                    contextSummary = ctx.ContextSummary,
                    signalHealthSummary = ctx.InputSummary,
                    recentActivitySummary = ctx.ContextSummary,
                    maintenanceStatusSummary = ctx.ContextSummary
                };

                return Results.Ok(response);
            });

            app.MapPost("/api/assistant/equipment/{equipmentId}", async (
                string equipmentId,
                IAssistantService service,
                ILoggerFactory loggerFactory,
                CancellationToken cancellationToken) =>
            {
                var log = loggerFactory.CreateLogger("AssistantEndpoints.Equipment");
                var overallStart = DateTime.UtcNow;

                log.LogInformation("Assistant equipment endpoint ENTER for {EquipmentId} at {StartTime}",
                    equipmentId, overallStart);

                var result = await service.GetEquipmentAssistantResponseAsync(
                    equipmentId, cancellationToken);

                var overallEnd = DateTime.UtcNow;
                var totalMs = (overallEnd - overallStart).TotalMilliseconds;

                log.LogInformation("Assistant equipment endpoint EXIT for {EquipmentId} after {DurationMs} ms",
                    equipmentId, totalMs);

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
