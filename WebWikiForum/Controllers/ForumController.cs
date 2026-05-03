using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using WebWikiForum.Data;
using WebWikiForum.Models;
using WebWikiForum.ViewModels;
using System.Threading.Tasks;
using System.Linq;
using System;
using WebWikiForum.Services;

namespace WebWikiForum.Controllers
{
    public class ForumController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IActivityService _activityService;
        private readonly IConfiguration _configuration;

        public ForumController(ApplicationDbContext context, IActivityService activityService, IConfiguration configuration)
        {
            _context = context;
            _activityService = activityService;
            _configuration = configuration;
        }

        private async Task<Dictionary<string, string?>> _GetAuthorAvatars(IEnumerable<string> usernames)
        {
            var distinctUsernames = usernames.Where(u => !string.IsNullOrEmpty(u)).Distinct().ToList();
            return await _context.Users
                .AsNoTracking()
                .Where(u => distinctUsernames.Contains(u.Username))
                .ToDictionaryAsync(u => u.Username, u => u.AvatarUrl);
        }

        // Trang chủ cộng đồng / Tất cả thảo luận
        public async Task<IActionResult> CommunityHub()
        {
            var trending = await _context.Discussions
                .OrderByDescending(d => d.ViewCount + (d.ReplyCount * 5))
                .Take(3)
                .ToListAsync();

            var recent = await _context.Discussions
                .OrderByDescending(d => d.CreatedAt)
                .Take(4)
                .ToListAsync();

            // Giả lập/Lấy dữ liệu sự kiện nếu có bảng riêng.
            // Vì đã thêm bảng News/Events, tôi lấy tin tức gần đây luôn.
            var news = await _context.News.OrderByDescending(n => n.PublishDate).Take(3).ToListAsync();

            // Đồng bộ avatar tác giả
            var authors = recent.Select(d => d.Author)
                .Concat(trending.Select(d => d.Author))
                .ToList();
            var avatars = await _GetAuthorAvatars(authors);
            ViewBag.AuthorAvatars = avatars;

            ViewBag.Trending = trending;
            ViewBag.News = news;
            ViewBag.DiscordInviteLink = _configuration["Discord:InviteLink"] ?? "https://discord.gg/";
            
            return View(recent);
        }

        public async Task<IActionResult> Community(string category = "All", string sort = "Default")
        {
            ViewBag.CurrentCategory = category;
            ViewBag.CurrentSort = sort;

            // Thống kê cho các thẻ danh mục (chỉ khi xem Tất cả và KHÔNG có bộ lọc)
            if (category == "All" && sort == "Default")
            {
                var stats = new List<CategorySummary>();
                var defs = new[] {
                    new { Id = "General", Name = "General Discussion", Desc = "Anything and everything related to the VTubing scene.", Icon = "forum" },
                    new { Id = "Agencies", Name = "Agencies", Desc = "Hololive, Nijisanji, VShojo, and other corporate updates.", Icon = "corporate_fare" },
                    new { Id = "Lore", Name = "Lore & Theories", Desc = "Deep dives into character backstories and world building.", Icon = "auto_stories" },
                    new { Id = "Technical", Name = "Technical Help", Desc = "VTube Studio, OBS settings, and rigging assistance.", Icon = "build" }
                };

                foreach(var d in defs) {
                    var items = await _context.Discussions.Where(dis => dis.Category == d.Id).OrderByDescending(dis => dis.CreatedAt).ToListAsync();
                    var last = items.FirstOrDefault();
                    stats.Add(new CategorySummary {
                        Identifier = d.Id,
                        Name = d.Name,
                        Description = d.Desc,
                        Icon = d.Icon,
                        TopicCount = items.Count,
                        ActiveMemberCount = items.Select(i => i.Author).Distinct().Count(),
                        LastPostTitle = last?.Title,
                        LastPostAuthor = last?.Author,
                        LastPostDate = last?.CreatedAt
                    });
                }
                ViewBag.CategoryStats = stats;
            }

            var query = _context.Discussions.AsQueryable();

            if (category != "All")
            {
                query = query.Where(d => d.Category == category);
            }

            if (sort == "Recent")
            {
                query = query.OrderByDescending(d => d.CreatedAt);
            }
            else if (sort == "Trending")
            {
                query = query.OrderByDescending(d => d.ViewCount + (d.ReplyCount * 5));
            }
            else
            {
                query = query.OrderByDescending(d => d.IsPinned).ThenByDescending(d => d.LastReplyDate ?? d.CreatedAt);
            }

            // Danh sách trending toàn cục cho sidebar
            ViewBag.Trending = await _context.Discussions
                .OrderByDescending(d => d.ViewCount + (d.ReplyCount * 5))
                .Take(5)
                .ToListAsync();

            var results = await query.ToListAsync();

            // Đồng bộ avatar tác giả
            var authors = results.Select(d => d.Author).ToList();
            if (ViewBag.CategoryStats != null)
            {
                var stats = (IEnumerable<CategorySummary>)ViewBag.CategoryStats;
                authors.AddRange(stats.Where(s => !string.IsNullOrEmpty(s.LastPostAuthor)).Select(s => s.LastPostAuthor ?? "Anonymous"));
            }
            if (ViewBag.Trending != null)
            {
                authors.AddRange(((List<Discussion>)ViewBag.Trending).Select(d => d.Author));
            }
            ViewBag.AuthorAvatars = await _GetAuthorAvatars(authors);

            return View(results);
        }

