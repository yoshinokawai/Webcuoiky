using System;
using System.ComponentModel.DataAnnotations;

namespace WebWikiForum.ViewModels
{
    public class VtuberViewModel
    {
        [Required(ErrorMessage = "Wiki_Required_Name")]
        [Display(Name = "Wiki_Label_Name")]
        public string Name { get; set; }

        [Display(Name = "Wiki_Label_Age")]
        public int? Age { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Wiki_Label_Debut")]
        public DateTime? DebutDate { get; set; }

        [Display(Name = "Wiki_Label_Birthday")]
        public string Birthday { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Wiki_Label_Lore")]
        public string Lore { get; set; }

        [Display(Name = "Wiki_Label_Agency")]
        public int? AgencyId { get; set; }
    }
}
