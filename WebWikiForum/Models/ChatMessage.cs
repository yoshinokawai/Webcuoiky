using System;
using System.ComponentModel.DataAnnotations;

namespace WebWikiForum.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        [Required, MaxLength(64)]
        public string SessionId { get; set; } = string.Empty;  // GUID từ localStorage của user

        public int? UserId { get; set; }        // null nếu là khách

        [MaxLength(100)]
        public string? Username { get; set; }   // tên hiển thị

        /// <summary>"user" | "bot" | "admin"</summary>
        [Required, MaxLength(10)]
        public string Role { get; set; } = "user";

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Admin đã đọc tin nhắn user này chưa</summary>
        public bool IsRead { get; set; } = false;
    }
}
