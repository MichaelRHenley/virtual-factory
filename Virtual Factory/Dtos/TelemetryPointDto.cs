namespace Virtual_Factory.Dtos
{
    /// <summary>API response representation of a telemetry point definition.</summary>
    public sealed record TelemetryPointDto(
        string Id,
        string AssetId,
        string Topic,
        string PointName,
        string DisplayName,
        string DataType,
        string? Unit,
        string? Description,
        bool IsWritable,
        string Category,
        IReadOnlyDictionary<string, string> Tags,
        DateTimeOffset CreatedAt);
}
