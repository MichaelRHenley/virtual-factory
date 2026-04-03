using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public sealed class SeededSkuRunProfileProvider : ISkuRunProfileProvider
    {
        private readonly List<SkuRunProfileDto> _profiles;

        public SeededSkuRunProfileProvider()
        {
            _profiles = new List<SkuRunProfileDto>
            {
                // deburring-station-01 active SKU (example: BELT-2M)
                new SkuRunProfileDto
                {
                    Sku = "BELT-2M",
                    SkuDescription = "2m timing belt",
                    EquipmentId = "DEBURRING-STATION-01",
                    SignalName = "spindle-speed",
                    TargetValue = 60,
                    MinRecommended = 55,
                    MaxRecommended = 65,
                    UnitOfMeasure = "Hz",
                    InterpretationHintLow = "Speed below target may reduce deburr quality or throughput",
                    InterpretationHintHigh = "Speed above target may increase wear or chatter risk",
                    InterpretationHintNearLimit = "Speed drifting near limits for this SKU",
                },
                new SkuRunProfileDto
                {
                    Sku = "BELT-2M",
                    SkuDescription = "2m timing belt",
                    EquipmentId = "DEBURRING-STATION-01",
                    SignalName = "motor-temperature",
                    TargetValue = 55,
                    MinRecommended = 20,
                    MaxRecommended = 80,
                    UnitOfMeasure = "C",
                    InterpretationHintHigh = "Temperature above profile range may indicate load or cooling issues",
                },
                new SkuRunProfileDto
                {
                    Sku = "BELT-2M",
                    SkuDescription = "2m timing belt",
                    EquipmentId = "DEBURRING-STATION-01",
                    SignalName = "motor-vibration",
                    TargetValue = 1.5,
                    MinRecommended = 0,
                    MaxRecommended = 2.5,
                    UnitOfMeasure = "mm/s",
                    InterpretationHintHigh = "Vibration above profile range may indicate imbalance or belt misalignment",
                },

                // inspection-station-01 active SKU (example: INSPECT-01)
                new SkuRunProfileDto
                {
                    Sku = "INSPECT-01",
                    SkuDescription = "Inspection sequence",
                    EquipmentId = "INSPECTION-STATION-01",
                    SignalName = "cycle-count",
                    TargetValue = null,
                    MinRecommended = null,
                    MaxRecommended = null,
                    UnitOfMeasure = "count",
                    InterpretationHintNearLimit = "Cycle count approaching profile threshold for this inspection SKU",
                },
                new SkuRunProfileDto
                {
                    Sku = "INSPECT-01",
                    SkuDescription = "Inspection sequence",
                    EquipmentId = "INSPECTION-STATION-01",
                    SignalName = "motor-temperature",
                    TargetValue = 45,
                    MinRecommended = 20,
                    MaxRecommended = 70,
                    UnitOfMeasure = "C",
                    InterpretationHintHigh = "Sustained high temperature during inspection may affect camera stability",
                },

                // case-packer-01 active SKU (example: CASE-PACK-01)
                new SkuRunProfileDto
                {
                    Sku = "CASE-PACK-01",
                    SkuDescription = "Case packing sequence",
                    EquipmentId = "CASE-PACKER-01",
                    SignalName = "conveyor-speed",
                    TargetValue = 1.2,
                    MinRecommended = 1.0,
                    MaxRecommended = 1.4,
                    UnitOfMeasure = "m/s",
                    InterpretationHintLow = "Conveyor speed below profile target may limit packing throughput",
                    InterpretationHintHigh = "Conveyor speed above profile range may cause case instability",
                },
                new SkuRunProfileDto
                {
                    Sku = "CASE-PACK-01",
                    SkuDescription = "Case packing sequence",
                    EquipmentId = "CASE-PACKER-01",
                    SignalName = "motor-vibration",
                    TargetValue = 1.2,
                    MinRecommended = 0,
                    MaxRecommended = 2.0,
                    UnitOfMeasure = "mm/s",
                    InterpretationHintHigh = "Higher vibration than profile may indicate conveyor or case handling issues",
                },
            };
        }

        public Task<List<SkuRunProfileDto>> GetProfilesForSkuAsync(string sku, string equipmentId)
        {
            var keySku = (sku ?? string.Empty).Trim();
            var keyEq  = (equipmentId ?? string.Empty).Trim().ToUpperInvariant();

            var list = _profiles
                .Where(p => string.Equals(p.Sku, keySku, StringComparison.OrdinalIgnoreCase)
                         && string.Equals(p.EquipmentId, keyEq, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Task.FromResult(list);
        }

        public Task<SkuRunProfileDto?> GetProfileAsync(string sku, string equipmentId, string signalName)
        {
            var keySku  = (sku ?? string.Empty).Trim();
            var keyEq   = (equipmentId ?? string.Empty).Trim().ToUpperInvariant();
            var keySig  = (signalName ?? string.Empty).Trim();

            var profile = _profiles.FirstOrDefault(p =>
                string.Equals(p.Sku, keySku, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(p.EquipmentId, keyEq, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(p.SignalName, keySig, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(profile);
        }
    }
}
