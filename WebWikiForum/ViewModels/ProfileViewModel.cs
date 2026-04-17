using System.ComponentModel.DataAnnotations;

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
    }
}
