namespace Virtual_Factory.Models;

/// <summary>
/// ISA-95 hierarchy levels, ordered from the broadest organisational scope
/// down to an individual piece of equipment on the production floor.
/// Explicit integer values are assigned so the meaning is preserved if the
/// enum is serialised to a database, message payload, or profile file.
/// </summary>
public enum AssetType
{
    /// <summary>Top-level organisational unit that spans all sites (ISA-95 Level 4).</summary>
    Enterprise = 1,

    /// <summary>A physical or logical manufacturing site within the enterprise (ISA-95 Level 3).</summary>
    Site = 2,

    /// <summary>A functional zone within a site, e.g. assembly, packaging, utilities (ISA-95 Level 2).</summary>
    Area = 3,

    /// <summary>A production line within an area (ISA-95 Level 1).</summary>
    Line = 4,

    /// <summary>An individual piece of equipment on a line (ISA-95 Level 0).</summary>
    Equipment = 5
}
