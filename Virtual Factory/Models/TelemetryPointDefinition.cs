namespace Virtual_Factory.Models
{
    /// <summary>
    /// Defines a single telemetry or state point that belongs to an asset and
    /// is addressable via a Unified Namespace MQTT topic. The definition is
    /// intentionally static — it describes the shape of the point, not its
    /// current value. Live values are held separately in <see cref="LatestPointValue"/>,
    /// keeping the schema stable as the UNS evolves toward an interoperability layer.
    /// </summary>
    public class TelemetryPointDefinition
    {
        /// <summary>Unique identifier for this point definition.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Id of the asset this point is attached to.</summary>
        public string AssetId { get; set; } = string.Empty;

        /// <summary>
        /// Full MQTT topic for this point.
        /// Example: "enterprise/site-alpha/area-assembly/line-a/conveyor01/temperature".
        /// </summary>
        public string Topic { get; set; } = string.Empty;

        /// <summary>
        /// Point name; matches the leaf segment of <see cref="Topic"/> (e.g. "temperature").
        /// Used to look up the point by name within its parent asset.
        /// </summary>
        public string PointName { get; set; } = string.Empty;

        /// <summary>Human-readable label for this point.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Primitive data type hint: "double", "int", "string", "bool".</summary>
        public string DataType { get; set; } = "string";

        /// <summary>Engineering unit, e.g. "°C", "rpm", "bar". Null if unitless or dimensionless.</summary>
        public string? Unit { get; set; }

        /// <summary>Optional description of what this point measures or represents.</summary>
        public string? Description { get; set; }

        /// <summary>
        /// Indicates whether this point accepts writes via POST /api/write.
        /// Read-only points (sensors, computed values) should have this set to false.
        /// </summary>
        public bool IsWritable { get; set; }

        /// <summary>
        /// Logical grouping for the point, e.g. "Telemetry", "State", "Setpoint", "Diagnostic".
        /// Allows clients to filter points by purpose without parsing the topic.
        /// </summary>
        public string Category { get; set; } = "Telemetry";

        /// <summary>Arbitrary key/value metadata for extensibility and future semantic mapping.</summary>
        public Dictionary<string, string> Tags { get; set; } = new();

        /// <summary>When this point definition was created (UTC).</summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
