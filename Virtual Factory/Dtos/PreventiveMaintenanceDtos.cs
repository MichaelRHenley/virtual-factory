using System;
using System.Collections.Generic;

namespace Virtual_Factory.Dtos
{
    public sealed class PreventiveMaintenanceTaskDto
    {
        public string TaskId { get; set; } = string.Empty;
        public string EquipmentId { get; set; } = string.Empty;
        public string TaskDescription { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty;
        public DateTime DueDateUtc { get; set; }
        public int? DueRuntimeHours { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public bool IsOverdue { get; set; }

        public string? TriggerSignalName { get; set; }
        public double? TriggerThreshold { get; set; }
        public double? CurrentSignalValue { get; set; }
        public bool IsThresholdApproaching { get; set; }
        public bool IsThresholdExceeded { get; set; }
    }
}
