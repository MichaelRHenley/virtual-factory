using Virtual_Factory.Dtos;
using Virtual_Factory.Infrastructure;

namespace Virtual_Factory.Services
{
    public sealed class OperationalContextService : IOperationalContextService
    {
        private readonly ILatestPointValueStore        _store;
        private readonly IEquipmentEventSummaryService _eventSummary;
        private readonly IEquipmentAvailabilityService _availability;
        private readonly IWorkOrderAdapter             _workOrders;
        private readonly IScheduleAdapter              _schedules;
        private readonly IMaterialAdapter              _materials;

        public OperationalContextService(
            ILatestPointValueStore store,
            IEquipmentEventSummaryService eventSummary,
            IEquipmentAvailabilityService availability,
            IWorkOrderAdapter workOrders,
            IScheduleAdapter schedules,
            IMaterialAdapter materials)
        {
            _store        = store;
            _eventSummary = eventSummary;
            _availability = availability;
            _workOrders   = workOrders;
            _schedules    = schedules;
            _materials    = materials;
        }

        public async Task<EquipmentContextDto?> GetContextAsync(
            string equipmentId,
            CancellationToken cancellationToken = default)
        {
            // ── 1. Live telemetry state ────────────────────────────────────────────
            var equipmentPoints = _store.GetAll()
                .Where(p => string.Equals(
                    TopicParser.ExtractEquipmentName(p.Topic),
                    equipmentId,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            // ── 2. Event summary (24 h counts + latest event) ─────────────────────
            var allSummaries = await _eventSummary.GetSummaryAsync(cancellationToken);
            var summary = allSummaries.FirstOrDefault(s =>
                string.Equals(s.EquipmentId, equipmentId, StringComparison.OrdinalIgnoreCase));

            // Unknown equipment — nothing to return
            if (equipmentPoints.Count == 0 && summary is null)
                return null;

            // ── 3. Derive current run/alarm state from live topics ─────────────────
            var runState   = "unknown";
            var alarmState = "normal";
            var newestTs   = DateTimeOffset.MinValue;

            foreach (var p in equipmentPoints)
            {
                if (p.TimestampUtc > newestTs)
                    newestTs = p.TimestampUtc;

                var topic = (p.Topic ?? string.Empty).ToLowerInvariant();
                var value = (p.Value?.ToString() ?? string.Empty).ToLowerInvariant();

                if (topic.EndsWith("/run-status"))
                {
                    runState = value == "true"  ? "running"
                             : value == "false" ? "stopped"
                             : value;
                }

                if (topic.EndsWith("/alarm-state") || topic.Contains("alarm"))
                {
                    alarmState = value == "true"  ? "alarm"
                               : value == "false" ? "normal"
                               : value;
                }
            }

            var isOffline = equipmentPoints.Count > 0
                && (DateTimeOffset.UtcNow - newestTs).TotalSeconds > 10;

            var currentStatus = isOffline             ? "offline"
                              : alarmState == "alarm" ? "alarm"
                              : runState;

            // ── 4. Availability ────────────────────────────────────────────────────
            var avail1h  = await _availability.GetStateAvailabilityAsync(equipmentId, 1);
            var avail24h = await _availability.GetStateAvailabilityAsync(equipmentId, 24);

            // ── 5. Operational data via adapters ───────────────────────────────────
            var workOrders      = await _workOrders.GetByEquipmentAsync(equipmentId);
            var scheduleEntries = await _schedules.GetByEquipmentAsync(equipmentId);
            var materials       = await _materials.GetByEquipmentAsync(equipmentId);

            // Active work order: prefer in-progress, then open; newest start first
            WorkOrderDto? activeWorkOrder = null;
            var workOrder = workOrders
                .Where(w => w.Status == "in-progress" || w.Status == "open")
                .OrderByDescending(w => w.Status == "in-progress" ? 1 : 0)
                .ThenByDescending(w => w.ScheduledStartUtc)
                .FirstOrDefault();

            if (workOrder is not null)
            {
                activeWorkOrder = new WorkOrderDto
                {
                    Id                = workOrder.Id,
                    WorkOrderNumber   = workOrder.WorkOrderNumber,
                    Title             = workOrder.Title,
                    Status            = workOrder.Status,
                    Priority          = workOrder.Priority,
                    ScheduledStartUtc = workOrder.ScheduledStartUtc,
                    ScheduledEndUtc   = workOrder.ScheduledEndUtc,
                };
            }

            // Schedule: active window first, then next upcoming
            ScheduleItemDto? scheduledProduct = null;
            var now = DateTimeOffset.UtcNow;
            var scheduleEntry = scheduleEntries
                .Where(s => s.EndUtc > now)
                .OrderBy(s => s.StartUtc <= now ? 0 : 1)
                .ThenBy(s => s.StartUtc)
                .FirstOrDefault();

            if (scheduleEntry is not null)
            {
                scheduledProduct = new ScheduleItemDto
                {
                    Id           = scheduleEntry.Id,
                    Title        = scheduleEntry.Title,
                    ScheduleType = scheduleEntry.ScheduleType,
                    Status       = scheduleEntry.Status,
                    StartUtc     = scheduleEntry.StartUtc,
                    EndUtc       = scheduleEntry.EndUtc,
                };
            }

            // Material: first result; stock status is deterministic from code hash
            MaterialStatusDto? materialStatus = null;
            var material = materials.FirstOrDefault();
            if (material is not null)
            {
                var stockIndex = Math.Abs(material.Code.GetHashCode()) % 3;
                materialStatus = new MaterialStatusDto
                {
                    MaterialCode = material.Code,
                    Description  = material.Description,
                    Unit         = material.Unit,
                    StockStatus  = stockIndex switch
                    {
                        0 => "available",
                        1 => "low",
                        _ => "critical",
                    },
                };
            }

            // ── 6. Assemble response ───────────────────────────────────────────────
            return new EquipmentContextDto
            {
                EquipmentId      = equipmentId,
                CurrentStatus    = currentStatus,
                AlarmState       = alarmState,
                Availability1h   = avail1h?.RunningPercent,
                Availability24h  = avail24h?.RunningPercent,
                StopCount24h     = summary?.StopCount24h  ?? 0,
                AlarmCount24h    = summary?.AlarmCount24h ?? 0,
                LatestEvent      = summary?.LatestEvent,
                ActiveWorkOrder  = activeWorkOrder,
                ScheduledProduct = scheduledProduct,
                MaterialStatus   = materialStatus,
            };
        }
    }
}
