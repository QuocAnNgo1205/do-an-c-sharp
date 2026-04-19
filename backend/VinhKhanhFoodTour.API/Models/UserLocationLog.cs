using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhFoodTour.Models
{
    /// <summary>
    /// Lưu trữ vị trí thực tế của người dùng Tourist tại một thời điểm nhất định.
    /// Dùng để vẽ Heatmap (bản đồ nhiệt) về mật độ khách du lịch.
    /// </summary>
    public class UserLocationLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string DeviceId { get; set; } = string.Empty;

        public double Latitude { get; set; }
        
        public double Longitude { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// (Tùy chọn) TourId nếu người dùng đang trong một tour cụ thể
        /// </summary>
        public int? CurrentTourId { get; set; }
    }
}