        // Chi tiết luồng thảo luận
        public async Task<IActionResult> Topic(int id)
        {
            var discussion = await _context.Discussions
                .Include(d => d.Replies)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (discussion == null) return NotFound();

            // Lấy danh sách ID đã thích của người dùng hiện tại
            var likedDiscussionIds = new List<int>();
            var likedReplyIds = new List<int>();

            if (User.Identity?.IsAuthenticated == true)
            {
                var username = User.Identity.Name ?? "Anonymous";
                likedDiscussionIds = await _context.DiscussionLikes
                    .Where(l => l.Username == username && l.DiscussionId != null)
                    .Select(l => l.DiscussionId ?? 0)
                    .ToListAsync();
                
                likedReplyIds = await _context.DiscussionLikes
                    .Where(l => l.Username == username && l.ReplyId != null)
                    .Select(l => l.ReplyId ?? 0)
                    .ToListAsync();
            }

            ViewBag.LikedDiscussionIds = likedDiscussionIds;
            ViewBag.LikedReplyIds = likedReplyIds;

            // Đồng bộ avatar tác giả
            var authors = new List<string> { discussion.Author };
            authors.AddRange(discussion.Replies.Select(r => r.Author));
            ViewBag.AuthorAvatars = await _GetAuthorAvatars(authors);

            // Kiểm tra quyền Editor
            bool isAssignedEditor = false;
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Editor"))
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                isAssignedEditor = await _context.EditorAssignments.AnyAsync(a => a.EditorUserId == userId && a.DiscussionId == id);
            }
            ViewBag.IsAssignedEditor = isAssignedEditor;

