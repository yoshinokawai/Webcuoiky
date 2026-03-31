using Microsoft.AspNetCore.Mvc;

namespace WebWikiForum.Controllers
{
    public class EditorController : Controller
    {
        public IActionResult EditorHub()
        {
            return View();
        }
        public IActionResult FanTools()
        {
            return View();
        }

    }
}
