using Virtual_Factory.Dtos;
using Virtual_Factory.Models;
using Virtual_Factory.Services;
using Microsoft.EntityFrameworkCore;
using Virtual_Factory.Data;

namespace Virtual_Factory.Endpoints
{
    /// <summary>Minimal API endpoints for reading telemetry values from the latest-value cache.</summary>
    public static class TelemetryEndpoints
    {
        public static IEndpointRouteBuilder MapTelemetryEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/points/latest", (ILatestPointValueStore store) =>
                Results.Ok(store.GetAll().Select(ToDto)));

            app.MapGet("/api/read", (string topic, ILatestPointValueStore store) =>
            {
                var value = store.GetByTopic(topic);
                return value is null ? Results.NotFound() : Results.Ok(ToDto(value));
            });

            app.MapPost("/api/write", (WritePointRequest request, ILatestPointValueStore store) =>
            {
                var existing = store.GetByTopic(request.Topic);
                var updated = new LatestPointValue
                {
                    Topic       = request.Topic,
                    Value       = request.Value,
                    Source      = request.Source,
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Status      = "Good",
                    AssetId     = existing?.AssetId,
                    PointName   = existing?.PointName,
                    Metadata    = existing?.Metadata ?? []
                };
                store.SetValue(updated);
                return Results.Ok(ToDto(updated));
            });
           
            app.MapGet("/api/points/history/by-signal",
                async (
                    string equipment,
                    string signal,
                    int minutes,
                    AppDbContext db,
                    CancellationToken cancellationToken) =>
                {
                    if (string.IsNullOrWhiteSpace(equipment))
                        return Results.BadRequest("equipment is required.");

                    if (string.IsNullOrWhiteSpace(signal))
                        return Results.BadRequest("signal is required.");

                    if (minutes <= 0 || minutes > 1440)
                        return Results.BadRequest("minutes must be between 1 and 1440.");

                    var sinceUtc = DateTime.UtcNow.AddMinutes(-minutes);

                    var rows = await db.TelemetryPointHistories
                        .AsNoTracking()
                        .Where(x =>
                            x.EquipmentName == equipment &&
                            x.SignalName == signal &&
                            x.TimestampUtc >= sinceUtc &&
                            x.ValueNumber != null)
                        .OrderBy(x => x.TimestampUtc)
                        .Select(x => new
                        {
                            x.TimestampUtc,
                            x.ValueNumber
                        })
                        .ToListAsync(cancellationToken);

                    return Results.Ok(rows);
                });

