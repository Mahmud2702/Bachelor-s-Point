using System.ComponentModel.DataAnnotations;

namespace Bachelor_s_Point.Application.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "User name is required")]
        [MaxLength(100)]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Valid email is required")]
        [MaxLength(150)]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string? Password { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public int RoleId { get; set; }
    }
}
