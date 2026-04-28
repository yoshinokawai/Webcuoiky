using System;
using System.ComponentModel.DataAnnotations;

namespace WebWikiForum.ViewModels
{
    public class VtuberViewModel
    {
        [Required(ErrorMessage = "VTuber Name is required")]
        [Display(Name = "Wiki_Label_Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Wiki_Label_Age")]
        public int? Age { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Wiki_Label_Debut")]
        public DateTime? DebutDate { get; set; }

        [Display(Name = "Wiki_Label_Birthday")]
        public string Birthday { get; set; } = string.Empty;

        [Required(ErrorMessage = "Region is required")]
        [Display(Name = "Region")]
        public string Region { get; set; } = string.Empty;

        [Required(ErrorMessage = "Language is required")]
        [Display(Name = "Language")]
        public string Language { get; set; } = string.Empty;

        [Required(ErrorMessage = "At least one tag is required (e.g., Gaming)")]
        [Display(Name = "Tags")]
        public string Tags { get; set; } = string.Empty;

        [DataType(DataType.MultilineText)]
        [Display(Name = "Wiki_Label_Lore")]
        public string Lore { get; set; } = string.Empty;

        [Display(Name = "Wiki_Label_Agency")]
        public int? AgencyId { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [Display(Name = "YouTube Channel URL")]
        public string? YoutubeUrl { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [Display(Name = "Wiki_Label_IntroVideo")]
        public string? IntroVideoUrl { get; set; }
    }
}
