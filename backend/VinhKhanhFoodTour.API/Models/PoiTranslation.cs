using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhFoodTour.Models
{
    public class PoiTranslation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PoiId { get; set; }

        [Required]
        [MaxLength(10)]
        public string LanguageCode { get; set; } = "vi"; // "vi", "en"

        [Required]
        [MaxLength(255)]

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; } // Nội dung văn bản cho Text-to-Speech

        public string? AudioFilePath { get; set; } // Link file âm thanh (.mp3)

        public string? ImageUrl { get; set; } // Link ảnh quán

        // Navigation Property
        [ForeignKey("PoiId")]
        public virtual Poi? Poi { get; set; }
    }
}