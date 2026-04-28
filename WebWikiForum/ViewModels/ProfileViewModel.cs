using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using WebWikiForum.Models;

namespace WebWikiForum.ViewModels
{
    public class ProfileViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Role { get; set; } = "User";

        public string? Bio { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL for the avatar image.")]
        [Display(Name = "Avatar URL")]
        public string? AvatarUrl { get; set; }

        public IFormFile? AvatarFile { get; set; }
        public string? CoverImageUrl { get; set; }
        public IFormFile? CoverImageFile { get; set; }

        public string? DiscordUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? YoutubeUrl { get; set; }
        public string? WebsiteUrl { get; set; }

        public List<Activity> RecentActivities { get; set; } = new();
    }
}
