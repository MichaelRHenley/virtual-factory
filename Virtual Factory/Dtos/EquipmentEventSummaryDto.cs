namespace Virtual_Factory.Dtos;

public sealed class EquipmentEventSummaryDto
{
    public string EquipmentId { get; set; } = "";
    public int StopCount24h { get; set; }
    public int AlarmCount24h { get; set; }
    public LatestEventDto? LatestEvent { get; set; }
    public CurrentAlarmDto? LongestCurrentAlarm { get; set; }
}

public sealed class LatestEventDto
{
    public string EventType { get; set; } = "";
    public string? EventName { get; set; }
    public string State { get; set; } = "";
    public DateTime StartTimeUtc { get; set; }
    public int DurationSeconds { get; set; }
}

public sealed class CurrentAlarmDto
{
    public string EventName { get; set; } = "";
    public DateTime StartTimeUtc { get; set; }
    public int DurationSeconds { get; set; }
}