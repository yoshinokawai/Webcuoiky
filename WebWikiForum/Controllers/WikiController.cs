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

        public async Task<IActionResult> Agencies(string? searchTerm, string? region, string? focus)
        {
            var agencies = _context.Agencies.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                agencies = agencies.Where(a => a.Name.Contains(searchTerm) || a.Description.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(region) && region != "all")
            {
                agencies = agencies.Where(a => a.Region == region);
            }

            if (!string.IsNullOrEmpty(focus) && focus != "all")
            {
                agencies = agencies.Where(a => a.Focus.Contains(focus));
            }

            ViewData["SearchTerm"] = searchTerm;
            ViewData["Region"] = region;
            ViewData["Focus"] = focus;

            return View(await agencies.ToListAsync());
        }

        public async Task<IActionResult> Independent(string? searchTerm, string? region, string? language, string? tag)
        {
            var vtubers = _context.Vtubers.Where(v => v.IsIndependent && v.Status == "Approved").AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                vtubers = vtubers.Where(v => v.Name.Contains(searchTerm) || v.Lore.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(region) && region != "all")
            {
                vtubers = vtubers.Where(v => v.Region == region);
            }

            if (!string.IsNullOrEmpty(language) && language != "all")
            {
                vtubers = vtubers.Where(v => v.Language.Contains(language));
            }

            if (!string.IsNullOrEmpty(tag))
            {
                vtubers = vtubers.Where(v => v.Tags.Contains(tag));
            }

            ViewData["SearchTerm"] = searchTerm;
            ViewData["Region"] = region;
            ViewData["Language"] = language;
            ViewData["Tag"] = tag;

            return View(await vtubers.ToListAsync());
        }
        public IActionResult Translation()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Agencies = await _context.Agencies.ToListAsync();
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(VtuberViewModel model, IFormFile avatarFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
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
                        IsIndependent = model.AgencyId == null,
                        Region = model.Region,
                        Language = model.Language,
                        Tags = model.Tags,
                        Status = "Approved" // Auto-approve for demo
                    };

                    _context.Add(vtuber);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"VTuber '{vtuber.Name}' has been created successfully!";
                    return RedirectToAction("Independent");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error saving VTuber: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
            ViewBag.Agencies = await _context.Agencies.ToListAsync();
            return View(model);
        }

        [Authorize]
        [HttpGet]
        public IActionResult CreateAgency()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateAgency(AgencyViewModel model, IFormFile logoFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string fileName = await _fileService.UploadImageAsync(logoFile, "agencies");

                    var agency = new Agency
                    {
                        Name = model.Name,
                        Region = model.Region,
                        Focus = model.Focus,
                        Description = model.Description,
                        TalentCount = model.TalentCount,
                        LogoUrl = string.IsNullOrEmpty(fileName) ? null : "/uploads/agencies/" + fileName
                    };

                    _context.Add(agency);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Agency '{agency.Name}' has been created successfully!";
                    return RedirectToAction("Agencies");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error saving Agency: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteAgency(int id)
        {
            var agency = await _context.Agencies.FindAsync(id);
            if (agency == null) return NotFound();

            try {
                var relatedVtubers = await _context.Vtubers.Where(v => v.AgencyId == id).ToListAsync();
                foreach (var vtuber in relatedVtubers) {
                    vtuber.AgencyId = null;
                    vtuber.IsIndependent = true;
                }
                
                if (!string.IsNullOrEmpty(agency.LogoUrl)) {
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", agency.LogoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                }

                _context.Agencies.Remove(agency);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Agency '{agency.Name}' deleted.";
            } catch (Exception ex) {
                TempData["ErrorMessage"] = "Error deleting: " + ex.Message;
            }
            return RedirectToAction("Agencies");
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

            // Increment ViewCount
            try
            {
                vtuber.ViewCount++;
                _context.Update(vtuber);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // Silently fail if view count update fails to not block the user
            }
            
            return View(vtuber);
        }
    }
}
