namespace Virtual_Factory.Dtos
{
    /// <summary>Request body for POST /api/write.</summary>
    public sealed record WritePointRequest(
        string Topic,
        string Value,
        string Source = "api");
}
