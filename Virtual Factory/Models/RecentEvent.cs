namespace Virtual_Factory.Models
{
    /// <summary>
    /// Represents a single event captured from the MQTT subscriber or generated
    /// by an API write. Events are appended to a bounded, rolling in-memory store
    /// and surfaced through GET /api/events/recent. The model is intentionally
    /// flat so it serialises cleanly to JSON without circular references.
    /// </summary>
    public class RecentEvent
    {
        /// <summary>Unique event identifier, assigned at the moment of capture.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// MQTT topic this event originated from.
        /// Example: "enterprise/site-alpha/area-assembly/line-a/conveyor01/temperature".
        /// </summary>
        public string Topic { get; set; } = string.Empty;

        /// <summary>Human-readable message or raw payload content for this event.</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Identifies the originator of the event, e.g. "mqtt", "api", "simulator", "seed".
        /// Non-nullable so event log entries are always traceable to a source.
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Broad category used to filter or group events without parsing the topic.
        /// Typical values: "Telemetry", "WriteRequest", "Operational", "System".
        /// </summary>
        public string EventType { get; set; } = "Telemetry";

        /// <summary>When the event was recorded (UTC).</summary>
        public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Id of the asset related to this event.
        /// Null if the topic could not be resolved to an asset at capture time.
        /// </summary>
        public string? AssetId { get; set; }

        /// <summary>
        /// Arbitrary key/value metadata attached to the event.
        /// Use for additional context such as quality codes, routing tags, or
        /// correlation identifiers without breaking the core event shape.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}

