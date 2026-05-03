using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebWikiForum.Models
{
    /// <summary>
    /// Lưu phân công: Editor nào được quản lý bài viết forum nào.
    /// Admin tạo bản ghi này từ Dashboard để cấp quyền cho Editor.
    /// </summary>
    public class EditorAssignment
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Id của User có role Editor được giao việc</summary>
        public int EditorUserId { get; set; }

        /// <summary>Id của bài viết Discussion được giao</summary>
        public int DiscussionId { get; set; }

        /// <summary>Thời điểm Admin giao bài</summary>
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Id của Admin đã thực hiện phân công</summary>
        public int AssignedByAdminId { get; set; }

        // ── Navigation properties ──
        [ForeignKey(nameof(EditorUserId))]
        public virtual User Editor { get; set; } = default!;

        [ForeignKey(nameof(DiscussionId))]
        public virtual Discussion Discussion { get; set; } = default!;
    }
}
