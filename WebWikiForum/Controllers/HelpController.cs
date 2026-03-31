using Microsoft.AspNetCore.Mvc;

namespace WebWikiForum.Controllers
{
    public class HelpController : Controller
    {
        public IActionResult HelpCenter()
        {
            return View();
        }
        public IActionResult Guidelines()
        {
            return View();
        }

    }
}
