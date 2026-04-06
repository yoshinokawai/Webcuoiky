using System;
using System.Collections.Generic;

namespace WebWikiForum.ViewModels
{
    public class ForumHomeViewModel
    {
        public List<CategorySummary> Categories { get; set; } = new List<CategorySummary>();
    }

    public class CategorySummary
    {
        public string Name { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty; // For filtering (e.g. "General")
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int TopicCount { get; set; }
        public int ActiveMemberCount { get; set; }
        public string? LastPostTitle { get; set; }
        public string? LastPostAuthor { get; set; }
        public DateTime? LastPostDate { get; set; }
    }
}
