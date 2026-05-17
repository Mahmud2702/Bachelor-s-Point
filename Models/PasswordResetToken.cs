using System.ComponentModel.DataAnnotations;

namespace Bachelor_s_Point.Models
{
    /// <summary>
    /// Temporary OTP record for password reset. Stored separately from PendingRegistration
    /// because registration creates new users while this one resets existing users.
    /// </summary>
    public class PasswordResetToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        /// <summary>SHA-256 hash of (email + ":" + otp). Never store plain text OTP.</summary>
        [Required]
        public string OtpHash { get; set; } = string.Empty;

        public DateTime OtpExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int AttemptCount { get; set; } = 0;
    }
}
