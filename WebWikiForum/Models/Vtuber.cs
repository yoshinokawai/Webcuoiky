using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebWikiForum.Models
{
    public class Vtuber
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public int? Age { get; set; }

        public DateTime? DebutDate { get; set; }

        [StringLength(50)]
        public string Birthday { get; set; }

        public string Lore { get; set; }

        public string AvatarUrl { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [StringLength(50)]
        public string Language { get; set; }

        [StringLength(50)]
        public string Region { get; set; }

        public string Tags { get; set; } // Comma-separated list

        public bool IsIndependent { get; set; } = true;
        
        public int ViewCount { get; set; } = 0;

        // Foreign Key
        public int? AgencyId { get; set; }
        [ForeignKey("AgencyId")]
        public virtual Agency? Agency { get; set; }
    }
}
