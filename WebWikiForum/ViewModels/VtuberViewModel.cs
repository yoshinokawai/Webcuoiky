using System;
using System.ComponentModel.DataAnnotations;

namespace WebWikiForum.ViewModels
{
    public class VtuberViewModel
    {
        [Required(ErrorMessage = "VTuber Name is required")]
        [Display(Name = "Wiki_Label_Name")]
        public string Name { get; set; }

        [Display(Name = "Wiki_Label_Age")]
        public int? Age { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Wiki_Label_Debut")]
        public DateTime? DebutDate { get; set; }

        [Display(Name = "Wiki_Label_Birthday")]
        public string Birthday { get; set; }

        [Required(ErrorMessage = "Region is required")]
        [Display(Name = "Region")]
        public string Region { get; set; }

        [Required(ErrorMessage = "Language is required")]
        [Display(Name = "Language")]
        public string Language { get; set; }

        [Required(ErrorMessage = "At least one tag is required (e.g., Gaming)")]
        [Display(Name = "Tags")]
        public string Tags { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Wiki_Label_Lore")]
        public string Lore { get; set; }

        [Display(Name = "Wiki_Label_Agency")]
        public int? AgencyId { get; set; }
    }
}
