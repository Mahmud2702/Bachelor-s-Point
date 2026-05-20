using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bachelor_s_Point.Models
{
    /// <summary>
    /// One row per successful login. Lets admins see user login activity.
    /// </summary>
    public class LoginHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public DateTime LoginAt { get; set; } = DateTime.Now;

        /// <summary>Snapshot of the user's email at login time (handy for the admin list).</summary>
        [MaxLength(150)]
        public string? Email { get; set; }
    }
}
