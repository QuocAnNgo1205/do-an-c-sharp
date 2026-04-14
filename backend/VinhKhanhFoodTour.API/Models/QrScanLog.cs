using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhFoodTour.Models
{
    public class QrScanLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PoiId { get; set; }

        [Required]
        [MaxLength(255)]
        public string DeviceId { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [ForeignKey("PoiId")]
        public virtual Poi? Poi { get; set; }
    }
}
