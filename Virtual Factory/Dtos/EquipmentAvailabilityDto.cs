namespace Virtual_Factory.Dtos
{
    public class EquipmentAvailabilityDto
    {
        public string EquipmentId { get; set; } = string.Empty;
        public double RunningPercent { get; set; }
        public double StoppedPercent { get; set; }
        public double OfflinePercent { get; set; }
        public double AlarmPercent { get; set; }
        public int WindowHours { get; set; }
    }
}