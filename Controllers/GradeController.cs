using KMSI.Data;
using KMSI.Models.ViewModels;
using KMSI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMSI.Controllers
{
    [Authorize(Policy = "HOAdminAndAbove")]
    public class GradeController : BaseController
    {
        public GradeController(KMSIDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            var companyId = GetCurrentCompanyId();
            var grades = await _context.Grades
                .Include(g => g.Company)
                .Where(g => g.CompanyId == companyId)
                .OrderBy(g => g.SortOrder ?? int.MaxValue)
                .ThenBy(g => g.GradeName)
                .ToListAsync();

            ViewBag.Companies = await _context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            ViewData["Breadcrumb"] = "Grade Management";
            return View(grades);
        }

        [HttpGet]
        public async Task<IActionResult> GetGrade(int id)
        {
            var grade = await _context.Grades
                .Include(g => g.Company)
                .FirstOrDefaultAsync(g => g.GradeId == id);

            if (grade == null)
                return NotFound();

            return Json(new
            {
                gradeId = grade.GradeId,
                companyId = grade.CompanyId,
                gradeCode = grade.GradeCode,
                gradeName = grade.GradeName,
                description = grade.Description,
                duration = grade.Duration,
                sortOrder = grade.SortOrder,
                isActive = grade.IsActive
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(GradeViewModel model)
        {
            try
            {
                // Check if grade code already exists in the same company
                var existingGrade = await _context.Grades
                    .FirstOrDefaultAsync(g => g.GradeCode == model.GradeCode && g.CompanyId == model.CompanyId);
                if (existingGrade != null)
                {
                    return Json(new { success = false, message = "Grade code already exists in this company." });
                }

                var grade = new Grade
                {
                    CompanyId = model.CompanyId,
                    GradeCode = model.GradeCode,
                    GradeName = model.GradeName,
                    Description = model.Description,
                    Duration = model.Duration,
                    SortOrder = model.SortOrder,
                    IsActive = model.IsActive,
                    CreatedBy = GetCurrentUserId(),
                    CreatedDate = DateTime.Now
                };

                _context.Grades.Add(grade);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Grade created successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating grade: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(GradeViewModel model)
        {
            try
            {
                var grade = await _context.Grades.FindAsync(model.GradeId);
                if (grade == null)
                {
                    return Json(new { success = false, message = "Grade not found." });
                }

                // Check if grade code already exists in the same company (excluding current grade)
                var existingGrade = await _context.Grades
                    .FirstOrDefaultAsync(g => g.GradeCode == model.GradeCode && g.CompanyId == model.CompanyId && g.GradeId != model.GradeId);
                if (existingGrade != null)
                {
                    return Json(new { success = false, message = "Grade code already exists in this company." });
                }

                grade.CompanyId = model.CompanyId;
                grade.GradeCode = model.GradeCode;
                grade.GradeName = model.GradeName;
                grade.Description = model.Description;
                grade.Duration = model.Duration;
                grade.SortOrder = model.SortOrder;
                grade.IsActive = model.IsActive;
                grade.UpdatedBy = GetCurrentUserId();
                grade.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Grade updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating grade: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var grade = await _context.Grades.FindAsync(id);
                if (grade == null)
                {
                    return Json(new { success = false, message = "Grade not found." });
                }

                // Check if grade has active students
                var hasStudents = await _context.Students.AnyAsync(s => s.CurrentGradeId == id && s.IsActive);
                if (hasStudents)
                {
                    return Json(new { success = false, message = "Cannot delete grade that has active students." });
                }

                // Check if grade has grade books
                var hasGradeBooks = await _context.GradeBooks.AnyAsync(gb => gb.GradeId == id);
                if (hasGradeBooks)
                {
                    return Json(new { success = false, message = "Cannot delete grade that has associated books." });
                }

                // Check if grade has registrations
                var hasRegistrations = await _context.Registrations.AnyAsync(r => r.RequestedGradeId == id && r.Status != "Cancelled");
                if (hasRegistrations)
                {
                    return Json(new { success = false, message = "Cannot delete grade that has active registrations." });
                }

                // Check if grade has class schedules
                var hasSchedules = await _context.ClassSchedules.AnyAsync(cs => cs.GradeId == id && cs.Status != "Cancelled");
                if (hasSchedules)
                {
                    return Json(new { success = false, message = "Cannot delete grade that has active class schedules." });
                }

                // Soft delete - set IsActive to false
                grade.IsActive = false;
                grade.UpdatedBy = GetCurrentUserId();
                grade.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Grade deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting grade: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCompaniesByUser()
        {
            var userLevel = GetCurrentUserLevel();
            var companyId = GetCurrentCompanyId();

            var companies = userLevel == "SUPER"
                ? await _context.Companies
                    .Where(c => c.IsActive)
                    .Select(c => new { value = c.CompanyId, text = c.CompanyName })
                    .OrderBy(c => c.text)
                    .ToListAsync()
                : await _context.Companies
                    .Where(c => c.CompanyId == companyId && c.IsActive)
                    .Select(c => new { value = c.CompanyId, text = c.CompanyName })
                    .OrderBy(c => c.text)
                    .ToListAsync();

            return Json(companies);
        }

        [HttpGet]
        public async Task<IActionResult> GetNextSortOrder(int companyId)
        {
            var maxSortOrder = await _context.Grades
                .Where(g => g.CompanyId == companyId)
                .MaxAsync(g => (int?)g.SortOrder) ?? 0;

            return Json(new { nextSortOrder = maxSortOrder + 1 });
        }
    }
}