            return View(discussion);
        }

        // Tạo chủ đề mới
        [HttpGet]
        public IActionResult StartTopic()
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> StartTopic(Discussion model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                model.Author = User.Identity?.Name ?? "Anonymous";
                model.LastReplyDate = DateTime.Now;
                
                _context.Discussions.Add(model);
                await _context.SaveChangesAsync();

                await _activityService.LogActivityAsync(model.Title, model.Content, "Community", "Created", model.Author, $"/Forum/Topic/{model.Id}", "New Discussion");
                
                return RedirectToAction("Community");
            }
            return View(model);
        }

        // Đăng trả lời
        [HttpPost]
        public async Task<IActionResult> PostReply(int discussionId, string content)
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login", "Account");
            if (string.IsNullOrEmpty(content)) return BadRequest();

            var discussion = await _context.Discussions.FindAsync(discussionId);
            if (discussion == null) return NotFound();

            if (discussion.IsLocked)
            {
                // Nếu bài bị khóa, chỉ Admin hoặc Editor được giao mới được comment (hoặc cấm luôn cũng được, nhưng thường là cấm hết)
                // Theo yêu cầu thông thường, bị khóa là không ai comment được.
                return Forbid();
            }

            var reply = new DiscussionReply
            {
                DiscussionId = discussionId,
                Content = content,
                Author = User.Identity?.Name ?? "Anonymous",
                CreatedAt = DateTime.Now
            };

            discussion.ReplyCount++;
            discussion.LastReplier = User.Identity?.Name;
            discussion.LastReplyDate = DateTime.Now;

            _context.DiscussionReplies.Add(reply);
            _context.Update(discussion);
            await _context.SaveChangesAsync();

            await _activityService.LogActivityAsync($"Reply to: {discussion.Title}", content, "Community", "Commented", User.Identity?.Name ?? "Anonymous", $"/Forum/Topic/{discussionId}", "New Comment");

            return RedirectToAction("Topic", new { id = discussionId });
        }

        // --- Hệ thống thích ---

        [HttpPost]
        public async Task<IActionResult> ToggleLikeDiscussion(int id)
        {
            if (User.Identity?.IsAuthenticated != true) return Unauthorized();
 
            var username = User.Identity?.Name ?? "Anonymous";
            var like = await _context.DiscussionLikes
                .FirstOrDefaultAsync(l => l.DiscussionId == id && l.Username == username);

            var discussion = await _context.Discussions.FindAsync(id);
            if (discussion == null) return NotFound();

            bool isLiked;
            if (like == null)
            {
                _context.DiscussionLikes.Add(new DiscussionLike { DiscussionId = id, Username = username });
                isLiked = true;
            }
            else
            {
                _context.DiscussionLikes.Remove(like);
                isLiked = false;
            }

            await _context.SaveChangesAsync();
            
            // Đồng bộ LikeCount cho chắc chắn
            discussion.LikeCount = await _context.DiscussionLikes.CountAsync(l => l.DiscussionId == id);
            _context.Update(discussion);
            await _context.SaveChangesAsync();

            return Json(new { success = true, likeCount = discussion.LikeCount, isLiked = isLiked });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLikeReply(int id)
        {
            if (User.Identity?.IsAuthenticated != true) return Unauthorized();
 
            var username = User.Identity?.Name ?? "Anonymous";
            var like = await _context.DiscussionLikes
                .FirstOrDefaultAsync(l => l.ReplyId == id && l.Username == username);

            var reply = await _context.DiscussionReplies.FindAsync(id);
            if (reply == null) return NotFound();

            bool isLiked;
            if (like == null)
            {
                _context.DiscussionLikes.Add(new DiscussionLike { ReplyId = id, Username = username });
                isLiked = true;
            }
            else
            {
                _context.DiscussionLikes.Remove(like);
                isLiked = false;
            }

            await _context.SaveChangesAsync();

            // Đồng bộ LikeCount cho chắc chắn
            reply.LikeCount = await _context.DiscussionLikes.CountAsync(l => l.ReplyId == id);
            _context.Update(reply);
            await _context.SaveChangesAsync();

            return Json(new { success = true, likeCount = reply.LikeCount, isLiked = isLiked });
        }

        // --- Quản lý chủ đề ---

        [HttpGet]
        public async Task<IActionResult> EditTopic(int id)
        {
            var discussion = await _context.Discussions.FindAsync(id);
            if (discussion == null) return NotFound();

            // Kiểm tra quyền truy cập
            bool isAssignedEditor = false;
            if (User.IsInRole("Editor"))
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                isAssignedEditor = await _context.EditorAssignments.AnyAsync(a => a.EditorUserId == userId && a.DiscussionId == id);
            }

            if (discussion.Author != (User.Identity?.Name ?? "") && !User.IsInRole("Admin") && !isAssignedEditor)
            {
                return Forbid();
            }

            return View(discussion);
        }

        [HttpPost]
        public async Task<IActionResult> EditTopic(Discussion model)
        {
            var discussion = await _context.Discussions.FindAsync(model.Id);
            if (discussion == null) return NotFound();

            // Kiểm tra quyền truy cập
            bool isAssignedEditor = false;
            if (User.IsInRole("Editor"))
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                isAssignedEditor = await _context.EditorAssignments.AnyAsync(a => a.EditorUserId == userId && a.DiscussionId == model.Id);
            }

            if (discussion.Author != (User.Identity?.Name ?? "") && !User.IsInRole("Admin") && !isAssignedEditor)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                discussion.Title = model.Title;
                discussion.Content = model.Content;
                discussion.Category = model.Category;
                discussion.UpdatedAt = DateTime.Now;

                _context.Update(discussion);
                await _context.SaveChangesAsync();
                return RedirectToAction("Topic", new { id = discussion.Id });
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTopic(int id)
        {
            var discussion = await _context.Discussions.FindAsync(id);
            if (discussion == null) return NotFound();

            // Kiểm tra quyền truy cập
            bool isAssignedEditor = false;
            if (User.IsInRole("Editor"))
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                isAssignedEditor = await _context.EditorAssignments.AnyAsync(a => a.EditorUserId == userId && a.DiscussionId == id);
            }

            if (discussion.Author != (User.Identity?.Name ?? "") && !User.IsInRole("Admin") && !isAssignedEditor)
            {
                return Forbid();
            }

            _context.Discussions.Remove(discussion);
            await _context.SaveChangesAsync();
            return RedirectToAction("Community");
        }

        [HttpPost]
        public async Task<IActionResult> TogglePin(int id)
        {
            var discussion = await _context.Discussions.FindAsync(id);
            if (discussion == null) return NotFound();

            bool isAssignedEditor = false;
            if (User.IsInRole("Editor"))
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                isAssignedEditor = await _context.EditorAssignments.AnyAsync(a => a.EditorUserId == userId && a.DiscussionId == id);
            }

            if (!User.IsInRole("Admin") && !isAssignedEditor)
            {
                return Forbid();
            }

            discussion.IsPinned = !discussion.IsPinned;
            await _context.SaveChangesAsync();
            return RedirectToAction("Topic", new { id = id });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLock(int id)
        {
            var discussion = await _context.Discussions.FindAsync(id);
            if (discussion == null) return NotFound();

            bool isAssignedEditor = false;
            if (User.IsInRole("Editor"))
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                isAssignedEditor = await _context.EditorAssignments.AnyAsync(a => a.EditorUserId == userId && a.DiscussionId == id);
            }

            if (!User.IsInRole("Admin") && !isAssignedEditor)
            {
                return Forbid();
            }

            discussion.IsLocked = !discussion.IsLocked;
            await _context.SaveChangesAsync();
            return RedirectToAction("Topic", new { id = id });
        }

        // --- Quản lý trả lời ---

        [HttpPost]
        public async Task<IActionResult> EditReply(int id, string content)
        {
            var reply = await _context.DiscussionReplies.Include(r => r.Discussion).FirstOrDefaultAsync(r => r.Id == id);
            if (reply == null) return NotFound();

            // Kiểm tra quyền truy cập
            bool isAssignedEditor = false;
            if (User.IsInRole("Editor") && reply.Discussion != null)
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                isAssignedEditor = await _context.EditorAssignments.AnyAsync(a => a.EditorUserId == userId && a.DiscussionId == reply.DiscussionId);
            }

            if (reply.Author != (User.Identity?.Name ?? "") && !User.IsInRole("Admin") && !isAssignedEditor)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(content)) return BadRequest();

            reply.Content = content;
            _context.Update(reply);
            await _context.SaveChangesAsync();

            return Json(new { success = true, content = reply.Content });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReply(int id)
        {
            var reply = await _context.DiscussionReplies.Include(r => r.Discussion).FirstOrDefaultAsync(r => r.Id == id);
            if (reply == null) return NotFound();

            // Kiểm tra quyền truy cập
            bool isAssignedEditor = false;
            if (User.IsInRole("Editor") && reply.Discussion != null)
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                isAssignedEditor = await _context.EditorAssignments.AnyAsync(a => a.EditorUserId == userId && a.DiscussionId == reply.DiscussionId);
            }

            if (reply.Author != (User.Identity?.Name ?? "") && !User.IsInRole("Admin") && !isAssignedEditor)
            {
                return Forbid();
            }

            var discussionId = reply.DiscussionId;
            var discussion = await _context.Discussions.FindAsync(discussionId);
            if (discussion != null)
            {
                discussion.ReplyCount--;
                _context.Update(discussion);
            }

            _context.DiscussionReplies.Remove(reply);
            await _context.SaveChangesAsync();
            
            return RedirectToAction("Topic", new { id = discussionId });
        }
    }
}
