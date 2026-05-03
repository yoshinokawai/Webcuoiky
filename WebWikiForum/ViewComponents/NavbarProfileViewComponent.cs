using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebWikiForum.Data;
using WebWikiForum.Models;

namespace WebWikiForum.ViewComponents
{
    public class NavbarProfileViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public NavbarProfileViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }



        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (UserClaimsPrincipal.Identity?.IsAuthenticated != true)
            {
                return View("Default", (User?)null);
            }

            var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return View("Default", (User?)null);
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == int.Parse(userId));

            if (user != null)
            {
                var query = _context.Activities.AsNoTracking().AsQueryable();

                if (user.Role != "Admin")
                {
                    query = query.Where(a => a.Action != "Liked");

                    var myDiscussionIds = await _context.Discussions
                        .Where(d => d.Author == user.Username)
                        .Select(d => d.Id)
                        .ToListAsync();

                    var myRepliedDiscussionIds = await _context.DiscussionReplies
                        .Where(r => r.Author == user.Username)
                        .Select(r => r.DiscussionId)
                        .ToListAsync();

                    var relevantUrls = myDiscussionIds.Concat(myRepliedDiscussionIds)
                        .Distinct()
                        .Select(id => $"/Forum/Topic/{id}")
                        .ToList();

                    if (relevantUrls.Any())
                    {
                        query = query.Where(a => a.Action != "Commented" || relevantUrls.Contains(a.LinkUrl));
                    }
                    else
                    {
                        query = query.Where(a => a.Action != "Commented");
                    }
                }

                var activities = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10)
                    .ToListAsync();
                ViewData["RecentActivities"] = activities;
            }

            return View("Default", user);
        }
    }
}
