using System;
using System.Collections.Generic;

namespace Virtual_Factory.Dtos
{
    public sealed class EquipmentEventDto
    {
        public string EquipmentId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }
}
