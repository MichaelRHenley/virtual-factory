using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Virtual_Factory.Data;
using Virtual_Factory.Infrastructure;

namespace Virtual_Factory.Services
{
    public class EquipmentStateEventService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Dictionary<string, EquipmentStateSnapshot> _lastStates =
            new(StringComparer.OrdinalIgnoreCase);

        public EquipmentStateEventService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await SeedLastStatesAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));

            while (!stoppingToken.IsCancellationRequested)
            {
                await timer.WaitForNextTickAsync(stoppingToken);

                using var scope = _scopeFactory.CreateScope();

                var store = scope.ServiceProvider.GetRequiredService<ILatestPointValueStore>();
                var writer = scope.ServiceProvider.GetRequiredService<EquipmentStateEventWriter>();

                var latest = store.GetAll().ToList();
                if (latest.Count == 0)
                    continue;

                var grouped = latest
                    .GroupBy(x => TopicParser.ExtractEquipmentName(x.Topic), StringComparer.OrdinalIgnoreCase);

                foreach (var group in grouped)
                {
                    var equipmentName = group.Key;

                    // Skip topics that could not be resolved to an equipment-level name
                    // (e.g. line-aggregate topics that lack an equipment segment).
                    if (string.Equals(equipmentName, "unknown", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var rows = group.ToList();

                    var newestTimestamp = rows.Max(x => x.TimestampUtc.UtcDateTime);
                    var source = rows.FirstOrDefault()?.Source;

                    var runState = "unknown";
                    var alarmState = "normal";
                    var connectivityState = (DateTime.UtcNow - newestTimestamp).TotalSeconds > 10
                        ? "offline"
                        : "online";

                    foreach (var row in rows)
                    {
                        var topic = row.Topic?.ToLowerInvariant() ?? string.Empty;
                        var value = row.Value?.ToString()?.ToLowerInvariant() ?? string.Empty;

                        if (topic.EndsWith("/run-status"))
                        {
                            runState = value == "true" ? "running"
                                : value == "false" ? "stopped"
                                : value;
                        }

                        if (topic.EndsWith("/alarm-state") || topic.Contains("alarm"))
                        {
                            alarmState = value == "true" ? "alarm"
                                : value == "false" ? "normal"
                                : value;
                        }
                    }

                    var current = new EquipmentStateSnapshot
                    {
                        EquipmentName = equipmentName,
                        RunState = runState,
                        AlarmState = alarmState,
                        ConnectivityState = connectivityState,
                        Source = source,
                        TimestampUtc = newestTimestamp
                    };

                    _lastStates.TryGetValue(equipmentName, out var previous);

                    await writer.WriteEventsAsync(current, previous, stoppingToken);

                    _lastStates[equipmentName] = current;
                }
            }
        }

        private async Task SeedLastStatesAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var allEvents = await db.EquipmentStateEvents
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (allEvents.Count == 0)
                return;

            foreach (var group in allEvents.GroupBy(e => e.EquipmentName, StringComparer.OrdinalIgnoreCase))
            {
                var snapshot = new EquipmentStateSnapshot
                {
                    EquipmentName = group.Key,
                    TimestampUtc = group.Max(e => e.TimestampUtc)
                };

                var latestRun = group
                    .Where(e => e.EventType == "run-state-changed")
                    .MaxBy(e => e.TimestampUtc);
                if (latestRun?.NewState is not null)
                    snapshot.RunState = latestRun.NewState;

                var latestAlarm = group
                    .Where(e => e.EventType == "alarm-state-changed")
                    .MaxBy(e => e.TimestampUtc);
                if (latestAlarm?.NewState is not null)
                    snapshot.AlarmState = latestAlarm.NewState;

                var latestConnectivity = group
                    .Where(e => e.EventType == "connectivity-state-changed")
                    .MaxBy(e => e.TimestampUtc);
                if (latestConnectivity?.NewState is not null)
                    snapshot.ConnectivityState = latestConnectivity.NewState;

                _lastStates[group.Key] = snapshot;
            }
        }
    }
}