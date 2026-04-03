using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Virtual_Factory.Services;

namespace Virtual_Factory.Endpoints
{
    public static class MaintenanceEndpoints
    {
        public static IEndpointRouteBuilder MapMaintenanceEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/maintenance");

            group.MapGet("/open", async (IMaintenanceAdapter adapter, string equipmentId) =>
            {
                if (string.IsNullOrWhiteSpace(equipmentId))
                    return Results.BadRequest("equipmentId is required");

                var tasks = await adapter.GetOpenPmTasksAsync(equipmentId);
                return Results.Ok(tasks);
            });

            group.MapGet("/overdue", async (IMaintenanceAdapter adapter, string equipmentId) =>
            {
                if (string.IsNullOrWhiteSpace(equipmentId))
                    return Results.BadRequest("equipmentId is required");

                var tasks = await adapter.GetOverduePmTasksAsync(equipmentId);
                return Results.Ok(tasks);
            });

            group.MapGet("/upcoming", async (IMaintenanceAdapter adapter, string equipmentId) =>
            {
                if (string.IsNullOrWhiteSpace(equipmentId))
                    return Results.BadRequest("equipmentId is required");

                var tasks = await adapter.GetUpcomingPmTasksAsync(equipmentId);
                return Results.Ok(tasks);
            });

            return endpoints;
        }
    }
}
