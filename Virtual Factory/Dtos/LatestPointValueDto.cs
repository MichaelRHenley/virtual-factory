namespace Virtual_Factory.Dtos
{
    /// <summary>API response representation of the latest cached value for a single MQTT topic.</summary>
    public sealed record LatestPointValueDto(
        string Topic,
        string Value,
        DateTimeOffset TimestampUtc,
        string Status,
        string Source,
        string? AssetId,
        string? PointName,
        IReadOnlyDictionary<string, string> Metadata);
}
