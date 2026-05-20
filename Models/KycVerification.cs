using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bachelor_s_Point.Models
{
    /// <summary>
    /// KYC (Know Your Customer) identity verification record.
    /// One record per user. A user must be Verified before posting or booking rooms.
    /// </summary>
    public class KycVerification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        [MaxLength(150)]
        public string FullNameOnNid { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string NidNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string NidFrontImagePath { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? NidBackImagePath { get; set; }

        /// <summary>Selfie / live photo of the user holding or near the NID.</summary>
        [Required]
        [MaxLength(500)]
        public string UserPhotoPath { get; set; } = string.Empty;

        /// <summary>"Pending", "Verified", or "Rejected".</summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        public DateTime? ReviewedAt { get; set; }

        /// <summary>Admin user id who reviewed this submission.</summary>
        public int? ReviewedByAdminId { get; set; }

        [MaxLength(300)]
        public string? RejectionReason { get; set; }
    }
}
