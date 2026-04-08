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
            return View(results);
        }

        // Discussion Thread Details
        public async Task<IActionResult> Topic(int id)
        {
            var discussion = await _context.Discussions
                .Include(d => d.Replies)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (discussion == null) return NotFound();

            // Increment ViewCount
            discussion.ViewCount++;
            _context.Update(discussion);
            await _context.SaveChangesAsync();

            return View(discussion);
        }

        // Create Topic
        [HttpGet]
        public IActionResult StartTopic()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> StartTopic(Discussion model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                model.Author = User.Identity.Name ?? "Anonymous";
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
            if (!User.Identity.IsAuthenticated) return RedirectToAction("Login", "Account");
            if (string.IsNullOrEmpty(content)) return BadRequest();

            var discussion = await _context.Discussions.FindAsync(discussionId);
            if (discussion == null) return NotFound();

            var reply = new DiscussionReply
            {
                DiscussionId = discussionId,
                Content = content,
                Author = User.Identity.Name,
                CreatedAt = DateTime.Now
            };

            discussion.ReplyCount++;
            discussion.LastReplier = User.Identity.Name;
            discussion.LastReplyDate = DateTime.Now;

            _context.DiscussionReplies.Add(reply);
            _context.Update(discussion);
            await _context.SaveChangesAsync();

            await _activityService.LogActivityAsync($"Reply to: {discussion.Title}", content, "Community", "Commented", User.Identity.Name, $"/Forum/Topic/{discussionId}", "New Comment");

            return RedirectToAction("Topic", new { id = discussionId });
        }
    }
}
