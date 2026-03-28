using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Virtual_Factory.Models
{
    [Table("EquipmentStateEvents")]
    public class EquipmentStateEvent
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string EquipmentName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? PreviousState { get; set; }

        [MaxLength(50)]
        public string? NewState { get; set; }

        public DateTime TimestampUtc { get; set; }

        [MaxLength(50)]
        public string? Source { get; set; }
    }
}