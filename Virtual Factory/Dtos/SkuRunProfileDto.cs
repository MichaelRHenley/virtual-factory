using System;

namespace Virtual_Factory.Dtos
{
    public sealed class SkuRunProfileDto
    {
        public string Sku { get; set; } = string.Empty;
        public string SkuDescription { get; set; } = string.Empty;
        public string EquipmentId { get; set; } = string.Empty;
        public string SignalName { get; set; } = string.Empty;
        public double? TargetValue { get; set; }
        public double? MinRecommended { get; set; }
        public double? MaxRecommended { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public string? InterpretationHintHigh { get; set; }
        public string? InterpretationHintLow { get; set; }
        public string? InterpretationHintNearLimit { get; set; }
    }
}
