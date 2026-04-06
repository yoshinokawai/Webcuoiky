using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using WebWikiForum.Data;
using WebWikiForum.Models;
using WebWikiForum.Services;
using WebWikiForum.ViewModels;
using System.IO;
using System;

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
            var vtubers = await _context.Vtubers
                                        .Include(v => v.Agency)
                                        .OrderByDescending(v => v.Id)
                                        .ToListAsync();

            var agencies = await _context.Agencies
                                        .OrderByDescending(a => a.Id)
                                        .ToListAsync();

            var news = await _context.News
                                     .OrderByDescending(n => n.PublishDate)
                                     .ToListAsync();

            var viewModel = new AdminDashboardViewModel
            {
                Vtubers = vtubers,
                Agencies = agencies,
                News = news
            };

            return View(viewModel);
        }

        // POST: Admin/Approve/5
        [HttpPost]
        public async Task<IActionResult> Approve(int id, string status = "Active")
        {
            var vtuber = await _context.Vtubers.FindAsync(id);
            if (vtuber == null)
            {
                return NotFound();
            }

            // Allowed statuses: Active, Graduated, Hiatus
            vtuber.Status = status;
            _context.Update(vtuber);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Dashboard));
        }

        // POST: Admin/ApproveAgency/5
        [HttpPost]
        public async Task<IActionResult> ApproveAgency(int id, string status = "Active")
        {
            var agency = await _context.Agencies.FindAsync(id);
            if (agency == null)
            {
                return NotFound();
            }

            // Allowed statuses: Active, Defunct
            agency.Status = status;
            _context.Update(agency);
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
