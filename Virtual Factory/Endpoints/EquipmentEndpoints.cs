using Virtual_Factory.Services;

namespace Virtual_Factory.Endpoints
{
    public static class EquipmentEndpoints
    {
        public static IEndpointRouteBuilder MapEquipmentEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/equipment/event-summary", async (
                IEquipmentEventSummaryService service,
                CancellationToken cancellationToken) =>
            {
                var data = await service.GetSummaryAsync(cancellationToken);
                return Results.Ok(data);
            });

            app.MapGet("/api/equipment/{equipmentId}/event-timeline", async (
    string equipmentId,
    int? hours,
    IEquipmentEventSummaryService service,
    CancellationToken cancellationToken) =>
            {
                var timeline = await service.GetTimelineAsync(
                    equipmentId,
                    hours ?? 24,
                    cancellationToken);

                return Results.Ok(timeline);
            });

            app.MapGet("/api/equipment/availability", async (
    int? hours,
    IEquipmentAvailabilityService service) =>
            {
                var result = await service.GetAvailabilityAsync(hours ?? 24);
                return Results.Ok(result);
            });

            app.MapGet("/api/equipment/state-availability", async (
    int? hours,
    IEquipmentAvailabilityService service) =>
            {
                var result = await service.GetStateAvailabilityAsync(hours ?? 24);
                return Results.Ok(result);
            });

            app.MapGet("/api/equipment/{equipmentId}/last-alarm", async (
    string equipmentId,
    IEquipmentEventSummaryService service,
    CancellationToken cancellationToken) =>
            {
                var ts = await service.GetLastAlarmAsync(equipmentId, cancellationToken);
                return ts.HasValue
                    ? Results.Ok(new { timestampUtc = ts.Value })
                    : Results.NotFound();
            });

            app.MapGet("/api/equipment/{equipmentId}/last-stopped", async (
    string equipmentId,
    IEquipmentEventSummaryService service,
    CancellationToken cancellationToken) =>
            {
                var ts = await service.GetLastStoppedAsync(equipmentId, cancellationToken);
                return ts.HasValue
                    ? Results.Ok(new { timestampUtc = ts.Value })
                    : Results.NotFound();
            });

            app.MapGet("/api/equipment/{equipmentId}/state-availability", async (
    string equipmentId,
    int? hours,
    IEquipmentAvailabilityService service) =>
            {
                var result = await service.GetStateAvailabilityAsync(equipmentId, hours ?? 24);
                return result is null ? Results.NotFound() : Results.Ok(result);
            });

            app.MapGet("/api/equipment/{equipmentId}/context", async (
    string equipmentId,
    IOperationalContextService service,
    CancellationToken cancellationToken) =>
            {
                var context = await service.GetContextAsync(equipmentId, cancellationToken);
                return context is null ? Results.NotFound() : Results.Ok(context);
            });

            app.MapGet("/api/equipment/{equipmentId}/context/summary", async (
    string equipmentId,
    IEquipmentContextSummaryService service,
    CancellationToken cancellationToken) =>
            {
                var summary = await service.GetSummaryAsync(equipmentId, cancellationToken);
                return summary is null ? Results.NotFound() : Results.Ok(summary);
            });

            return app;
        }
    }
}