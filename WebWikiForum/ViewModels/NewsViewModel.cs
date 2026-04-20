using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace WebWikiForum.ViewModels
{
    public class NewsViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type is required")]
        public string Type { get; set; } = string.Empty; // Event, Debut, Music, ASMR, Gaming

        public string? Content { get; set; }

        public string Author { get; set; } = "Admin";

        public bool IsFeatured { get; set; } = false;

        public string? CurrentImageUrl { get; set; }
        
        public string? SourceUrl { get; set; }
    }
}
