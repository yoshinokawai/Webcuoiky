using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using WebWikiForum.Data;
using WebWikiForum.Models;
using WebWikiForum.Services;
using WebWikiForum.ViewModels;
using System.IO;
using System;
using System.Security.Claims;

namespace WebWikiForum.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;

        public AdminController(ApplicationDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }


        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var vtubers = await _context.Vtubers
                                        .Include(v => v.Agency)
                                        .OrderByDescending(v => v.Id)
                                        .ToListAsync();

            var agencies = await _context.Agencies
                                        .OrderByDescending(a => a.Id)
                                        .ToListAsync();

            var news = await _context.News
                                     .OrderByDescending(n => n.PublishDate)
                                     .ToListAsync();

            // Lấy tất cả tài khoản người dùng (trừ chính Admin đang đăng nhập ra cuối)
            var currentAdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var users = await _context.Users
                                      .OrderBy(u => u.Role)
                                      .ThenBy(u => u.Username)
                                      .ToListAsync();

            // Phân công Editor → Discussion (kèm navigation)
            var assignments = await _context.EditorAssignments
                                            .Include(a => a.Discussion)
                                            .Include(a => a.Editor)
                                            .ToListAsync();

            // Danh sách Discussion để populate dropdown giao bài
            var discussions = await _context.Discussions
                                            .OrderByDescending(d => d.CreatedAt)
                                            .ToListAsync();

            var viewModel = new AdminDashboardViewModel
            {
                Vtubers      = vtubers,
                Agencies     = agencies,
                News         = news,
                Users        = users,
                EditorAssignments = assignments,
                Discussions  = discussions
            };

            return View(viewModel);
        }

        // ── Quản lý Role Người dùng ────────────────────────────────────────

        // POST: Admin/AssignRole
        [HttpPost]
        public async Task<IActionResult> AssignRole(int userId, string role)
        {
            // Không cho Admin tự hạ role của chính mình
            var currentAdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == currentAdminId)
            {
                TempData["ErrorMessage"] = "Bạn không thể thay đổi role của chính mình.";
                return RedirectToAction(nameof(Dashboard));
            }

            var validRoles = new[] { "User", "Editor", "Admin" };
            if (!validRoles.Contains(role))
            {
                TempData["ErrorMessage"] = "Role không hợp lệ.";
                return RedirectToAction(nameof(Dashboard));
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var oldRole = user.Role;
            user.Role = role;

            // Nếu hạ từ Editor xuống User/Admin: xóa hết phân công của người đó
            if (oldRole == "Editor" && role != "Editor")
            {
                var assignments = _context.EditorAssignments.Where(a => a.EditorUserId == userId);
                _context.EditorAssignments.RemoveRange(assignments);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã đổi role của {user.Username} thành {role}.";
            return RedirectToAction(nameof(Dashboard));
        }

        // POST: Admin/AssignEditorDiscussion
        [HttpPost]
        public async Task<IActionResult> AssignEditorDiscussion(int editorUserId, int discussionId)
        {
            var currentAdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            // Kiểm tra user phải là Editor
            var editor = await _context.Users.FindAsync(editorUserId);
            if (editor == null || editor.Role != "Editor")
            {
                TempData["ErrorMessage"] = "Người dùng này không phải Editor.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Kiểm tra Discussion tồn tại
            var discussion = await _context.Discussions.FindAsync(discussionId);
            if (discussion == null) return NotFound();

            // Tránh trùng lặp
            var exists = await _context.EditorAssignments
                .AnyAsync(a => a.EditorUserId == editorUserId && a.DiscussionId == discussionId);
            if (exists)
            {
                TempData["ErrorMessage"] = "Bài viết này đã được giao cho Editor rồi.";
                return RedirectToAction(nameof(Dashboard));
            }

            _context.EditorAssignments.Add(new EditorAssignment
            {
                EditorUserId      = editorUserId,
                DiscussionId      = discussionId,
                AssignedAt        = DateTime.UtcNow,
                AssignedByAdminId = currentAdminId
            });
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã giao bài '{discussion.Title}' cho {editor.Username}.";
            return RedirectToAction(nameof(Dashboard));
        }

        // POST: Admin/RemoveEditorAssignment
        [HttpPost]
        public async Task<IActionResult> RemoveEditorAssignment(int assignmentId)
        {
            var assignment = await _context.EditorAssignments
                .Include(a => a.Editor)
                .Include(a => a.Discussion)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null) return NotFound();

            _context.EditorAssignments.Remove(assignment);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa phân công bài '{assignment.Discussion.Title}' khỏi {assignment.Editor.Username}.";
            return RedirectToAction(nameof(Dashboard));
        }

        // POST: Admin/Approve/5
        [HttpPost]
        public async Task<IActionResult> Approve(int id, string status = "Active")
        {
            var vtuber = await _context.Vtubers.FindAsync(id);
            if (vtuber == null)
            {
                return NotFound();
            }

            // Allowed statuses: Active, Graduated, Hiatus
            vtuber.Status = status;
            _context.Update(vtuber);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Dashboard));
        }

        // POST: Admin/ApproveAgency/5
        [HttpPost]
        public async Task<IActionResult> ApproveAgency(int id, string status = "Active")
        {
            var agency = await _context.Agencies.FindAsync(id);
            if (agency == null)
            {
                return NotFound();
            }

            // Allowed statuses: Active, Defunct
            agency.Status = status;
            _context.Update(agency);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Dashboard));
        }

        // POST: Admin/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var vtuber = await _context.Vtubers.FindAsync(id);
            if (vtuber == null)
            {
                return NotFound();
            }

            try
            {
                // Delete image file if exists
                if (!string.IsNullOrEmpty(vtuber.AvatarUrl))
                {
                    var fileName = Path.GetFileName(vtuber.AvatarUrl);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        _fileService.DeleteFile(fileName, "vtubers");
                    }
                }

                _context.Vtubers.Remove(vtuber);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"VTuber '{vtuber.Name}' deleted.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting: " + ex.Message;
            }

            return RedirectToAction(nameof(Dashboard));
        }

        // ── Chat Management ──────────────────────────────────────────────

        // GET: Admin/ChatSessions
        public async Task<IActionResult> ChatSessions()
        {
            // 1. Lấy danh sách tin nhắn của User đã đăng nhập (Group theo UserId)
            var authUsers = await _context.ChatMessages
                .Where(m => m.UserId != null)
                .GroupBy(m => m.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    LastActive = g.Max(m => m.CreatedAt),
                    TotalSessions = g.Select(m => m.SessionId).Distinct().Count(),
                    UnreadCount = g.Count(m => m.Role == "user" && !m.IsRead)
                })
                .ToListAsync();

            // Lấy thông tin thực từ bảng Users để đồng bộ Avatar/Tên
            var authUserIds = authUsers.Select(u => u.UserId).ToList();
            var userInfos = await _context.Users
                .Where(u => authUserIds.Contains(u.Id))
                .ToListAsync();

            var authSummaries = authUsers.Select(u => {
                var info = userInfos.FirstOrDefault(ui => ui.Id == u.UserId);
                return new UserChatSummary {
                    UserId = u.UserId,
                    Username = info?.Username ?? "Unknown",
                    AvatarUrl = info?.AvatarUrl,
                    LastActive = u.LastActive,
                    TotalSessions = u.TotalSessions,
                    UnreadCount = u.UnreadCount
                };
            }).ToList();

            // 2. Lấy danh sách Guest (UserId == null, Group theo SessionId)
            var guestUsers = await _context.ChatMessages
                .Where(m => m.UserId == null)
                .GroupBy(m => m.SessionId)
                .Select(g => new UserChatSummary
                {
                    UserId = null,
                    Username = g.Where(m => m.Role == "user").OrderByDescending(m => m.Id).Select(m => m.Username).FirstOrDefault() ?? "Guest",
                    AvatarUrl = null, // Guest không có avatar
                    LastActive = g.Max(m => m.CreatedAt),
                    TotalSessions = 1,
                    UnreadCount = g.Count(m => m.Role == "user" && !m.IsRead)
                })
                .ToListAsync();

            var allUsers = authSummaries.Concat(guestUsers)
                .OrderByDescending(u => u.LastActive)
                .ToList();

            return View(allUsers);
        }

        // GET: Admin/UserSessions?username=...&userId=...
        public async Task<IActionResult> UserSessions(string? username, int? userId)
        {
            var query = _context.ChatMessages.AsNoTracking();
            if (userId.HasValue) query = query.Where(m => m.UserId == userId.Value);
            else query = query.Where(m => m.Username == username && m.UserId == null);

            // Lấy Avatar của User này
            string? userAvatar = null;
            if (userId.HasValue) {
                userAvatar = await _context.Users.Where(u => u.Id == userId.Value).Select(u => u.AvatarUrl).FirstOrDefaultAsync();
            }

            var sessions = await query
                .GroupBy(m => m.SessionId)
                .Select(g => new ChatSessionSummary
                {
                    SessionId = g.Key,
                    Username = g.Where(m => m.Role == "user").OrderByDescending(m => m.Id).Select(m => m.Username).FirstOrDefault() ?? "Guest",
                    AvatarUrl = userAvatar,
                    LastMessage = g.OrderByDescending(m => m.Id).Select(m => m.Message).FirstOrDefault() ?? "",
                    LastMessageAt = g.Max(m => m.CreatedAt),
                    UnreadCount = g.Count(m => m.Role == "user" && !m.IsRead),
                    TotalMessages = g.Count()
                })
                .OrderByDescending(s => s.LastMessageAt)
                .ToListAsync();

            ViewBag.TargetUser = username ?? "Guest";
            return View(sessions);
        }

        // GET: Admin/ChatDetail?sessionId=...
        public async Task<IActionResult> ChatDetail(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return NotFound();

            // Tìm UserId gắn với session này để lấy avatar
            var userId = await _context.ChatMessages
                .Where(m => m.SessionId == sessionId && m.UserId != null)
                .OrderByDescending(m => m.Id)
                .Select(m => (int?)m.UserId)
                .FirstOrDefaultAsync();

            if (userId.HasValue)
            {
                ViewBag.UserAvatar = await _context.Users
                    .Where(u => u.Id == userId.Value)
                    .Select(u => u.AvatarUrl)
                    .FirstOrDefaultAsync();
            }

            var messages = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.Id)
                .ToListAsync();

            // Đánh dấu đã đọc tất cả tin nhắn user trong session này
            var unread = messages.Where(m => m.Role == "user" && !m.IsRead).ToList();
            if (unread.Any())
            {
                unread.ForEach(m => m.IsRead = true);
                await _context.SaveChangesAsync();
            }

            ViewBag.SessionId = sessionId;
            return View(messages);
        }

        // POST: Admin/AdminReply
        [HttpPost]
        public async Task<IActionResult> AdminReply([FromBody] AdminReplyRequest req)
        {
            if (string.IsNullOrEmpty(req.SessionId) || string.IsNullOrEmpty(req.Message))
                return BadRequest(new { error = "SessionId and Message are required." });

            var adminName = User.Identity?.Name ?? "Admin";

            // Tìm UserId gắn với session này để đồng bộ lịch sử
            var targetUserId = await _context.ChatMessages
                .Where(m => m.SessionId == req.SessionId && m.UserId != null)
                .OrderByDescending(m => m.Id)
                .Select(m => (int?)m.UserId)
                .FirstOrDefaultAsync();

            _context.ChatMessages.Add(new WebWikiForum.Models.ChatMessage
            {
                SessionId = req.SessionId,
                UserId    = targetUserId,
                Username  = $"👑 {adminName}",
                Role      = "admin",
                Message   = req.Message.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsRead    = true
            });
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // POST: Admin/DeleteChatSession
        [HttpPost]
        public async Task<IActionResult> DeleteChatSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return BadRequest();

            var messages = await _context.ChatMessages.Where(m => m.SessionId == sessionId).ToListAsync();
            if (messages.Any())
            {
                _context.ChatMessages.RemoveRange(messages);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        // POST: Admin/DeleteUserChatHistory
        [HttpPost]
        public async Task<IActionResult> DeleteUserChatHistory(int? userId, string? username)
        {
            IQueryable<WebWikiForum.Models.ChatMessage> query = _context.ChatMessages;

            if (userId.HasValue)
            {
                query = query.Where(m => m.UserId == userId.Value);
            }
            else if (!string.IsNullOrEmpty(username))
            {
                query = query.Where(m => m.Username == username && m.UserId == null);
            }
            else return BadRequest();

            var messages = await query.ToListAsync();
            if (messages.Any())
            {
                _context.ChatMessages.RemoveRange(messages);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }
    }
}

public class UserChatSummary
{
    public int? UserId { get; set; }
    public string Username { get; set; } = "Guest";
    public string? AvatarUrl { get; set; }
    public DateTime LastActive { get; set; }
    public int TotalSessions { get; set; }
    public int UnreadCount { get; set; }
}

public class ChatSessionSummary
{
    public string SessionId     { get; set; } = string.Empty;
    public string? Username     { get; set; }
    public string? AvatarUrl     { get; set; }
    public string? LastMessage  { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int UnreadCount      { get; set; }
    public int TotalMessages    { get; set; }
}

public class AdminReplyRequest
{
    public string? SessionId { get; set; }
    public string? Message   { get; set; }
}

