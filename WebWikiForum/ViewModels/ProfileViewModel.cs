using System.ComponentModel.DataAnnotations;

namespace WebWikiForum.ViewModels
{
    public class ProfileViewModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Role { get; set; }

        public string? Bio { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL for the avatar image.")]
        [Display(Name = "Avatar URL")]
        public string? AvatarUrl { get; set; }

        public IFormFile? AvatarFile { get; set; }
    }
}
