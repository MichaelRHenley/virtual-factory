using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Virtual_Factory.Models
{
    [Table("TelemetryPointHistory")]
    public class TelemetryPointHistory
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(300)]
        public string Topic { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EquipmentName { get; set; }

        [MaxLength(100)]
        public string? SignalName { get; set; }

        [MaxLength(100)]
        public string? ValueText { get; set; }

        public double? ValueNumber { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }

        [MaxLength(50)]
        public string? Source { get; set; }

        public DateTime TimestampUtc { get; set; }

        public DateTime CreatedUtc { get; set; }
    }
}