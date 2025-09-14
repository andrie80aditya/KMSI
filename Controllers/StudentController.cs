using KMSI.Data;
using KMSI.Models.ViewModels;
using KMSI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMSI.Controllers
{
    [Authorize(Policy = "BranchManagerAndAbove")]
    public class StudentController : BaseController
    {
        public StudentController(KMSIDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            var companyId = GetCurrentCompanyId();
            var siteId = GetCurrentSiteId();
            var userLevel = GetCurrentUserLevel();

            var studentsQuery = _context.Students
                .Include(s => s.Company)
                .Include(s => s.Site)
                .Include(s => s.CurrentGrade)
                .Include(s => s.AssignedTeacher)
                .ThenInclude(t => t.User)
                .Where(s => s.CompanyId == companyId);

            // Filter by site if user is not HO Admin or above
            if (!IsHOAdmin() && siteId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.SiteId == siteId.Value);
            }

            var students = await studentsQuery
                .OrderBy(s => s.StudentCode)
                .ToListAsync();

            ViewBag.Companies = await _context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            ViewBag.Sites = await _context.Sites
                .Where(s => s.CompanyId == companyId && s.IsActive)
                .OrderBy(s => s.SiteName)
                .ToListAsync();

            ViewBag.Grades = await _context.Grades
                .Where(g => g.CompanyId == companyId && g.IsActive)
                .OrderBy(g => g.SortOrder ?? int.MaxValue)
                .ThenBy(g => g.GradeName)
                .ToListAsync();

            ViewBag.Teachers = await _context.Teachers
                .Include(t => t.User)
                .Where(t => t.CompanyId == companyId && t.IsActive)
                .OrderBy(t => t.User.FirstName)
                .ToListAsync();

            ViewData["Breadcrumb"] = "Student Management";
            return View(students);
        }

        [HttpGet]
        public async Task<IActionResult> GetStudent(int id)
        {
            var student = await _context.Students
                .Include(s => s.Company)
                .Include(s => s.Site)
                .Include(s => s.CurrentGrade)
                .Include(s => s.AssignedTeacher)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
                return NotFound();

            return Json(new
            {
                studentId = student.StudentId,
                companyId = student.CompanyId,
                siteId = student.SiteId,
                studentCode = student.StudentCode,
                firstName = student.FirstName,
                lastName = student.LastName,
                dateOfBirth = student.DateOfBirth?.ToString("yyyy-MM-dd"),
                gender = student.Gender,
                phone = student.Phone,
                email = student.Email,
                address = student.Address,
                city = student.City,
                parentName = student.ParentName,
                parentPhone = student.ParentPhone,
                parentEmail = student.ParentEmail,
                photoPath = student.PhotoPath,
                registrationDate = student.RegistrationDate.ToString("yyyy-MM-dd"),
                status = student.Status,
                currentGradeId = student.CurrentGradeId,
                assignedTeacherId = student.AssignedTeacherId,
                notes = student.Notes,
                isActive = student.IsActive
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(StudentViewModel model)
        {
            try
            {
                // Check if student code already exists in the same company
                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentCode == model.StudentCode && s.CompanyId == model.CompanyId);
                if (existingStudent != null)
                {
                    return Json(new { success = false, message = "Student code already exists in this company." });
                }

                // Check if email already exists (if provided)
                if (!string.IsNullOrEmpty(model.Email))
                {
                    var existingEmail = await _context.Students
                        .FirstOrDefaultAsync(s => s.Email == model.Email && s.CompanyId == model.CompanyId);
                    if (existingEmail != null)
                    {
                        return Json(new { success = false, message = "Email already exists in this company." });
                    }
                }

                var student = new Student
                {
                    CompanyId = model.CompanyId,
                    SiteId = model.SiteId,
                    StudentCode = model.StudentCode,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    DateOfBirth = model.DateOfBirth.HasValue ? DateOnly.FromDateTime(model.DateOfBirth.Value) : null,
                    Gender = model.Gender,
                    Phone = model.Phone,
                    Email = model.Email,
                    Address = model.Address,
                    City = model.City,
                    ParentName = model.ParentName,
                    ParentPhone = model.ParentPhone,
                    ParentEmail = model.ParentEmail,
                    PhotoPath = model.PhotoPath,
                    RegistrationDate = DateOnly.FromDateTime(model.RegistrationDate),
                    Status = model.Status,
                    CurrentGradeId = model.CurrentGradeId,
                    AssignedTeacherId = model.AssignedTeacherId,
                    Notes = model.Notes,
                    IsActive = model.IsActive,
                    CreatedBy = GetCurrentUserId(),
                    CreatedDate = DateTime.Now
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Student created successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating student: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(StudentViewModel model)
        {
            try
            {
                var student = await _context.Students.FindAsync(model.StudentId);
                if (student == null)
                {
                    return Json(new { success = false, message = "Student not found." });
                }

                // Check if student code already exists in the same company (excluding current student)
                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentCode == model.StudentCode && s.CompanyId == model.CompanyId && s.StudentId != model.StudentId);
                if (existingStudent != null)
                {
                    return Json(new { success = false, message = "Student code already exists in this company." });
                }

                // Check if email already exists (if provided, excluding current student)
                if (!string.IsNullOrEmpty(model.Email))
                {
                    var existingEmail = await _context.Students
                        .FirstOrDefaultAsync(s => s.Email == model.Email && s.CompanyId == model.CompanyId && s.StudentId != model.StudentId);
                    if (existingEmail != null)
                    {
                        return Json(new { success = false, message = "Email already exists in this company." });
                    }
                }

                student.CompanyId = model.CompanyId;
                student.SiteId = model.SiteId;
                student.StudentCode = model.StudentCode;
                student.FirstName = model.FirstName;
                student.LastName = model.LastName;
                student.DateOfBirth = model.DateOfBirth.HasValue ? DateOnly.FromDateTime(model.DateOfBirth.Value) : null;
                student.Gender = model.Gender;
                student.Phone = model.Phone;
                student.Email = model.Email;
                student.Address = model.Address;
                student.City = model.City;
                student.ParentName = model.ParentName;
                student.ParentPhone = model.ParentPhone;
                student.ParentEmail = model.ParentEmail;
                student.PhotoPath = model.PhotoPath;
                student.RegistrationDate = DateOnly.FromDateTime(model.RegistrationDate);
                student.Status = model.Status;
                student.CurrentGradeId = model.CurrentGradeId;
                student.AssignedTeacherId = model.AssignedTeacherId;
                student.Notes = model.Notes;
                student.IsActive = model.IsActive;
                student.UpdatedBy = GetCurrentUserId();
                student.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Student updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating student: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var student = await _context.Students.FindAsync(id);
                if (student == null)
                {
                    return Json(new { success = false, message = "Student not found." });
                }

                // Check if student has billing records
                var hasBilling = await _context.StudentBillings.AnyAsync(sb => sb.StudentId == id);
                if (hasBilling)
                {
                    return Json(new { success = false, message = "Cannot delete student who has billing records." });
                }

                // Check if student has examination records
                var hasExaminations = await _context.StudentExaminations.AnyAsync(se => se.StudentId == id);
                if (hasExaminations)
                {
                    return Json(new { success = false, message = "Cannot delete student who has examination records." });
                }

                // Check if student has grade history
                var hasGradeHistory = await _context.StudentGradeHistories.AnyAsync(sgh => sgh.StudentId == id);
                if (hasGradeHistory)
                {
                    return Json(new { success = false, message = "Cannot delete student who has grade history records." });
                }

                // Check if student has class schedules
                var hasSchedules = await _context.ClassSchedules.AnyAsync(cs => cs.StudentId == id && cs.Status != "Cancelled");
                if (hasSchedules)
                {
                    return Json(new { success = false, message = "Cannot delete student who has active class schedules." });
                }

                // Check if student has attendance records
                var hasAttendance = await _context.Attendances.AnyAsync(a => a.StudentId == id);
                if (hasAttendance)
                {
                    return Json(new { success = false, message = "Cannot delete student who has attendance records." });
                }

                // Soft delete - set IsActive to false
                student.IsActive = false;
                student.UpdatedBy = GetCurrentUserId();
                student.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Student deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting student: " + ex.Message });
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

        [HttpGet]
        public async Task<IActionResult> GetGradesByCompany(int companyId)
        {
            var grades = await _context.Grades
                .Where(g => g.CompanyId == companyId && g.IsActive)
                .Select(g => new { value = g.GradeId, text = g.GradeName })
                .OrderBy(g => g.text)
                .ToListAsync();

            return Json(grades);
        }

        [HttpGet]
        public async Task<IActionResult> GetTeachersBySite(int siteId)
        {
            var teachers = await _context.Teachers
                .Include(t => t.User)
                .Where(t => t.SiteId == siteId && t.IsActive)
                .Select(t => new {
                    TeacherId = t.TeacherId,
                    FirstName = t.User.FirstName,
                    LastName = t.User.LastName,
                    TeacherCode = t.TeacherCode
                })
                .ToListAsync();

            var result = teachers.Select(t => new {
                value = t.TeacherId,
                text = $"{t.FirstName} {t.LastName} ({t.TeacherCode})"
            }).OrderBy(t => t.text).ToList();

            return Json(result);
        }

        [HttpGet]
        //public async Task<IActionResult> GenerateStudentCode(int companyId, int siteId)
        //{
        //    try
        //    {
        //        var company = await _context.Companies.FindAsync(companyId);
        //        if (company == null)
        //            return Json(new { success = false, message = "Company not found." });

        //        var currentYear = DateTime.Now.Year;
        //        var currentMonth = DateTime.Now.Month;
        //        var yearMonth = $"{currentYear:00}{currentMonth:00}";

        //        // Get the last student code for this company and year-month
        //        var lastStudent = await _context.Students
        //            .Where(s => s.CompanyId == companyId && s.StudentCode.StartsWith($"{company.CompanyCode}-{yearMonth}"))
        //            .OrderByDescending(s => s.StudentCode)
        //            .FirstOrDefaultAsync();

        //        int nextSequence = 1;
        //        if (lastStudent != null)
        //        {
        //            var lastSequence = lastStudent.StudentCode.Substring(lastStudent.StudentCode.LastIndexOf('-') + 1);
        //            if (int.TryParse(lastSequence, out int seq))
        //            {
        //                nextSequence = seq + 1;
        //            }
        //        }

        //        var studentCode = $"{company.CompanyCode}-{yearMonth}{nextSequence:00000}";
        //        return Json(new { success = true, studentCode = studentCode });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Error generating student code: " + ex.Message });
        //    }
        //}

        public async Task<IActionResult> GenerateStudentCode(int companyId, int siteId)
        {
            try
            {
                var company = await _context.Companies.FindAsync(companyId);
                if (company == null)
                    return Json(new { success = false, message = "Company not found." });

                var currentYear = DateTime.Now.Year;
                var currentMonth = DateTime.Now.Month;
                var yearMonth = $"{currentYear}{currentMonth:00}";

                // Get the last student code for this company and year-month
                var lastStudent = await _context.Students
                    .Where(s => s.CompanyId == companyId && s.StudentCode.StartsWith($"{company.CompanyCode}-{yearMonth}"))
                    .OrderByDescending(s => s.StudentCode)
                    .FirstOrDefaultAsync();

                int nextSequence = 1;
                if (lastStudent != null)
                {
                    var lastSequencePart = lastStudent.StudentCode.Substring(lastStudent.StudentCode.Length - 5);
                    if (int.TryParse(lastSequencePart, out int seq))
                    {
                        nextSequence = seq + 1;
                    }
                }

                var studentCode = $"{company.CompanyCode}-{yearMonth}{nextSequence:00000}";
                return Json(new { success = true, studentCode });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error generating student code: " + ex.Message });
            }
        }
    }
}
