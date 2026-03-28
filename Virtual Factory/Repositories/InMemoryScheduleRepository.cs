using Virtual_Factory.Models;

namespace Virtual_Factory.Repositories
{
    /// <inheritdoc cref="IScheduleRepository"/>
    public sealed class InMemoryScheduleRepository : IScheduleRepository
    {
        private readonly Dictionary<string, ScheduleEntry> _store = new();

        public void Add(ScheduleEntry entry) => _store[entry.Id] = entry;

        public IReadOnlyList<ScheduleEntry> GetAll() => _store.Values.ToList();

        public ScheduleEntry? GetById(string id) =>
            _store.TryGetValue(id, out var entry) ? entry : null;

        public IReadOnlyList<ScheduleEntry> GetByAsset(string assetId) =>
            _store.Values.Where(s => s.AssetId == assetId).ToList();
    }
}
