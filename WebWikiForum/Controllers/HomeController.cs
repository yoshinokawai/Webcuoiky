using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using WebWikiForum.Data;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using WebWikiForum.ViewModels;
using System;
using WebWikiForum.Models;

namespace WebWikiForum.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public HomeController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            // Top 5 VTubers for the Spotlight section
            ViewBag.Spotlight = await _context.Vtubers
                .Include(v => v.Agency)
                .OrderByDescending(v => v.ViewCount)
                .ThenBy(v => v.Id)
                .Take(5)
                .ToListAsync();

            // Top 5 VTubers for the Trending section
            ViewBag.Trending = await _context.Vtubers
                .Include(v => v.Agency)
                .OrderByDescending(v => v.ViewCount)
                .ThenBy(v => v.Id)
                .Take(5)
                .ToListAsync();

            // Top 5 Agencies for the Browse section
            ViewBag.Agencies = await _context.Agencies
                .OrderBy(a => a.Name)
                .Take(5)
                .ToListAsync();

            // Recent Activities for the sidebar/feed
            var recentActivities = await _context.Activities
                .OrderByDescending(a => a.Timestamp)
                .Take(4)
                .ToListAsync();
            ViewBag.RecentActivities = recentActivities;

            // Fetch avatars for these authors
            var authors = recentActivities.Select(a => a.Author).Distinct().ToList();
            var userAvatars = await _context.Users
                .Where(u => authors.Contains(u.Username))
                .Select(u => new { u.Username, u.AvatarUrl })
                .ToDictionaryAsync(u => u.Username, u => u.AvatarUrl);
            ViewBag.UserAvatars = userAvatars;

            // Fetch top 3 news for the Home page
            ViewBag.RecentNews = await _context.News
                .OrderByDescending(n => n.PublishDate)
                .Take(3)
                .ToListAsync();
                
            return View();
        }

        public async Task<IActionResult> Explore(string? searchTerm, string? sortBy, string? contentType, string? status, string? tab, string? language)
        {
            // Defaults for persistence and logic
            status ??= "Active";
            tab ??= "All";
            language ??= "All";
            sortBy ??= "Newest Debut";
            contentType ??= "All Types";

            var query = _context.Vtubers.Include(v => v.Agency).AsQueryable();

            // Filter by Search Term
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(v => v.Name.Contains(searchTerm) || v.Lore.Contains(searchTerm) || v.Tags.Contains(searchTerm));
            }

            // Filter by Status (Highly robust: maps Active to Approved for backwards compatibility)
            if (status != "All")
            {
                if (status == "Active")
                {
                    query = query.Where(v => v.Status == "Active" || v.Status == "Approved");
                }
                else
                {
                    query = query.Where(v => v.Status == status);
                }
            }

            // Filter by Language (Extremely permissive match for EN/JP/ID/KR)
            if (language != "All")
            {
                string l = language;
                if (l == "JP")
                {
                    query = query.Where(v => v.Language.Contains("JP") || v.Language.Contains("jp") || v.Language.Contains("Japanese") || v.Language.Contains("japanese") || v.Language.Contains("Japan"));
                }
                else if (l == "EN")
                {
                    query = query.Where(v => v.Language.Contains("EN") || v.Language.Contains("en") || v.Language.Contains("English") || v.Language.Contains("english"));
                }
                else if (l == "KR")
                {
                    query = query.Where(v => v.Language.Contains("KR") || v.Language.Contains("kr") || v.Language.Contains("Korean") || v.Language.Contains("korean"));
                }
                else if (l == "CN")
                {
                    query = query.Where(v => v.Language.Contains("CN") || v.Language.Contains("cn") || v.Language.Contains("Chinese") || v.Language.Contains("chinese"));
                }
                else if (l == "ID")
                {
                    query = query.Where(v => v.Language.Contains("ID") || v.Language.Contains("id") || v.Language.Contains("Indonesian") || v.Language.Contains("indonesian") || v.Language.Contains("Indo"));
                }
                else if (l == "VN")
                {
                    query = query.Where(v => v.Language.Contains("VN") || v.Language.Contains("vn") || v.Language.Contains("Vietnamese") || v.Language.Contains("vietnamese") || v.Language.Contains("Viet"));
                }
                else
                {
                    query = query.Where(v => v.Language.Contains(l));
                }
            }

            // Filter by Content Type (Tags)
            if (contentType != "All Types")
            {
                query = query.Where(v => v.Tags.Contains(contentType));
            }

            // Filter by Tab
            if (tab == "Agencies")
            {
                query = query.Where(v => v.AgencyId != null);
            }
            else if (tab == "Independent")
            {
                query = query.Where(v => v.IsIndependent == true);
            }

            // Sorting
            query = sortBy switch
            {
                "Most Popular" => query.OrderByDescending(v => v.ViewCount),
                "A-Z" => query.OrderBy(v => v.Name),
                "Z-A" => query.OrderByDescending(v => v.Name),
                _ => query.OrderByDescending(v => v.DebutDate) // Default: Newest Debut
            };

            // Statistics for Sidebar
            ViewBag.TotalArticles = await _context.Vtubers.CountAsync() + await _context.Agencies.CountAsync() + 15;
            ViewBag.TotalEditors = (await _context.Vtubers.CountAsync() / 2) + 5;
            ViewBag.TalentsTracked = await _context.Vtubers.CountAsync();
            ViewBag.AgenciesCount = await _context.Agencies.CountAsync();

            // Trending VTubers (Top 2 by views)
            ViewBag.Trending = await _context.Vtubers
                .Include(v => v.Agency)
                .OrderByDescending(v => v.ViewCount)
                .Take(2)
                .ToListAsync();

            // ViewData for maintaining filter state in the UI
            ViewData["SearchTerm"] = searchTerm;
            ViewData["SortBy"] = sortBy;
            ViewData["ContentType"] = contentType;
            ViewData["Status"] = status;
            ViewData["Tab"] = tab;
            ViewData["Language"] = language;

            var vtubers = await query.ToListAsync();
            return View(vtubers);
        }

        public async Task<IActionResult> AboutUs()
        {
            var admins = await _context.Users
                .Where(u => u.Role == "Admin")
                .OrderBy(u => u.Id)
                .ToListAsync();
            return View(admins);
        }

        public async Task<IActionResult> RecentChanges()
        {
            var activities = await _context.Activities
                .OrderByDescending(a => a.Timestamp)
                .Take(20)
                .ToListAsync();

            var last24h = DateTime.Now.AddDays(-1);
            
            var viewModel = new RecentChangesViewModel
            {
                RecentActivities = activities,
                TotalArticles = await _context.Vtubers.CountAsync() + await _context.Agencies.CountAsync() + await _context.News.CountAsync(),
                TotalEditors = await _context.Activities.Select(a => a.Author).Distinct().CountAsync(),
                TotalMedia = await _context.Activities.CountAsync(a => a.ActivityType == "Media") + (await _context.Vtubers.CountAsync() * 2), // Estimation
                Last24hEdits = await _context.Activities.CountAsync(a => a.Timestamp > last24h),
                
                TopContributors = (await _context.Activities
                    .GroupBy(a => a.Author)
                    .Select(g => new { Author = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(3)
                    .ToListAsync())
                    .Select((x, index) => new ContributorViewModel
                    {
                        Name = x.Author,
                        EditCount = x.Count,
                        Rank = index + 1,
                        AvatarUrl = $"https://ui-avatars.com/api/?name={x.Author}&background=random"
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        public IActionResult VirtualEvents()
        {
            return RedirectToAction("VirtualEvents", "Wiki");
        }

        public async Task<IActionResult> TopTalent()
        {
            var topVtubers = await _context.Vtubers
                .Include(v => v.Agency)
                .OrderByDescending(v => v.ViewCount)
                .Take(20) // Show top 20
                .ToListAsync();
                
            return View(topVtubers);
        }

        public IActionResult Donate()
        {
            return View();
        }

        public IActionResult JoinDiscord()
        {
            ViewBag.DiscordInviteLink = _configuration["Discord:InviteLink"] ?? "https://discord.gg/";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
