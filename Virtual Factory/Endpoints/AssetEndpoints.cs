using Virtual_Factory.Dtos;
using Virtual_Factory.Models;
using Virtual_Factory.Repositories;

namespace Virtual_Factory.Endpoints
{
    /// <summary>Minimal API endpoints for the asset hierarchy.</summary>
    public static class AssetEndpoints
    {
        public static IEndpointRouteBuilder MapAssetEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/assets", (IAssetRepository assets) =>
                Results.Ok(assets.GetAll().Select(ToDto)));

            app.MapGet("/api/assets/{id}", (string id, IAssetRepository assets) =>
            {
                var asset = assets.GetById(id);
                return asset is null ? Results.NotFound() : Results.Ok(ToDto(asset));
            });

            app.MapGet("/api/assets/{id}/children", (string id, IAssetRepository assets) =>
                Results.Ok(assets.GetChildren(id).Select(ToDto)));

            app.MapGet("/api/assets/{id}/points", (string id, IAssetRepository assets, ITelemetryPointRepository points) =>
            {
                if (assets.GetById(id) is null)
                    return Results.NotFound();

                return Results.Ok(points.GetByAssetId(id).Select(ToPointDto));
            });

            app.MapGet("/api/browse", (string? path, IAssetRepository assets) =>
            {
                if (string.IsNullOrEmpty(path))
                    return Results.Ok(new BrowseResultDto(null, []));

                var asset = assets.GetByPath(path);
                if (asset is null)
                    return Results.Ok(new BrowseResultDto(null, []));

                var children = assets.GetChildren(asset.Id).Select(ToDto).ToList();
                return Results.Ok(new BrowseResultDto(ToDto(asset), children));
            });

            return app;
        }

        private static TelemetryPointDto ToPointDto(TelemetryPointDefinition p) => new(
            p.Id,
            p.AssetId,
            p.Topic,
            p.PointName,
            p.DisplayName,
            p.DataType,
            p.Unit,
            p.Description,
            p.IsWritable,
            p.Category,
            p.Tags,
            p.CreatedAt);

        private static AssetDto ToDto(Asset a) => new(
            a.Id,
            a.Name,
            a.DisplayName,
            a.AssetType.ToString(),
            a.ParentId,
            a.Path,
            a.Description,
            a.Tags,
            a.ProfileId,
            a.ChildrenIds,
            a.CreatedAt);
    }
}
