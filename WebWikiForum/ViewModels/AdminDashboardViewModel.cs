using System.Collections.Generic;
using WebWikiForum.Models;

namespace WebWikiForum.ViewModels
{
    public class AdminDashboardViewModel
    {
        public IEnumerable<Vtuber> Vtubers { get; set; }
        public IEnumerable<Agency> Agencies { get; set; }
    }
}
