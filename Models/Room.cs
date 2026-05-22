using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bachelor_s_Point.Models
{
    public class Room
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(150)]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [MaxLength(2000)]
        public string? Description { get; set; }

        // Base room rent per month
        [Required(ErrorMessage = "Room cost is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Room cost must be a positive number")]
        public int Price { get; set; }

        // Estimated additional monthly costs — nullable so existing rooms aren't broken
        [Range(0, int.MaxValue, ErrorMessage = "Wifi cost must be a positive number")]
        public int? WifiCost { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Meal cost must be a positive number")]
        public int? MealCostPerMonth { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Maid cost must be a positive number")]
        public int? MaidCostPerMonth { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [MaxLength(200)]
        public string? Location { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? Owner { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsAvailable { get; set; } = true;

        public bool IsApproved { get; set; } = false;

        public DateTime? ApprovedAt { get; set; }

        public ICollection<RoomImage>? Images { get; set; }
    }
}
