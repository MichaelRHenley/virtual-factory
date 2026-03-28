using Virtual_Factory.Models;

namespace Virtual_Factory.Repositories
{
    /// <inheritdoc cref="IEventRepository"/>
    /// <remarks>
    /// Events are stored in insertion order. No eviction policy is applied in v0.1;
    /// a ring-buffer or capped-list strategy will be added in a future iteration.
    /// </remarks>
    public sealed class InMemoryEventRepository : IEventRepository
    {
        private readonly List<RecentEvent> _store = [];

        public void Add(RecentEvent evt) => _store.Add(evt);

        public IReadOnlyList<RecentEvent> GetRecent(int count = 100) =>
            _store.TakeLast(count).ToList();

        public IReadOnlyList<RecentEvent> GetByAsset(string assetId, int count = 50) =>
            _store.Where(e => e.AssetId == assetId).TakeLast(count).ToList();
    }
}
