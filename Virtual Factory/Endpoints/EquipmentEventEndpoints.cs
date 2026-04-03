using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Virtual_Factory.Services;

namespace Virtual_Factory.Endpoints
{
    public static class EquipmentEventEndpoints
    {
        public static IEndpointRouteBuilder MapEquipmentEventEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/events");

            group.MapGet("/recent", async (IEquipmentEventAdapter adapter, string equipmentId, int hours) =>
            {
                if (string.IsNullOrWhiteSpace(equipmentId))
                    return Results.BadRequest("equipmentId is required");

                if (hours <= 0)
                    hours = 2;

                var events = await adapter.GetRecentEventsAsync(equipmentId, hours);
                return Results.Ok(events);
            });

            group.MapGet("/last-alarm", async (IEquipmentEventAdapter adapter, string equipmentId) =>
            {
                if (string.IsNullOrWhiteSpace(equipmentId))
                    return Results.BadRequest("equipmentId is required");

                var evt = await adapter.GetLastAlarmAsync(equipmentId);
                return evt is null ? Results.NotFound() : Results.Ok(evt);
            });

            group.MapGet("/last-stop", async (IEquipmentEventAdapter adapter, string equipmentId) =>
            {
                if (string.IsNullOrWhiteSpace(equipmentId))
                    return Results.BadRequest("equipmentId is required");

                var evt = await adapter.GetLastStopAsync(equipmentId);
                return evt is null ? Results.NotFound() : Results.Ok(evt);
            });

            return endpoints;
        }
    }
}
