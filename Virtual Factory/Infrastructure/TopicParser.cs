namespace Virtual_Factory.Infrastructure
{
    /// <summary>
    /// Canonical MQTT topic parser for the Virtual Factory namespace.
    /// Supports three topic formats:
    ///   [prefix-]enterprise/site/area/line/equipment/signal  (6+ segments, first segment ends with "enterprise")
    ///   site/area/line/equipment/signal                      (5+ segments)
    ///   equipment/signal                                     (2+ segments, flat fallback)
    /// </summary>
    public static class TopicParser
    {
        /// <summary>
        /// Parses a topic into its ISA-95 hierarchy components.
        /// Returns null for topics that cannot be mapped (fewer than 2 segments).
        /// Flat topics are placed under default-site/default-area/default-line.
        /// </summary>
        public static (string Site, string Area, string Line, string Equipment)? TryParse(string? topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return null;

            var parts = topic.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // [prefix-]enterprise/site/area/line/equipment/signal
            // Matches "enterprise", "virtual-enterprise", "acme-enterprise", etc.
            if (parts.Length >= 6 &&
                parts[0].EndsWith("enterprise", StringComparison.OrdinalIgnoreCase))
            {
                return (parts[1], parts[2], parts[3], parts[4]);
            }

            // Enterprise-prefixed topics with fewer than 6 segments are line-level or
            // area-level aggregates (e.g. enterprise/site/area/line/signal). There is no
            // equipment segment, so return null rather than misclassifying the line as equipment.
            if (parts[0].EndsWith("enterprise", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // site/area/line/equipment/signal
            if (parts.Length >= 5)
            {
                return (parts[0], parts[1], parts[2], parts[3]);
            }

            // equipment/signal  —  flat fallback
            if (parts.Length >= 2)
            {
                return ("default-site", "default-area", "default-line", parts[0]);
            }

            return null;
        }

        /// <summary>
        /// Extracts the equipment name segment from a topic.
        /// Returns "unknown" for topics that cannot be parsed.
        /// </summary>
        public static string ExtractEquipmentName(string? topic)
        {
            var parsed = TryParse(topic);
            return parsed?.Equipment ?? "unknown";
        }
    }
}
