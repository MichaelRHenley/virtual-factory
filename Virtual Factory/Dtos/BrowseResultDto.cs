namespace Virtual_Factory.Dtos
{
    /// <summary>Response returned by the browse endpoint, containing the matched asset and its direct children.</summary>
    public sealed record BrowseResultDto(
        AssetDto? Asset,
        IReadOnlyList<AssetDto> Children);
}
