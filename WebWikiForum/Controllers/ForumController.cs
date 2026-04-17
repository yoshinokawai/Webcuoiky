using Microsoft.AspNetCore.Mvc;
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

        public ForumController(ApplicationDbContext context, IActivityService activityService)
        {
            _context = context;
            _activityService = activityService;
        }

        private async Task<Dictionary<string, string?>> _GetAuthorAvatars(IEnumerable<string> usernames)
        {
            var distinctUsernames = usernames.Where(u => !string.IsNullOrEmpty(u)).Distinct().ToList();
            return await _context.Users
                .AsNoTracking()
                .Where(u => distinctUsernames.Contains(u.Username))
                .ToDictionaryAsync(u => u.Username, u => u.AvatarUrl);
        }

        // Community Hub / All Discussions
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

            // Mock/Fetch some basic events if we had a dedicated table.
            // Since we added News/Events, I'll fetch recent News too.
            var news = await _context.News.OrderByDescending(n => n.PublishDate).Take(3).ToListAsync();

            // Sync Avatars
            var authors = recent.Select(d => d.Author)
                .Concat(trending.Select(d => d.Author))
                .ToList();
            var avatars = await _GetAuthorAvatars(authors);
            ViewBag.AuthorAvatars = avatars;

            ViewBag.Trending = trending;
            ViewBag.News = news;
            
            return View(recent);
        }

        public async Task<IActionResult> Community(string category = "All", string sort = "Default")
        {
            ViewBag.CurrentCategory = category;
            ViewBag.CurrentSort = sort;

            // Stats for Home cards (only if visiting All and NO sort requested)
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

            // Global trending for sidebar
            ViewBag.Trending = await _context.Discussions
                .OrderByDescending(d => d.ViewCount + (d.ReplyCount * 5))
                .Take(5)
                .ToListAsync();

            var results = await query.ToListAsync();

            // Sync Avatars
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

        // Discussion Thread Details
        public async Task<IActionResult> Topic(int id)
        {
            var discussion = await _context.Discussions
                .Include(d => d.Replies)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (discussion == null) return NotFound();

            // Get Liked IDs for the current user
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

            // Sync Avatars
            var authors = new List<string> { discussion.Author };
            authors.AddRange(discussion.Replies.Select(r => r.Author));
            ViewBag.AuthorAvatars = await _GetAuthorAvatars(authors);

            return View(discussion);
        }

        // Create Topic
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

        // Post Reply
        [HttpPost]
        public async Task<IActionResult> PostReply(int discussionId, string content)
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login", "Account");
            if (string.IsNullOrEmpty(content)) return BadRequest();

            var discussion = await _context.Discussions.FindAsync(discussionId);
            if (discussion == null) return NotFound();

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

        // --- Like System ---

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
            
            // Sync LikeCount to be safe
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

            // Sync LikeCount to be safe
            reply.LikeCount = await _context.DiscussionLikes.CountAsync(l => l.ReplyId == id);
            _context.Update(reply);
            await _context.SaveChangesAsync();

            return Json(new { success = true, likeCount = reply.LikeCount, isLiked = isLiked });
        }

        // --- Topic Management ---

        [HttpGet]
        public async Task<IActionResult> EditTopic(int id)
        {
            var discussion = await _context.Discussions.FindAsync(id);
            if (discussion == null) return NotFound();

            // Auth Check
            if (discussion.Author != (User.Identity?.Name ?? "") && !User.IsInRole("Admin"))
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

            // Auth Check
            if (discussion.Author != (User.Identity?.Name ?? "") && !User.IsInRole("Admin"))
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

            // Auth Check
            if (discussion.Author != (User.Identity?.Name ?? "") && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            _context.Discussions.Remove(discussion);
            await _context.SaveChangesAsync();
            return RedirectToAction("Community");
        }

        // --- Reply Management ---

        [HttpPost]
        public async Task<IActionResult> EditReply(int id, string content)
        {
            var reply = await _context.DiscussionReplies.FindAsync(id);
            if (reply == null) return NotFound();

            // Auth Check
            if (reply.Author != (User.Identity?.Name ?? "") && !User.IsInRole("Admin"))
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
            var reply = await _context.DiscussionReplies.FindAsync(id);
            if (reply == null) return NotFound();

            // Auth Check
            if (reply.Author != (User.Identity?.Name ?? "") && !User.IsInRole("Admin"))
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
