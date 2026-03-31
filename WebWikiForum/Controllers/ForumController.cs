using Microsoft.AspNetCore.Mvc;

namespace WebWikiForum.Controllers
{
    public class ForumController : Controller
    {
        public IActionResult Community()
        {
            return View();
        }
        public IActionResult WikiForum()
        {
            return View();
        }

    }
}
