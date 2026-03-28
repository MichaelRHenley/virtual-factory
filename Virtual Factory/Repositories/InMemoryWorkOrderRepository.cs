using Virtual_Factory.Models;

namespace Virtual_Factory.Repositories
{
    /// <inheritdoc cref="IWorkOrderRepository"/>
    public sealed class InMemoryWorkOrderRepository : IWorkOrderRepository
    {
        private readonly Dictionary<string, WorkOrder> _store = new();

        public void Add(WorkOrder workOrder) => _store[workOrder.Id] = workOrder;

        public IReadOnlyList<WorkOrder> GetAll() => _store.Values.ToList();

        public WorkOrder? GetById(string id) =>
            _store.TryGetValue(id, out var wo) ? wo : null;

        public IReadOnlyList<WorkOrder> GetByAsset(string assetId) =>
            _store.Values.Where(w => w.AssetId == assetId).ToList();
    }
}
