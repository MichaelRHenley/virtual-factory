using Microsoft.EntityFrameworkCore;
using Virtual_Factory.Data;
using Virtual_Factory.Dtos;
using Virtual_Factory.Models;

namespace Virtual_Factory.Services
{
    public sealed class EquipmentEventSummaryService : IEquipmentEventSummaryService
    {
        private readonly AppDbContext _db;

        public EquipmentEventSummaryService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<EquipmentEventSummaryDto>> GetSummaryAsync(
    CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var cutoff = now.AddHours(-24);

            var rows = await _db.TelemetryPointHistories
                .AsNoTracking()
                .Where(x =>
                    x.TimestampUtc >= cutoff &&
                    (x.SignalName == "alarm-state" || x.SignalName == "run-status"))
                .ToListAsync(cancellationToken);

            var allEquipment = await _db.TelemetryPointHistories
                .Select(x => x.EquipmentName)
                .Distinct()
                .ToListAsync(cancellationToken);

            var groupedRows = rows
                .GroupBy(x => x.EquipmentName)
                .ToDictionary(g => g.Key, g => g.ToList());

            var results = allEquipment.Select(eq =>
            {
                groupedRows.TryGetValue(eq, out var equipmentRows);
                equipmentRows ??= new List<TelemetryPointHistory>();

                var alarmRows = equipmentRows
                    .Where(x => x.SignalName == "alarm-state")
                    .OrderBy(x => x.TimestampUtc)
                    .ToList();

                var runRows = equipmentRows
                    .Where(x => x.SignalName == "run-status")
                    .OrderBy(x => x.TimestampUtc)
                    .ToList();

                var alarmCount24h = CountAlarmTransitions(alarmRows);
                var stopCount24h = CountTransitionsToStopped(runRows);

                var latestRow = equipmentRows
                    .Where(x => x.SignalName == "alarm-state" || x.SignalName == "run-status")
                    .OrderByDescending(x => x.TimestampUtc)
                    .FirstOrDefault();

                var activeAlarmRow = FindCurrentActiveAlarmStart(alarmRows);

                return new EquipmentEventSummaryDto
                {
                    EquipmentId = eq,
                    StopCount24h = stopCount24h,
                    AlarmCount24h = alarmCount24h,

                    LatestEvent = latestRow == null ? null :
                        new LatestEventDto
                        {
                            EventType = latestRow.SignalName == "alarm-state"
                                ? "Alarm"
                                : "Stop",

                            EventName = latestRow.SignalName,
                            State = NormalizeState(
                                latestRow.SignalName,
                                latestRow.ValueText),

                            StartTimeUtc = latestRow.TimestampUtc,
                            DurationSeconds = 0
                        },

                    LongestCurrentAlarm = activeAlarmRow == null ? null :
                        new CurrentAlarmDto
                        {
                            EventName = "Alarm",
                            StartTimeUtc = activeAlarmRow.TimestampUtc,
                            DurationSeconds =
                                (int)(now - activeAlarmRow.TimestampUtc)
                                .TotalSeconds
                        }
                };
            })
            .OrderBy(x => x.EquipmentId)
            .ToList();

            return results;
        }

        private static int CountAlarmTransitions(List<TelemetryPointHistory> rows)
        {
            int count = 0;
            bool previousActive = false;

            foreach (var row in rows.OrderBy(x => x.TimestampUtc))
            {
                bool currentActive =
                    (row.ValueText ?? "").Trim().ToLowerInvariant() switch
                    {
                        "true" => true,
                        "alarm" => true,
                        "active" => true,
                        _ => false
                    };

                if (currentActive && !previousActive)
                    count++;

                previousActive = currentActive;
            }

            return count;
        }

        private static TelemetryPointHistory? FindCurrentActiveAlarmStart(List<TelemetryPointHistory> alarmRows)
        {
            if (alarmRows == null || alarmRows.Count == 0)
                return null;

            var ordered = alarmRows
                .OrderBy(x => x.TimestampUtc)
                .ToList();

            var latest = ordered.LastOrDefault();
            if (latest == null || !IsAlarmActive(latest.ValueText))
                return null;

            TelemetryPointHistory? activeStart = null;
            bool previousActive = false;

            foreach (var row in ordered)
            {
                bool currentActive = IsAlarmActive(row.ValueText);

                if (currentActive && !previousActive)
                {
                    activeStart = row;
                }

                previousActive = currentActive;
            }

            return activeStart;
        }

        private static int CountTransitionsToStopped(List<TelemetryPointHistory> rows)
        {
            int count = 0;
            bool previousStopped = false;

            foreach (var row in rows.OrderBy(x => x.TimestampUtc))
            {
                bool currentStopped = IsStopped(row.ValueText);

                if (currentStopped && !previousStopped)
                {
                    count++;
                }

                previousStopped = currentStopped;
            }

            return count;
        }

        private static bool IsAlarmActive(string? valueText)
        {
            var value = (valueText ?? "").Trim().ToLowerInvariant();
            return value == "true" || value == "alarm" || value == "active";
        }

        private static bool IsStopped(string? valueText)
        {
            var value = (valueText ?? "").Trim().ToLowerInvariant();
            return value == "false" || value == "stopped";
        }

        private static string NormalizeState(string signalName, string? valueText)
        {
            if (signalName == "alarm-state")
            {
                return IsAlarmActive(valueText) ? "Active" : "Cleared";
            }

            if (signalName == "run-status")
            {
                return IsStopped(valueText) ? "Stopped" : "Running";
            }

            return valueText ?? "Unknown";
        }
    }
}