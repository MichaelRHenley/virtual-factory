using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;
using System.Linq;
using Virtual_Factory.Dtos;
using Virtual_Factory.Services;

namespace Virtual_Factory.Endpoints
{
    public static class BomInventoryEndpoints
    {
        public static IEndpointRouteBuilder MapBomInventoryEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("/api");

            group.MapGet("/bom/{sku}", async (string sku, IBomAdapter bomAdapter) =>
            {
                if (string.IsNullOrWhiteSpace(sku))
                    return Results.BadRequest("sku is required");

                var bom = await bomAdapter.GetBomBySkuAsync(sku);
                return bom.Count == 0 ? Results.NotFound() : Results.Ok(bom);
            });

            group.MapGet("/inventory/material/{materialId}", async (string materialId, IInventoryAdapter inventoryAdapter) =>
            {
                if (string.IsNullOrWhiteSpace(materialId))
                    return Results.BadRequest("materialId is required");

                var item = await inventoryAdapter.GetInventoryByMaterialAsync(materialId);
                return item is null ? Results.NotFound() : Results.Ok(item);
            });

            group.MapPost("/inventory/materials", async (HttpContext httpContext, IInventoryAdapter inventoryAdapter) =>
            {
                var materialIds = await httpContext.Request.ReadFromJsonAsync<List<string>>();
                materialIds ??= new List<string>();

                var items = await inventoryAdapter.GetInventoryForMaterialsAsync(materialIds.Distinct());
                return Results.Ok(items);
            });

            return endpoints;
        }
    }
}
