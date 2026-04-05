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
            var vtubers = _context.Vtubers.Where(v => v.IsIndependent && (v.Status == "Approved" || v.Status == "Active")).AsQueryable();

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
                    return RedirectToAction("Dashboard", "Admin");
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
        public async Task<IActionResult> EditAgency(int id)
        {
            var agency = await _context.Agencies.FindAsync(id);
            if (agency == null) return NotFound();

            var model = new AgencyViewModel
            {
                Name = agency.Name,
                Region = agency.Region,
                Focus = agency.Focus,
                Description = agency.Description,
                TalentCount = agency.TalentCount
            };

            ViewBag.CurrentLogo = agency.LogoUrl;
            ViewBag.Id = agency.Id;
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditAgency(int id, AgencyViewModel model, IFormFile? logoFile)
        {
            if (ModelState.IsValid)
            {
                var agency = await _context.Agencies.FindAsync(id);
                if (agency == null) return NotFound();

                try
                {
                    if (logoFile != null && logoFile.Length > 0)
                    {
                        // Delete old logo if exists
                        if (!string.IsNullOrEmpty(agency.LogoUrl))
                        {
                            string oldFileName = Path.GetFileName(agency.LogoUrl);
                            _fileService.DeleteFile(oldFileName, "agencies");
                        }

                        // Upload new logo
                        string fileName = await _fileService.UploadImageAsync(logoFile, "agencies");
                        agency.LogoUrl = "/uploads/agencies/" + fileName;
                    }

                    agency.Name = model.Name;
                    agency.Region = model.Region;
                    agency.Focus = model.Focus;
                    agency.Description = model.Description;
                    agency.TalentCount = model.TalentCount;

                    _context.Update(agency);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Agency '{agency.Name}' updated successfully!";
                    return RedirectToAction("AgencyDetails", new { id = agency.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating Agency: {ex.Message}");
                }
            }

            ViewBag.CurrentLogo = (await _context.Agencies.FindAsync(id))?.LogoUrl;
            ViewBag.Id = id;
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
                    string fileName = Path.GetFileName(agency.LogoUrl);
                    _fileService.DeleteFile(fileName, "agencies");
                }

                _context.Agencies.Remove(agency);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Agency '{agency.Name}' deleted.";
            } catch (Exception ex) {
                TempData["ErrorMessage"] = "Error deleting: " + ex.Message;
            }
            return RedirectToAction("Agencies");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var vtuber = await _context.Vtubers.FindAsync(id);
            if (vtuber == null) return NotFound();

            try
            {
                // Delete image file if exists
                if (!string.IsNullOrEmpty(vtuber.AvatarUrl))
                {
                    string fileName = Path.GetFileName(vtuber.AvatarUrl);
                    _fileService.DeleteFile(fileName, "vtubers");
                }

                _context.Vtubers.Remove(vtuber);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"VTuber '{vtuber.Name}' has been deleted.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting VTuber: " + ex.Message;
            }

            return RedirectToAction("Dashboard", "Admin");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var vtuber = await _context.Vtubers.FindAsync(id);
            if (vtuber == null) return NotFound();

            var model = new VtuberViewModel
            {
                Name = vtuber.Name,
                Age = vtuber.Age,
                DebutDate = vtuber.DebutDate,
                Birthday = vtuber.Birthday,
                Lore = vtuber.Lore,
                AgencyId = vtuber.AgencyId,
                Region = vtuber.Region,
                Language = vtuber.Language,
                Tags = vtuber.Tags
            };

            ViewBag.Agencies = await _context.Agencies.ToListAsync();
            ViewBag.CurrentAvatar = vtuber.AvatarUrl;
            ViewBag.Id = vtuber.Id;
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Edit(int id, VtuberViewModel model, IFormFile? avatarFile)
        {
            if (ModelState.IsValid)
            {
                var vtuber = await _context.Vtubers.FindAsync(id);
                if (vtuber == null) return NotFound();

                try
                {
                    if (avatarFile != null && avatarFile.Length > 0)
                    {
                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(vtuber.AvatarUrl))
                        {
                            string oldFileName = Path.GetFileName(vtuber.AvatarUrl);
                            _fileService.DeleteFile(oldFileName, "vtubers");
                        }

                        // Upload new image
                        string fileName = await _fileService.UploadImageAsync(avatarFile, "vtubers");
                        vtuber.AvatarUrl = "/uploads/vtubers/" + fileName;
                    }

                    vtuber.Name = model.Name;
                    vtuber.Age = model.Age;
                    vtuber.DebutDate = model.DebutDate;
                    vtuber.Birthday = model.Birthday;
                    vtuber.Lore = model.Lore;
                    vtuber.AgencyId = model.AgencyId;
                    vtuber.IsIndependent = model.AgencyId == null;
                    vtuber.Region = model.Region;
                    vtuber.Language = model.Language;
                    vtuber.Tags = model.Tags;

                    _context.Update(vtuber);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"VTuber '{vtuber.Name}' updated successfully!";
                    return RedirectToAction("Dashboard", "Admin");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating VTuber: {ex.Message}");
                }
            }

            ViewBag.Agencies = await _context.Agencies.ToListAsync();
            ViewBag.CurrentAvatar = (await _context.Vtubers.FindAsync(id))?.AvatarUrl;
            ViewBag.Id = id;
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

        [HttpGet]
        public async Task<IActionResult> SearchSuggest(string query)
        {
            if (string.IsNullOrEmpty(query)) return Json(new List<object>());

            var results = await _context.Vtubers
                .Include(v => v.Agency)
                .Where(v => v.Name.Contains(query) || (v.Agency != null && v.Agency.Name.Contains(query)))
                .Take(8)
                .Select(v => new {
                    id = v.Id,
                    name = v.Name,
                    avatarUrl = v.AvatarUrl,
                    agencyName = v.Agency != null ? v.Agency.Name : "Independent"
                })
                .ToListAsync();

            return Json(results);
        }
        [HttpGet]
        public async Task<IActionResult> AgencyDetails(int id)
        {
            var agency = await _context.Agencies
                .Include(a => a.Vtubers)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (agency == null)
            {
                return NotFound();
            }

            return View(agency);
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ApiCreateAgency([FromBody] AgencyViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var agency = new Agency
                    {
                        Name = model.Name,
                        Region = model.Region,
                        Focus = model.Focus,
                        Description = model.Description ?? "Quickly added",
                        TalentCount = 0
                    };

                    _context.Agencies.Add(agency);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, id = agency.Id, name = agency.Name });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
            return Json(new { success = false, message = "Invalid data" });
        }
    }
}
