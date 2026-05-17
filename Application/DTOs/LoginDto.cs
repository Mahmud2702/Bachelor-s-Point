using System.ComponentModel.DataAnnotations;

namespace Bachelor_s_Point.Application.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Valid email is required")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string? Password { get; set; }

        /// <summary>
        /// When true, the auth cookie lasts 30 days instead of the default 8 hours.
        /// </summary>
        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}
