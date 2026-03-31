using System;
using System.ComponentModel.DataAnnotations;

namespace WebWikiForum.ViewModels
{
    public class VtuberViewModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [Display(Name = "VTuber Name")]
        public string Name { get; set; }

        public int? Age { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Debut Date")]
        public DateTime? DebutDate { get; set; }

        [Display(Name = "Birthday (e.g., Dec 25)")]
        public string Birthday { get; set; }

        [DataType(DataType.MultilineText)]
        public string Lore { get; set; }

        [Display(Name = "Agency")]
        public int? AgencyId { get; set; }
    }
}
