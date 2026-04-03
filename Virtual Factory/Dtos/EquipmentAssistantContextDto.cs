namespace Virtual_Factory.Dtos
{
    public sealed class EquipmentAssistantContextDto
    {
        public string EquipmentId { get; set; } = string.Empty;
        public string CurrentState { get; set; } = "Unknown";

        public EquipmentEventSummaryDto? EventSummary { get; set; }
        public EquipmentStateAvailabilityDto? Availability1h { get; set; }
        public List<EquipmentEventTimelineItemDto> RecentTimeline { get; set; } = new();

        public EquipmentContextDto? OperationalContext { get; set; }

        public DateTime? LastAlarmUtc { get; set; }
        public DateTime? LastStoppedUtc { get; set; }

        public List<AssistantSignalSnapshotDto> KeySignals { get; set; } = new();
        public string HumanSummary   { get; set; } = string.Empty;
        public string InputSummary   { get; set; } = string.Empty;
        public string ContextSummary { get; set; } = string.Empty;
    }

    public sealed class AssistantSignalSnapshotDto
    {
        public string    SignalName     { get; set; } = string.Empty;
        public string    Value          { get; set; } = string.Empty;
        public string    Status         { get; set; } = string.Empty;
        public DateTime? TimestampUtc   { get; set; }
        public double?   NumericValue   { get; set; }
        public string    TrendDirection { get; set; } = string.Empty;
    }
}
