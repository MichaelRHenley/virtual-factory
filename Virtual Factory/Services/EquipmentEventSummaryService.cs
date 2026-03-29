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

            var recentEvents = await _db.EquipmentStateEvents
                .AsNoTracking()
                .Where(x => x.TimestampUtc >= cutoff)
                .ToListAsync(cancellationToken);

            var allEquipment = await _db.EquipmentStateEvents
                .AsNoTracking()
                .Select(x => x.EquipmentName)
                .Distinct()
                .ToListAsync(cancellationToken);

            // All alarm-state-changed events (no time filter) ordered chronologically so
            // FindActiveAlarmStart can walk transitions to locate the unmatched alarm open.
            // No time filter because an active alarm may have started before the 24h window.
            var alarmTransitionsByEquipment = (await _db.EquipmentStateEvents
                .AsNoTracking()
                .Where(x => x.EventType == "alarm-state-changed")
                .OrderBy(x => x.TimestampUtc)
                .ToListAsync(cancellationToken))
                .GroupBy(x => x.EquipmentName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var groupedRecent = recentEvents
                .GroupBy(x => x.EquipmentName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var results = allEquipment.Select(equipment =>
            {
                groupedRecent.TryGetValue(equipment, out var events);
                events ??= [];

                var alarmCount = events.Count(x =>
                    x.EventType == "alarm-state-changed" &&
                    string.Equals(x.NewState, "alarm", StringComparison.OrdinalIgnoreCase));

                var stopCount = events.Count(x =>
                    x.EventType == "run-state-changed" &&
                    string.Equals(x.NewState, "stopped", StringComparison.OrdinalIgnoreCase));

                var latestSignificant = events
                    .Where(x => x.EventType is "alarm-state-changed" or "run-state-changed")
                    .MaxBy(x => x.TimestampUtc);

                LatestEventDto? latestEventDto = null;
                if (latestSignificant != null)
                {
                    latestEventDto = new LatestEventDto
                    {
                        EventType = latestSignificant.EventType == "alarm-state-changed" ? "Alarm" : "Stop",
                        EventName = latestSignificant.EventType,
                        State = latestSignificant.NewState ?? "Unknown",
                        StartTimeUtc = latestSignificant.TimestampUtc,
                        DurationSeconds = (int)(now - latestSignificant.TimestampUtc).TotalSeconds
                    };
                }

                CurrentAlarmDto? currentAlarm = null;
                alarmTransitionsByEquipment.TryGetValue(equipment, out var alarmTransitions);
                var activeAlarmStart = FindActiveAlarmStart(alarmTransitions);
                if (activeAlarmStart.HasValue)
                {
                    currentAlarm = new CurrentAlarmDto
                    {
                        EventName = "Alarm",
                        StartTimeUtc = activeAlarmStart.Value,
                        DurationSeconds = (int)(now - activeAlarmStart.Value).TotalSeconds
                    };
                }

                return new EquipmentEventSummaryDto
                {
                    EquipmentId = equipment,
                    AlarmCount24h = alarmCount,
                    StopCount24h = stopCount,
                    LatestEvent = latestEventDto,
                    LongestCurrentAlarm = currentAlarm
                };
            })
            .OrderBy(x => x.EquipmentId)
            .ToList();

            return results;
        }

        /// <summary>
        /// Walks alarm transitions in chronological order and returns the start timestamp
        /// of the currently active alarm period, or null if no alarm is active.
        /// Each "alarm" transition opens a period; each non-alarm transition closes it.
        /// </summary>
        private static DateTime? FindActiveAlarmStart(List<EquipmentStateEvent>? transitions)
        {
            if (transitions is null)
                return null;

            DateTime? alarmStart = null;

            foreach (var t in transitions) // ordered ascending by TimestampUtc
            {
                if (string.Equals(t.NewState, "alarm", StringComparison.OrdinalIgnoreCase))
                    alarmStart = t.TimestampUtc;
                else
                    alarmStart = null;
            }

            return alarmStart;
        }

        public async Task<List<EquipmentEventTimelineItemDto>> GetTimelineAsync(
    string equipmentId,
    int hours = 24,
    CancellationToken cancellationToken = default)
        {
            var cutoff = DateTime.UtcNow.AddHours(-hours);

            var events = await _db.EquipmentStateEvents
                .AsNoTracking()
                .Where(x =>
                    x.EquipmentName == equipmentId &&
                    x.TimestampUtc >= cutoff)
                .OrderByDescending(x => x.TimestampUtc)
                .ToListAsync(cancellationToken);

            var results = new List<EquipmentEventTimelineItemDto>();

            for (int i = 0; i < events.Count; i++)
            {
                var current = events[i];
                var previousOlder = i < events.Count - 1 ? events[i + 1] : null;

                int? durationSeconds = null;

                if (previousOlder != null)
                {
                    durationSeconds = (int)Math.Round(
                        (current.TimestampUtc - previousOlder.TimestampUtc)
                        .TotalSeconds);
                }

                results.Add(new EquipmentEventTimelineItemDto
                {
                    Id = current.Id,
                    EquipmentId = current.EquipmentName,
                    EventType = current.EventType,
                    NewState = current.NewState,
                    PreviousState = current.PreviousState,
                    Source = current.Source,
                    TimestampUtc = current.TimestampUtc,
                    DurationSeconds = durationSeconds
                });
            }

            return results;
        }

    }
}