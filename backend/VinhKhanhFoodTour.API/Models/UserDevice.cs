using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhFoodTour.Models
{
    public class UserDevice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string DeviceId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? DeviceName { get; set; }

        [MaxLength(50)]
        public string? Os { get; set; }

        public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;

        public bool IsRevoked { get; set; } = false;

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
