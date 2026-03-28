namespace Virtual_Factory.Models
{
    /// <summary>
    /// Represents a maintenance request associated with an equipment asset.
    /// A maintenance request captures the originating fault or service need and
    /// tracks its lifecycle until resolution. Once approved it typically results
    /// in one or more <see cref="WorkOrder"/> records being created.
    /// </summary>
    public class MaintenanceRequest
    {
        /// <summary>Unique identifier for this maintenance request.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Human-readable request number used on the shop floor (e.g. "MR-2025-00017").</summary>
        public string RequestNumber { get; set; } = string.Empty;

        /// <summary>Id of the equipment asset this request is raised against.</summary>
        public string AssetId { get; set; } = string.Empty;

        /// <summary>Short title describing the fault or service needed.</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Lifecycle status of the request.
        /// Suggested values: "Open", "InReview", "Approved", "InProgress", "Resolved", "Cancelled".
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Severity of the reported issue.
        /// Suggested values: "Low", "Medium", "High", "Critical".
        /// </summary>
        public string Severity { get; set; } = string.Empty;

        /// <summary>Optional human-readable description of the fault or service requirement.</summary>
        public string? Description { get; set; }

        /// <summary>Identifier of the person or role who raised the request.</summary>
        public string RequestedBy { get; set; } = string.Empty;

        /// <summary>When this maintenance request was created (UTC).</summary>
        public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>Deadline by which the maintenance must be completed (UTC); null if no deadline is set.</summary>
        public DateTimeOffset? NeededByUtc { get; set; }

        /// <summary>Arbitrary key/value metadata for extensibility and future interoperability.</summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
