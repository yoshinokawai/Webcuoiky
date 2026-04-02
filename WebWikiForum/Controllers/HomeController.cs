using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebWikiForum.Data;
using System.Threading.Tasks;
using System.Linq;

namespace WebWikiForum.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Explore()
        {
            return View();
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        public IActionResult RecentChanges()
        {
            return View();
        }

        public IActionResult VirtualEvents()
        {
            return View();
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
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
