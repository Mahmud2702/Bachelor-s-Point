using System.ComponentModel.DataAnnotations;

namespace Bachelor_s_Point.Models
{
    /// <summary>
    /// Standalone admin credentials table — completely separate from the User table.
    /// Admin accounts are seeded or manually inserted; there is no public registration path.
    /// </summary>
    public class Admin
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Name { get; set; } = "Admin";
    }
}
