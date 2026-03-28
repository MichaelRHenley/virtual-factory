namespace Virtual_Factory.Models
{
    /// <summary>
    /// A node in the ISA-95 asset hierarchy, ranging from Enterprise down to
    /// individual Equipment. The <see cref="Path"/> mirrors the MQTT topic prefix
    /// used by the Unified Namespace, so asset addressing and topic addressing
    /// stay in sync without extra mapping.
    /// </summary>
    public class Asset
    {
        /// <summary>Unique identifier (slug-style, e.g. "conveyor01").</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Machine-friendly name used in paths and topics.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Human-readable label intended for UI display.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>ISA-95 hierarchy level of this asset.</summary>
        public AssetType AssetType { get; set; }

        /// <summary>Id of the parent asset; null for the root Enterprise asset.</summary>
        public string? ParentId { get; set; }

        /// <summary>
        /// Slash-delimited path from the root, matching the MQTT topic prefix.
        /// Example: "enterprise/site-alpha/area-assembly/line-a/conveyor01".
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>Optional human-readable description of this asset's role.</summary>
        public string? Description { get; set; }

        /// <summary>Arbitrary key/value metadata for extensibility.</summary>
        public Dictionary<string, string> Tags { get; set; } = new();

        /// <summary>Optional reference to a <see cref="MetadataProfile"/> describing this asset's points.</summary>
        public string? ProfileId { get; set; }

        /// <summary>Ids of direct child assets; kept denormalised for fast hierarchy traversal.</summary>
        public List<string> ChildrenIds { get; set; } = new();

        /// <summary>When this asset record was created (UTC).</summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
