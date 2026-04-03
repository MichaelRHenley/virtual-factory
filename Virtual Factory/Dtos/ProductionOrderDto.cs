namespace Virtual_Factory.Dtos
{
    public sealed class ProductionOrderDto
    {
        public string OrderId { get; set; } = string.Empty;
        public string EquipmentId { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string SkuDescription { get; set; } = string.Empty;
        public int PlannedQuantity { get; set; }
        public int CompletedQuantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime PlannedStartUtc { get; set; }
        public DateTime PlannedEndUtc { get; set; }
        public int Priority { get; set; }
        public string? CustomerOrderRef { get; set; }
    }
}
