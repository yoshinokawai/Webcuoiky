using System.Collections.Generic;
using WebWikiForum.Models;

namespace WebWikiForum.ViewModels
{
    public class AdminDashboardViewModel
    {
        public IEnumerable<Vtuber> Vtubers { get; set; } = new List<Vtuber>();
        public IEnumerable<Agency> Agencies { get; set; } = new List<Agency>();
        public IEnumerable<News> News { get; set; } = new List<News>();

        /// <summary>Danh sách toàn bộ tài khoản để Admin quản lý role</summary>
        public IEnumerable<User> Users { get; set; } = new List<User>();

        /// <summary>Phân công Editor → Discussion</summary>
        public IEnumerable<EditorAssignment> EditorAssignments { get; set; } = new List<EditorAssignment>();

        /// <summary>Danh sách Discussion để populate dropdown giao bài</summary>
        public IEnumerable<Discussion> Discussions { get; set; } = new List<Discussion>();
    }
}
