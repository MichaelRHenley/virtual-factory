namespace Virtual_Factory.Models
{
    /// <summary>
    /// Represents a generic work order in a vendor-neutral manufacturing simulation.
    /// A work order captures the intent, scope, scheduling, and current lifecycle
    /// state of a discrete unit of work associated with a specific asset. The
    /// <see cref="Path"/> mirrors the asset's MQTT topic prefix so work-order
    /// events can be published to the Unified Namespace without extra mapping.
    /// </summary>
    public class WorkOrder
    {
        /// <summary>Unique identifier (slug-style, e.g. "wo-100245").</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Human-readable work order number used on the shop floor (e.g. "WO-2025-00042").</summary>
        public string WorkOrderNumber { get; set; } = string.Empty;

        /// <summary>Short title describing the work to be performed.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Lifecycle status of the work order.
        /// Suggested values: "Planned", "InProgress", "OnHold", "Complete", "Cancelled".
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Id of the asset this work order is associated with.</summary>
        public string AssetId { get; set; } = string.Empty;

        /// <summary>
        /// Slash-delimited path inherited from the associated asset, matching its MQTT topic prefix.
        /// Example: "enterprise/site-alpha/area-assembly/line-a/conveyor01".
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Priority level of the work order.
        /// Suggested values: "Low", "Normal", "High", "Critical".
        /// </summary>
        public string Priority { get; set; } = string.Empty;

        /// <summary>Optional human-readable description of the work to be performed.</summary>
        public string? Description { get; set; }

        /// <summary>When this work order record was created (UTC).</summary>
        public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>Scheduled start time for the work order (UTC); null if not yet scheduled.</summary>
        public DateTimeOffset? ScheduledStartUtc { get; set; }

        /// <summary>Scheduled end time for the work order (UTC); null if not yet scheduled.</summary>
        public DateTimeOffset? ScheduledEndUtc { get; set; }

        /// <summary>Arbitrary key/value metadata for extensibility and future interoperability.</summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
