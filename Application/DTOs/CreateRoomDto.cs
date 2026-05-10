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

        [Required(ErrorMessage = "Price is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Price must be a positive number")]
        public int Price { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [MaxLength(200)]
        public string? Location { get; set; }
    }
}
