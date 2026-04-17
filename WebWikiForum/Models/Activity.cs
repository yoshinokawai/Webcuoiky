using System;
using System.ComponentModel.DataAnnotations;

namespace WebWikiForum.Models
{
    public class Activity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string ActivityType { get; set; } = string.Empty; // "Article", "Media", "User", "Community"

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty; // "Created", "Updated", "Deleted", "Commented"

        [Required]
        [StringLength(100)]
        public string Author { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string? LinkUrl { get; set; }
        
        // Helper to get formatted detail like "+1,420 chars" or similar if we decide to store it
        public string? Detail { get; set; }
    }
}
