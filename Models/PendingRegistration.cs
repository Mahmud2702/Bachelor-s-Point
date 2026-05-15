using System.ComponentModel.DataAnnotations;

namespace Bachelor_s_Point.Models
{
    /// <summary>
    /// Temporary holding table for users who have started registration
    /// but haven't verified their email OTP yet. Once verified, the row
    /// is deleted and a real User is created.
    /// </summary>
    public class PendingRegistration
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(150)]
        public string? FullName { get; set; }

        [Required]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        [Required]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? PhoneNumber { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        /// <summary>Already-hashed password. Copied straight to User.PasswordHash on verification.</summary>
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>Role to assign once verified (Admin or User).</summary>
        public int TargetRoleId { get; set; }

        /// <summary>SHA-256 hash of (email + ":" + otp) — never store the plain OTP.</summary>
        [Required]
        public string OtpHash { get; set; } = string.Empty;

        public DateTime OtpExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int AttemptCount { get; set; } = 0;
    }
}
