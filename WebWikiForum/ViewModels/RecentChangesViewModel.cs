using System;
using System.Collections.Generic;
using WebWikiForum.Models;

namespace WebWikiForum.ViewModels
{
    public class RecentChangesViewModel
    {
        public List<Activity> RecentActivities { get; set; } = new List<Activity>();
        
        // Stats
        public int TotalArticles { get; set; }
        public int TotalEditors { get; set; }
        public int TotalMedia { get; set; }
        public int Last24hEdits { get; set; }
        
        // Contributors
        public List<ContributorViewModel> TopContributors { get; set; } = new List<ContributorViewModel>();
    }

    public class ContributorViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int EditCount { get; set; }
        public string? AvatarUrl { get; set; }
        public int Rank { get; set; }
    }
}
