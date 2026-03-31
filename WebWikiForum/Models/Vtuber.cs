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

        // Foreign Key
        public int? AgencyId { get; set; }
        [ForeignKey("AgencyId")]
        public virtual Agency Agency { get; set; }
    }
}
