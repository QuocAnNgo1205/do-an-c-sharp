using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhFoodTour.Models
{
    /// <summary>Tracks each time a device starts/opens a Tour.</summary>
    public class TourUsageLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TourId { get; set; }

        /// <summary>Anonymous device identifier from mobile/web client.</summary>
        [MaxLength(255)]
        public string? DeviceId { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [ForeignKey("TourId")]
        public virtual Tour? Tour { get; set; }
    }
}
