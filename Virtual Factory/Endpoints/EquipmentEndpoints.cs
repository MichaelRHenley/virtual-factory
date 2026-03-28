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

            return app;
        }
    }
}