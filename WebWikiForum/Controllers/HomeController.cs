using Microsoft.AspNetCore.Mvc;

namespace WebWikiForum.Controllers
{
    public class HomeController : Controller
    {
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

        public IActionResult TopTalent()
        {
            return View();
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
