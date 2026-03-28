namespace Virtual_Factory.Models
{
    /// <summary>
    /// Represents a reusable metadata profile definition for assets or telemetry points.
    /// Profiles centralise structural and semantic descriptors so multiple assets can
    /// share a common shape without duplicating configuration. This model is intentionally
    /// forward-compatible with i3X-style semantic mapping and profile-driven interoperability.
    /// Assets reference a profile via <see cref="Asset.ProfileId"/>.
    /// </summary>
    public class MetadataProfile
    {
        /// <summary>Unique profile identifier (e.g. "conveyor-v1").</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Machine-friendly name for the profile (e.g. "conveyor-standard").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Semantic version of this profile definition (e.g. "1.0", "2.1.0").</summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>Human-readable description of the profile's intended use and scope.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Category of entity this profile applies to.
        /// Suggested values: "Asset", "TelemetryPoint", "Equipment", "Line".
        /// </summary>
        public string ProfileType { get; set; } = string.Empty;

        /// <summary>
        /// Arbitrary key/value attributes that define the profile's structure and defaults.
        /// Keys are attribute names; values are their default or descriptive values.
        /// </summary>
        public Dictionary<string, string> Attributes { get; set; } = new();

        /// <summary>
        /// Telemetry point type names supported by assets using this profile
        /// (e.g. "temperature", "speed", "vibration").
        /// </summary>
        public List<string> SupportedPointTypes { get; set; } = new();

        /// <summary>When this profile was created (UTC).</summary>
        public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    }
}
