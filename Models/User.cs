using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bachelor_s_Point.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "User name is required")]
        [MaxLength(100)]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Valid email is required")]
        [MaxLength(150)]
        public string? Email { get; set; }

        // No [Required] here intentionally. The admin Edit flow lets the form
        // be submitted with a blank password to keep the existing hash.
        // The service layer enforces presence on Create/Register paths.
        // Non-nullable + default = string.Empty keeps the SQL column NOT NULL.
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public int RoleId { get; set; }

        [ForeignKey("RoleId")]
        public Role? Role { get; set; }

        public DateTime? LastLogin { get; set; }

        public ICollection<Room>? Rooms { get; set; }
    }
}
