using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace VinhKhanhFoodTour.Models
{
    public enum PoiStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class Poi
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OwnerId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public Point Location { get; set; } = null!;

        public double TriggerRadius { get; set; } = 20.0;

        public PoiStatus Status { get; set; } = PoiStatus.Pending;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? RejectionReason { get; set; }

        // Navigation Properties
        [ForeignKey("OwnerId")]
        public virtual User? Owner { get; set; }

        // 1 Quán có nhiều bản dịch (Vi, En,...)
        public virtual ICollection<PoiTranslation> Translations { get; set; } = new List<PoiTranslation>();

        // 1 Quán có nhiều lượt nghe lịch sử
        public virtual ICollection<NarrationLog> NarrationLogs { get; set; } = new List<NarrationLog>();
    }
}