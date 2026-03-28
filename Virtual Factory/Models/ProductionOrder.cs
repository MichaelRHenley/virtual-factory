namespace Virtual_Factory.Models
{
    /// <summary>
    /// Represents a production order assigned to a line or area in the virtual factory.
    /// A production order defines what product to make, in what quantity, and within
    /// which time window. Status transitions can be published to the Unified Namespace
    /// so connected systems such as ERP and MES stay aligned without polling.
    /// </summary>
    public class ProductionOrder
    {
        /// <summary>Unique identifier for this production order (e.g. "po-20250001").</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Human-readable order number used on the shop floor (e.g. "PO-2025-00001").</summary>
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>Id of the line or area asset this order is scheduled on.</summary>
        public string AssetId { get; set; } = string.Empty;

        /// <summary>Vendor-neutral product code identifying what is being manufactured.</summary>
        public string ProductCode { get; set; } = string.Empty;

        /// <summary>Human-readable description of the product being manufactured.</summary>
        public string ProductDescription { get; set; } = string.Empty;

        /// <summary>Target quantity to be produced in this order.</summary>
        public decimal PlannedQuantity { get; set; }

        /// <summary>Quantity successfully produced so far; updated as the order progresses.</summary>
        public decimal CompletedQuantity { get; set; }

        /// <summary>Unit of measure for the quantities (e.g. "units", "kg", "litre").</summary>
        public string UnitOfMeasure { get; set; } = string.Empty;

        /// <summary>
        /// Lifecycle status of the production order.
        /// Suggested values: "Pending", "Active", "Complete", "Cancelled".
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Scheduled start time for this production order (UTC).</summary>
        public DateTimeOffset PlannedStartUtc { get; set; }

        /// <summary>Scheduled end time for this production order (UTC).</summary>
        public DateTimeOffset PlannedEndUtc { get; set; }

        /// <summary>Arbitrary key/value metadata for extensibility and simulation seeding.</summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
