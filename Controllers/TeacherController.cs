using KMSI.Data;
using KMSI.Models.ViewModels;
using KMSI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Cryptography;

namespace KMSI.Controllers
{
    [Authorize(Policy = "BranchManagerAndAbove")]
    public class TeacherController : BaseController
    {
        public TeacherController(KMSIDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            var companyId = GetCurrentCompanyId();
            var siteId = GetCurrentSiteId();
            var userLevel = GetCurrentUserLevel();

            var teachersQuery = _context.Teachers
                .Include(t => t.User)
                .Include(t => t.Company)
                .Include(t => t.Site)
                .Where(t => t.CompanyId == companyId);

            // Filter by site if user is not HO Admin or above
            if (!IsHOAdmin() && siteId.HasValue)
            {
                teachersQuery = teachersQuery.Where(t => t.SiteId == siteId.Value);
            }

            var teachers = await teachersQuery
                .OrderBy(t => t.User.FirstName)
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

            ViewData["Breadcrumb"] = "Teacher Management";
            return View(teachers);
        }

        [HttpGet]
        public async Task<IActionResult> GetTeacher(int id)
        {
            var teacher = await _context.Teachers
                .Include(t => t.User)
                .Include(t => t.Company)
                .Include(t => t.Site)
                .FirstOrDefaultAsync(t => t.TeacherId == id);

            if (teacher == null)
                return NotFound();

            return Json(new
            {
                teacherId = teacher.TeacherId,
                userId = teacher.UserId,
                companyId = teacher.CompanyId,
                siteId = teacher.SiteId,
                teacherCode = teacher.TeacherCode,
                specialization = teacher.Specialization,
                experienceYears = teacher.ExperienceYears,
                hourlyRate = teacher.HourlyRate,
                maxStudentsPerDay = teacher.MaxStudentsPerDay,
                isAvailableForTrial = teacher.IsAvailableForTrial,
                isActive = teacher.IsActive,
                username = teacher.User?.Username,
                email = teacher.User?.Email,
                firstName = teacher.User?.FirstName,
                lastName = teacher.User?.LastName,
                phone = teacher.User?.Phone,
                address = teacher.User?.Address,
                city = teacher.User?.City,
                dateOfBirth = teacher.User?.DateOfBirth?.ToString("yyyy-MM-dd"),
                gender = teacher.User?.Gender
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(TeacherViewModel model)
        {
            try
            {
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

                // Check if teacher code already exists
                var existingTeacher = await _context.Teachers
                    .FirstOrDefaultAsync(t => t.TeacherCode == model.TeacherCode);
                if (existingTeacher != null)
                {
                    return Json(new { success = false, message = "Teacher code already exists." });
                }

                // Get Teacher user level
                var teacherUserLevel = await _context.UserLevels
                    .FirstOrDefaultAsync(ul => ul.LevelCode == "TEACHER");
                if (teacherUserLevel == null)
                {
                    return Json(new { success = false, message = "Teacher user level not found." });
                }

                // Create User first
                var user = new User
                {
                    CompanyId = model.CompanyId,
                    SiteId = model.SiteId,
                    UserLevelId = teacherUserLevel.UserLevelId,
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = HashPassword("teacher123"), // Default password
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

                // Create Teacher
                var teacher = new Teacher
                {
                    UserId = user.UserId,
                    CompanyId = model.CompanyId,
                    SiteId = model.SiteId,
                    TeacherCode = model.TeacherCode,
                    Specialization = model.Specialization,
                    ExperienceYears = model.ExperienceYears,
                    HourlyRate = model.HourlyRate,
                    MaxStudentsPerDay = model.MaxStudentsPerDay,
                    IsAvailableForTrial = model.IsAvailableForTrial,
                    IsActive = model.IsActive,
                    CreatedBy = GetCurrentUserId(),
                    CreatedDate = DateTime.Now
                };

                _context.Teachers.Add(teacher);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Teacher created successfully. Default password: teacher123" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating teacher: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(TeacherViewModel model)
        {
            try
            {
                var teacher = await _context.Teachers
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.TeacherId == model.TeacherId);
                if (teacher == null)
                {
                    return Json(new { success = false, message = "Teacher not found." });
                }

                // Check if username already exists (excluding current user)
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username && u.UserId != teacher.UserId);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "Username already exists." });
                }

                // Check if email already exists (excluding current user)
                var existingEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.UserId != teacher.UserId);
                if (existingEmail != null)
                {
                    return Json(new { success = false, message = "Email already exists." });
                }

                // Check if teacher code already exists (excluding current teacher)
                var existingTeacher = await _context.Teachers
                    .FirstOrDefaultAsync(t => t.TeacherCode == model.TeacherCode && t.TeacherId != model.TeacherId);
                if (existingTeacher != null)
                {
                    return Json(new { success = false, message = "Teacher code already exists." });
                }

                // Update User
                teacher.User.CompanyId = model.CompanyId;
                teacher.User.SiteId = model.SiteId;
                teacher.User.Username = model.Username;
                teacher.User.Email = model.Email;
                teacher.User.FirstName = model.FirstName;
                teacher.User.LastName = model.LastName;
                teacher.User.Phone = model.Phone;
                teacher.User.Address = model.Address;
                teacher.User.City = model.City;
                teacher.User.DateOfBirth = model.DateOfBirth;
                teacher.User.Gender = model.Gender;
                teacher.User.IsActive = model.IsActive;
                teacher.User.UpdatedBy = GetCurrentUserId();
                teacher.User.UpdatedDate = DateTime.Now;

                // Update Teacher
                teacher.CompanyId = model.CompanyId;
                teacher.SiteId = model.SiteId;
                teacher.TeacherCode = model.TeacherCode;
                teacher.Specialization = model.Specialization;
                teacher.ExperienceYears = model.ExperienceYears;
                teacher.HourlyRate = model.HourlyRate;
                teacher.MaxStudentsPerDay = model.MaxStudentsPerDay;
                teacher.IsAvailableForTrial = model.IsAvailableForTrial;
                teacher.IsActive = model.IsActive;
                teacher.UpdatedBy = GetCurrentUserId();
                teacher.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Teacher updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating teacher: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var teacher = await _context.Teachers
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.TeacherId == id);
                if (teacher == null)
                {
                    return Json(new { success = false, message = "Teacher not found." });
                }

                // Check if teacher has active students
                var hasStudents = await _context.Students.AnyAsync(s => s.AssignedTeacherId == id && s.IsActive);
                if (hasStudents)
                {
                    return Json(new { success = false, message = "Cannot delete teacher who has active students." });
                }

                // Check if teacher has active class schedules
                var hasSchedules = await _context.ClassSchedules.AnyAsync(cs => cs.TeacherId == id && cs.Status != "Cancelled");
                if (hasSchedules)
                {
                    return Json(new { success = false, message = "Cannot delete teacher who has active class schedules." });
                }

                // Soft delete - set IsActive to false for both User and Teacher
                teacher.IsActive = false;
                teacher.UpdatedBy = GetCurrentUserId();
                teacher.UpdatedDate = DateTime.Now;

                teacher.User.IsActive = false;
                teacher.User.UpdatedBy = GetCurrentUserId();
                teacher.User.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Teacher deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting teacher: " + ex.Message });
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
