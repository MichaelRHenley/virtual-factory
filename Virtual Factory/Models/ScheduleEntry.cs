namespace Virtual_Factory.Models
{
    /// <summary>
    /// Represents a schedule entry for production, maintenance, or shift activity
    /// in the virtual factory. Each entry is scoped to a specific asset and carries
    /// enough information for downstream systems to coordinate work without requiring
    /// vendor-specific scheduling extensions.
    /// </summary>
    public class ScheduleEntry
    {
        /// <summary>Unique identifier for this schedule entry.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Id of the asset this entry is scoped to.</summary>
        public string AssetId { get; set; } = string.Empty;

        /// <summary>
        /// Category of activity this entry represents.
        /// Suggested values: "Production", "Maintenance", "Shift", "Downtime", "Inspection".
        /// </summary>
        public string ScheduleType { get; set; } = string.Empty;

        /// <summary>Short title for the schedule entry (e.g. "Morning Shift", "PM Maintenance Window").</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>Scheduled start time for this entry (UTC).</summary>
        public DateTimeOffset StartUtc { get; set; }

        /// <summary>Scheduled end time for this entry (UTC).</summary>
        public DateTimeOffset EndUtc { get; set; }

        /// <summary>
        /// Lifecycle status of the schedule entry.
        /// Suggested values: "Scheduled", "Active", "Complete", "Cancelled".
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Optional human-readable description of what this entry represents.</summary>
        public string? Description { get; set; }

        /// <summary>Arbitrary key/value metadata for extensibility and scheduling interoperability.</summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
