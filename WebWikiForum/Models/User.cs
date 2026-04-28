using System.ComponentModel.DataAnnotations;

namespace WebWikiForum.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Role: "Admin" or "User"</summary>
        public string Role { get; set; } = "User";

        [StringLength(6)]
        public string? SecurityPin { get; set; }

        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public string? CoverImageUrl { get; set; }

        public string? DiscordUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? YoutubeUrl { get; set; }
        public string? WebsiteUrl { get; set; }
    }
}
