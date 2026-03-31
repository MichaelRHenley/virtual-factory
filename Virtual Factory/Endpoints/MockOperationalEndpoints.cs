using Virtual_Factory.Repositories;

namespace Virtual_Factory.Endpoints
{
    /// <summary>
    /// Simulated REST endpoints that stand in for external ERP / MES data sources.
    /// Backed by the same in-memory repositories as the rest of the app but exposed
    /// over HTTP so OperationalContextService consumes them as remote calls.
    /// </summary>
    public static class MockOperationalEndpoints
    {
        public static IEndpointRouteBuilder MapMockOperationalEndpoints(
            this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/mock/work-orders", (
                string? equipmentName,
                IAssetRepository assets,
                IWorkOrderRepository workOrders) =>
            {
                if (string.IsNullOrWhiteSpace(equipmentName))
                    return Results.BadRequest("equipmentName is required");

                var asset = assets.GetAll().FirstOrDefault(a =>
                    a.Name.Equals(equipmentName, StringComparison.OrdinalIgnoreCase));

                if (asset is null)
                    return Results.Ok(Array.Empty<object>());

                return Results.Ok(workOrders.GetByAsset(asset.Id));
            })
            .WithName("MockWorkOrders")
            .WithTags("Mock");

            app.MapGet("/api/mock/schedules", (
                string? equipmentName,
                IAssetRepository assets,
                IScheduleRepository schedules) =>
            {
                if (string.IsNullOrWhiteSpace(equipmentName))
                    return Results.BadRequest("equipmentName is required");

                var asset = assets.GetAll().FirstOrDefault(a =>
                    a.Name.Equals(equipmentName, StringComparison.OrdinalIgnoreCase));

                if (asset is null)
                    return Results.Ok(Array.Empty<object>());

                return Results.Ok(schedules.GetByAsset(asset.Id));
            })
            .WithName("MockSchedules")
            .WithTags("Mock");

            app.MapGet("/api/mock/materials", (
                string? equipmentName,
                IAssetRepository assets,
                IMaterialRepository materials) =>
            {
                if (string.IsNullOrWhiteSpace(equipmentName))
                    return Results.BadRequest("equipmentName is required");

                var asset = assets.GetAll().FirstOrDefault(a =>
                    a.Name.Equals(equipmentName, StringComparison.OrdinalIgnoreCase));

                if (asset is null)
                    return Results.Ok(Array.Empty<object>());

                return Results.Ok(
                    materials.GetAll()
                        .Where(m => m.UsedBy.Contains(asset.Id, StringComparer.OrdinalIgnoreCase))
                        .ToList());
            })
            .WithName("MockMaterials")
            .WithTags("Mock");

            return app;
        }
    }
}
