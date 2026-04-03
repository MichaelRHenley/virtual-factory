using Microsoft.EntityFrameworkCore;
using Virtual_Factory.Data;
using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public sealed class EquipmentAssistantContextBuilder : IEquipmentAssistantContextBuilder
    {
        private readonly IEquipmentEventSummaryService   _eventSummary;
        private readonly IEquipmentAvailabilityService   _availability;
        private readonly IOperationalContextService      _operationalContext;
        private readonly IEquipmentContextSummaryService _contextSummary;
        private readonly ILatestPointValueStore          _store;
        private readonly ISignalMetadataProvider         _signalMetadata;
        private readonly ISkuRunProfileProvider          _skuRunProfiles;
        private readonly AppDbContext                    _db;
        private readonly ILogger<EquipmentAssistantContextBuilder> _log;

        public EquipmentAssistantContextBuilder(
            IEquipmentEventSummaryService eventSummary,
            IEquipmentAvailabilityService availability,
            IOperationalContextService operationalContext,
            IEquipmentContextSummaryService contextSummary,
            ILatestPointValueStore store,
            ISignalMetadataProvider signalMetadata,
            ISkuRunProfileProvider skuRunProfiles,
            AppDbContext db,
            ILogger<EquipmentAssistantContextBuilder> log)
        {
            _eventSummary      = eventSummary;
            _availability      = availability;
            _operationalContext = operationalContext;
            _contextSummary    = contextSummary;
            _store             = store;
            _signalMetadata    = signalMetadata;
            _skuRunProfiles    = skuRunProfiles;
            _db                = db;
            _log               = log;
        }

        public async Task<EquipmentAssistantContextDto?> BuildAsync(
            string equipmentId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                equipmentId = equipmentId?.Trim().ToUpperInvariant() ?? string.Empty;
                _log.LogInformation("Assistant context lookup for {EquipmentId}", equipmentId);

                if (string.IsNullOrEmpty(equipmentId))
                    return null;

                // ── Sequential DB-backed calls ────────────────────────────────────
                // All services below share the same scoped AppDbContext.  Running them
                // in parallel via Task.WhenAll causes "A second operation was started on
                // this context instance before a previous operation completed."
                // Each call is awaited individually so EF Core never sees concurrent ops.

                var summaryList = await _eventSummary.GetSummaryAsync(cancellationToken);
                _log.LogInformation("BuildAsync: event summary loaded for {EquipmentId}", equipmentId);

                var timeline = await _eventSummary.GetTimelineAsync(equipmentId, hours: 2, cancellationToken);
                _log.LogInformation("BuildAsync: timeline loaded ({Count} events) for {EquipmentId}", timeline.Count, equipmentId);

                var lastAlarm   = await _eventSummary.GetLastAlarmAsync(equipmentId, cancellationToken);
                var lastStopped = await _eventSummary.GetLastStoppedAsync(equipmentId, cancellationToken);

                var avail1h = await _availability.GetStateAvailabilityAsync(equipmentId, hours: 1);

                // opContext and humanSummary both call GetContextAsync internally, which
                // also issues DB queries (availability + adapters).  Keep sequential.
                var opContext = await _operationalContext.GetContextAsync(equipmentId, cancellationToken);
                _log.LogInformation("BuildAsync: opContext loaded for {EquipmentId} (status={Status})", equipmentId, opContext?.CurrentStatus ?? "null");

                var humanSummary = await _contextSummary.GetSummaryAsync(equipmentId, cancellationToken);
                _log.LogInformation("BuildAsync: human summary built for {EquipmentId} (length={Length})", equipmentId, humanSummary?.SummaryText?.Length ?? 0);

                // Match topics by substring containment — the same way the dashboard
                // mini-trends resolve equipment telemetry from hierarchical MQTT paths.
                // e.g. "…/line-c/deburring-station-01/run-status" contains "deburring-station-01"
                var keySignals = _store.GetAll()
                    .Where(p => p.Topic != null &&
                                p.Topic.Contains(equipmentId, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p.Topic)
                    .Select(p => new AssistantSignalSnapshotDto
                    {
                        SignalName   = p.PointName ?? p.Topic.Split('/').LastOrDefault() ?? p.Topic,
                        Value        = p.Value?.ToString() ?? string.Empty,
                        Status       = p.Status,
                        TimestampUtc = p.TimestampUtc.UtcDateTime,
                    })
                    .ToList();

                _log.LogInformation(
                    "BuildAsync: {SignalCount} telemetry signals found for {EquipmentId}",
                    keySignals.Count, equipmentId);

                // ── Trend history (30-min numeric readings, sequential DB call) ────
                var since = DateTime.UtcNow.AddMinutes(-30);
                var trendRows = await _db.TelemetryPointHistories
                    .AsNoTracking()
                    .Where(x => x.EquipmentName == equipmentId
                             && x.ValueNumber != null
                             && x.TimestampUtc >= since)
                    .OrderBy(x => x.SignalName)
                    .ThenBy(x => x.TimestampUtc)
                    .Select(x => new { x.SignalName, x.ValueNumber })
                    .ToListAsync(cancellationToken);

                var trendBySignal = trendRows
                    .GroupBy(x => x.SignalName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key!,
                        g => ComputeTrend(g.Select(r => r.ValueNumber!.Value).ToList()),
                        StringComparer.OrdinalIgnoreCase);

                // Attach trend direction and parsed numeric value to each signal
                foreach (var sig in keySignals)
                {
                    if (double.TryParse(sig.Value, System.Globalization.NumberStyles.Any,
                                        System.Globalization.CultureInfo.InvariantCulture, out var num))
                        sig.NumericValue = num;

                    if (trendBySignal.TryGetValue(sig.SignalName, out var trend))
                        sig.TrendDirection = trend;
                }

                // Only return null when the equipmentId is completely unknown across ALL sources
                var hasAnyData = opContext is not null
                              || summaryList.Any(s => string.Equals(s.EquipmentId, equipmentId, StringComparison.OrdinalIgnoreCase))
                              || timeline.Count > 0
                              || keySignals.Count > 0;

                if (!hasAnyData)
                {
                    _log.LogDebug("BuildAsync: no data found for equipment {EquipmentId}", equipmentId);
                    return null;
                }

                // ── Event summary for this equipment ─────────────────────────────
                var eventSummary = summaryList.FirstOrDefault(s =>
                    string.Equals(s.EquipmentId, equipmentId, StringComparison.OrdinalIgnoreCase));

                // ── Current state: prefer operational context, fall back to store ─
                var currentState = opContext?.CurrentStatus ?? "unknown";

                // ── Build legacy HumanSummary from live signals ───────────────────
                string humanSummaryText;
                if (keySignals.Count > 0)
                {
                    var runSignal   = keySignals.FirstOrDefault(s =>
                        s.SignalName.Equals("run-status", StringComparison.OrdinalIgnoreCase));
                    var alarmSignal = keySignals.FirstOrDefault(s =>
                        s.SignalName.Equals("alarm-state", StringComparison.OrdinalIgnoreCase));

                    var runState = runSignal?.Value?.ToLowerInvariant() switch
                    {
                        "true"    => "running",
                        "running" => "running",
                        "false"   => "stopped",
                        "stopped" => "stopped",
                        _         => currentState,
                    };
                    var alarmState = alarmSignal?.Value?.ToLowerInvariant() switch
                    {
                        "true"  => "alarm",
                        "alarm" => "alarm",
                        _       => "normal",
                    };

                    var signalLines = string.Join("\n", keySignals.Select(s =>
                        $"  {s.SignalName}: {s.Value} ({s.Status})"));

                    humanSummaryText =
                        $"Equipment {equipmentId} is currently {runState} " +
                        $"with {alarmState} alarm state.\n" +
                        $"{keySignals.Count} live telemetry signals:\n" +
                        signalLines;
                }
                else
                {
                    humanSummaryText = humanSummary?.SummaryText ?? string.Empty;
                }

                // ── Build structured InputSummary and short ContextSummary ────────
                var inputSummary = BuildInputSummary(
                    equipmentId, currentState, avail1h, lastAlarm, lastStopped,
                    keySignals, eventSummary, opContext, _signalMetadata);

                var contextSummary = BuildContextSummary(
                    equipmentId, currentState, avail1h, lastAlarm, lastStopped,
                    keySignals, eventSummary, opContext);

                var result = new EquipmentAssistantContextDto
                {
                    EquipmentId        = equipmentId,
                    CurrentState       = currentState,
                    EventSummary       = eventSummary,
                    Availability1h     = avail1h,
                    RecentTimeline     = timeline,
                    OperationalContext = opContext,
                    LastAlarmUtc       = lastAlarm,
                    LastStoppedUtc     = lastStopped,
                    KeySignals         = keySignals,
                    HumanSummary       = humanSummaryText,
                    InputSummary       = inputSummary,
                    ContextSummary     = contextSummary,
                };

                _log.LogInformation(
                    "BuildAsync: context built for {EquipmentId} — hasOpContext={HasOpContext}, summaryLength={Length}",
                    equipmentId, opContext is not null, result.HumanSummary.Length);

                return result;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "BuildAsync failed for equipment {EquipmentId}", equipmentId);
                return null;
            }
        }

        // ── Helper methods ─────────────────────────────────────────────────────────

        private static string ComputeTrend(IList<double> values)
        {
            if (values.Count < 3) return string.Empty;
            var first = values[0];
            var last  = values[^1];
            if (Math.Abs(first) < 1e-9) return string.Empty;
            var pct = (last - first) / Math.Abs(first) * 100.0;
            return pct >  2.0 ? "rising"
                 : pct < -2.0 ? "falling"
                 :               "stable";
        }

        private static string FormatRelativeTime(DateTime utc)
        {
            var age = DateTime.UtcNow - utc;
            if (age.TotalMinutes < 90) return $"{(int)age.TotalMinutes}m ago";
            if (age.TotalHours   < 48) return $"{(int)age.TotalHours}h ago";
            return $"{(int)age.TotalDays}d ago";
        }

        private static string BuildInputSummary(
            string equipmentId,
            string currentState,
            EquipmentStateAvailabilityDto? avail1h,
            DateTime? lastAlarm,
            DateTime? lastStopped,
            List<AssistantSignalSnapshotDto> signals,
            EquipmentEventSummaryDto? eventSummary,
            EquipmentContextDto? opCtx,
            ISignalMetadataProvider signalMetadata)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"equipment_id: {equipmentId}");
            sb.AppendLine($"run_status: {currentState}");

            if (avail1h is not null)
                sb.AppendLine($"availability_1h: {avail1h.RunningPercent:F1}%");

            if (opCtx is not null)
            {
                if (opCtx.Availability24h.HasValue)
                    sb.AppendLine($"availability_24h: {opCtx.Availability24h:F1}%");
                sb.AppendLine($"stop_count_24h: {opCtx.StopCount24h} | alarm_count_24h: {opCtx.AlarmCount24h}");

                if (opCtx.ActiveProductionOrder is not null)
                {
                    var po = opCtx.ActiveProductionOrder;
                    sb.AppendLine();
                    sb.AppendLine("production_order:");
                    sb.AppendLine($"  id: {po.OrderId}");
                    sb.AppendLine($"  sku: {po.Sku} ({po.SkuDescription})");
                    sb.AppendLine($"  qty_planned: {po.PlannedQuantity} | qty_completed: {po.CompletedQuantity}");
                    sb.AppendLine($"  status: {po.Status} | priority: {po.Priority}");
                    if (!string.IsNullOrWhiteSpace(po.CustomerOrderRef))
                        sb.AppendLine($"  customer_ref: {po.CustomerOrderRef}");

                    if (!string.IsNullOrWhiteSpace(opCtx.ActiveSku))
                        sb.AppendLine($"  active_sku: {opCtx.ActiveSku}");

                    if (opCtx.BomItems.Count > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine("  bom:");
                        foreach (var item in opCtx.BomItems)
                        {
                            sb.AppendLine(
                                $"    - material_id: {item.MaterialId} | desc: {item.MaterialDescription} | req_qty: {item.RequiredQuantity} {item.UnitOfMeasure}");
                        }
                    }

                    if (opCtx.InventoryItems.Count > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine("  inventory:");
                        foreach (var inv in opCtx.InventoryItems)
                        {
                            var net = inv.AvailableQuantity - inv.ReservedQuantity;
                            sb.AppendLine(
                                $"    - material_id: {inv.MaterialId} | avail: {inv.AvailableQuantity} | reserved: {inv.ReservedQuantity} | net: {net} {inv.UnitOfMeasure} | status: {inv.StockStatus}");
                        }
                    }

                    if (opCtx.HasMaterialShortage)
                    {
                        sb.AppendLine();
                        sb.AppendLine("  material_shortage: true");
                    }
                }

                if (opCtx.OpenPmTasks.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("maintenance:");
                    foreach (var task in opCtx.OpenPmTasks)
                    {
                        var dueIn = task.DueDateUtc - DateTime.UtcNow;
                        var dueText = task.IsOverdue
                            ? $"overdue by {Math.Abs(dueIn.Days)}d"
                            : dueIn.TotalDays >= 0
                                ? $"due in {dueIn.Days}d"
                                : "due date passed";
                        sb.AppendLine(
                            $"  - id: {task.TaskId} | desc: {task.TaskDescription} | type: {task.TaskType} | priority: {task.Priority} | status: {task.Status} | {dueText}");
                    }

                    if (opCtx.HasOverduePm)
                    {
                        sb.AppendLine($"  overdue_count: {opCtx.OverduePmCount}");
                    }
                    if (opCtx.UpcomingPmCount > 0)
                    {
                        sb.AppendLine($"  upcoming_count: {opCtx.UpcomingPmCount}");
                    }
                }
            }
            else if (eventSummary is not null)
            {
                sb.AppendLine($"stop_count_24h: {eventSummary.StopCount24h} | alarm_count_24h: {eventSummary.AlarmCount24h}");
            }

            if (lastAlarm.HasValue)
                sb.AppendLine($"last_alarm: {FormatRelativeTime(lastAlarm.Value)}");
            if (lastStopped.HasValue)
                sb.AppendLine($"last_stopped: {FormatRelativeTime(lastStopped.Value)}");

            if (signals.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("signals:");

                var setpoints = signals
                    .Where(s => s.SignalName.EndsWith("-setpoint", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(
                        s => s.SignalName[..^"-setpoint".Length],
                        s => s,
                        StringComparer.OrdinalIgnoreCase);

                int normalCount = 0, nearLimitCount = 0, abnormalCount = 0;
                var insightLines = new List<string>();

                foreach (var sig in signals)
                {
                    if (sig.SignalName.EndsWith("-setpoint", StringComparison.OrdinalIgnoreCase))
                        continue; // printed inline with its pair

                    var trend = string.IsNullOrEmpty(sig.TrendDirection)
                        ? string.Empty
                        : $" [trend: {sig.TrendDirection}]";

                    var meta = signalMetadata.GetMetadata(sig.SignalName);
                    string classification;

                    if (meta is null || !sig.NumericValue.HasValue ||
                        (!meta.MinNormal.HasValue && !meta.MaxNormal.HasValue))
                    {
                        classification = "Unknown (No baseline available for interpretation)";
                    }
                    else
                    {
                        var v = sig.NumericValue.Value;
                        var min = meta.MinNormal ?? double.NegativeInfinity;
                        var max = meta.MaxNormal ?? double.PositiveInfinity;
                        var margin = (max - min) * 0.05; // 5% band near limits when both present

                        if (v < min)
                        {
                            classification = "Low";
                            abnormalCount++;
                        }
                        else if (v > max)
                        {
                            classification = "High";
                            abnormalCount++;
                        }
                        else if (meta.MinNormal.HasValue && meta.MaxNormal.HasValue &&
                                 (v <= min + margin || v >= max - margin))
                        {
                            classification = "NearLimit";
                            nearLimitCount++;
                        }
                        else
                        {
                            classification = "Normal";
                            normalCount++;
                        }
                    }

                    sb.AppendLine($"  {sig.SignalName}: {sig.Value} ({sig.Status}) [{classification}]{trend}");

                    // Collect evidence-based signal insights only when a metadata hint exists
                    string? hint = classification switch
                    {
                        "Low"       => meta?.InterpretationHintLow,
                        "High"      => meta?.InterpretationHintHigh,
                        "NearLimit" => meta?.InterpretationHintNearLimit,
                        _            => null,
                    };

                    if (!string.IsNullOrWhiteSpace(hint))
                    {
                        insightLines.Add($"- {sig.SignalName}: {hint}");
                    }

                    if (setpoints.TryGetValue(sig.SignalName, out var sp))
                    {
                        sb.AppendLine($"  {sp.SignalName}: {sp.Value} ({sp.Status})");

                        if (sig.NumericValue.HasValue && sp.NumericValue.HasValue)
                        {
                            var delta = sig.NumericValue.Value - sp.NumericValue.Value;
                            var pct   = sp.NumericValue.Value != 0
                                          ? delta / Math.Abs(sp.NumericValue.Value) * 100.0
                                          : 0;
                            var sign  = delta >= 0 ? "+" : "";
                            sb.AppendLine($"  → deviation: {sign}{delta:F2} ({sign}{pct:F1}%)");
                        }
                    }
                }

                sb.AppendLine();
                sb.AppendLine("signal_health:");
                sb.AppendLine($"  normal: {normalCount}");
                sb.AppendLine($"  near_limit: {nearLimitCount}");
                sb.AppendLine($"  abnormal: {abnormalCount}");

                if (insightLines.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("signal_insights:");
                    foreach (var line in insightLines)
                        sb.AppendLine($"  {line}");
                }
            }

            if (opCtx is not null)
            {
                sb.AppendLine();

                var wo = opCtx.CurrentWorkOrder
                      ?? (opCtx.ActiveWorkOrder is not null
                            ? new WorkOrderContextDto
                              {
                                  WorkOrderId       = opCtx.ActiveWorkOrder.WorkOrderNumber,
                                  Description       = opCtx.ActiveWorkOrder.Title,
                                  Status            = opCtx.ActiveWorkOrder.Status,
                                  ScheduledStartUtc = opCtx.ActiveWorkOrder.ScheduledStartUtc,
                                  ScheduledEndUtc   = opCtx.ActiveWorkOrder.ScheduledEndUtc,
                              }
                            : null);

                if (wo is not null)
                {
                    var startAge = wo.ScheduledStartUtc.HasValue
                        ? $"started {FormatRelativeTime(wo.ScheduledStartUtc.Value.UtcDateTime)}"
                        : string.Empty;
                    sb.AppendLine(
                        $"work_order: {wo.WorkOrderId} | {wo.Description} | {wo.Status}" +
                        (startAge.Length > 0 ? " | " + startAge : string.Empty));
                }

                if (opCtx.ScheduledProduct is not null)
                {
                    var sp = opCtx.ScheduledProduct;
                    sb.AppendLine($"schedule: {sp.Title} | {sp.ScheduleType} | {sp.Status}");
                }

                if (opCtx.MaterialStatus is not null)
                {
                    var ms = opCtx.MaterialStatus;
                    sb.AppendLine($"material: {ms.MaterialCode} ({ms.Description}) | stock: {ms.StockStatus}");
                }

                if (opCtx.RecentEvents.Count > 0 ||
                    opCtx.LastAlarmEvent is not null ||
                    opCtx.LastStopEvent is not null)
                {
                    sb.AppendLine();
                    sb.AppendLine("recent_events:");

                    if (opCtx.LastAlarmEvent is not null && opCtx.TimeSinceLastAlarm.HasValue)
                    {
                        var mins = opCtx.TimeSinceLastAlarm.Value.TotalMinutes;
                        sb.AppendLine($"  last_alarm: {opCtx.LastAlarmEvent.Description} ({mins:F0} minutes ago)");
                    }

                    if (opCtx.LastStopEvent is not null && opCtx.TimeSinceLastStop.HasValue)
                    {
                        var hours = opCtx.TimeSinceLastStop.Value.TotalHours;
                        sb.AppendLine($"  last_stop: {opCtx.LastStopEvent.Description} ({hours:F1} hours ago)");
                    }

                    foreach (var e in opCtx.RecentEvents
                                 .OrderByDescending(e => e.TimestampUtc)
                                 .Take(10))
                    {
                        sb.AppendLine(
                            $"  - {e.TimestampUtc:O} | {e.EventType} | {e.Severity} | {e.Description} (src={e.Source})");
                    }
                }
            }

            return sb.ToString().TrimEnd();
        }

        private static string BuildContextSummary(
            string equipmentId,
            string currentState,
            EquipmentStateAvailabilityDto? avail1h,
            DateTime? lastAlarm,
            DateTime? lastStopped,
            List<AssistantSignalSnapshotDto> signals,
            EquipmentEventSummaryDto? eventSummary,
            EquipmentContextDto? opCtx)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"{equipmentId} is {currentState}");

            var alarmSig = signals.FirstOrDefault(s =>
                s.SignalName.Equals("alarm-state", StringComparison.OrdinalIgnoreCase));
            var alarmActive = alarmSig?.Value?.ToLowerInvariant() is "true" or "alarm";
            sb.Append(alarmActive ? " with an active alarm" : " with no active alarms");

            if (avail1h is not null)
                sb.Append($", {avail1h.RunningPercent:F0}% available in the last hour");

            sb.Append('.');

            if (opCtx?.ActiveProductionOrder is not null && !string.IsNullOrWhiteSpace(opCtx.ActiveSku))
            {
                var po = opCtx.ActiveProductionOrder;
                sb.Append($" Active SKU {po.Sku} requires {opCtx.BomItems.Count} materials.");

                if (opCtx.BomItems.Count > 0)
                {
                    var example = opCtx.BomItems[0];
                    var inv = opCtx.InventoryItems
                        .FirstOrDefault(i => string.Equals(i.MaterialId, example.MaterialId, StringComparison.OrdinalIgnoreCase));
                    if (inv is not null)
                    {
                        var net = inv.AvailableQuantity - inv.ReservedQuantity;
                        sb.Append($" Example: {example.MaterialId} needs {example.RequiredQuantity} {example.UnitOfMeasure}, net {net} on hand ({inv.StockStatus}).");
                    }
                }

                if (opCtx.HasMaterialShortage)
                {
                    sb.Append(" Order is currently blocked or at risk due to material shortage.");
                }
                else
                {
                    sb.Append(" Materials appear sufficient to run the order.");
                }
            }

            if (opCtx is not null && opCtx.OpenPmTasks.Count > 0)
            {
                var overdue = opCtx.OpenPmTasks.FirstOrDefault(t => t.IsOverdue);
                if (overdue is not null)
                {
                    var daysOver = (DateTime.UtcNow - overdue.DueDateUtc).Days;
                    if (daysOver < 0) daysOver = 0;
                    sb.Append($" Maintenance risk: overdue PM '{overdue.TaskDescription}' ({daysOver} days overdue).");
                }

                var upcoming = opCtx.OpenPmTasks
                    .Where(t => !t.IsOverdue && t.DueDateUtc > DateTime.UtcNow)
                    .OrderBy(t => t.DueDateUtc)
                    .FirstOrDefault();
                if (upcoming is not null)
                {
                    var days = (upcoming.DueDateUtc - DateTime.UtcNow).Days;
                    sb.Append($" Upcoming PM in {days} days: {upcoming.TaskDescription}.");
                }
            }

            if (opCtx is not null &&
                (opCtx.LastAlarmEvent is not null || opCtx.LastStopEvent is not null))
            {
                if (opCtx.TimeSinceLastAlarm.HasValue)
                {
                    var mins = opCtx.TimeSinceLastAlarm.Value.TotalMinutes;
                    sb.Append($" Last alarm occurred {mins:F0} minutes ago.");
                }

                if (opCtx.TimeSinceLastStop.HasValue)
                {
                    var hrs = opCtx.TimeSinceLastStop.Value.TotalHours;
                    sb.Append($" Last stop occurred {hrs:F1} hours ago.");
                }
            }

            // Setpoint deviations ≥ 5%
            var setpoints = signals
                .Where(s => s.SignalName.EndsWith("-setpoint", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(
                    s => s.SignalName[..^"-setpoint".Length],
                    s => s,
                    StringComparer.OrdinalIgnoreCase);

            foreach (var sig in signals)
            {
                if (!setpoints.TryGetValue(sig.SignalName, out var sp)) continue;
                if (!sig.NumericValue.HasValue || !sp.NumericValue.HasValue) continue;
                if (sp.NumericValue.Value == 0) continue;

                var pct = (sig.NumericValue.Value - sp.NumericValue.Value)
                          / Math.Abs(sp.NumericValue.Value) * 100.0;

                if (Math.Abs(pct) >= 5.0)
                {
                    var dir = pct > 0 ? "above" : "below";
                    sb.Append($" {sig.SignalName} is {Math.Abs(pct):F0}% {dir} setpoint.");
                }
            }

            // Trend summary (exclude setpoint signals)
            var rising = signals
                .Where(s => s.TrendDirection == "rising" &&
                            !s.SignalName.EndsWith("-setpoint", StringComparison.OrdinalIgnoreCase))
                .Select(s => s.SignalName)
                .ToList();
            var falling = signals
                .Where(s => s.TrendDirection == "falling" &&
                            !s.SignalName.EndsWith("-setpoint", StringComparison.OrdinalIgnoreCase))
                .Select(s => s.SignalName)
                .ToList();

            if (rising.Count > 0)  sb.Append($" Rising: {string.Join(", ", rising)}.");
            if (falling.Count > 0) sb.Append($" Falling: {string.Join(", ", falling)}.");

            // 24h counts
            var stops  = eventSummary?.StopCount24h  ?? 0;
            var alarms = eventSummary?.AlarmCount24h ?? 0;
            if (stops > 0 || alarms > 0)
                sb.Append($" {stops} stop(s), {alarms} alarm(s) in last 24h.");

            if (lastAlarm.HasValue)
                sb.Append($" Last alarm: {FormatRelativeTime(lastAlarm.Value)}.");
            if (lastStopped.HasValue)
                sb.Append($" Last stop: {FormatRelativeTime(lastStopped.Value)}.");

            return sb.ToString();
        }
    }
}
