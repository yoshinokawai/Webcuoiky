using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebWikiForum.Models
{
    public class Agency
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string LogoUrl { get; set; }

        [StringLength(50)]
        public string Region { get; set; }

        [StringLength(50)]
        public string Focus { get; set; }

        public string Description { get; set; }

        public int TalentCount { get; set; }
        
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public string? WebsiteUrl { get; set; }
        public string? YoutubeUrl { get; set; }
        public string? TwitterUrl { get; set; }

        public virtual ICollection<Vtuber> Vtubers { get; set; }
    }
}
