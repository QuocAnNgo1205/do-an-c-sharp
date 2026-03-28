using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhFoodTour.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RoleId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Navigation Properties
        [ForeignKey("RoleId")]
        public virtual Role? Role { get; set; }

        // 1 Owner có thể quản lý nhiều quán ốc
        public virtual ICollection<Poi> OwnedPois { get; set; } = new List<Poi>();
    }
}