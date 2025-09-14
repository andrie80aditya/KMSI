using KMSI.Data;
using KMSI.Models.ViewModels;
using KMSI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMSI.Controllers
{
    [Authorize(Policy = "BranchManagerAndAbove")]
    public class RegistrationController : BaseController
    {
        public RegistrationController(KMSIDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            var companyId = GetCurrentCompanyId();
            var siteId = GetCurrentSiteId();
            var userLevel = GetCurrentUserLevel();

            var registrationsQuery = _context.Registrations
                .Include(r => r.Company)
                .Include(r => r.Site)
                .Include(r => r.Student)
                .Include(r => r.AssignedTeacher)
                .ThenInclude(t => t.User)
                .Include(r => r.RequestedGrade)
                .Where(r => r.CompanyId == companyId);

            // Filter by site if user is not HO Admin or above
            if (!IsHOAdmin() && siteId.HasValue)
            {
                registrationsQuery = registrationsQuery.Where(r => r.SiteId == siteId.Value);
            }

            var registrations = await registrationsQuery
                .OrderByDescending(r => r.RegistrationDate)
                .ToListAsync();

            ViewBag.Companies = await _context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            ViewBag.Sites = await _context.Sites
                .Where(s => s.CompanyId == companyId && s.IsActive)
                .OrderBy(s => s.SiteName)
                .ToListAsync();

            ViewBag.Students = await _context.Students
                .Where(s => s.CompanyId == companyId && s.IsActive)
                .OrderBy(s => s.FirstName)
                .ToListAsync();

            ViewBag.Grades = await _context.Grades
                .Where(g => g.CompanyId == companyId && g.IsActive)
                .OrderBy(g => g.SortOrder ?? int.MaxValue)
                .ThenBy(g => g.GradeName)
                .ToListAsync();

            ViewBag.Teachers = await _context.Teachers
                .Include(t => t.User)
                .Where(t => t.CompanyId == companyId && t.IsActive && t.IsAvailableForTrial)
                .OrderBy(t => t.User.FirstName)
                .ToListAsync();

            ViewData["Breadcrumb"] = "Registration Management";
            return View(registrations);
        }

        [HttpGet]
        public async Task<IActionResult> GetRegistration(int id)
        {
            var registration = await _context.Registrations
                .Include(r => r.Company)
                .Include(r => r.Site)
                .Include(r => r.Student)
                .Include(r => r.AssignedTeacher)
                .Include(r => r.RequestedGrade)
                .FirstOrDefaultAsync(r => r.RegistrationId == id);

            if (registration == null)
                return NotFound();

            return Json(new
            {
                registrationId = registration.RegistrationId,
                companyId = registration.CompanyId,
                siteId = registration.SiteId,
                studentId = registration.StudentId,
                registrationCode = registration.RegistrationCode,
                registrationDate = registration.RegistrationDate.ToString("yyyy-MM-dd"),
                trialDate = registration.TrialDate?.ToString("yyyy-MM-dd"),
                trialTime = registration.TrialTime?.ToString(@"hh\:mm"),
                assignedTeacherId = registration.AssignedTeacherId,
                requestedGradeId = registration.RequestedGradeId,
                status = registration.Status,
                trialResult = registration.TrialResult,
                confirmationDate = registration.ConfirmationDate?.ToString("yyyy-MM-dd"),
                paymentStatus = registration.PaymentStatus,
                paymentAmount = registration.PaymentAmount,
                notes = registration.Notes,
                studentFirstName = registration.Student?.FirstName,
                studentLastName = registration.Student?.LastName,
                studentPhone = registration.Student?.Phone,
                studentEmail = registration.Student?.Email,
                parentName = registration.Student?.ParentName,
                parentPhone = registration.Student?.ParentPhone,
                parentEmail = registration.Student?.ParentEmail
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(RegistrationViewModel model)
        {
            try
            {
                // Check if registration code already exists in the same company
                var existingRegistration = await _context.Registrations
                    .FirstOrDefaultAsync(r => r.RegistrationCode == model.RegistrationCode && r.CompanyId == model.CompanyId);
                if (existingRegistration != null)
                {
                    return Json(new { success = false, message = "Registration code already exists in this company." });
                }

                var registration = new Registration
                {
                    CompanyId = model.CompanyId,
                    SiteId = model.SiteId,
                    StudentId = model.StudentId,
                    RegistrationCode = model.RegistrationCode,
                    RegistrationDate = DateOnly.FromDateTime(model.RegistrationDate),
                    TrialDate = model.TrialDate.HasValue ? DateOnly.FromDateTime(model.TrialDate.Value) : null,
                    TrialTime = model.TrialTime.HasValue ? TimeOnly.FromTimeSpan(model.TrialTime.Value) : null,
                    AssignedTeacherId = model.AssignedTeacherId,
                    RequestedGradeId = model.RequestedGradeId,
                    Status = model.Status,
                    TrialResult = model.TrialResult,
                    ConfirmationDate = model.ConfirmationDate.HasValue ? DateOnly.FromDateTime(model.ConfirmationDate.Value) : null,
                    PaymentStatus = model.PaymentStatus,
                    PaymentAmount = model.PaymentAmount,
                    Notes = model.Notes,
                    CreatedBy = GetCurrentUserId(),
                    CreatedDate = DateTime.Now
                };

                _context.Registrations.Add(registration);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Registration created successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating registration: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(RegistrationViewModel model)
        {
            try
            {
                var registration = await _context.Registrations.FindAsync(model.RegistrationId);
                if (registration == null)
                {
                    return Json(new { success = false, message = "Registration not found." });
                }

                // Check if registration code already exists in the same company (excluding current registration)
                var existingRegistration = await _context.Registrations
                    .FirstOrDefaultAsync(r => r.RegistrationCode == model.RegistrationCode && r.CompanyId == model.CompanyId && r.RegistrationId != model.RegistrationId);
                if (existingRegistration != null)
                {
                    return Json(new { success = false, message = "Registration code already exists in this company." });
                }

                registration.CompanyId = model.CompanyId;
                registration.SiteId = model.SiteId;
                registration.StudentId = model.StudentId;
                registration.RegistrationCode = model.RegistrationCode;
                registration.RegistrationDate = DateOnly.FromDateTime(model.RegistrationDate);
                registration.TrialDate = model.TrialDate.HasValue ? DateOnly.FromDateTime(model.TrialDate.Value) : null;
                registration.TrialTime = model.TrialTime.HasValue ? TimeOnly.FromTimeSpan(model.TrialTime.Value) : null;
                registration.AssignedTeacherId = model.AssignedTeacherId;
                registration.RequestedGradeId = model.RequestedGradeId;
                registration.Status = model.Status;
                registration.TrialResult = model.TrialResult;
                registration.ConfirmationDate = model.ConfirmationDate.HasValue ? DateOnly.FromDateTime(model.ConfirmationDate.Value) : null;
                registration.PaymentStatus = model.PaymentStatus;
                registration.PaymentAmount = model.PaymentAmount;
                registration.Notes = model.Notes;
                registration.UpdatedBy = GetCurrentUserId();
                registration.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Registration updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating registration: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var registration = await _context.Registrations.FindAsync(id);
                if (registration == null)
                {
                    return Json(new { success = false, message = "Registration not found." });
                }

                // Check if registration is in progress or confirmed
                if (registration.Status == "Confirmed" || registration.PaymentStatus == "Paid")
                {
                    return Json(new { success = false, message = "Cannot delete confirmed or paid registration." });
                }

                // Hard delete for registrations (or soft delete based on business rules)
                _context.Registrations.Remove(registration);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Registration deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting registration: " + ex.Message });
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
        public async Task<IActionResult> GetStudentsByCompany(int companyId)
        {
            var students = await _context.Students
                .Where(s => s.CompanyId == companyId && s.IsActive)
                .Select(s => new {
                    StudentId = s.StudentId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    StudentCode = s.StudentCode,
                    Phone = s.Phone,
                    Email = s.Email,
                    ParentName = s.ParentName,
                    ParentPhone = s.ParentPhone,
                    ParentEmail = s.ParentEmail
                })
                .ToListAsync();

            var result = students.Select(s => new {
                value = s.StudentId,
                text = $"{s.FirstName} {s.LastName} ({s.StudentCode})",
                phone = s.Phone,
                email = s.Email,
                parentName = s.ParentName,
                parentPhone = s.ParentPhone,
                parentEmail = s.ParentEmail
            }).OrderBy(s => s.text).ToList();

            return Json(result);
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
                .Where(t => t.SiteId == siteId && t.IsActive && t.IsAvailableForTrial)
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
        public async Task<IActionResult> GenerateRegistrationCode(int companyId, int siteId)
        {
            try
            {
                var company = await _context.Companies.FindAsync(companyId);
                if (company == null)
                    return Json(new { success = false, message = "Company not found." });

                var currentDate = DateTime.Now;
                var yearMonth = $"{currentDate.Year:0000}{currentDate.Month:00}";

                // Get the last registration code for this company and year-month
                var lastRegistration = await _context.Registrations
                    .Where(r => r.CompanyId == companyId && r.RegistrationCode.StartsWith($"REG-{company.CompanyCode}-{yearMonth}"))
                    .OrderByDescending(r => r.RegistrationCode)
                    .FirstOrDefaultAsync();

                int nextSequence = 1;
                if (lastRegistration != null)
                {
                    var lastSequence = lastRegistration.RegistrationCode.Substring(lastRegistration.RegistrationCode.LastIndexOf('-') + 1);
                    if (int.TryParse(lastSequence, out int seq))
                    {
                        nextSequence = seq + 1;
                    }
                }

                var registrationCode = $"REG-{company.CompanyCode}-{yearMonth}-{nextSequence:0000}";
                return Json(new { success = true, registrationCode = registrationCode });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error generating registration code: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmRegistration(int id)
        {
            try
            {
                var registration = await _context.Registrations
                    .Include(r => r.Student)
                    .FirstOrDefaultAsync(r => r.RegistrationId == id);

                if (registration == null)
                {
                    return Json(new { success = false, message = "Registration not found." });
                }

                if (registration.Status == "Confirmed")
                {
                    return Json(new { success = false, message = "Registration is already confirmed." });
                }

                // Update registration status
                registration.Status = "Confirmed";
                registration.ConfirmationDate = DateOnly.FromDateTime(DateTime.Now);
                registration.UpdatedBy = GetCurrentUserId();
                registration.UpdatedDate = DateTime.Now;

                // Update student status to Active if payment is completed
                if (registration.PaymentStatus == "Paid" && registration.Student != null)
                {
                    registration.Student.Status = "Active";
                    registration.Student.CurrentGradeId = registration.RequestedGradeId;
                    registration.Student.AssignedTeacherId = registration.AssignedTeacherId;
                    registration.Student.UpdatedBy = GetCurrentUserId();
                    registration.Student.UpdatedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Registration confirmed successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error confirming registration: " + ex.Message });
            }
        }
    }
}
