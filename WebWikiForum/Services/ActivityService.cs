using System;
using System.Threading.Tasks;
using WebWikiForum.Data;
using WebWikiForum.Models;

namespace WebWikiForum.Services
{
    public interface IActivityService
    {
        Task LogActivityAsync(string title, string? description, string type, string action, string author, string? linkUrl = null, string? detail = null);
    }

    public class ActivityService : IActivityService
    {
        private readonly ApplicationDbContext _context;

        public ActivityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogActivityAsync(string title, string? description, string type, string action, string author, string? linkUrl = null, string? detail = null)
        {
            var activity = new Activity
            {
                Title = title,
                Description = description,
                ActivityType = type,
                Action = action,
                Author = author,
                Timestamp = DateTime.Now,
                LinkUrl = linkUrl,
                Detail = detail
            };

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();
        }
    }
}
