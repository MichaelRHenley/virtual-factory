namespace Virtual_Factory.Dtos
{
    public sealed class EquipmentStateAvailabilityDto
    {
        public string EquipmentId { get; set; } = string.Empty;
        public int WindowHours { get; set; }
        public double RunningSeconds { get; set; }
        public double StoppedSeconds { get; set; }
        public double UnknownSeconds { get; set; }
        public double RunningPercent { get; set; }
        public double StoppedPercent { get; set; }
        public double UnknownPercent { get; set; }
    }
}
