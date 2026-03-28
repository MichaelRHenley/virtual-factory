using Virtual_Factory.Models;

namespace Virtual_Factory.Repositories
{
    /// <inheritdoc cref="IMaterialRepository"/>
    public sealed class InMemoryMaterialRepository : IMaterialRepository
    {
        private readonly Dictionary<string, Material> _store = new();

        public void Add(Material material) => _store[material.Code] = material;

        public IReadOnlyList<Material> GetAll() => _store.Values.ToList();

        public Material? GetByCode(string code) =>
            _store.TryGetValue(code, out var material) ? material : null;
    }
}
