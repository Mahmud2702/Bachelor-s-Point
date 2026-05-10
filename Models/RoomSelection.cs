using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bachelor_s_Point.Models
{
    /// <summary>
    /// Tracks a user (seeker) selecting a room — used for booking history.
    /// One record per click of "Confirm Selection".
    /// </summary>
    public class RoomSelection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RoomId { get; set; }

        [ForeignKey("RoomId")]
        public Room? Room { get; set; }

        [Required]
        public int SeekerUserId { get; set; }

        [ForeignKey("SeekerUserId")]
        public User? Seeker { get; set; }

        [MaxLength(2000)]
        public string? Message { get; set; }

        public DateTime SelectedAt { get; set; } = DateTime.Now;
    }
}
