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

        // ==================== LOGIN ====================

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

            // Find user in database
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Username);

            if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            // Create authentication cookie
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

            // CHECK: If user has no Security PIN, redirect to Setup
            if (string.IsNullOrEmpty(user.SecurityPin))
            {
                TempData["WarningMessage"] = "Please set up a 6-digit Security PIN for account recovery.";
                return RedirectToAction("SetupPin");
            }

            TempData["SuccessMessage"] = $"Welcome back, {user.Username}!";
            return LocalRedirect(model.ReturnUrl ?? "/");
        }

        // ==================== REGISTER ====================

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

            // Check if username already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError("Username", "This username is already taken.");
                return View(model);
            }

            // Check if email already exists
            var existingEmail = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);
            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View(model);
            }

            // Admin role validation
            if (model.Role == "Admin")
            {
                if (model.AdminKey != "Yoshino")
                {
                    ModelState.AddModelError("AdminKey", "Invalid Admin Secret Key.");
                    return View(model);
                }
            }

            // Create the user
            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = HashPassword(model.Password),
                Role = model.Role ?? "User",
                CreatedAt = DateTime.UtcNow,
                SecurityPin = model.SecurityPin // Save the PIN
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Auto-login after registration
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

        // ==================== ACCESS DENIED ====================

        public IActionResult AccessDenied(string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // ==================== LOGOUT ====================

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // ==================== SECURITY PIN & FORGOT PASSWORD ====================

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

            // Store user ID in session or pass via TempData for the next step
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

        // ==================== USER PROFILE ====================

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
                AvatarUrl = user.AvatarUrl
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

            // Handle Avatar Upload
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

            user.Bio = model.Bio;
            // No longer updating AvatarUrl directly from text input to prevent overriding upload
            // user.AvatarUrl = model.AvatarUrl; 

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Your profile has been updated successfully.";

            return RedirectToAction("Profile");
        }

        // ==================== PASSWORD HELPERS ====================

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
