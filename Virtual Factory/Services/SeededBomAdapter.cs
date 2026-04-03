using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public sealed class SeededBomAdapter : IBomAdapter
    {
        private readonly List<BomItemDto> _items;

        public SeededBomAdapter()
        {
            _items = new List<BomItemDto>
            {
                new BomItemDto { Sku = "BELT-2M", MaterialId = "MAT-BELT-CORE", MaterialDescription = "Timing belt core", RequiredQuantity = 1m, UnitOfMeasure = "EA" },
                new BomItemDto { Sku = "BELT-2M", MaterialId = "MAT-BELT-COVER", MaterialDescription = "Belt cover", RequiredQuantity = 2m, UnitOfMeasure = "EA" },
                new BomItemDto { Sku = "BELT-2M", MaterialId = "MAT-BELT-HARDWARE", MaterialDescription = "Fastener kit", RequiredQuantity = 4m, UnitOfMeasure = "EA" },

                new BomItemDto { Sku = "INSPECT-01", MaterialId = "MAT-INSPECT-FIXTURE", MaterialDescription = "Inspection fixture", RequiredQuantity = 1m, UnitOfMeasure = "EA" },
                new BomItemDto { Sku = "INSPECT-01", MaterialId = "MAT-INSPECT-LABEL", MaterialDescription = "Inspection label", RequiredQuantity = 10m, UnitOfMeasure = "EA" },

                new BomItemDto { Sku = "CASE-PACK-01", MaterialId = "MAT-CASE", MaterialDescription = "Shipping case", RequiredQuantity = 1m, UnitOfMeasure = "EA" },
                new BomItemDto { Sku = "CASE-PACK-01", MaterialId = "MAT-INSERT", MaterialDescription = "Foam insert", RequiredQuantity = 1m, UnitOfMeasure = "EA" },
                new BomItemDto { Sku = "CASE-PACK-01", MaterialId = "MAT-TAPE", MaterialDescription = "Carton tape", RequiredQuantity = 0.25m, UnitOfMeasure = "ROLL" },
            };
        }

        public Task<List<BomItemDto>> GetBomBySkuAsync(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return Task.FromResult(new List<BomItemDto>());

            var list = _items
                .Where(b => string.Equals(b.Sku, sku, System.StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Task.FromResult(list);
        }
    }
}
