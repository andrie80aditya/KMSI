using KMSI.Data;
using KMSI.Models.ViewModels;
using KMSI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMSI.Controllers
{
    [Authorize(Policy = "BranchManagerAndAbove")]
    public class CertificateController : BaseController
    {
        public CertificateController(KMSIDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            var companyId = GetCurrentCompanyId();
            var siteId = GetCurrentSiteId();

            var certificatesQuery = _context.Certificates
                .Include(c => c.Student)
                .Include(c => c.Grade)
                .Include(c => c.StudentExamination)
                .ThenInclude(se => se.Examination)
                .ThenInclude(e => e.Site)
                .Where(c => c.Student.CompanyId == companyId);

            // Filter by site if user is not HO Admin or above
            if (!IsHOAdmin() && siteId.HasValue)
            {
                certificatesQuery = certificatesQuery.Where(c => c.StudentExamination.Examination.SiteId == siteId.Value);
            }

            var certificates = await certificatesQuery
                .OrderByDescending(c => c.IssueDate)
                .ThenBy(c => c.CertificateNumber)
                .ToListAsync();

            ViewData["Breadcrumb"] = "Certificate Management";
            return View(certificates);
        }

        [HttpGet]
        public async Task<IActionResult> GetCertificate(int id)
        {
            var certificate = await _context.Certificates
                .Include(c => c.Student)
                .Include(c => c.Grade)
                .Include(c => c.StudentExamination)
                .ThenInclude(se => se.Examination)
                .FirstOrDefaultAsync(c => c.CertificateId == id);

            if (certificate == null)
                return NotFound();

            return Json(new
            {
                certificateId = certificate.CertificateId,
                studentId = certificate.StudentId,
                studentExaminationId = certificate.StudentExaminationId,
                certificateNumber = certificate.CertificateNumber,
                gradeId = certificate.GradeId,
                issueDate = certificate.IssueDate.ToString("yyyy-MM-dd"),
                certificateTitle = certificate.CertificateTitle,
                issuedBy = certificate.IssuedBy,
                signedBy = certificate.SignedBy,
                status = certificate.Status,
                notes = certificate.Notes,
                studentName = $"{certificate.Student?.FirstName} {certificate.Student?.LastName}",
                studentCode = certificate.Student?.StudentCode,
                gradeName = certificate.Grade?.GradeName,
                examScore = certificate.StudentExamination?.Score,
                examResult = certificate.StudentExamination?.Result
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetEligibleStudents()
        {
            var companyId = GetCurrentCompanyId();
            var siteId = GetCurrentSiteId();

            var eligibleStudentsQuery = _context.StudentExaminations
                .Include(se => se.Student)
                .Include(se => se.Examination)
                .ThenInclude(e => e.Grade)
                .Include(se => se.Examination)
                .ThenInclude(e => e.Site)
                .Where(se => se.Student.CompanyId == companyId
                    && se.Result == "Pass"
                    && se.Score.HasValue
                    && !_context.Certificates.Any(c => c.StudentExaminationId == se.StudentExaminationId));

            // Filter by site if user is not HO Admin or above
            if (!IsHOAdmin() && siteId.HasValue)
            {
                eligibleStudentsQuery = eligibleStudentsQuery.Where(se => se.Examination.SiteId == siteId.Value);
            }

            var eligibleStudents = await eligibleStudentsQuery
                .OrderBy(se => se.Student.FirstName)
                .ThenBy(se => se.Student.LastName)
                .Select(se => new
                {
                    studentExaminationId = se.StudentExaminationId,
                    studentName = $"{se.Student.FirstName} {se.Student.LastName}",
                    studentCode = se.Student.StudentCode,
                    gradeName = se.Examination.Grade.GradeName,
                    examName = se.Examination.ExamName,
                    examDate = se.Examination.ExamDate.ToString("dd/MM/yyyy"),
                    score = se.Score,
                    maxScore = se.MaxScore,
                    result = se.Result
                })
                .ToListAsync();

            return Json(eligibleStudents);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateCertificate(GenerateCertificateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Please fill all required fields." });
                }

                // Get student examination details
                var studentExamination = await _context.StudentExaminations
                    .Include(se => se.Student)
                    .Include(se => se.Examination)
                    .ThenInclude(e => e.Grade)
                    .FirstOrDefaultAsync(se => se.StudentExaminationId == model.StudentExaminationId);

                if (studentExamination == null)
                {
                    return Json(new { success = false, message = "Student examination not found." });
                }

                if (studentExamination.Result != "Pass")
                {
                    return Json(new { success = false, message = "Certificate can only be generated for passed students." });
                }

                // Check if certificate already exists
                var existingCertificate = await _context.Certificates
                    .FirstOrDefaultAsync(c => c.StudentExaminationId == model.StudentExaminationId);
                if (existingCertificate != null)
                {
                    return Json(new { success = false, message = "Certificate already exists for this student examination." });
                }

                // Generate certificate number
                string certificateNumber;
                if (model.AutoGenerateNumber)
                {
                    var companyId = studentExamination.Student.CompanyId;
                    var year = DateTime.Now.Year.ToString().Substring(2);
                    var month = DateTime.Now.Month.ToString("00");

                    var lastCertificate = await _context.Certificates
                        .Where(c => c.Student.CompanyId == companyId
                            && c.CertificateNumber.StartsWith($"CERT-{year}{month}"))
                        .OrderByDescending(c => c.CertificateNumber)
                        .FirstOrDefaultAsync();

                    int sequence = 1;
                    if (lastCertificate != null)
                    {
                        var lastSequence = lastCertificate.CertificateNumber.Substring(9);
                        if (int.TryParse(lastSequence, out int lastSeq))
                        {
                            sequence = lastSeq + 1;
                        }
                    }

                    certificateNumber = $"CERT-{year}{month}{sequence:000}";
                }
                else
                {
                    certificateNumber = model.CustomCertificateNumber;

                    // Check if custom number already exists
                    var existingNumber = await _context.Certificates
                        .FirstOrDefaultAsync(c => c.CertificateNumber == certificateNumber);
                    if (existingNumber != null)
                    {
                        return Json(new { success = false, message = "Certificate number already exists." });
                    }
                }

                var certificate = new Certificate
                {
                    StudentId = studentExamination.StudentId,
                    StudentExaminationId = model.StudentExaminationId,
                    CertificateNumber = certificateNumber,
                    GradeId = studentExamination.Examination.GradeId,
                    IssueDate = DateOnly.FromDateTime(DateTime.Today),
                    CertificateTitle = model.CertificateTitle,
                    IssuedBy = model.IssuedBy,
                    SignedBy = model.SignedBy,
                    Status = "Issued",
                    Notes = model.Notes,
                    PrintCount = 0,
                    CreatedBy = GetCurrentUserId(),
                    CreatedDate = DateTime.Now
                };

                _context.Certificates.Add(certificate);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Certificate generated successfully.", certificateNumber = certificateNumber });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error generating certificate: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(CertificateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Please fill all required fields." });
                }

                var certificate = await _context.Certificates.FindAsync(model.CertificateId);
                if (certificate == null)
                {
                    return Json(new { success = false, message = "Certificate not found." });
                }

                // Check if certificate number already exists (excluding current certificate)
                var existingCertificate = await _context.Certificates
                    .FirstOrDefaultAsync(c => c.CertificateNumber == model.CertificateNumber
                        && c.CertificateId != model.CertificateId);
                if (existingCertificate != null)
                {
                    return Json(new { success = false, message = "Certificate number already exists." });
                }

                certificate.CertificateNumber = model.CertificateNumber;
                certificate.IssueDate = DateOnly.FromDateTime(model.IssueDate);
                certificate.CertificateTitle = model.CertificateTitle;
                certificate.IssuedBy = model.IssuedBy;
                certificate.SignedBy = model.SignedBy;
                certificate.Status = model.Status;
                certificate.Notes = model.Notes;
                certificate.UpdatedBy = GetCurrentUserId();
                certificate.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Certificate updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating certificate: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var certificate = await _context.Certificates.FindAsync(id);
                if (certificate == null)
                {
                    return Json(new { success = false, message = "Certificate not found." });
                }

                // Only allow deletion if not printed or minimal print count
                if (certificate.PrintCount > 5)
                {
                    return Json(new { success = false, message = "Cannot delete certificate that has been printed multiple times." });
                }

                // Hard delete
                _context.Certificates.Remove(certificate);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Certificate deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting certificate: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> PrintCertificate(int id)
        {
            try
            {
                var certificate = await _context.Certificates
                    .Include(c => c.Student)
                    .Include(c => c.Grade)
                    .Include(c => c.StudentExamination)
                    .ThenInclude(se => se.Examination)
                    .FirstOrDefaultAsync(c => c.CertificateId == id);

                if (certificate == null)
                {
                    return Json(new { success = false, message = "Certificate not found." });
                }

                // Update print count
                certificate.PrintCount++;
                certificate.LastPrintDate = DateTime.Now;
                certificate.UpdatedBy = GetCurrentUserId();
                certificate.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                // In a real application, you would generate a PDF here
                // For now, we'll just return success with certificate data
                var certificateData = new
                {
                    certificateNumber = certificate.CertificateNumber,
                    studentName = $"{certificate.Student?.FirstName} {certificate.Student?.LastName}",
                    studentCode = certificate.Student?.StudentCode,
                    gradeName = certificate.Grade?.GradeName,
                    certificateTitle = certificate.CertificateTitle,
                    issueDate = certificate.IssueDate.ToString("dd MMMM yyyy"),
                    issuedBy = certificate.IssuedBy,
                    signedBy = certificate.SignedBy,
                    examName = certificate.StudentExamination?.Examination?.ExamName,
                    examDate = certificate.StudentExamination?.Examination?.ExamDate.ToString("dd MMMM yyyy"),
                    score = certificate.StudentExamination?.Score,
                    maxScore = certificate.StudentExamination?.MaxScore
                };

                return Json(new
                {
                    success = true,
                    message = $"Certificate printed successfully. Print count: {certificate.PrintCount}",
                    data = certificateData
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error printing certificate: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int id, string status)
        {
            try
            {
                var certificate = await _context.Certificates.FindAsync(id);
                if (certificate == null)
                {
                    return Json(new { success = false, message = "Certificate not found." });
                }

                certificate.Status = status;
                certificate.UpdatedBy = GetCurrentUserId();
                certificate.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Certificate status changed to {status}." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error changing status: " + ex.Message });
            }
        }
    }
}
