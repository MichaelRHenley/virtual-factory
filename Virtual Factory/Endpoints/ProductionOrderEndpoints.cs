using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Virtual_Factory.Services;

namespace Virtual_Factory.Endpoints
{
    public static class ProductionOrderEndpoints
    {
        public static IEndpointRouteBuilder MapProductionOrderEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api/production-orders");

            group.MapGet(string.Empty, async (IProductionOrderAdapter adapter) =>
            {
                var orders = await adapter.GetAllAsync();
                return Results.Ok(orders);
            });

            group.MapGet("/active", async (IProductionOrderAdapter adapter, string equipmentId) =>
            {
                if (string.IsNullOrWhiteSpace(equipmentId))
                {
                    return Results.BadRequest("equipmentId is required");
                }

                var order = await adapter.GetActiveOrderAsync(equipmentId);
                return order is null ? Results.NotFound() : Results.Ok(order);
            });

            group.MapGet("/scheduled", async (IProductionOrderAdapter adapter, string equipmentId) =>
            {
                if (string.IsNullOrWhiteSpace(equipmentId))
                {
                    return Results.BadRequest("equipmentId is required");
                }

                var orders = await adapter.GetScheduledOrdersAsync(equipmentId);
                return Results.Ok(orders);
            });

            return endpoints;
        }
    }
}
