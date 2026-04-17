using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebWikiForum.Data;
using WebWikiForum.Services;
using WebWikiForum.ViewModels;
using WebWikiForum.Models;
using System.Linq;

namespace WebWikiForum.Controllers
{
    public class WikiController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly IActivityService _activityService;

        public WikiController(ApplicationDbContext context, IFileService fileService, IActivityService activityService)
        {
            _context = context;
            _fileService = fileService;
            _activityService = activityService;
        }

        [HttpGet]
        public async Task<IActionResult> VirtualEvents(string? type)
        {
            var query = _context.News.AsQueryable();

            if (!string.IsNullOrEmpty(type) && type != "all")
            {
                query = query.Where(n => n.Type == type);
            }

            var events = await query.OrderByDescending(n => n.PublishDate).ToListAsync();
            ViewData["CurrentType"] = type ?? "all";
            
            return View(events);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateEvent(NewsViewModel model, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string? imageUrl = null;
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        imageUrl = await _fileService.UploadImageAsync(imageFile, "news");
                    }

                    if (string.IsNullOrEmpty(imageUrl)) {
                        imageUrl = "https://images.unsplash.com/photo-1620641788421-7a1c342ea42e?w=800&auto=format&fit=crop"; // Default
                    }

                    var news = new News
                    {
                        Title = model.Title,
                        Type = model.Type,
                        Content = model.Content,
                        Author = model.Author,
                        IsFeatured = model.IsFeatured,
                        ImageUrl = imageUrl,
                        PublishDate = DateTime.Now
                    };

                    _context.News.Add(news);
                    await _context.SaveChangesAsync();

                    await _activityService.LogActivityAsync(news.Title, news.Content, "Article", "Created", news.Author, "/Wiki/VirtualEvents", "New Event");

                    TempData["SuccessMessage"] = "Event/News added successfully!";
                    return RedirectToAction("VirtualEvents");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error: " + ex.Message);
                }
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();

            var model = new NewsViewModel
            {
                Title = news.Title,
                Type = news.Type,
                Content = news.Content,
                Author = news.Author,
                IsFeatured = news.IsFeatured,
                CurrentImageUrl = news.ImageUrl
            };

            ViewBag.Id = id;
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> EditEvent(int id, NewsViewModel model, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                var news = await _context.News.FindAsync(id);
                if (news == null) return NotFound();

                try
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Delete old image if it's on Cloudinary (not a generic unsplash link)
                        if (!string.IsNullOrEmpty(news.ImageUrl) && news.ImageUrl.Contains("res.cloudinary.com"))
                        {
                            var fileName = Path.GetFileName(news.ImageUrl);
                            if (!string.IsNullOrEmpty(fileName))
                            {
                                _fileService.DeleteFile(fileName, "news");
                            }
                        }
                        news.ImageUrl = await _fileService.UploadImageAsync(imageFile, "news");
                    }

                    news.Title = model.Title;
                    news.Type = model.Type;
                    news.Content = model.Content;
                    news.Author = model.Author;
                    news.IsFeatured = model.IsFeatured;

                    _context.Update(news);
                    await _context.SaveChangesAsync();

                    await _activityService.LogActivityAsync(news.Title, news.Content, "Article", "Updated", news.Author, "/Wiki/VirtualEvents", "Updated Event");

