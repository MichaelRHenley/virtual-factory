using Virtual_Factory.Dtos;
using Virtual_Factory.Infrastructure;
using Virtual_Factory.Models;

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
        private readonly IProductionOrderAdapter       _productionOrders;
        private readonly IBomAdapter                   _bom;
        private readonly IInventoryAdapter             _inventory;
        private readonly IMaintenanceAdapter           _maintenance;
        private readonly IEquipmentEventAdapter        _equipmentEvents;
        private readonly ILogger<OperationalContextService> _log;

        public OperationalContextService(
            ILatestPointValueStore store,
            IEquipmentEventSummaryService eventSummary,
            IEquipmentAvailabilityService availability,
            IWorkOrderAdapter workOrders,
            IScheduleAdapter schedules,
            IMaterialAdapter materials,
            IProductionOrderAdapter productionOrders,
            IBomAdapter bom,
            IInventoryAdapter inventory,
            IMaintenanceAdapter maintenance,
            IEquipmentEventAdapter equipmentEvents,
            ILogger<OperationalContextService> log)
        {
            _store        = store;
            _eventSummary = eventSummary;
            _availability = availability;
            _workOrders   = workOrders;
            _schedules    = schedules;
            _materials    = materials;
            _productionOrders = productionOrders;
            _bom          = bom;
            _inventory    = inventory;
            _log          = log;
        }

        public async Task<EquipmentContextDto?> GetContextAsync(
            string equipmentId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await GetContextCoreAsync(equipmentId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        private async Task<EquipmentContextDto?> GetContextCoreAsync(
            string equipmentId,
            CancellationToken cancellationToken)
        {
            // Defaults for BOM/inventory context so we can safely use them in the final DTO
            string? activeSku = null;
            var bomItems = new List<BomItemDto>();
            var inventoryItems = new List<InventoryItemDto>();
            var hasShortage = false;

            var openPmTasks = new List<PreventiveMaintenanceTaskDto>();
            var hasOverduePm = false;
            var overduePmCount = 0;
            var upcomingPmCount = 0;

            var recentEvents = new List<EquipmentEventDto>();
            EquipmentEventDto? lastAlarmEvent = null;
            EquipmentEventDto? lastStopEvent = null;
            TimeSpan? timeSinceLastAlarm = null;
            TimeSpan? timeSinceLastStop = null;

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
            /*if (equipmentPoints.Count == 0
    && summary is null
    && !(await _workOrders.GetByEquipmentAsync(equipmentId)).Any())
            {
                return null;
            }*/

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

            // ── 5a. Production order (adapter is in-memory / API-safe) ───────────
            ProductionOrderDto? activeProductionOrder = null;
            try
            {
                activeProductionOrder = await _productionOrders.GetActiveOrderAsync(equipmentId);
                activeSku = activeProductionOrder?.Sku;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogWarning(ex, "Production-order adapter unavailable for {EquipmentId} — skipping", equipmentId);
            }

            // ── 5b. BOM + inventory for active SKU ──────────────────────────────
            if (!string.IsNullOrWhiteSpace(activeSku))
            {
                try
                {
                    bomItems = await _bom.GetBomBySkuAsync(activeSku);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.LogWarning(ex, "BOM adapter unavailable for {EquipmentId} — skipping", equipmentId);
                }

                if (bomItems.Count > 0)
                {
                    try
                    {
                        var materialIds = bomItems
                            .Select(b => b.MaterialId)
                            .Where(id => !string.IsNullOrWhiteSpace(id))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        if (materialIds.Count > 0)
                        {
                            inventoryItems = await _inventory.GetInventoryForMaterialsAsync(materialIds);

                            // Determine shortages based on StockStatus or available vs required
                            var inventoryById = inventoryItems
                                .ToDictionary(i => i.MaterialId, StringComparer.OrdinalIgnoreCase);

                            foreach (var bom in bomItems)
                            {
                                if (!inventoryById.TryGetValue(bom.MaterialId, out var inv))
                                {
                                    hasShortage = true;
                                    break;
                                }

                                if (string.Equals(inv.StockStatus, "short", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(inv.StockStatus, "at_risk", StringComparison.OrdinalIgnoreCase))
                                {
                                    hasShortage = true;
                                    break;
                                }

                                var netAvailable = inv.AvailableQuantity - inv.ReservedQuantity;
                                if (netAvailable < bom.RequiredQuantity)
                                {
                                    hasShortage = true;
                                    break;
                                }
                            }
                        }

            // ── 5c. Preventive maintenance context ──────────────────────────────
            try
            {
                openPmTasks = await _maintenance.GetOpenPmTasksAsync(equipmentId);

                if (openPmTasks.Count > 0)
                {
                    var overdue = openPmTasks.Where(t => t.IsOverdue).ToList();
                    hasOverduePm = overdue.Count > 0;
                    overduePmCount = overdue.Count;

                    var upcoming = openPmTasks
                        .Where(t => !t.IsOverdue && t.DueDateUtc > DateTime.UtcNow)
                        .ToList();
                    upcomingPmCount = upcoming.Count;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogWarning(ex, "Maintenance adapter unavailable for {EquipmentId} — skipping", equipmentId);
            }

            // ── 5d. Equipment event history ─────────────────────────────────────
            try
            {
                recentEvents = await _equipmentEvents.GetRecentEventsAsync(equipmentId, 2);

                lastAlarmEvent = await _equipmentEvents.GetLastAlarmAsync(equipmentId);
                lastStopEvent  = await _equipmentEvents.GetLastStopAsync(equipmentId);

                var nowTs = DateTime.UtcNow;
                if (lastAlarmEvent is not null)
                    timeSinceLastAlarm = nowTs - lastAlarmEvent.TimestampUtc;
                if (lastStopEvent is not null)
                    timeSinceLastStop = nowTs - lastStopEvent.TimestampUtc;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogWarning(ex, "Equipment event adapter unavailable for {EquipmentId} — skipping", equipmentId);
            }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _log.LogWarning(ex, "Inventory adapter unavailable for {EquipmentId} — skipping", equipmentId);
                    }
                }
            }

            // ── 5. Operational data via adapters ───────────────────────────────────
            // Each adapter makes an HTTP call to the mock API (localhost:5177).  Guard each
            // call individually so that an unavailable mock service never blocks the
            // telemetry-based context from being built.
            IReadOnlyList<WorkOrder> workOrders;
            try   { workOrders = await _workOrders.GetByEquipmentAsync(equipmentId); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogWarning(ex, "Work-order adapter unavailable for {EquipmentId} — skipping", equipmentId);
                workOrders = [];
            }

            IReadOnlyList<ScheduleEntry> scheduleEntries;
            try   { scheduleEntries = await _schedules.GetByEquipmentAsync(equipmentId); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogWarning(ex, "Schedule adapter unavailable for {EquipmentId} — skipping", equipmentId);
                scheduleEntries = [];
            }

            IReadOnlyList<Material> materials;
            try   { materials = await _materials.GetByEquipmentAsync(equipmentId); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogWarning(ex, "Material adapter unavailable for {EquipmentId} — skipping", equipmentId);
                materials = [];
            }

            _log.LogInformation("Work orders for {EquipmentId}: {Count}", equipmentId, workOrders.Count);
            foreach (var w in workOrders)
                _log.LogInformation(
                    "Filtering WO {WorkOrderId}: assetId={AssetId} status={Status}",
                    w.WorkOrderNumber, w.AssetId, w.Status);

            // Active work order: prefer InProgress, then Open (case-insensitive); newest start first
            WorkOrderDto? activeWorkOrder = null;
            var workOrder = workOrders
                .Where(w => string.Equals(w.Status, "InProgress", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(w.Status, "Open", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(w => string.Equals(w.Status, "InProgress", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
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

            // Current work order: active statuses only (case-insensitive), newest scheduled start first
            WorkOrderContextDto? currentWorkOrder = null;
            var activeStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "Released", "Running", "Scheduled", "InProgress" };

            var currentWo = workOrders
                .Where(w => activeStatuses.Contains(w.Status, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(w => w.ScheduledStartUtc)
                .FirstOrDefault();

            if (currentWo is not null)
            {
                currentWorkOrder = new WorkOrderContextDto
                {
                    WorkOrderId       = currentWo.WorkOrderNumber,
                    Description       = currentWo.Description ?? currentWo.Title,
                    Status            = currentWo.Status,
                    ScheduledStartUtc = currentWo.ScheduledStartUtc,
                    ScheduledEndUtc   = currentWo.ScheduledEndUtc,
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
                CurrentWorkOrder = currentWorkOrder,
                ScheduledProduct = scheduledProduct,
                MaterialStatus   = materialStatus,
                ActiveProductionOrder = activeProductionOrder,
                ActiveSku        = activeSku,
                BomItems         = bomItems,
                InventoryItems   = inventoryItems,
                HasMaterialShortage = hasShortage,
                OpenPmTasks      = openPmTasks,
                HasOverduePm     = hasOverduePm,
                OverduePmCount   = overduePmCount,
                UpcomingPmCount  = upcomingPmCount,
                RecentEvents     = recentEvents,
                LastAlarmEvent   = lastAlarmEvent,
                LastStopEvent    = lastStopEvent,
                TimeSinceLastAlarm = timeSinceLastAlarm,
                TimeSinceLastStop  = timeSinceLastStop,
            };
        }
    }
}

