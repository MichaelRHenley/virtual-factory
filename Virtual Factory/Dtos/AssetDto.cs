namespace Virtual_Factory.Dtos
{
    /// <summary>API response representation of an asset node in the ISA-95 hierarchy.</summary>
    public sealed record AssetDto(
        string Id,
        string Name,
        string DisplayName,
        string AssetType,
        string? ParentId,
        string Path,
        string? Description,
        IReadOnlyDictionary<string, string> Tags,
        string? ProfileId,
        IReadOnlyList<string> ChildrenIds,
        DateTimeOffset CreatedAt);
}
