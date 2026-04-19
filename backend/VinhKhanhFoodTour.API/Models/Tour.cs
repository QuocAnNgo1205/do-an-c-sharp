using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhFoodTour.Models
{
    public class Tour
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EstimatedPrice { get; set; }

        [MaxLength(500)]
        public string? ThumbnailUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<TourPoi> TourPois { get; set; } = new List<TourPoi>();
        public virtual ICollection<TourUsageLog> UsageLogs { get; set; } = new List<TourUsageLog>();
    }
}
