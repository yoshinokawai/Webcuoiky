using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebWikiForum.Data;
using WebWikiForum.Models;
using WebWikiForum.ViewModels;
using WebWikiForum.Services;

namespace WebWikiForum.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;

        public AccountController(ApplicationDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        // ==================== ĐĂNG NHẬP ====================


        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return LocalRedirect(returnUrl);
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewData["ReturnUrl"] = model.ReturnUrl ?? "/";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Tìm người dùng trong cơ sở dữ liệu
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Username);

            if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            // Tạo cookie xác thực
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // KIỂM TRA: Nếu người dùng chưa có mã PIN bảo mật, chuyển hướng đến trang thiết lập
            if (string.IsNullOrEmpty(user.SecurityPin))
            {
                TempData["WarningMessage"] = "Please set up a 6-digit Security PIN for account recovery.";
                return RedirectToAction("SetupPin");
            }

            TempData["SuccessMessage"] = $"Welcome back, {user.Username}!";
            return LocalRedirect(model.ReturnUrl ?? "/");
        }

        // ==================== ĐĂNG KÝ ====================


        [HttpGet]
        public IActionResult CreateAccount()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra xem tên đăng nhập đã tồn tại chưa
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError("Username", "This username is already taken.");
                return View(model);
            }

            // Kiểm tra xem email đã tồn tại chưa
            var existingEmail = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);
            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View(model);
            }

            // Tạo tài khoản người dùng (luôn là User — Admin trao role từ Dashboard)
            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = HashPassword(model.Password),
                Role = "User", // Mọi tài khoản mới đều là User — Admin nâng quyền từ Dashboard
                CreatedAt = DateTime.UtcNow,
                SecurityPin = model.SecurityPin // Lưu mã PIN
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Tự động đăng nhập sau khi đăng ký
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            TempData["SuccessMessage"] = $"Welcome to VTWiki, {user.Username}! Your account has been created.";
            return RedirectToAction("Index", "Home");
        }

        // ==================== TỪ CHỐI TRUY CẬP ====================


        public IActionResult AccessDenied(string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // ==================== ĐĂNG XUẤT ====================


        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // ==================== MÃ PIN BẢO MẬT & QUÊN MẬT KHẨU ====================


        [HttpGet]
        public IActionResult SetupPin()
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SetupPin(string pin)
        {
            if (string.IsNullOrEmpty(pin) || pin.Length != 6 || !long.TryParse(pin, out _))
            {
                ModelState.AddModelError("", "Please enter a valid 6-digit number.");
                return View();
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Login");
            
            var userId = int.Parse(userIdStr);
            var user = await _context.Users.FindAsync(userId);

            if (user != null)
            {
                user.SecurityPin = pin;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Security PIN has been set up successfully.";
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string identifier)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == identifier || u.Username == identifier);

            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View();
            }

            // Lưu ID người dùng trong session hoặc truyền qua TempData cho bước tiếp theo
            TempData["ResetUserId"] = user.Id;
            return RedirectToAction("VerifyPin");
        }

        [HttpGet]
        public IActionResult VerifyPin()
        {
            if (TempData["ResetUserId"] == null) return RedirectToAction("ForgotPassword");
            TempData.Keep("ResetUserId");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyPin(string pin)
        {
            if (TempData["ResetUserId"] == null) return RedirectToAction("ForgotPassword");
            int userId = TempData["ResetUserId"] as int? ?? 0;
            TempData.Keep("ResetUserId");

            var user = await _context.Users.FindAsync(userId);
            if (user != null && user.SecurityPin == pin)
            {
                TempData["PinVerified"] = true;
                return RedirectToAction("ResetPassword");
            }

            ModelState.AddModelError("", "Invalid Security PIN.");
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (TempData["ResetUserId"] == null || TempData["PinVerified"] == null) 
                return RedirectToAction("ForgotPassword");
            
            TempData.Keep("ResetUserId");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string newPassword, string confirmPassword)
        {
            if (TempData["ResetUserId"] == null) return RedirectToAction("ForgotPassword");
            int userId = TempData["ResetUserId"] as int? ?? 0;

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                TempData.Keep("ResetUserId");
                return View();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.PasswordHash = HashPassword(newPassword);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Password has been reset successfully. Please login.";
                return RedirectToAction("Login");
            }

            return RedirectToAction("ForgotPassword");
        }

        // ==================== HỒ SƠ NGƯỜI DÙNG ====================


        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");
 
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Logout");

            var userId = int.Parse(userIdStr);
            var user = await _context.Users.FindAsync(userId);
 
            if (user == null) return RedirectToAction("Logout");

            var model = new ProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                Role = user.Role,
                Bio = user.Bio,
                AvatarUrl = user.AvatarUrl,
                CoverImageUrl = user.CoverImageUrl,
                DiscordUrl = user.DiscordUrl,
                TwitterUrl = user.TwitterUrl,
                YoutubeUrl = user.YoutubeUrl,
                WebsiteUrl = user.WebsiteUrl,
                RecentActivities = await _context.Activities
                    .Where(a => a.Author == user.Username)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            if (User.Identity?.IsAuthenticated != true) return RedirectToAction("Login");
 
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToAction("Logout");

            var userId = int.Parse(userIdStr);
            var user = await _context.Users.FindAsync(userId);
 
            if (user == null) return RedirectToAction("Logout");

            // Xử lý upload ảnh đại diện
            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                try 
                {
                    var avatarUrl = await _fileService.UploadImageAsync(model.AvatarFile, "avatars");
                    if (!string.IsNullOrEmpty(avatarUrl))
                    {
                        user.AvatarUrl = avatarUrl;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to upload image: " + ex.Message);
                    return View("Profile", model);
                }
            }

            // Xử lý upload ảnh bìa
            if (model.CoverImageFile != null && model.CoverImageFile.Length > 0)
            {
                try
                {
                    var coverUrl = await _fileService.UploadImageAsync(model.CoverImageFile, "covers");
                    if (!string.IsNullOrEmpty(coverUrl))
                    {
                        user.CoverImageUrl = coverUrl;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Failed to upload cover: " + ex.Message);
                    return View("Profile", model);
                }
            }

            user.Bio = model.Bio;
            user.DiscordUrl = model.DiscordUrl;
            user.TwitterUrl = model.TwitterUrl;
            user.YoutubeUrl = model.YoutubeUrl;
            user.WebsiteUrl = model.WebsiteUrl;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Your profile has been updated successfully.";

            return RedirectToAction("Profile");
        }

        [HttpGet("Account/Member/{username}")]
        public async Task<IActionResult> Member(string username)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                Role = user.Role,
                Bio = user.Bio,
                AvatarUrl = user.AvatarUrl,
                CoverImageUrl = user.CoverImageUrl,
                DiscordUrl = user.DiscordUrl,
                TwitterUrl = user.TwitterUrl,
                YoutubeUrl = user.YoutubeUrl,
                WebsiteUrl = user.WebsiteUrl,
                RecentActivities = await _context.Activities
                    .Where(a => a.Author == user.Username)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(5)
                    .ToListAsync()
            };

            return View("PublicProfile", model);
        }

        // ==================== HÀM TIỆN ÍCH MẬT KHẨU ====================


        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var saltedPassword = "VTWiki_Salt_" + password;
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                return Convert.ToBase64String(bytes);
            }
        }

        private static bool VerifyPassword(string password, string storedHash)
        {
            var hash = HashPassword(password);
            return hash == storedHash;
        }
    }
}
