using System.ComponentModel.DataAnnotations;

namespace VinhKhanhFoodTour.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; } = string.Empty; // "Admin", "Owner", "Tourist"

        // Navigation Property: 1 Role có nhiều Users
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}