using System;
using System.ComponentModel.DataAnnotations;

namespace WebWikiForum.Models
{
    public class Donation
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [Required]
        [Range(10000, double.MaxValue, ErrorMessage = "Số tiền tối thiểu là 10,000 VND")]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string? Message { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Completed, Failed

        public string? TransactionId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property if we want to link it
        public virtual User? User { get; set; }
    }
}
