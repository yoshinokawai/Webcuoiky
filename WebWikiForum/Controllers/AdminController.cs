using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using WebWikiForum.Data;
using WebWikiForum.Models;
using WebWikiForum.Services;
using System.IO;

namespace WebWikiForum.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;

        public AdminController(ApplicationDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            // Lấy tất cả VTuber kèm theo Entity Agency (để hiển thị tên Agency nếu có)
            var vtubers = await _context.Vtubers
                                        .Include(v => v.Agency)
                                        .OrderByDescending(v => v.Id)
                                        .ToListAsync();
            return View(vtubers);
        }

        // POST: Admin/Approve/5
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var vtuber = await _context.Vtubers.FindAsync(id);
            if (vtuber == null)
            {
                return NotFound();
            }

            vtuber.Status = "Approved";
            _context.Update(vtuber);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Dashboard));
        }

        // POST: Admin/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var vtuber = await _context.Vtubers.FindAsync(id);
            if (vtuber == null)
            {
                return NotFound();
            }

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
                TempData["SuccessMessage"] = $"VTuber '{vtuber.Name}' deleted.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting: " + ex.Message;
            }

            return RedirectToAction(nameof(Dashboard));
        }
    }
}
