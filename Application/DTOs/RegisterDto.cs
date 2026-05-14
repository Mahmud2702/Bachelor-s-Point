using System.ComponentModel.DataAnnotations;

namespace Bachelor_s_Point.Application.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(150)]
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "User name is required")]
        [MaxLength(100)]
        [Display(Name = "User Name")]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [MaxLength(150)]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [MaxLength(30)]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string? ConfirmPassword { get; set; }

        // Not shown on form — set by controller based on ?as= parameter
        public int RoleId { get; set; }
    }
}
