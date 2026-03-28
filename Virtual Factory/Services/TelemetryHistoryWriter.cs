using Virtual_Factory.Data;
using Virtual_Factory.Models;

namespace Virtual_Factory.Services
{
    public class TelemetryHistoryWriter
    {
        private readonly AppDbContext _db;

        public TelemetryHistoryWriter(AppDbContext db)
        {
            _db = db;
        }

        public async Task WriteAsync(IEnumerable<LatestPointValue> points)
        {
            if (!points.Any())
                return;

            var rows = points.Select(p => new TelemetryPointHistory
            {
                Topic = p.Topic,
                EquipmentName = GetEquipmentName(p.Topic),
                SignalName = GetSignalName(p.Topic),
                ValueText = p.Value?.ToString(),
                ValueNumber = TryParseNumber(p.Value),
                Status = p.Status,
                Source = p.Source,
                TimestampUtc = p.TimestampUtc.UtcDateTime,
                CreatedUtc = DateTime.UtcNow
            });

            _db.TelemetryPointHistories.AddRange(rows);

            await _db.SaveChangesAsync();
        }

        private static string GetEquipmentName(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return "unknown";

            var parts = topic.Split('/');
            return parts.Length >= 2 ? parts[^2] : "unknown";
        }

        private static string GetSignalName(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return "";

            var parts = topic.Split('/');
            return parts[^1];
        }

        private static double? TryParseNumber(object? value)
        {
            if (value == null)
                return null;

            if (double.TryParse(value.ToString(), out var parsed))
                return parsed;

            return null;
        }
    }
}