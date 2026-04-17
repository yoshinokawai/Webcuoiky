using System;

namespace WebWikiForum.Models
{
    public class DiscussionLike
    {
        public int Id { get; set; }
        
        // The user who liked
        public string Username { get; set; } = string.Empty;
        
        // Link to either a Discussion or a Reply
        public int? DiscussionId { get; set; }
        public Discussion? Discussion { get; set; }
        
        public int? ReplyId { get; set; }
        public DiscussionReply? Reply { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
