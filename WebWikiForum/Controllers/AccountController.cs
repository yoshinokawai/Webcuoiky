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

namespace WebWikiForum.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==================== LOGIN ====================

        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            if (User.Identity.IsAuthenticated)
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
                new Claim(ClaimTypes.Role, "User")
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

            TempData["SuccessMessage"] = $"Welcome back, {user.Username}!";
            return LocalRedirect(model.ReturnUrl ?? "/");
        }

        // ==================== REGISTER ====================

        [HttpGet]
        public IActionResult CreateAccount()
        {
            if (User.Identity.IsAuthenticated)
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

            // Create the user
            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = HashPassword(model.Password),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Auto-login after registration
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, "User")
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

        // ==================== LOGOUT ====================

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
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
