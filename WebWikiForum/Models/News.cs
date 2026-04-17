using System;
using System.ComponentModel.DataAnnotations;

namespace WebWikiForum.Models
{
    public class News
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty; // Event, Debut, Music, ASMR, Gaming

        public string? Content { get; set; }

        public string? ImageUrl { get; set; }

        public string Author { get; set; } = "Admin";

        public DateTime PublishDate { get; set; } = DateTime.Now;

        public bool IsFeatured { get; set; } = false;
    }
}
