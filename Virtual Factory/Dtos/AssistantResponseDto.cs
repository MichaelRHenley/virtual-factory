namespace Virtual_Factory.Dtos
{
    public sealed class AssistantResponseDto
    {
        public string  EquipmentId       { get; set; } = string.Empty;
        public string  InputSummary      { get; set; } = string.Empty;
        public string  AssistantResponse { get; set; } = string.Empty;
        public string  ContextSummary    { get; set; } = string.Empty;
        public string? Answer            => AssistantResponse;

        // Structured activity metrics — null when not available
        public double? Availability1h { get; set; }
        public int?    Stops24h       { get; set; }
        public int?    Alarms24h      { get; set; }
    }
}
