using System.ComponentModel.DataAnnotations;

namespace Bachelor_s_Point.Application.DTOs
{
    public class VerifyOtpDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "OTP is required")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be exactly 6 digits")]
        [Display(Name = "Verification Code")]
        public string? Otp { get; set; }
    }
}
