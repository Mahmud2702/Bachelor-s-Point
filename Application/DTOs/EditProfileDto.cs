using System.ComponentModel.DataAnnotations;

namespace Bachelor_s_Point.Application.DTOs
{
    public class EditProfileDto
    {
        [Required(ErrorMessage = "Full name is required")]
        [MaxLength(150)]
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "User name is required")]
        [MaxLength(100)]
        [Display(Name = "User Name")]
        public string? UserName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [MaxLength(30)]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }
    }
}