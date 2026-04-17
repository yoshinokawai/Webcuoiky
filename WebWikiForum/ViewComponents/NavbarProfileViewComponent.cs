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

            return View("Default", user);
        }
    }
}
