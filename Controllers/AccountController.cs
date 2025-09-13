using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using KMSI.Data;
using KMSI.Models;

namespace KMSI.Controllers
{
    public class AccountController : Controller
    {
        private readonly KMSIDbContext _context;

        public AccountController(KMSIDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Username and password are required.";
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            var user = await _context.Users
                .Include(u => u.Company)
                .Include(u => u.Site)
                .Include(u => u.UserLevel)
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            if (user == null || !VerifyPassword(password, user.PasswordHash))
            {
                ViewBag.Error = "Invalid username or password.";
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            // Update last login date
            user.LastLoginDate = DateTime.Now;
            await _context.SaveChangesAsync();

            // Create claims
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new("FullName", $"{user.FirstName} {user.LastName}"),
                new("UserLevel", user.UserLevel?.LevelCode ?? ""),
                new("UserLevelName", user.UserLevel?.LevelName ?? ""),
                new("CompanyId", user.CompanyId.ToString()),
                new("CompanyName", user.Company?.CompanyName ?? ""),
                new("SiteId", user.SiteId?.ToString() ?? ""),
                new("SiteName", user.Site?.SiteName ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            // Simple hash verification - in production, use proper password hashing like BCrypt
            using var sha256 = SHA256.Create();
            var hashedInput = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
            return hashedInput == hashedPassword;
        }

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }
    }
}
