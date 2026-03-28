namespace Virtual_Factory.Models
{
    /// <summary>
    /// Represents a material, spare part, consumable, or product code used in the
    /// virtual factory simulation. Materials link raw inputs and finished goods to
    /// the equipment assets that consume or produce them.
    /// </summary>
    public class Material
    {
        /// <summary>Unique material or part code (e.g. "MAT-BELT-01", "PROD-FRAME-A").</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Human-readable description of the material.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Unit of measure for stock quantities (e.g. "each", "roll", "spool").</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Material category.
        /// Typical values: "Raw Material", "Consumable", "Spare Part", "Semi-Finished Good", "Finished Good".
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Ids of equipment assets that use or produce this material.</summary>
        public List<string> UsedBy { get; set; } = new();
    }
}
