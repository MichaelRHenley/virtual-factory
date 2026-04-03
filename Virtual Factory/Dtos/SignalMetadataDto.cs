using System;

namespace Virtual_Factory.Dtos
{
    public sealed class SignalMetadataDto
    {
        public string SignalName { get; set; } = string.Empty;
        public double? MinNormal { get; set; }
        public double? MaxNormal { get; set; }
        public double? Setpoint { get; set; }
        public string Unit { get; set; } = string.Empty;
        public bool SupportsTrendAnalysis { get; set; }

        public string? InterpretationHintNormal    { get; set; }
        public string? InterpretationHintNearLimit { get; set; }
        public string? InterpretationHintHigh      { get; set; }
        public string? InterpretationHintLow       { get; set; }
    }
}
