using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public sealed class SeededEquipmentEventAdapter : IEquipmentEventAdapter
    {
        private readonly List<EquipmentEventDto> _events;

        public SeededEquipmentEventAdapter()
        {
            var now = DateTime.UtcNow;

            _events = new List<EquipmentEventDto>
            {
                // deburring-station-01
                new EquipmentEventDto
                {
                    EquipmentId = "DEBURRING-STATION-01",
                    EventType = "Alarm",
                    Description = "Spindle overload alarm",
                    TimestampUtc = now.AddMinutes(-38),
                    Severity = "High",
                    Source = "PLC",
                },
                new EquipmentEventDto
                {
                    EquipmentId = "DEBURRING-STATION-01",
                    EventType = "Stop",
                    Description = "Operator-initiated stop for tool change",
                    TimestampUtc = now.AddHours(-2.4),
                    Severity = "Info",
                    Source = "Operator",
                },
                new EquipmentEventDto
                {
                    EquipmentId = "DEBURRING-STATION-01",
                    EventType = "Transition",
                    Description = "State changed to normal",
                    TimestampUtc = now.AddHours(-2.3),
                    Severity = "Info",
                    Source = "PLC",
                },
                new EquipmentEventDto
                {
                    EquipmentId = "DEBURRING-STATION-01",
                    EventType = "Transition",
                    Description = "State changed to running",
                    TimestampUtc = now.AddHours(-2.2),
                    Severity = "Info",
                    Source = "PLC",
                },

                // inspection-station-01
                new EquipmentEventDto
                {
                    EquipmentId = "INSPECTION-STATION-01",
                    EventType = "Alarm",
                    Description = "Camera exposure fault",
                    TimestampUtc = now.AddMinutes(-15),
                    Severity = "Medium",
                    Source = "VisionSystem",
                },
                new EquipmentEventDto
                {
                    EquipmentId = "INSPECTION-STATION-01",
                    EventType = "Stop",
                    Description = "Auto-stop due to missing part",
                    TimestampUtc = now.AddHours(-1.1),
                    Severity = "Low",
                    Source = "PLC",
                },
                new EquipmentEventDto
                {
                    EquipmentId = "INSPECTION-STATION-01",
                    EventType = "Transition",
                    Description = "State changed to normal",
                    TimestampUtc = now.AddHours(-1.0),
                    Severity = "Info",
                    Source = "PLC",
                },
                new EquipmentEventDto
                {
                    EquipmentId = "INSPECTION-STATION-01",
                    EventType = "Transition",
                    Description = "State changed to running",
                    TimestampUtc = now.AddMinutes(-30),
                    Severity = "Info",
                    Source = "PLC",
                },

                // case-packer-01
                new EquipmentEventDto
                {
                    EquipmentId = "CASE-PACKER-01",
                    EventType = "Alarm",
                    Description = "Case jam at discharge",
                    TimestampUtc = now.AddHours(-3),
                    Severity = "High",
                    Source = "PLC",
                },
                new EquipmentEventDto
                {
                    EquipmentId = "CASE-PACKER-01",
                    EventType = "Stop",
                    Description = "Emergency stop pressed",
                    TimestampUtc = now.AddHours(-5),
                    Severity = "High",
                    Source = "Operator",
                },
                new EquipmentEventDto
                {
                    EquipmentId = "CASE-PACKER-01",
                    EventType = "Transition",
                    Description = "State changed to normal",
                    TimestampUtc = now.AddHours(-2.9),
                    Severity = "Info",
                    Source = "PLC",
                },
                new EquipmentEventDto
                {
                    EquipmentId = "CASE-PACKER-01",
                    EventType = "Transition",
                    Description = "State changed to running",
                    TimestampUtc = now.AddHours(-2.5),
                    Severity = "Info",
                    Source = "PLC",
                },
            };
        }

        private static string Normalize(string equipmentId) =>
            (equipmentId ?? string.Empty).Trim().ToUpperInvariant();

        public Task<List<EquipmentEventDto>> GetRecentEventsAsync(string equipmentId, int hours)
        {
            var key = Normalize(equipmentId);
            if (hours <= 0) hours = 1;

            var since = DateTime.UtcNow.AddHours(-hours);
            var list = _events
                .Where(e => Normalize(e.EquipmentId) == key && e.TimestampUtc >= since)
                .OrderByDescending(e => e.TimestampUtc)
                .ToList();

            return Task.FromResult(list);
        }

        public Task<EquipmentEventDto?> GetLastAlarmAsync(string equipmentId)
        {
            var key = Normalize(equipmentId);
            var evt = _events
                .Where(e => Normalize(e.EquipmentId) == key &&
                            string.Equals(e.EventType, "Alarm", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.TimestampUtc)
                .FirstOrDefault();

            return Task.FromResult(evt);
        }

        public Task<EquipmentEventDto?> GetLastStopAsync(string equipmentId)
        {
            var key = Normalize(equipmentId);
            var evt = _events
                .Where(e => Normalize(e.EquipmentId) == key &&
                            string.Equals(e.EventType, "Stop", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.TimestampUtc)
                .FirstOrDefault();

            return Task.FromResult(evt);
        }
    }
}
