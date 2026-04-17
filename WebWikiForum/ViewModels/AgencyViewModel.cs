using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WebWikiForum.ViewModels
{
    public class AgencyViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [Display(Name = "Agency Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Region is required")]
        [Display(Name = "Region")]
        public string Region { get; set; } = string.Empty;

        [Required(ErrorMessage = "Focus is required")]
        [Display(Name = "Core Focus")]
        public string Focus { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Number of Talents")]
        public int TalentCount { get; set; } = 0;

        [Url]
        [Display(Name = "Official Website")]
        public string? WebsiteUrl { get; set; }

        [Url]
        [Display(Name = "YouTube Channel")]
        public string? YoutubeUrl { get; set; }

        [Url]
        [Display(Name = "Twitter / X")]
        public string? TwitterUrl { get; set; }
    }
}
