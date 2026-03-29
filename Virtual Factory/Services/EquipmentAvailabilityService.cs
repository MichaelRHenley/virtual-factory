using Microsoft.EntityFrameworkCore;
using Virtual_Factory.Data;
using Virtual_Factory.Dtos;
using Virtual_Factory.Models;

namespace Virtual_Factory.Services
{
    public class EquipmentAvailabilityService : IEquipmentAvailabilityService
    {
        private readonly AppDbContext _db;

        public EquipmentAvailabilityService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<EquipmentAvailabilityDto>> GetAvailabilityAsync(int hours = 24)
        {
            if (hours <= 0) hours = 24;

            var windowStart = DateTime.UtcNow.AddHours(-hours);
            var windowEnd = DateTime.UtcNow;
            var totalWindowSeconds = (windowEnd - windowStart).TotalSeconds;

            var rows = await _db.TelemetryPointHistories
                .AsNoTracking()
                .Where(x => x.TimestampUtc >= windowStart)
                .Where(x => x.SignalName == "run-status" || x.SignalName == "alarm-state")
                .Where(x => x.EquipmentName != null && x.EquipmentName != "")
                .OrderBy(x => x.EquipmentName)
                .ThenBy(x => x.SignalName)
                .ThenBy(x => x.TimestampUtc)
                .ToListAsync();

            var equipmentNames = rows
                .Select(x => x.EquipmentName!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            var results = new List<EquipmentAvailabilityDto>();

            foreach (var equipment in equipmentNames)
            {
                var equipmentRows = rows
                    .Where(x => string.Equals(x.EquipmentName, equipment, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var runningSeconds = CalculateSignalSeconds(
                    equipmentRows,
                    "run-status",
                    v => string.Equals(v, "running", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase),
                    windowStart,
                    windowEnd);

                var stoppedSeconds = CalculateSignalSeconds(
                    equipmentRows,
                    "run-status",
                    v => string.Equals(v, "stopped", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(v, "false", StringComparison.OrdinalIgnoreCase),
                    windowStart,
                    windowEnd);

                var alarmSeconds = CalculateSignalSeconds(
                    equipmentRows,
                    "alarm-state",
                    v => string.Equals(v, "alarm", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase),
                    windowStart,
                    windowEnd);

                var offlineSeconds = CalculateOfflineSeconds(
                    equipmentRows,
                    windowStart,
                    windowEnd,
                    offlineThresholdSeconds: 60);

                results.Add(new EquipmentAvailabilityDto
                {
                    EquipmentId = equipment,
                    RunningPercent = Math.Round((runningSeconds / totalWindowSeconds) * 100.0, 1),
                    StoppedPercent = Math.Round((stoppedSeconds / totalWindowSeconds) * 100.0, 1),
                    AlarmPercent = Math.Round((alarmSeconds / totalWindowSeconds) * 100.0, 1),
                    OfflinePercent = Math.Round((offlineSeconds / totalWindowSeconds) * 100.0, 1),
                    WindowHours = hours
                });
            }

            return results
                .OrderBy(x => x.EquipmentId)
                .ToList();
        }

        public async Task<List<EquipmentStateAvailabilityDto>> GetStateAvailabilityAsync(int hours = 24)
        {
            if (hours <= 0) hours = 24;

            var windowEnd   = DateTime.UtcNow;
            var windowStart = windowEnd.AddHours(-hours);
            var totalWindowSeconds = (windowEnd - windowStart).TotalSeconds;

            var allRunEvents = await _db.EquipmentStateEvents
                .AsNoTracking()
                .Where(x => x.EventType == "run-state-changed")
                .OrderBy(x => x.EquipmentName)
                .ThenBy(x => x.TimestampUtc)
                .ToListAsync();

            var allEquipmentNames = await _db.EquipmentStateEvents
                .AsNoTracking()
                .Select(x => x.EquipmentName)
                .Distinct()
                .ToListAsync();

            var eventsByEquipment = allRunEvents
                .GroupBy(x => x.EquipmentName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var results = new List<EquipmentStateAvailabilityDto>();

            foreach (var equipmentName in allEquipmentNames)
            {
                eventsByEquipment.TryGetValue(equipmentName, out var events);
                events ??= [];

                var (runningSeconds, stoppedSeconds, unknownSeconds) =
                    CalculateRunStopSeconds(events, windowStart, windowEnd);

                results.Add(new EquipmentStateAvailabilityDto
                {
                    EquipmentId    = equipmentName,
                    WindowHours    = hours,
                    RunningSeconds = Math.Round(runningSeconds, 1),
                    StoppedSeconds = Math.Round(stoppedSeconds, 1),
                    UnknownSeconds = Math.Round(unknownSeconds, 1),
                    RunningPercent = Math.Round((runningSeconds / totalWindowSeconds) * 100.0, 1),
                    StoppedPercent = Math.Round((stoppedSeconds / totalWindowSeconds) * 100.0, 1),
                    UnknownPercent = Math.Round((unknownSeconds / totalWindowSeconds) * 100.0, 1),
                });
            }

            return results.OrderBy(x => x.EquipmentId).ToList();
        }

        public async Task<EquipmentStateAvailabilityDto?> GetStateAvailabilityAsync(string equipmentId, int hours = 24)
        {
            if (hours <= 0) hours = 24;

            var windowEnd   = DateTime.UtcNow;
            var windowStart = windowEnd.AddHours(-hours);
            var totalWindowSeconds = (windowEnd - windowStart).TotalSeconds;

            var events = await _db.EquipmentStateEvents
                .AsNoTracking()
                .Where(x => x.EventType == "run-state-changed"
                         && x.EquipmentName == equipmentId)
                .OrderBy(x => x.TimestampUtc)
                .ToListAsync();

            if (events.Count == 0)
                return null;

            var (runningSeconds, stoppedSeconds, unknownSeconds) =
                CalculateRunStopSeconds(events, windowStart, windowEnd);

            return new EquipmentStateAvailabilityDto
            {
                EquipmentId    = equipmentId,
                WindowHours    = hours,
                RunningSeconds = Math.Round(runningSeconds, 1),
                StoppedSeconds = Math.Round(stoppedSeconds, 1),
                UnknownSeconds = Math.Round(unknownSeconds, 1),
                RunningPercent = Math.Round((runningSeconds / totalWindowSeconds) * 100.0, 1),
                StoppedPercent = Math.Round((stoppedSeconds / totalWindowSeconds) * 100.0, 1),
                UnknownPercent = Math.Round((unknownSeconds / totalWindowSeconds) * 100.0, 1),
            };
        }

        /// <summary>
        /// Walks the run-state-changed transition sequence for one equipment and returns
        /// how many seconds within [windowStart, windowEnd) were spent in each state.
        /// The three values always sum to (windowEnd - windowStart).TotalSeconds.
        /// </summary>
        private static (double RunningSeconds, double StoppedSeconds, double UnknownSeconds)
            CalculateRunStopSeconds(
                List<EquipmentStateEvent> allRunEvents,
                DateTime windowStart,
                DateTime windowEnd)
        {
            double runningSeconds = 0;
            double stoppedSeconds = 0;
            double unknownSeconds = 0;

            // Entry state: NewState of the last event strictly before windowStart.
            // The list is already ordered ascending so we can break early.
            string? entryState = null;
            foreach (var ev in allRunEvents)
            {
                if (ev.TimestampUtc < windowStart)
                    entryState = ev.NewState;
                else
                    break;
            }

            var windowEvents = allRunEvents
                .Where(x => x.TimestampUtc >= windowStart && x.TimestampUtc < windowEnd)
                .ToList();

            var currentState = entryState;
            var cursor = windowStart;

            foreach (var ev in windowEvents)
            {
                if (ev.TimestampUtc > cursor)
                    Accumulate(ref runningSeconds, ref stoppedSeconds, ref unknownSeconds,
                               currentState, (ev.TimestampUtc - cursor).TotalSeconds);

                cursor = ev.TimestampUtc;
                currentState = ev.NewState;
            }

            if (windowEnd > cursor)
                Accumulate(ref runningSeconds, ref stoppedSeconds, ref unknownSeconds,
                           currentState, (windowEnd - cursor).TotalSeconds);

            return (runningSeconds, stoppedSeconds, unknownSeconds);
        }

        private static void Accumulate(
            ref double runningSeconds,
            ref double stoppedSeconds,
            ref double unknownSeconds,
            string? state,
            double duration)
        {
            if (string.Equals(state, "running", StringComparison.OrdinalIgnoreCase))
                runningSeconds += duration;
            else if (string.Equals(state, "stopped", StringComparison.OrdinalIgnoreCase))
                stoppedSeconds += duration;
            else
                unknownSeconds += duration;
        }

        private static double CalculateSignalSeconds(
            List<TelemetryPointHistory> rows,
            string signalName,
            Func<string?, bool> isActive,
            DateTime windowStart,
            DateTime windowEnd)
        {
            var signalRows = rows
                .Where(x => string.Equals(x.SignalName, signalName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.TimestampUtc)
                .ToList();

            if (!signalRows.Any())
                return 0;

            double seconds = 0;

            for (int i = 0; i < signalRows.Count; i++)
            {
                var current = signalRows[i];
                var nextTime = i < signalRows.Count - 1
                    ? signalRows[i + 1].TimestampUtc
                    : windowEnd;

                var segmentStart = current.TimestampUtc < windowStart ? windowStart : current.TimestampUtc;
                var segmentEnd = nextTime > windowEnd ? windowEnd : nextTime;

                if (segmentEnd <= segmentStart)
                    continue;

                if (isActive(current.ValueText))
                {
                    seconds += (segmentEnd - segmentStart).TotalSeconds;
                }
            }

            return seconds;
        }

        private static double CalculateOfflineSeconds(
            List<TelemetryPointHistory> rows,
            DateTime windowStart,
            DateTime windowEnd,
            int offlineThresholdSeconds)
        {
            var ordered = rows
                .OrderBy(x => x.TimestampUtc)
                .ToList();

            if (!ordered.Any())
                return (windowEnd - windowStart).TotalSeconds;

            double offlineSeconds = 0;

            var leadingGap = (ordered.First().TimestampUtc - windowStart).TotalSeconds;
            if (leadingGap > offlineThresholdSeconds)
            {
                offlineSeconds += leadingGap;
            }

            for (int i = 0; i < ordered.Count - 1; i++)
            {
                var gap = (ordered[i + 1].TimestampUtc - ordered[i].TimestampUtc).TotalSeconds;
                if (gap > offlineThresholdSeconds)
                {
                    offlineSeconds += gap;
                }
            }

            var trailingGap = (windowEnd - ordered.Last().TimestampUtc).TotalSeconds;
            if (trailingGap > offlineThresholdSeconds)
            {
                offlineSeconds += trailingGap;
            }

            return Math.Min(offlineSeconds, (windowEnd - windowStart).TotalSeconds);
        }
    }
}