namespace Virtual_Factory.Dtos
{
    public sealed class EquipmentEventTimelineItemDto
    {
        public long Id { get; set; }
        public string EquipmentId { get; set; } = "";
        public string EventType { get; set; } = "";
        public string NewState { get; set; } = "";
        public string? PreviousState { get; set; }
        public string? Source { get; set; }
        public DateTime TimestampUtc { get; set; }
        public int? DurationSeconds { get; set; }
    }
}