                    TempData["SuccessMessage"] = "Event updated successfully!";
                    return RedirectToAction("VirtualEvents");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error: " + ex.Message);
                }
            }
            ViewBag.Id = id;
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();

            try
            {
                if (!string.IsNullOrEmpty(news.ImageUrl) && news.ImageUrl.Contains("res.cloudinary.com"))
                {
                    var fileName = Path.GetFileName(news.ImageUrl);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        _fileService.DeleteFile(fileName, "news");
                    }
                }
                _context.News.Remove(news);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Event deleted.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }
            return RedirectToAction("VirtualEvents");
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

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Agencies = await _context.Agencies.ToListAsync();
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(VtuberViewModel model, IFormFile? avatarFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string? fileName = null;
                    if (avatarFile != null && avatarFile.Length > 0)
                    {
                        fileName = await _fileService.UploadImageAsync(avatarFile, "vtubers");
                    }
                    
                    if (string.IsNullOrEmpty(fileName)) {
                        fileName = "https://images.unsplash.com/photo-1620641788421-7a1c342ea42e?w=800&auto=format&fit=crop"; // Default
                    }

                    var vtuber = new Vtuber
                    {
                        Name = model.Name,
                        Age = model.Age,
                        DebutDate = model.DebutDate,
                        Birthday = model.Birthday,
                        Lore = model.Lore,
                        AvatarUrl = fileName,
                        AgencyId = model.AgencyId,
                        IsIndependent = model.AgencyId == null,
                        Region = model.Region,
                        Language = model.Language,
                        Tags = model.Tags,
                        YoutubeUrl = model.YoutubeUrl,
                        Status = "Approved" // Auto-approve for demo
                    };

                    _context.Add(vtuber);
                    await _context.SaveChangesAsync();
                    
                    await _activityService.LogActivityAsync(vtuber.Name, vtuber.Lore, "Article", "Created", User.Identity?.Name ?? "Admin", $"/Wiki/Details/{vtuber.Id}", "New VTuber");
                    
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

        [Authorize(Roles = "Admin")]
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
                TalentCount = agency.TalentCount,
                WebsiteUrl = agency.WebsiteUrl,
                YoutubeUrl = agency.YoutubeUrl,
                TwitterUrl = agency.TwitterUrl
            };

            ViewBag.CurrentLogo = agency.LogoUrl;
            ViewBag.Id = agency.Id;
            return View(model);
        }

        [Authorize(Roles = "Admin")]
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
                            var oldFileName = Path.GetFileName(agency.LogoUrl);
                            if (!string.IsNullOrEmpty(oldFileName))
                            {
                                _fileService.DeleteFile(oldFileName, "agencies");
                            }
                        }

                        // Upload new logo
                        string? fileName = await _fileService.UploadImageAsync(logoFile, "agencies");
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            agency.LogoUrl = fileName;
                        }
                    }

                    agency.Name = model.Name;
                    agency.Region = model.Region;
                    agency.Focus = model.Focus;
                    agency.Description = model.Description;
                    agency.TalentCount = model.TalentCount;
                    agency.WebsiteUrl = model.WebsiteUrl;
                    agency.YoutubeUrl = model.YoutubeUrl;
                    agency.TwitterUrl = model.TwitterUrl;

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

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateAgency()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateAgency(AgencyViewModel model, IFormFile logoFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string? fileName = null;
                    if (logoFile != null && logoFile.Length > 0)
                    {
                        fileName = await _fileService.UploadImageAsync(logoFile, "agencies");
                    }
                    
                    if (string.IsNullOrEmpty(fileName)) {
                        fileName = "https://images.unsplash.com/photo-1620641788421-7a1c342ea42e?w=800&auto=format&fit=crop"; // Default logo
                    }

                    var agency = new Agency
                    {
                        Name = model.Name,
                        Region = model.Region,
                        Focus = model.Focus,
                        Description = model.Description,
                        TalentCount = model.TalentCount,
                        WebsiteUrl = model.WebsiteUrl,
                        YoutubeUrl = model.YoutubeUrl,
                        TwitterUrl = model.TwitterUrl,
                        LogoUrl = fileName
                    };

                    _context.Add(agency);
                    await _context.SaveChangesAsync();

                    await _activityService.LogActivityAsync(agency.Name, agency.Description, "Article", "Created", User.Identity?.Name ?? "Admin", $"/Wiki/AgencyDetails/{agency.Id}", "New Agency");

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

        [Authorize(Roles = "Admin")]
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
                    var fileName = Path.GetFileName(agency.LogoUrl);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        _fileService.DeleteFile(fileName, "agencies");
                    }
                }

                _context.Agencies.Remove(agency);
                await _context.SaveChangesAsync();

                await _activityService.LogActivityAsync(agency.Name, agency.Description, "Article", "Deleted", User.Identity?.Name ?? "Admin", "/Wiki/Agencies", "Agency Deleted");
                TempData["SuccessMessage"] = $"Agency '{agency.Name}' deleted.";
            } catch (Exception ex) {
                TempData["ErrorMessage"] = "Error deleting: " + ex.Message;
            }
            return RedirectToAction("Agencies");
        }

        [Authorize(Roles = "Admin")]
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
                    var fileName = Path.GetFileName(vtuber.AvatarUrl);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        _fileService.DeleteFile(fileName, "vtubers");
                    }
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

        [Authorize(Roles = "Admin")]
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
                Tags = vtuber.Tags,
                YoutubeUrl = vtuber.YoutubeUrl
            };

            ViewBag.Agencies = await _context.Agencies.ToListAsync();
            ViewBag.CurrentAvatar = vtuber.AvatarUrl;
            ViewBag.Id = vtuber.Id;
            return View(model);
        }

        [Authorize(Roles = "Admin")]
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
                            var oldFileName = Path.GetFileName(vtuber.AvatarUrl);
                            if (!string.IsNullOrEmpty(oldFileName))
                            {
                                _fileService.DeleteFile(oldFileName, "vtubers");
                            }
                        }

                        // Upload new image
                        string? fileName = await _fileService.UploadImageAsync(avatarFile, "vtubers");
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            vtuber.AvatarUrl = fileName;
                        }
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
                    vtuber.YoutubeUrl = model.YoutubeUrl;

                    _context.Update(vtuber);
                    await _context.SaveChangesAsync();

                    await _activityService.LogActivityAsync(vtuber.Name, vtuber.Lore, "Article", "Updated", User.Identity?.Name ?? "Admin", $"/Wiki/Details/{vtuber.Id}", "Updated VTuber");

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

            var queryLower = query.ToLower();

            var vtubers = await _context.Vtubers
                .Include(v => v.Agency)
                .Where(v => v.Name.Contains(queryLower) || (v.Agency != null && v.Agency.Name.Contains(queryLower)))
                .Take(5)
                .Select(v => new {
                    id = v.Id,
                    name = v.Name,
                    avatarUrl = v.AvatarUrl,
                    subtext = v.Agency != null ? v.Agency.Name : "Independent",
                    type = "vtuber"
                })
                .ToListAsync();

            var agencies = await _context.Agencies
                .Where(a => a.Name.Contains(queryLower))
                .Take(3)
                .Select(a => new {
                    id = a.Id,
                    name = a.Name,
                    avatarUrl = a.LogoUrl,
                    subtext = a.Focus ?? "Agency",
                    type = "agency"
                })
                .ToListAsync();

            var combined = vtubers.Cast<object>().Concat(agencies.Cast<object>()).ToList();

            return Json(combined);
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
        [Authorize(Roles = "Admin")]
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
