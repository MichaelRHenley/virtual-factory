using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    /// <summary>
    /// In-memory, seeded adapter for production orders.
    /// Intended for demos and can be replaced later by an API-backed adapter
    /// without changing callers.
    /// </summary>
    public sealed class SeededProductionOrderAdapter : IProductionOrderAdapter
    {
        private readonly List<ProductionOrderDto> _orders;

        public SeededProductionOrderAdapter()
        {
            var now = DateTime.UtcNow;

            _orders = new List<ProductionOrderDto>
            {
                // deburring-station-01
                new()
                {
                    OrderId         = "PO-1001",
                    EquipmentId     = "deburring-station-01",
                    Sku             = "SK-DBR-01",
                    SkuDescription  = "Aluminum housing deburr",
                    PlannedQuantity = 500,
                    CompletedQuantity = 220,
                    Status          = "Running",
                    PlannedStartUtc = now.AddHours(-1),
                    PlannedEndUtc   = now.AddHours(3),
                    Priority        = 1,
                    CustomerOrderRef = "CO-9001"
                },
                new()
                {
                    OrderId         = "PO-1002",
                    EquipmentId     = "deburring-station-01",
                    Sku             = "SK-DBR-02",
                    SkuDescription  = "Steel bracket edge deburr",
                    PlannedQuantity = 800,
                    CompletedQuantity = 0,
                    Status          = "Scheduled",
                    PlannedStartUtc = now.AddHours(3),
                    PlannedEndUtc   = now.AddHours(9),
                    Priority        = 2,
                    CustomerOrderRef = "CO-9002"
                },

                // inspection-station-01
                new()
                {
                    OrderId         = "PO-2001",
                    EquipmentId     = "inspection-station-01",
                    Sku             = "SK-INSP-01",
                    SkuDescription  = "Camera inspection – housings",
                    PlannedQuantity = 500,
                    CompletedQuantity = 180,
                    Status          = "Running",
                    PlannedStartUtc = now.AddMinutes(-45),
                    PlannedEndUtc   = now.AddHours(2),
                    Priority        = 1,
                    CustomerOrderRef = "CO-9101"
                },
                new()
                {
                    OrderId         = "PO-2002",
                    EquipmentId     = "inspection-station-01",
                    Sku             = "SK-INSP-02",
                    SkuDescription  = "Final visual inspection – brackets",
                    PlannedQuantity = 700,
                    CompletedQuantity = 0,
                    Status          = "Scheduled",
                    PlannedStartUtc = now.AddHours(2),
                    PlannedEndUtc   = now.AddHours(7),
                    Priority        = 2,
                    CustomerOrderRef = "CO-9102"
                },

                // case-packer-01
                new()
                {
                    OrderId         = "PO-3001",
                    EquipmentId     = "case-packer-01",
                    Sku             = "SK-CASE-01",
                    SkuDescription  = "Pack housings into 24-piece cases",
                    PlannedQuantity = 200,
                    CompletedQuantity = 60,
                    Status          = "Running",
                    PlannedStartUtc = now.AddMinutes(-30),
                    PlannedEndUtc   = now.AddHours(1),
                    Priority        = 1,
                    CustomerOrderRef = "CO-9201"
                },
                new()
                {
                    OrderId         = "PO-3002",
                    EquipmentId     = "case-packer-01",
                    Sku             = "SK-CASE-02",
                    SkuDescription  = "Pack brackets into 12-piece cases",
                    PlannedQuantity = 150,
                    CompletedQuantity = 0,
                    Status          = "Scheduled",
                    PlannedStartUtc = now.AddHours(1),
                    PlannedEndUtc   = now.AddHours(4),
                    Priority        = 2,
                    CustomerOrderRef = "CO-9202"
                }
            };
        }

        public Task<ProductionOrderDto?> GetActiveOrderAsync(string equipmentId)
        {
            var active = _orders
                .Where(o => string.Equals(o.EquipmentId, equipmentId, StringComparison.OrdinalIgnoreCase))
                .Where(o => string.Equals(o.Status, "Running", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(o.Status, "InProgress", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(o => o.PlannedStartUtc)
                .FirstOrDefault();

            return Task.FromResult<ProductionOrderDto?>(active);
        }

        public Task<List<ProductionOrderDto>> GetScheduledOrdersAsync(string equipmentId)
        {
            var scheduled = _orders
                .Where(o => string.Equals(o.EquipmentId, equipmentId, StringComparison.OrdinalIgnoreCase))
                .Where(o => string.Equals(o.Status, "Scheduled", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(o.Status, "Planned", StringComparison.OrdinalIgnoreCase))
                .OrderBy(o => o.PlannedStartUtc)
                .ToList();

            return Task.FromResult(scheduled);
        }

        public Task<List<ProductionOrderDto>> GetAllAsync()
        {
            // return copy to avoid external mutation
            return Task.FromResult(_orders.ToList());
        }
    }
}
