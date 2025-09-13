using KMSI.Data;
using KMSI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Cryptography;
using KMSI.Models.ViewModels;

namespace KMSI.Controllers
{
    [Authorize(Policy = "HOAdminAndAbove")]
    public class UserController : BaseController
    {
        public UserController(KMSIDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            var companyId = GetCurrentCompanyId();
            var users = await _context.Users
                .Include(u => u.Company)
                .Include(u => u.Site)
                .Include(u => u.UserLevel)
                .Where(u => u.CompanyId == companyId)
                .OrderBy(u => u.FirstName)
                .ToListAsync();

            ViewBag.Companies = await _context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            ViewBag.Sites = await _context.Sites
                .Where(s => s.CompanyId == companyId && s.IsActive)
                .OrderBy(s => s.SiteName)
                .ToListAsync();

            ViewBag.UserLevels = await _context.UserLevels
                .Where(ul => ul.IsActive)
                .OrderBy(ul => ul.SortOrder)
                .ToListAsync();

            ViewData["Breadcrumb"] = "User Management";
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.Company)
                .Include(u => u.Site)
                .Include(u => u.UserLevel)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
                return NotFound();

            return Json(new
            {
                userId = user.UserId,
                companyId = user.CompanyId,
                siteId = user.SiteId,
                userLevelId = user.UserLevelId,
                username = user.Username,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                phone = user.Phone,
                address = user.Address,
                city = user.City,
                dateOfBirth = user.DateOfBirth?.ToString("yyyy-MM-dd"),
                gender = user.Gender,
                isActive = user.IsActive
            });
        }
    
        [HttpPost]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Please fill all required fields." });
                }

                // Check if username already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "Username already exists." });
                }

                // Check if email already exists
                var existingEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);
                if (existingEmail != null)
                {
                    return Json(new { success = false, message = "Email already exists." });
                }

                var user = new User
                {
                    CompanyId = model.CompanyId,
                    SiteId = model.SiteId,
                    UserLevelId = model.UserLevelId,
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = HashPassword(model.Password),
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Phone = model.Phone,
                    Address = model.Address,
                    City = model.City,
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    IsActive = model.IsActive,
                    CreatedBy = GetCurrentUserId(),
                    CreatedDate = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "User created successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating user: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(UserViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Please fill all required fields." });
                }

                var user = await _context.Users.FindAsync(model.UserId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Check if username already exists (excluding current user)
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username && u.UserId != model.UserId);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "Username already exists." });
                }

                // Check if email already exists (excluding current user)
                var existingEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.UserId != model.UserId);
                if (existingEmail != null)
                {
                    return Json(new { success = false, message = "Email already exists." });
                }

                user.CompanyId = model.CompanyId;
                user.SiteId = model.SiteId;
                user.UserLevelId = model.UserLevelId;
                user.Username = model.Username;
                user.Email = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Phone = model.Phone;
                user.Address = model.Address;
                user.City = model.City;
                user.DateOfBirth = model.DateOfBirth;
                user.Gender = model.Gender;
                user.IsActive = model.IsActive;
                user.UpdatedBy = GetCurrentUserId();
                user.UpdatedDate = DateTime.Now;

                // Update password if provided
                if (!string.IsNullOrEmpty(model.Password))
                {
                    user.PasswordHash = HashPassword(model.Password);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "User updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating user: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                // Don't allow deleting current user
                if (user.UserId == GetCurrentUserId())
                {
                    return Json(new { success = false, message = "Cannot delete your own account." });
                }

                // Soft delete - set IsActive to false
                user.IsActive = false;
                user.UpdatedBy = GetCurrentUserId();
                user.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "User deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting user: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSitesByCompany(int companyId)
        {
            var sites = await _context.Sites
                .Where(s => s.CompanyId == companyId && s.IsActive)
                .Select(s => new { value = s.SiteId, text = s.SiteName })
                .OrderBy(s => s.text)
                .ToListAsync();

            return Json(sites);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }
    }
}