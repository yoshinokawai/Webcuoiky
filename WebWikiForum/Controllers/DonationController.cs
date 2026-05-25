using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebWikiForum.Data;
using WebWikiForum.Models;
using WebWikiForum.Services;
using System.Security.Claims;
using System.Linq;

namespace WebWikiForum.Controllers
{
    public class DonationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IVNPayService _vnPayService;

        public DonationController(ApplicationDbContext context, IVNPayService vnPayService)
        {
            _context = context;
            _vnPayService = vnPayService;
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
                TempData["Error"] = "Số tiền ủng hộ tối thiểu là 10,000 VND";
                return RedirectToAction("Index");
            }

            int? userId = null;
            if (User.Identity.IsAuthenticated)
            {
                var username = User.Identity.Name;
                var user = _context.Users.FirstOrDefault(u => u.Username == username);
                if (user != null)
                {
                    userId = user.Id;
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
                TempData["Message"] = $"Lỗi thanh toán VNPay: {response?.VnPayResponseCode}";
                
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
