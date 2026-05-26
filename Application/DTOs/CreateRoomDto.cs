using System.ComponentModel.DataAnnotations;

namespace Bachelor_s_Point.Application.DTOs
{
    public class CreateRoomDto
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(150)]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Room cost is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Room cost must be a positive number")]
        [Display(Name = "Room Cost")]
        public int Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Wifi cost must be a positive number")]
        [Display(Name = "Wifi Cost")]
        public int? WifiCost { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Meal cost must be a positive number")]
        [Display(Name = "Meal Cost (per month)")]
        public int? MealCostPerMonth { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Maid cost must be a positive number")]
        [Display(Name = "Maid Cost (per month)")]
        public int? MaidCostPerMonth { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [MaxLength(200)]
        public string? Location { get; set; }

        [Display(Name = "Division")]
        [MaxLength(60)]
        public string? Division { get; set; }

        [Display(Name = "District")]
        [MaxLength(60)]
        public string? District { get; set; }
    }
}