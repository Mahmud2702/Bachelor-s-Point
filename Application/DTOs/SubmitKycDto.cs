using System.ComponentModel.DataAnnotations;

namespace Bachelor_s_Point.Application.DTOs
{
    public class SubmitKycDto
    {
        [Required(ErrorMessage = "Full name (as on NID) is required")]
        [MaxLength(150)]
        [Display(Name = "Full Name (as on NID)")]
        public string? FullNameOnNid { get; set; }

        [Required(ErrorMessage = "NID number is required")]
        [MaxLength(50)]
        [Display(Name = "NID Number")]
        public string? NidNumber { get; set; }
    }
}
