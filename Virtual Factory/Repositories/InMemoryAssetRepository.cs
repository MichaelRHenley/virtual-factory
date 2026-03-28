using Virtual_Factory.Models;

namespace Virtual_Factory.Repositories
{
    /// <inheritdoc cref="IAssetRepository"/>
    public sealed class InMemoryAssetRepository : IAssetRepository
    {
        private readonly Dictionary<string, Asset> _byId = new();
        private readonly Dictionary<string, Asset> _byPath = new();

        public void Add(Asset asset)
        {
            _byId[asset.Id] = asset;
            if (!string.IsNullOrEmpty(asset.Path))
                _byPath[asset.Path] = asset;
        }

        public IReadOnlyList<Asset> GetAll() => _byId.Values.ToList();

        public Asset? GetById(string id) =>
            _byId.TryGetValue(id, out var asset) ? asset : null;

        public Asset? GetByPath(string path) =>
            _byPath.TryGetValue(path, out var asset) ? asset : null;

        public IReadOnlyList<Asset> GetChildren(string parentId)
        {
            if (!_byId.TryGetValue(parentId, out var parent))
                return [];

            return parent.ChildrenIds
                .Select(id => _byId.TryGetValue(id, out var child) ? child : null)
                .OfType<Asset>()
                .ToList();
        }
    }
}
