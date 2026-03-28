namespace Virtual_Factory.Models
{
    /// <summary>
    /// Stores the latest known value for a single MQTT topic.
    /// The cache is keyed by <see cref="Topic"/> and updated by the subscriber
    /// service on every incoming message and by POST /api/write. Keeping the
    /// value as a plain string in v1 lets typed deserialisation be added later
    /// without changing the cache contract or the REST response shape.
    /// </summary>
    public class LatestPointValue
    {
        /// <summary>
        /// Full MQTT topic this entry represents.
        /// Example: "enterprise/site-alpha/area-assembly/line-a/conveyor01/temperature".
        /// </summary>
        public string Topic { get; set; } = string.Empty;

        /// <summary>Raw string payload of the most recent message on this topic.</summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>When this value was last written or received (UTC).</summary>
        public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Data-quality indicator for the value.
        /// Typical values: "Good", "Bad", "Uncertain", "Stale".
        /// Defaults to "Good" so seed data and simulator values read cleanly.
        /// </summary>
        public string Status { get; set; } = "Good";

        /// <summary>
        /// Identifies the last writer of this value, e.g. "mqtt", "api", "simulator", "seed".
        /// Useful for diagnostics and audit without requiring a full event log lookup.
        /// </summary>
        public string Source { get; set; } = "seed";

        /// <summary>
        /// Id of the asset that owns the point this topic belongs to.
        /// Null if the topic has not yet been matched to an asset definition.
        /// </summary>
        public string? AssetId { get; set; }

        /// <summary>
        /// Name of the point within its asset (the leaf segment of the topic).
        /// Null if the topic has not yet been matched to a point definition.
        /// </summary>
        public string? PointName { get; set; }

        /// <summary>
        /// Arbitrary key/value metadata attached to this cached value.
        /// Use for quality extensions, source routing info, or future schema hints.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}

