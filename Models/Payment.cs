using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bachelor_s_Point.Models
{
    public enum PaymentType
    {
        Registration = 1,
        RoomPosting  = 2
    }

    public enum PaymentStatus
    {
        Pending  = 0,
        Verified = 1,
        Rejected = 2
    }

    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        public PaymentType Type { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Transaction ID is required")]
        [MaxLength(150)]
        public string TransactionId { get; set; } = string.Empty;

        // Only for RoomPosting payments
        public int? RoomId { get; set; }

        [ForeignKey("RoomId")]
        public Room? Room { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        public DateTime? VerifiedAt { get; set; }

        [MaxLength(500)]
        public string? AdminNote { get; set; }
    }
}
