using System.Collections.Generic;

namespace Virtual_Factory.Dtos
{
    public sealed class BomItemDto
    {
        public string Sku { get; set; } = string.Empty;
        public string MaterialId { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
    }

    public sealed class InventoryItemDto
    {
        public string MaterialId { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public decimal AvailableQuantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public string StockStatus { get; set; } = string.Empty;
    }
}
