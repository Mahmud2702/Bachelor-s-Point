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

        [Required(ErrorMessage = "Price is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Price must be a positive number")]
        public int Price { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [MaxLength(200)]
        public string? Location { get; set; }

        // FK to the User who posted (owner) of this room
        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? Owner { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsAvailable { get; set; } = true;
    }
}
