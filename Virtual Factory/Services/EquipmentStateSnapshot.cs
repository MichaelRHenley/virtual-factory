namespace Virtual_Factory.Services
{
    public class EquipmentStateSnapshot
    {
        public string EquipmentName { get; set; } = string.Empty;
        public string RunState { get; set; } = "unknown";
        public string AlarmState { get; set; } = "normal";
        public string ConnectivityState { get; set; } = "online";
        public string? Source { get; set; }
        public DateTime TimestampUtc { get; set; }
    }
}