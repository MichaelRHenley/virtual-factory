using Virtual_Factory.Models;

namespace Virtual_Factory.Repositories
{
    /// <inheritdoc cref="ITelemetryPointRepository"/>
    public sealed class InMemoryTelemetryPointRepository : ITelemetryPointRepository
    {
        private readonly Dictionary<string, TelemetryPointDefinition> _byId = new();
        private readonly Dictionary<string, TelemetryPointDefinition> _byTopic = new();
        private readonly Dictionary<string, List<TelemetryPointDefinition>> _byAssetId = new();

        public void Add(TelemetryPointDefinition point)
        {
            _byId[point.Id] = point;

            if (!string.IsNullOrEmpty(point.Topic))
                _byTopic[point.Topic] = point;

            if (!string.IsNullOrEmpty(point.AssetId))
            {
                if (!_byAssetId.TryGetValue(point.AssetId, out var list))
                {
                    list = [];
                    _byAssetId[point.AssetId] = list;
                }
                list.Add(point);
            }
        }

        public IReadOnlyList<TelemetryPointDefinition> GetAll() => _byId.Values.ToList();

        public IReadOnlyList<TelemetryPointDefinition> GetByAssetId(string assetId) =>
            _byAssetId.TryGetValue(assetId, out var points) ? points : [];

        public TelemetryPointDefinition? GetByTopic(string topic) =>
            _byTopic.TryGetValue(topic, out var point) ? point : null;
    }
}
