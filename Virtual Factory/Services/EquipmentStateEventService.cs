using Microsoft.Extensions.Hosting;

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
                    .GroupBy(x => GetEquipmentName(x.Topic), StringComparer.OrdinalIgnoreCase);

                foreach (var group in grouped)
                {
                    var equipmentName = group.Key;
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

        private static string GetEquipmentName(string? topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return "unknown";

            var parts = topic.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 ? parts[^2] : "unknown";
        }
    }
}