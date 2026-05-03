using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebWikiForum.Models
{
    public class Discussion
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public string Author { get; set; } = "Anonymous";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public string Category { get; set; } = "General"; // General, Music, Gaming, Lore, FanArt

        public int ViewCount { get; set; } = 0;

        public int ReplyCount { get; set; } = 0;
        public int LikeCount { get; set; } = 0;

        public bool IsPinned { get; set; } = false;

        /// <summary>Khóa bài — không cho reply thêm</summary>
        public bool IsLocked { get; set; } = false;

        public string? LastReplier { get; set; }

        public DateTime? LastReplyDate { get; set; }

        // Navigation property for comments
        public ICollection<DiscussionReply> Replies { get; set; } = new List<DiscussionReply>();
    }

    public class DiscussionReply
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public string Author { get; set; } = "Anonymous";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int DiscussionId { get; set; }
        public Discussion Discussion { get; set; } = default!;
        public int LikeCount { get; set; } = 0;
    }
}
