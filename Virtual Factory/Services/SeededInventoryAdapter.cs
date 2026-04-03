using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public sealed class SeededInventoryAdapter : IInventoryAdapter
    {
        private readonly List<InventoryItemDto> _items;

        public SeededInventoryAdapter()
        {
            _items = new List<InventoryItemDto>
            {
                new InventoryItemDto
                {
                    MaterialId = "MAT-BELT-CORE",
                    MaterialDescription = "Timing belt core",
                    AvailableQuantity = 5m,
                    ReservedQuantity = 3m,
                    UnitOfMeasure = "EA",
                    StockStatus = "healthy"
                },
                new InventoryItemDto
                {
                    MaterialId = "MAT-BELT-COVER",
                    MaterialDescription = "Belt cover",
                    AvailableQuantity = 1m,
                    ReservedQuantity = 2m,
                    UnitOfMeasure = "EA",
                    StockStatus = "short"
                },
                new InventoryItemDto
                {
                    MaterialId = "MAT-BELT-HARDWARE",
                    MaterialDescription = "Fastener kit",
                    AvailableQuantity = 20m,
                    ReservedQuantity = 5m,
                    UnitOfMeasure = "EA",
                    StockStatus = "healthy"
                },

                new InventoryItemDto
                {
                    MaterialId = "MAT-INSPECT-FIXTURE",
                    MaterialDescription = "Inspection fixture",
                    AvailableQuantity = 2m,
                    ReservedQuantity = 1m,
                    UnitOfMeasure = "EA",
                    StockStatus = "healthy"
                },
                new InventoryItemDto
                {
                    MaterialId = "MAT-INSPECT-LABEL",
                    MaterialDescription = "Inspection label",
                    AvailableQuantity = 5m,
                    ReservedQuantity = 4m,
                    UnitOfMeasure = "EA",
                    StockStatus = "at_risk"
                },

                new InventoryItemDto
                {
                    MaterialId = "MAT-CASE",
                    MaterialDescription = "Shipping case",
                    AvailableQuantity = 50m,
                    ReservedQuantity = 10m,
                    UnitOfMeasure = "EA",
                    StockStatus = "healthy"
                },
                new InventoryItemDto
                {
                    MaterialId = "MAT-INSERT",
                    MaterialDescription = "Foam insert",
                    AvailableQuantity = 10m,
                    ReservedQuantity = 9m,
                    UnitOfMeasure = "EA",
                    StockStatus = "at_risk"
                },
                new InventoryItemDto
                {
                    MaterialId = "MAT-TAPE",
                    MaterialDescription = "Carton tape",
                    AvailableQuantity = 1m,
                    ReservedQuantity = 1m,
                    UnitOfMeasure = "ROLL",
                    StockStatus = "short"
                },
            };
        }

        public Task<List<InventoryItemDto>> GetInventoryForMaterialsAsync(IEnumerable<string> materialIds)
        {
            if (materialIds is null)
                return Task.FromResult(new List<InventoryItemDto>());

            var set = new HashSet<string>(materialIds, System.StringComparer.OrdinalIgnoreCase);
            var list = _items
                .Where(i => set.Contains(i.MaterialId))
                .ToList();

            return Task.FromResult(list);
        }

        public Task<InventoryItemDto?> GetInventoryByMaterialAsync(string materialId)
        {
            if (string.IsNullOrWhiteSpace(materialId))
                return Task.FromResult<InventoryItemDto?>(null);

            var item = _items.FirstOrDefault(i =>
                string.Equals(i.MaterialId, materialId, System.StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(item);
        }
    }
}
