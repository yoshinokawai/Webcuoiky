using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebWikiForum.Data;
using WebWikiForum.Services;
using WebWikiForum.ViewModels;
using WebWikiForum.Models;

namespace WebWikiForum.Controllers
{
    public class WikiController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;

        public WikiController(ApplicationDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        public IActionResult Agencies()
        {
            return View();
        }
        public IActionResult Independent()
        {
            return View();
        }
        public IActionResult Translation()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(VtuberViewModel model, IFormFile avatarFile)
        {
            if (ModelState.IsValid)
            {
                // Gọi Service để lưu file ảnh vật lý
                string fileName = await _fileService.UploadImageAsync(avatarFile, "vtubers");

                var vtuber = new Vtuber
                {
                    Name = model.Name,
                    Age = model.Age,
                    DebutDate = model.DebutDate,
                    Birthday = model.Birthday,
                    Lore = model.Lore,
                    AvatarUrl = string.IsNullOrEmpty(fileName) ? null : "/uploads/vtubers/" + fileName,
                    AgencyId = model.AgencyId,
                    Status = "Pending" // Trạng thái chờ Admin duyệt
                };

                _context.Add(vtuber);
                await _context.SaveChangesAsync();
                
                // Redirect to the newly created Details page
                return RedirectToAction(nameof(Details), new { id = vtuber.Id });
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var vtuber = await _context.Vtubers
                .Include(v => v.Agency)
                .FirstOrDefaultAsync(v => v.Id == id);
                
            if (vtuber == null)
            {
                return NotFound();
            }
            
            return View(vtuber);
        }
    }
}
