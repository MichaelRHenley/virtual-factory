using Virtual_Factory.Models;
using Virtual_Factory.Repositories;

namespace Virtual_Factory.Services
{
    /// <summary>
    /// Background service that simulates realistic, smoothly drifting telemetry
    /// values for every registered point definition and writes them to the
    /// <see cref="ILatestPointValueStore"/> on a fixed 3-second interval.
    /// </summary>
    public sealed class TelemetrySimulationService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(3);

        private readonly ITelemetryPointRepository _points;
        private readonly ILatestPointValueStore _store;

        // Per-topic state — all keyed by topic string
        private readonly Dictionary<string, double> _numericState  = new(StringComparer.Ordinal);
        private readonly Dictionary<string, long>   _counterState  = new(StringComparer.Ordinal);
        private readonly Dictionary<string, bool>   _boolState     = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int>    _boolTickState = new(StringComparer.Ordinal);

        public TelemetrySimulationService(
            ITelemetryPointRepository points,
            ILatestPointValueStore store)
        {
            _points = points;
            _store  = store;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(Interval, stoppingToken);
                SimulateTick();
            }
        }

        private void SimulateTick()
        {
            var now = DateTimeOffset.UtcNow;

            foreach (var point in _points.GetAll())
            {
                // Writable setpoints: only write if no value exists yet or the last writer was the simulator
                if (point.IsWritable)
                {
                    var existing = _store.GetByTopic(point.Topic);
                    if (existing is not null && existing.Source != "simulator")
                        continue;
                }

                _store.SetValue(new LatestPointValue
                {
                    Topic        = point.Topic,
                    Value        = ComputeValue(point),
                    TimestampUtc = now,
                    Status       = "Good",
                    Source       = "simulator",
                    AssetId      = point.AssetId,
                    PointName    = point.PointName,
                    Metadata     = []
                });
            }
        }

        // ── value computation ────────────────────────────────────────────────

        private string ComputeValue(TelemetryPointDefinition point)
        {
            var name = point.PointName;

            if (IsRunStatus(name, point.Category))
                return NextBool(point.Topic, trueWeight: 0.85).ToString().ToLowerInvariant();

            if (IsAlarmState(name, point.Category))
                return NextBool(point.Topic, trueWeight: 0.04).ToString().ToLowerInvariant();

            if (IsCounter(name))
                return NextCounter(point.Topic).ToString();

            if (name.Contains("temperature", StringComparison.OrdinalIgnoreCase))
                return Drift(point.Topic, seed: 55.0, min: 35.0, max: 85.0, step: 1.5).ToString("F2");

            if (name.Contains("vibration", StringComparison.OrdinalIgnoreCase))
                return Drift(point.Topic, seed: 1.5, min: 0.5, max: 8.0, step: 0.2).ToString("F3");

            if (name.Contains("pressure", StringComparison.OrdinalIgnoreCase))
                return Drift(point.Topic, seed: 5.0, min: 1.0, max: 10.0, step: 0.2).ToString("F2");

            if (name == "welding-current")
                return Drift(point.Topic, seed: 160.0, min: 80.0, max: 250.0, step: 4.0).ToString("F1");

            if (name == "welding-voltage")
                return Drift(point.Topic, seed: 24.0, min: 18.0, max: 30.0, step: 0.4).ToString("F1");

            if (name.StartsWith("laser-power", StringComparison.OrdinalIgnoreCase))
                return Drift(point.Topic, seed: 2000.0, min: 500.0, max: 3800.0, step: 50.0).ToString("F0");

            if (IsSpeed(name))
                return Drift(point.Topic, seed: 60.0, min: 0.0, max: 120.0, step: 2.0).ToString("F1");

            if (name == "ram-position")
                return Drift(point.Topic, seed: 120.0, min: 0.0, max: 250.0, step: 4.0).ToString("F1");

            if (name == "payload")
                return Drift(point.Topic, seed: 15.0, min: 0.0, max: 25.0, step: 0.5).ToString("F2");

            // Setpoints and remaining writable points: stable initial seed
            if (point.IsWritable || point.Category == "Setpoint")
                return Drift(point.Topic, seed: 60.0, min: 60.0, max: 60.0, step: 0.0).ToString("F1");

            // Generic numeric fallback
            return Drift(point.Topic, seed: 50.0, min: 0.0, max: 100.0, step: 1.0).ToString("F1");
        }

        // ── simulation primitives ────────────────────────────────────────────

        /// <summary>Drifts a numeric value by at most <paramref name="step"/> per tick, clamped to [min, max].</summary>
        private double Drift(string topic, double seed, double min, double max, double step)
        {
            if (!_numericState.TryGetValue(topic, out var current))
                current = seed;

            if (step > 0.0)
            {
                var delta = (Random.Shared.NextDouble() * 2.0 - 1.0) * step;
                current   = Math.Clamp(current + delta, min, max);
            }

            _numericState[topic] = current;
            return current;
        }

        /// <summary>
        /// Returns a bool that re-evaluates every 5 ticks with the given probability of being true,
        /// avoiding rapid per-tick flickering.
        /// </summary>
        private bool NextBool(string topic, double trueWeight)
        {
            var tick = _boolTickState.GetValueOrDefault(topic);
            _boolTickState[topic] = tick + 1;

            if (tick % 5 == 0)
                _boolState[topic] = Random.Shared.NextDouble() < trueWeight;

            return _boolState.GetValueOrDefault(topic);
        }

        /// <summary>Increments a monotonic counter per topic, seeding it with a random offset on first call.</summary>
        private long NextCounter(string topic)
        {
            if (!_counterState.TryGetValue(topic, out var count))
                count = Random.Shared.NextInt64(0, 1000);

            count++;
            _counterState[topic] = count;
            return count;
        }

        // ── classification helpers ───────────────────────────────────────────

        private static bool IsRunStatus(string name, string category) =>
            name == "run-status" || category == "State";

        private static bool IsAlarmState(string name, string category) =>
            name == "alarm-state" || category == "Diagnostic";

        private static bool IsCounter(string name) =>
            name.EndsWith("count", StringComparison.OrdinalIgnoreCase)
            || name == "throughput"
            || name == "layer-count";

        private static bool IsSpeed(string name) =>
            name.Contains("speed", StringComparison.OrdinalIgnoreCase)
            && !name.Contains("setpoint", StringComparison.OrdinalIgnoreCase);
    }
}
