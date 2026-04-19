using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhFoodTour.Models
{
    public class NarrationLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PoiId { get; set; }

        [Required]
        [MaxLength(255)]
        public string DeviceId { get; set; } = string.Empty; // Mã định danh điện thoại (tránh việc bắt user Tourist phải tạo tài khoản)

        public int ListenDurationSeconds { get; set; } = 0; // Thời lượng nghe thực tế (giây)

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation Property
        [ForeignKey("PoiId")]
        public virtual Poi? Poi { get; set; }
    }
}