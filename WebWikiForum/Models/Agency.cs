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

        public virtual ICollection<Vtuber> Vtubers { get; set; }
    }
}