            app.MapGet("/api/equipment/offline-status",
    async (
        string equipment,
        AppDbContext db,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(equipment))
            return Results.BadRequest("equipment is required.");

        equipment = equipment.ToLower();

        var latestRow = await db.TelemetryPointHistories
            .AsNoTracking()
            .Where(x =>
                x.EquipmentName.ToLower() == equipment &&
                x.SignalName == "run-status")
            .OrderByDescending(x => x.TimestampUtc)
            .Select(x => new
            {
                x.TimestampUtc,
                x.ValueText
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (latestRow == null)
        {
            return Results.Ok(new
            {
                equipment,
                hasRunData = false,
                isOffline = false,
                offlineSeconds = 0,
                lastOfflineSecondsAgo = (int?)null
            });
        }

        var isOffline =
            string.Equals(latestRow.ValueText, "stopped",
                StringComparison.OrdinalIgnoreCase);

        int offlineSeconds = 0;
        int? lastOfflineSecondsAgo = null;

        if (isOffline)
        {
            var stoppedRows = await db.TelemetryPointHistories
                .AsNoTracking()
                .Where(x =>
                    x.EquipmentName.ToLower() == equipment &&
                    x.SignalName == "run-status" &&
                    x.ValueText == "stopped")
                .OrderBy(x => x.TimestampUtc)
                .ToListAsync(cancellationToken);

            var latestTimestamp = latestRow.TimestampUtc;
            var startTimestamp = latestTimestamp;

            for (int i = stoppedRows.Count - 1; i >= 0; i--)
            {
                var current = stoppedRows[i].TimestampUtc;

                if ((startTimestamp - current).TotalSeconds <= 5)
                    startTimestamp = current;
                else
                    break;
            }

            offlineSeconds =
                (int)(DateTime.UtcNow - startTimestamp).TotalSeconds;
        }
        else
        {
            var lastOfflineRow = await db.TelemetryPointHistories
                .AsNoTracking()
                .Where(x =>
                    x.EquipmentName.ToLower() == equipment &&
                    x.SignalName == "run-status" &&
                    x.ValueText == "stopped")
                .OrderByDescending(x => x.TimestampUtc)
                .Select(x => x.TimestampUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastOfflineRow != default)
            {
                lastOfflineSecondsAgo =
                    (int)(DateTime.UtcNow - lastOfflineRow).TotalSeconds;
            }
        }

        return Results.Ok(new
        {
            equipment,
            hasRunData = true,
            isOffline,
            offlineSeconds,
            lastOfflineSecondsAgo
        });
    });

            app.MapGet("/api/equipment/summary-metrics",
    async (
        AppDbContext db,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var now = DateTime.UtcNow;

            var lastAlarmTimestamp = await db.EquipmentStateEvents
                .AsNoTracking()
                .Where(x => x.EventType == "alarm-state-changed" && x.NewState == "alarm")
                .OrderByDescending(x => x.TimestampUtc)
                .Select(x => (DateTime?)x.TimestampUtc)
                .FirstOrDefaultAsync(cancellationToken);

            var lastStoppedTimestamp = await db.EquipmentStateEvents
                .AsNoTracking()
                .Where(x => x.EventType == "run-state-changed" && x.NewState == "stopped")
                .OrderByDescending(x => x.TimestampUtc)
                .Select(x => (DateTime?)x.TimestampUtc)
                .FirstOrDefaultAsync(cancellationToken);

            return Results.Ok(new
            {
                lastAlarmSecondsAgo    = lastAlarmTimestamp.HasValue
                    ? (int?)(now - lastAlarmTimestamp.Value).TotalSeconds
                    : null,
                lastStoppedSecondsAgo  = lastStoppedTimestamp.HasValue
                    ? (int?)(now - lastStoppedTimestamp.Value).TotalSeconds
                    : null,
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Results.StatusCode(499);
        }
    });

            app.MapGet("/api/equipment/alarm-status",
    async (
        string equipment,
        AppDbContext db,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(equipment))
            return Results.BadRequest("equipment is required.");

        equipment = equipment.ToLower();

        var latestAlarmRow = await db.TelemetryPointHistories
            .AsNoTracking()
            .Where(x =>
                x.EquipmentName.ToLower() == equipment &&
                x.SignalName == "alarm-state")
            .OrderByDescending(x => x.TimestampUtc)
            .Select(x => new
            {
                x.TimestampUtc,
                x.ValueText
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (latestAlarmRow == null)
        {
            return Results.Ok(new
            {
                equipment,
                hasAlarmData = false,
                isAlarmActive = false,
                alarmActiveSeconds = 0,
                lastAlarmSecondsAgo = (int?)null
            });
        }

        var isAlarmActive =
            string.Equals(latestAlarmRow.ValueText, "true", StringComparison.OrdinalIgnoreCase);

        int alarmActiveSeconds = 0;
        int? lastAlarmSecondsAgo = null;

        if (isAlarmActive)
        {
            var alarmStartRow = await db.TelemetryPointHistories
                .AsNoTracking()
                .Where(x =>
                    x.EquipmentName.ToLower() == equipment.ToLower() &&
                    x.SignalName == "alarm-state" &&
                    x.ValueText == "true")
                .OrderBy(x => x.TimestampUtc)
                .ToListAsync(cancellationToken);

            var latestTimestamp = latestAlarmRow.TimestampUtc;
            var startTimestamp = latestTimestamp;

            for (int i = alarmStartRow.Count - 1; i >= 0; i--)
            {
                var current = alarmStartRow[i].TimestampUtc;

                if ((startTimestamp - current).TotalSeconds <= 5)
                {
                    startTimestamp = current;
                }
                else
                {
                    break;
                }
            }

            alarmActiveSeconds = (int)(DateTime.UtcNow - startTimestamp).TotalSeconds;
        }
        else
                {
                    var lastAlarmRow = await db.TelemetryPointHistories
                        .AsNoTracking()
                        .Where(x =>
                            x.EquipmentName.ToLower() == equipment.ToLower() &&
                            x.SignalName == "alarm-state" &&
                            x.ValueText == "true")
                        .OrderByDescending(x => x.TimestampUtc)
                        .Select(x => x.TimestampUtc)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (lastAlarmRow != default)
                    {
                        lastAlarmSecondsAgo =
                            (int)(DateTime.UtcNow - lastAlarmRow).TotalSeconds;
                    }
                }

        return Results.Ok(new
        {
            equipment,
            hasAlarmData = true,
            isAlarmActive,
            alarmActiveSeconds,
            lastAlarmSecondsAgo
        });
    });

            return app;
        }

        private static LatestPointValueDto ToDto(LatestPointValue v) => new(
            v.Topic,
            v.Value,
            v.TimestampUtc,
            v.Status,
            v.Source,
            v.AssetId,
            v.PointName,
            v.Metadata);
    }
}
