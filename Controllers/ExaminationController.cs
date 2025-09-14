using KMSI.Data;
using KMSI.Models.ViewModels;
using KMSI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMSI.Controllers
{
    [Authorize(Policy = "BranchManagerAndAbove")]
    public class ExaminationController : BaseController
    {
        public ExaminationController(KMSIDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            var companyId = GetCurrentCompanyId();
            var siteId = GetCurrentSiteId();
            var userLevel = GetCurrentUserLevel();

            var examinationsQuery = _context.Examinations
                .Include(e => e.Company)
                .Include(e => e.Site)
                .Include(e => e.Grade)
                .Include(e => e.ExaminerTeacher)
                .ThenInclude(t => t.User)
                .Include(e => e.StudentExaminations)
                .Where(e => e.CompanyId == companyId);

            // Filter by site if user is not HO Admin or above
            if (!IsHOAdmin() && siteId.HasValue)
            {
                examinationsQuery = examinationsQuery.Where(e => e.SiteId == siteId.Value);
            }

            var examinations = await examinationsQuery
                .OrderByDescending(e => e.ExamDate)
                .ThenBy(e => e.StartTime)
                .ToListAsync();

            // Get data for dropdowns
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
                .OrderBy(g => g.SortOrder ?? 0)
                .ThenBy(g => g.GradeName)
                .ToListAsync();

            ViewBag.Teachers = await _context.Teachers
                .Include(t => t.User)
                .Where(t => t.CompanyId == companyId && t.IsActive)
                .OrderBy(t => t.User.FirstName)
                .ToListAsync();

            ViewData["Breadcrumb"] = "Examination Management";
            return View(examinations);
        }

        [HttpGet]
        public async Task<IActionResult> GetExamination(int id)
        {
            var examination = await _context.Examinations
                .Include(e => e.Company)
                .Include(e => e.Site)
                .Include(e => e.Grade)
                .Include(e => e.ExaminerTeacher)
                .FirstOrDefaultAsync(e => e.ExaminationId == id);

            if (examination == null)
                return NotFound();

            return Json(new
            {
                examinationId = examination.ExaminationId,
                companyId = examination.CompanyId,
                siteId = examination.SiteId,
                examCode = examination.ExamCode,
                examName = examination.ExamName,
                gradeId = examination.GradeId,
                examDate = examination.ExamDate.ToString("yyyy-MM-dd"),
                startTime = examination.StartTime.ToString(@"hh\:mm"),
                endTime = examination.EndTime.ToString(@"hh\:mm"),
                location = examination.Location,
                examinerTeacherId = examination.ExaminerTeacherId,
                maxCapacity = examination.MaxCapacity,
                status = examination.Status,
                description = examination.Description
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(ExaminationViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Please fill all required fields." });
                }

                // Check if exam code already exists
                var existingExam = await _context.Examinations
                    .FirstOrDefaultAsync(e => e.ExamCode == model.ExamCode && e.CompanyId == model.CompanyId);
                if (existingExam != null)
                {
                    return Json(new { success = false, message = "Exam code already exists." });
                }

                var examination = new Examination
                {
                    CompanyId = model.CompanyId,
                    SiteId = model.SiteId,
                    ExamCode = model.ExamCode,
                    ExamName = model.ExamName,
                    GradeId = model.GradeId,
                    ExamDate = DateOnly.FromDateTime(model.ExamDate),
                    StartTime = TimeOnly.FromTimeSpan(model.StartTime),
                    EndTime = TimeOnly.FromTimeSpan(model.EndTime),
                    Location = model.Location,
                    ExaminerTeacherId = model.ExaminerTeacherId,
                    MaxCapacity = model.MaxCapacity,
                    Status = model.Status,
                    Description = model.Description,
                    CreatedBy = GetCurrentUserId(),
                    CreatedDate = DateTime.Now
                };

                _context.Examinations.Add(examination);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Examination created successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating examination: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(ExaminationViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Please fill all required fields." });
                }

                var examination = await _context.Examinations.FindAsync(model.ExaminationId);
                if (examination == null)
                {
                    return Json(new { success = false, message = "Examination not found." });
                }

                // Validate SiteId exists and belongs to CompanyId
                var siteExists = await _context.Sites
                    .AnyAsync(s => s.SiteId == model.SiteId
                        && s.CompanyId == model.CompanyId
                        && s.IsActive);
                if (!siteExists)
                {
                    return Json(new { success = false, message = "Invalid site selected for this company." });
                }

                // Validate GradeId exists and belongs to CompanyId  
                var gradeExists = await _context.Grades
                    .AnyAsync(g => g.GradeId == model.GradeId
                        && g.CompanyId == model.CompanyId
                        && g.IsActive);
                if (!gradeExists)
                {
                    return Json(new { success = false, message = "Invalid grade selected for this company." });
                }

                // Validate TeacherId exists and belongs to CompanyId
                var teacherExists = await _context.Teachers
                    .AnyAsync(t => t.TeacherId == model.ExaminerTeacherId
                        && t.CompanyId == model.CompanyId
                        && t.IsActive);
                if (!teacherExists)
                {
                    return Json(new { success = false, message = "Invalid teacher selected for this company." });
                }

                // Check if exam code already exists (excluding current exam)
                var existingExam = await _context.Examinations
                    .FirstOrDefaultAsync(e => e.ExamCode == model.ExamCode
                        && e.CompanyId == model.CompanyId
                        && e.ExaminationId != model.ExaminationId);
                if (existingExam != null)
                {
                    return Json(new { success = false, message = "Exam code already exists." });
                }

                // Update examination
                examination.CompanyId = model.CompanyId;
                examination.SiteId = model.SiteId;
                examination.ExamCode = model.ExamCode;
                examination.ExamName = model.ExamName;
                examination.GradeId = model.GradeId;
                examination.ExamDate = DateOnly.FromDateTime(model.ExamDate);
                examination.StartTime = TimeOnly.FromTimeSpan(model.StartTime);
                examination.EndTime = TimeOnly.FromTimeSpan(model.EndTime);
                examination.Location = model.Location;
                examination.ExaminerTeacherId = model.ExaminerTeacherId;
                examination.MaxCapacity = model.MaxCapacity;
                examination.Status = model.Status;
                examination.Description = model.Description;
                examination.UpdatedBy = GetCurrentUserId();
                examination.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Examination updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating examination: " + ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var examination = await _context.Examinations
                    .Include(e => e.StudentExaminations)
                    .FirstOrDefaultAsync(e => e.ExaminationId == id);
                if (examination == null)
                {
                    return Json(new { success = false, message = "Examination not found." });
                }

                // Check if there are student registrations
                if (examination.StudentExaminations != null && examination.StudentExaminations.Any())
                {
                    return Json(new { success = false, message = "Cannot delete examination with student registrations." });
                }

                // Hard delete if no registrations
                _context.Examinations.Remove(examination);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Examination deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting examination: " + ex.Message });
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
        public async Task<IActionResult> GetTeachersByCompany(int companyId)
        {
            var teachers = await _context.Teachers
                .Include(t => t.User)
                .Where(t => t.CompanyId == companyId && t.IsActive)
                .ToListAsync();

            var result = teachers.Select(t => new {
                value = t.TeacherId,
                text = $"{t.User.FirstName} {t.User.LastName} ({t.TeacherCode})"
            })
            .OrderBy(t => t.text)
            .ToList();

            return Json(result);
        }

        // Student Registration for Examination
        [HttpGet]
        public async Task<IActionResult> ManageStudents(int examinationId)
        {
            var examination = await _context.Examinations
                .Include(e => e.Grade)
                .Include(e => e.Site)
                .Include(e => e.StudentExaminations)
                .ThenInclude(se => se.Student)
                .FirstOrDefaultAsync(e => e.ExaminationId == examinationId);

            if (examination == null)
                return NotFound();

            // Get available students for this grade
            var availableStudents = await _context.Students
                .Where(s => s.CompanyId == examination.CompanyId
                    && s.SiteId == examination.SiteId
                    && s.CurrentGradeId == examination.GradeId
                    && s.IsActive
                    && s.Status == "Active"
                    && !_context.StudentExaminations.Any(se => se.ExaminationId == examinationId && se.StudentId == s.StudentId))
                .OrderBy(s => s.FirstName)
                .ThenBy(s => s.LastName)
                .ToListAsync();

            ViewBag.Examination = examination;
            ViewBag.AvailableStudents = availableStudents;
            ViewBag.RegisteredStudents = examination.StudentExaminations?.ToList() ?? new List<StudentExamination>();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterStudent(int examinationId, int studentId)
        {
            try
            {
                // Check if student is already registered
                var existingRegistration = await _context.StudentExaminations
                    .FirstOrDefaultAsync(se => se.ExaminationId == examinationId && se.StudentId == studentId);

                if (existingRegistration != null)
                {
                    return Json(new { success = false, message = "Student is already registered for this examination." });
                }

                // Check exam capacity
                var examination = await _context.Examinations
                    .Include(e => e.StudentExaminations)
                    .FirstOrDefaultAsync(e => e.ExaminationId == examinationId);

                if (examination == null)
                {
                    return Json(new { success = false, message = "Examination not found." });
                }

                var currentRegistrations = examination.StudentExaminations?.Count ?? 0;
                if (currentRegistrations >= examination.MaxCapacity)
                {
                    return Json(new { success = false, message = "Examination has reached maximum capacity." });
                }

                var studentExamination = new StudentExamination
                {
                    ExaminationId = examinationId,
                    StudentId = studentId,
                    RegistrationDate = DateTime.Now,
                    CreatedBy = GetCurrentUserId(),
                    CreatedDate = DateTime.Now
                };

                _context.StudentExaminations.Add(studentExamination);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Student registered successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error registering student: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UnregisterStudent(int examinationId, int studentId)
        {
            try
            {
                var studentExamination = await _context.StudentExaminations
                    .FirstOrDefaultAsync(se => se.ExaminationId == examinationId && se.StudentId == studentId);

                if (studentExamination == null)
                {
                    return Json(new { success = false, message = "Student registration not found." });
                }

                // Don't allow unregister if exam has been taken (has score)
                if (studentExamination.Score.HasValue)
                {
                    return Json(new { success = false, message = "Cannot unregister student who has taken the exam." });
                }

                _context.StudentExaminations.Remove(studentExamination);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Student unregistered successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error unregistering student: " + ex.Message });
            }
        }

        // Exam Result Entry
        [HttpGet]
        public async Task<IActionResult> EnterResults(int examinationId)
        {
            var examination = await _context.Examinations
                .Include(e => e.Grade)
                .Include(e => e.Site)
                .Include(e => e.ExaminerTeacher)
                .ThenInclude(t => t.User)
                .Include(e => e.StudentExaminations)
                .ThenInclude(se => se.Student)
                .FirstOrDefaultAsync(e => e.ExaminationId == examinationId);

            if (examination == null)
                return NotFound();

            ViewBag.Examination = examination;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveResult([FromBody] StudentExaminationViewModel model)
        {
            try
            {
                var studentExamination = await _context.StudentExaminations
                    .FirstOrDefaultAsync(se => se.StudentExaminationId == model.StudentExaminationId);

                if (studentExamination == null)
                {
                    return Json(new { success = false, message = "Student examination not found." });
                }

                studentExamination.AttendanceStatus = model.AttendanceStatus;
                studentExamination.StartTime = model.StartTime.HasValue ? TimeOnly.FromTimeSpan(model.StartTime.Value) : null;
                studentExamination.EndTime = model.EndTime.HasValue ? TimeOnly.FromTimeSpan(model.EndTime.Value) : null;
                studentExamination.Score = model.Score;
                studentExamination.MaxScore = model.MaxScore;
                studentExamination.Grade = model.Grade;
                studentExamination.Result = model.Result;
                studentExamination.TeacherNotes = model.TeacherNotes;
                studentExamination.UpdatedBy = GetCurrentUserId();
                studentExamination.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Exam result saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving result: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int id, string status)
        {
            try
            {
                var examination = await _context.Examinations.FindAsync(id);
                if (examination == null)
                {
                    return Json(new { success = false, message = "Examination not found." });
                }

                examination.Status = status;
                examination.UpdatedBy = GetCurrentUserId();
                examination.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Examination status changed to {status}." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error changing status: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentExamResult(int id)
        {
            var studentExam = await _context.StudentExaminations
                .Include(se => se.Student)
                .Include(se => se.Examination)
                .FirstOrDefaultAsync(se => se.StudentExaminationId == id);

            if (studentExam == null)
            {
                return Json(new { success = false, message = "Student exam result not found." });
            }

            var result = new
            {
                studentName = $"{studentExam.Student?.FirstName} {studentExam.Student?.LastName}",
                attendanceStatus = studentExam.AttendanceStatus,
                startTime = studentExam.StartTime?.ToString(@"HH\:mm"),
                endTime = studentExam.EndTime?.ToString(@"HH\:mm"),
                score = studentExam.Score,
                maxScore = studentExam.MaxScore,
                grade = studentExam.Grade,
                result = studentExam.Result,
                teacherNotes = studentExam.TeacherNotes
            };

            return Json(new { success = true, data = result });
        }
    }
}
