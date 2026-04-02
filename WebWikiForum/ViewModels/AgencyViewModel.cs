using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WebWikiForum.ViewModels
{
    public class AgencyViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [Display(Name = "Agency Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Region is required")]
        [Display(Name = "Region")]
        public string Region { get; set; }

        [Required(ErrorMessage = "Focus is required")]
        [Display(Name = "Core Focus")]
        public string Focus { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Number of Talents")]
        public int TalentCount { get; set; } = 0;
    }
}
