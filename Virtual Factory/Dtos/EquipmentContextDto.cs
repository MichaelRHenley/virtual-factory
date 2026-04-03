using System.Collections.Generic;

namespace Virtual_Factory.Dtos
{
    public sealed class EquipmentContextDto
    {
        public string EquipmentId      { get; set; } = string.Empty;
        /// <summary>running | stopped | alarm | offline | unknown</summary>
        public string CurrentStatus    { get; set; } = "unknown";
        /// <summary>alarm | normal</summary>
        public string AlarmState       { get; set; } = "normal";
        /// <summary>RunningPercent over the last 1 h. Null when no run-state history exists.</summary>
        public double? Availability1h  { get; set; }
        /// <summary>RunningPercent over the last 24 h.</summary>
        public double? Availability24h { get; set; }
        public int StopCount24h        { get; set; }
        public int AlarmCount24h       { get; set; }
        public LatestEventDto?    LatestEvent      { get; set; }
        public WorkOrderDto?         ActiveWorkOrder  { get; set; }
        public WorkOrderContextDto?  CurrentWorkOrder { get; set; }
        public ScheduleItemDto?      ScheduledProduct { get; set; }
        public MaterialStatusDto?    MaterialStatus   { get; set; }
        public ProductionOrderDto?   ActiveProductionOrder { get; set; }

        public string? ActiveSku { get; set; }
        public List<BomItemDto> BomItems { get; set; } = new();
        public List<InventoryItemDto> InventoryItems { get; set; } = new();
        public bool HasMaterialShortage { get; set; }

        public List<PreventiveMaintenanceTaskDto> OpenPmTasks { get; set; } = new();
        public bool HasOverduePm { get; set; }
        public int OverduePmCount { get; set; }
        public int UpcomingPmCount { get; set; }

        public List<EquipmentEventDto> RecentEvents { get; set; } = new();
        public EquipmentEventDto? LastAlarmEvent { get; set; }
        public EquipmentEventDto? LastStopEvent { get; set; }
        public TimeSpan? TimeSinceLastAlarm { get; set; }
        public TimeSpan? TimeSinceLastStop { get; set; }
    }

    public sealed class WorkOrderContextDto
    {
        public string WorkOrderId          { get; set; } = string.Empty;
        public string Description          { get; set; } = string.Empty;
        public string Status               { get; set; } = string.Empty;
        public DateTimeOffset? ScheduledStartUtc { get; set; }
        public DateTimeOffset? ScheduledEndUtc   { get; set; }
    }

    public sealed class WorkOrderDto
    {
        public string Id                         { get; set; } = string.Empty;
        public string WorkOrderNumber            { get; set; } = string.Empty;
        public string Title                      { get; set; } = string.Empty;
        public string Status                     { get; set; } = string.Empty;
        public string Priority                   { get; set; } = string.Empty;
        public DateTimeOffset? ScheduledStartUtc { get; set; }
        public DateTimeOffset? ScheduledEndUtc   { get; set; }
    }

    public sealed class ScheduleItemDto
    {
        public string Id             { get; set; } = string.Empty;
        public string Title          { get; set; } = string.Empty;
        public string ScheduleType   { get; set; } = string.Empty;
        public string Status         { get; set; } = string.Empty;
        public DateTimeOffset StartUtc { get; set; }
        public DateTimeOffset EndUtc   { get; set; }
    }

    public sealed class MaterialStatusDto
    {
        public string MaterialCode { get; set; } = string.Empty;
        public string Description  { get; set; } = string.Empty;
        public string Unit         { get; set; } = string.Empty;
        /// <summary>available | low | critical</summary>
        public string StockStatus  { get; set; } = "available";
    }
}
