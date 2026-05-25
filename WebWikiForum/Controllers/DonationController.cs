using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebWikiForum.Data;
using WebWikiForum.Models;
using WebWikiForum.Services;
using System.Security.Claims;
using System.Linq;
using Microsoft.Extensions.Localization;

namespace WebWikiForum.Controllers
{
    public class DonationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IVNPayService _vnPayService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public DonationController(ApplicationDbContext context, IVNPayService vnPayService, IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _vnPayService = vnPayService;
            _localizer = localizer;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayment(double amount, string message)
        {
            if (amount < 10000)
            {
                TempData["Error"] = _localizer["Donation_Error_MinAmount"].Value;
                return RedirectToAction("Index");
            }

            int? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var username = User.Identity?.Name;
                if (!string.IsNullOrEmpty(username))
                {
                    var user = _context.Users.FirstOrDefault(u => u.Username == username);
                    if (user != null)
                    {
                        userId = user.Id;
                    }
                }
            }

            var donation = new Donation
            {
                Amount = (decimal)amount,
                Message = message,
                UserId = userId,
                Status = "Pending"
            };

            _context.Donations.Add(donation);
            await _context.SaveChangesAsync();

            var model = new PaymentInformationModel
            {
                OrderType = "other",
                Amount = amount,
                OrderDescription = message ?? "Ung ho WikiForum",
                Name = "Anonymous",
                DonationId = donation.Id
            };

            var url = _vnPayService.CreatePaymentUrl(HttpContext, model);

            return Redirect(url);
        }

        public async Task<IActionResult> PaymentCallback()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (response == null || response.VnPayResponseCode != "00")
            {
                TempData["Message"] = string.Format(_localizer["Donation_Error_Callback"].Value, response?.VnPayResponseCode ?? "Unknown");
                
                // Update donation status if we have the ID
                if (response != null && response.DonationId > 0)
                {
                    var donation = _context.Donations.Find(response.DonationId);
                    if (donation != null)
                    {
                        donation.Status = "Failed";
                        donation.TransactionId = response.TransactionId;
                        await _context.SaveChangesAsync();
                    }
                }

                return RedirectToAction("Fail");
            }

            // Payment successful
            var successDonation = _context.Donations.Find(response.DonationId);
            if (successDonation != null)
            {
                successDonation.Status = "Completed";
                successDonation.TransactionId = response.TransactionId;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Success");
        }

        public IActionResult Success()
        {
            return View();
        }

        public IActionResult Fail()
        {
            return View();
        }
    }
}
