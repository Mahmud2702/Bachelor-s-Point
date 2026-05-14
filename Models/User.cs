using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bachelor_s_Point.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(150)]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "User name is required")]
        [MaxLength(100)]
        public string? UserName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Valid email is required")]
        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? PhoneNumber { get; set; }

        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public int RoleId { get; set; }

        [ForeignKey("RoleId")]
        public Role? Role { get; set; }

        public DateTime? LastLogin { get; set; }

        [MaxLength(500)]
        public string? ProfilePicturePath { get; set; }

        public ICollection<Room>? Rooms { get; set; }
    }
}
