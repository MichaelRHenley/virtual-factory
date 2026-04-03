using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public sealed class SeededSignalMetadataProvider : ISignalMetadataProvider
    {
        private readonly IReadOnlyDictionary<string, SignalMetadataDto> _byName;

        public SeededSignalMetadataProvider()
        {
            var items = new Dictionary<string, SignalMetadataDto>(StringComparer.OrdinalIgnoreCase)
            {
                ["motor-temperature"] = new SignalMetadataDto
                {
                    SignalName = "motor-temperature",
                    MinNormal = 20,
                    MaxNormal = 80,
                    Unit = "C",
                    SupportsTrendAnalysis = true,
                },
                ["motor-vibration"] = new SignalMetadataDto
                {
                    SignalName = "motor-vibration",
                    MinNormal = 0,
                    MaxNormal = 2.5,
                    Unit = "mm/s",
                    SupportsTrendAnalysis = true,
                },
                ["spindle-speed"] = new SignalMetadataDto
                {
                    SignalName = "spindle-speed",
                    MinNormal = 0,
                    MaxNormal = 120,
                    Unit = "Hz",
                    SupportsTrendAnalysis = true,
                },
                ["spindle-speed-setpoint"] = new SignalMetadataDto
                {
                    SignalName = "spindle-speed-setpoint",
                    MinNormal = 0,
                    MaxNormal = 120,
                    Unit = "Hz",
                    SupportsTrendAnalysis = false,
                },
                ["cycle-count"] = new SignalMetadataDto
                {
                    SignalName = "cycle-count",
                    MinNormal = null,
                    MaxNormal = null,
                    Unit = "count",
                    SupportsTrendAnalysis = true,
                    InterpretationHintNearLimit = "Approaching maintenance or lifecycle threshold",
                },
                ["run-status"] = new SignalMetadataDto
                {
                    SignalName = "run-status",
                    MinNormal = null,
                    MaxNormal = null,
                    Unit = string.Empty,
                    SupportsTrendAnalysis = false,
                },
                ["alarm-state"] = new SignalMetadataDto
                {
                    SignalName = "alarm-state",
                    MinNormal = null,
                    MaxNormal = null,
                    Unit = string.Empty,
                    SupportsTrendAnalysis = false,
                },
            };

            _byName = new ReadOnlyDictionary<string, SignalMetadataDto>(items);
        }

        public SignalMetadataDto? GetMetadata(string signalName)
        {
            if (string.IsNullOrWhiteSpace(signalName))
                return null;

            return _byName.TryGetValue(signalName, out var meta) ? meta : null;
        }
    }
}
