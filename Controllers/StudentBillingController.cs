using KMSI.Data;
using KMSI.Models.ViewModels;
using KMSI.Models;
using KMSI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMSI.Controllers
{
    [Authorize(Policy = "BranchManagerAndAbove")]
    public class StudentBillingController : BaseController
    {
        private readonly IPdfService _pdfService;
        private readonly IEmailService _emailService;

        public StudentBillingController(KMSIDbContext context, IPdfService pdfService, IEmailService emailService) : base(context)
        {
            _pdfService = pdfService;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            var companyId = GetCurrentCompanyId();
            var siteId = GetCurrentSiteId();

            var billingsQuery = _context.StudentBillings
                .Include(sb => sb.Student)
                .Include(sb => sb.Grade)
                .Include(sb => sb.Site)
                .Include(sb => sb.BillingPeriod)
                .Where(sb => sb.CompanyId == companyId);

            // Filter by site if user is not HO Admin or above
            if (!IsHOAdmin() && siteId.HasValue)
            {
                billingsQuery = billingsQuery.Where(sb => sb.SiteId == siteId.Value);
            }

            var billings = await billingsQuery
                .OrderByDescending(sb => sb.BillingDate)
                .ThenBy(sb => sb.BillingNumber)
                .ToListAsync();

            ViewData["Breadcrumb"] = "Student Billing";
            return View(billings);
        }

        [HttpGet]
        public async Task<IActionResult> GetBilling(int id)
        {
            var billing = await _context.StudentBillings
                .Include(sb => sb.Student)
                .Include(sb => sb.Grade)
                .Include(sb => sb.Site)
                .Include(sb => sb.BillingPeriod)
                .FirstOrDefaultAsync(sb => sb.BillingId == id);

            if (billing == null)
                return NotFound();

            return Json(new
            {
                billingId = billing.BillingId,
                companyId = billing.CompanyId,
                siteId = billing.SiteId,
                billingPeriodId = billing.BillingPeriodId,
                studentId = billing.StudentId,
                billingNumber = billing.BillingNumber,
                billingDate = billing.BillingDate.ToString("yyyy-MM-dd"),
                dueDate = billing.DueDate.ToString("yyyy-MM-dd"),
                gradeId = billing.GradeId,
                tuitionFee = billing.TuitionFee,
                bookFees = billing.BookFees ?? 0,
                otherFees = billing.OtherFees ?? 0,
                discount = billing.Discount ?? 0,
                tax = billing.Tax ?? 0,
                status = billing.Status,
                paymentDate = billing.PaymentDate?.ToString("yyyy-MM-dd"),
                paymentAmount = billing.PaymentAmount,
                paymentMethod = billing.PaymentMethod,
                paymentReference = billing.PaymentReference,
                notes = billing.Notes,
                studentName = $"{billing.Student?.FirstName} {billing.Student?.LastName}",
                studentCode = billing.Student?.StudentCode,
                gradeName = billing.Grade?.GradeName,
                siteName = billing.Site?.SiteName,
                totalAmount = billing.TuitionFee + (billing.BookFees ?? 0) + (billing.OtherFees ?? 0) - (billing.Discount ?? 0) + (billing.Tax ?? 0)
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetBillingPeriods()
        {
            var companyId = GetCurrentCompanyId();

            var periods = await _context.BillingPeriods
                .Where(bp => bp.CompanyId == companyId)
                .OrderByDescending(bp => bp.StartDate)
                .Select(bp => new {
                    value = bp.BillingPeriodId,
                    text = bp.PeriodName,
                    startDate = bp.StartDate,
                    endDate = bp.EndDate,
                    status = bp.Status
                })
                .ToListAsync();

            return Json(periods);
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveStudents(int? siteId = null, int? gradeId = null)
        {
            var companyId = GetCurrentCompanyId();
            var currentSiteId = GetCurrentSiteId();

            var studentsQuery = _context.Students
                .Include(s => s.CurrentGrade)
                .Include(s => s.Site)
                .Where(s => s.CompanyId == companyId && s.IsActive && s.Status == "Active");

            // Filter by site
            if (siteId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.SiteId == siteId.Value);
            }
            else if (!IsHOAdmin() && currentSiteId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.SiteId == currentSiteId.Value);
            }

            // Filter by grade
            if (gradeId.HasValue)
            {
                studentsQuery = studentsQuery.Where(s => s.CurrentGradeId == gradeId.Value);
            }

            var students = await studentsQuery
                .OrderBy(s => s.FirstName)
                .ThenBy(s => s.LastName)
                .Select(s => new {
                    studentId = s.StudentId,
                    studentName = $"{s.FirstName} {s.LastName}",
                    studentCode = s.StudentCode,
                    gradeName = s.CurrentGrade!.GradeName,
                    siteName = s.Site!.SiteName,
                    email = s.Email,
                    parentEmail = s.ParentEmail
                })
                .ToListAsync();

            return Json(students);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateBilling(GenerateBillingViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Please fill all required fields." });
                }

                var companyId = GetCurrentCompanyId();
                var billingPeriod = await _context.BillingPeriods.FindAsync(model.BillingPeriodId);
                if (billingPeriod == null)
                {
                    return Json(new { success = false, message = "Billing period not found." });
                }

                // Get students to generate billing for
                var studentsQuery = _context.Students
                    .Include(s => s.CurrentGrade)
                    .Include(s => s.Site)
                    .Where(s => s.CompanyId == companyId && s.IsActive && s.Status == "Active");

                if (model.SiteId.HasValue)
                {
                    studentsQuery = studentsQuery.Where(s => s.SiteId == model.SiteId.Value);
                }

                if (model.GradeId.HasValue)
                {
                    studentsQuery = studentsQuery.Where(s => s.CurrentGradeId == model.GradeId.Value);
                }

                if (!model.GenerateForAllActiveStudents && model.SelectedStudentIds != null && model.SelectedStudentIds.Any())
                {
                    studentsQuery = studentsQuery.Where(s => model.SelectedStudentIds.Contains(s.StudentId));
                }

                var students = await studentsQuery.ToListAsync();

                if (!students.Any())
                {
                    return Json(new { success = false, message = "No eligible students found." });
                }

                var generatedCount = 0;
                var errors = new List<string>();

                foreach (var student in students)
                {
                    try
                    {
                        // Check if billing already exists
                        var existingBilling = await _context.StudentBillings
                            .FirstOrDefaultAsync(sb => sb.BillingPeriodId == model.BillingPeriodId
                                && sb.StudentId == student.StudentId);

                        if (existingBilling != null)
                        {
                            errors.Add($"Billing already exists for {student.FirstName} {student.LastName}");
                            continue;
                        }

                        // Generate billing number
                        var year = DateTime.Now.Year.ToString().Substring(2);
                        var month = DateTime.Now.Month.ToString("00");

                        var lastBilling = await _context.StudentBillings
                            .Where(sb => sb.CompanyId == companyId
                                && sb.BillingNumber.StartsWith($"INV-{year}{month}"))
                            .OrderByDescending(sb => sb.BillingNumber)
                            .FirstOrDefaultAsync();

                        int sequence = 1;
                        if (lastBilling != null)
                        {
                            var lastSequence = lastBilling.BillingNumber.Substring(9);
                            if (int.TryParse(lastSequence, out int lastSeq))
                            {
                                sequence = lastSeq + 1;
                            }
                        }

                        var billingNumber = $"INV-{year}{month}{sequence:000}";

                        // Calculate tuition fee (use override or default based on grade)
                        decimal tuitionFee = model.OverrideTuitionFee ?? 500000; // Default tuition fee

                        var billing = new StudentBilling
                        {
                            CompanyId = companyId,
                            SiteId = student.SiteId,
                            BillingPeriodId = model.BillingPeriodId,
                            StudentId = student.StudentId,
                            BillingNumber = billingNumber,
                            BillingDate = DateOnly.FromDateTime(DateTime.Today),
                            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                            GradeId = student.CurrentGradeId!.Value,
                            TuitionFee = tuitionFee,
                            BookFees = model.BookFees ?? 0,
                            OtherFees = model.OtherFees ?? 0,
                            Discount = model.Discount ?? 0,
                            Tax = model.Tax ?? 0,
                            Status = "Outstanding",
                            Notes = model.Notes,
                            CreatedBy = GetCurrentUserId(),
                            CreatedDate = DateTime.Now
                        };

                        _context.StudentBillings.Add(billing);
                        generatedCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error for {student.FirstName} {student.LastName}: {ex.Message}");
                    }
                }

                if (generatedCount > 0)
                {
                    await _context.SaveChangesAsync();
                }

                var message = $"Generated {generatedCount} billing(s) successfully.";
                if (errors.Any())
                {
                    message += $" {errors.Count} error(s): {string.Join(", ", errors.Take(3))}";
                    if (errors.Count > 3) message += "...";
                }

                return Json(new
                {
                    success = generatedCount > 0,
                    message = message,
                    generatedCount = generatedCount,
                    errorCount = errors.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error generating billing: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(StudentBillingViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Please fill all required fields." });
                }

                var billing = await _context.StudentBillings.FindAsync(model.BillingId);
                if (billing == null)
                {
                    return Json(new { success = false, message = "Billing not found." });
                }

                billing.BillingDate = DateOnly.FromDateTime(model.BillingDate);
                billing.DueDate = DateOnly.FromDateTime(model.DueDate);
                billing.TuitionFee = model.TuitionFee;
                billing.BookFees = model.BookFees;
                billing.OtherFees = model.OtherFees;
                billing.Discount = model.Discount;
                billing.Tax = model.Tax;
                billing.Status = model.Status;
                billing.Notes = model.Notes;
                billing.UpdatedBy = GetCurrentUserId();
                billing.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Billing updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating billing: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(PaymentViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Please fill all required fields." });
                }

                var billing = await _context.StudentBillings
                    .Include(sb => sb.Student)
                    .FirstOrDefaultAsync(sb => sb.BillingId == model.BillingId);

                if (billing == null)
                {
                    return Json(new { success = false, message = "Billing not found." });
                }

                billing.PaymentDate = DateOnly.FromDateTime(model.PaymentDate);
                billing.PaymentAmount = model.PaymentAmount;
                billing.PaymentMethod = model.PaymentMethod;
                billing.PaymentReference = model.PaymentReference;
                billing.Status = "Paid";
                billing.ReceiptPrinted = false;
                billing.ReceiptPrintCount = 0;
                billing.UpdatedBy = GetCurrentUserId();
                billing.UpdatedDate = DateTime.Now;

                if (!string.IsNullOrEmpty(model.Notes))
                {
                    billing.Notes = string.IsNullOrEmpty(billing.Notes)
                        ? model.Notes
                        : billing.Notes + "\n" + model.Notes;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Payment processed successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error processing payment: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> PrintBilling(int id)
        {
            try
            {
                var billing = await _context.StudentBillings
                    .Include(sb => sb.Student)
                    .Include(sb => sb.Grade)
                    .Include(sb => sb.Site)
                    .Include(sb => sb.BillingPeriod)
                    .FirstOrDefaultAsync(sb => sb.BillingId == id);

                if (billing == null)
                {
                    return Json(new { success = false, message = "Billing not found." });
                }

                // Update print count
                billing.ReceiptPrintCount = (billing.ReceiptPrintCount ?? 0) + 1;
                billing.ReceiptPrinted = true;
                billing.LastPrintDate = DateTime.Now;
                billing.UpdatedBy = GetCurrentUserId();
                billing.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                // Generate PDF
                var pdfBytes = _pdfService.GenerateBillingPdf(billing);

                // Check if we should send email
                var studentEmail = billing.Student?.Email;
                var parentEmail = billing.Student?.ParentEmail;
                var studentName = $"{billing.Student?.FirstName} {billing.Student?.LastName}";

                bool emailSent = false;
                string emailMessage = "";

                if (!string.IsNullOrEmpty(studentEmail) || !string.IsNullOrEmpty(parentEmail))
                {
                    var totalAmount = billing.TuitionFee + (billing.BookFees ?? 0) + (billing.OtherFees ?? 0) - (billing.Discount ?? 0) + (billing.Tax ?? 0);

                    // Send email
                    emailSent = await _emailService.SendBillingEmailAsync(
                        studentEmail,
                        parentEmail,
                        studentName,
                        pdfBytes,
                        billing.BillingNumber,
                        totalAmount,
                        billing.DueDate.ToDateTime(TimeOnly.MinValue)
                    );

                    emailMessage = emailSent
                        ? "Invoice sent to email successfully."
                        : "Invoice generated but email sending failed.";
                }
                else
                {
                    emailMessage = "No email addresses found. Invoice ready for download only.";
                }

                // Return PDF for download
                var fileName = $"Invoice_{billing.BillingNumber}.pdf";

                return Json(new
                {
                    success = true,
                    message = $"Invoice printed successfully. Print count: {billing.ReceiptPrintCount}. {emailMessage}",
                    pdfData = Convert.ToBase64String(pdfBytes),
                    fileName = fileName,
                    emailSent = emailSent
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error printing billing: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var billing = await _context.StudentBillings.FindAsync(id);
                if (billing == null)
                {
                    return Json(new { success = false, message = "Billing not found." });
                }

                // Only allow deletion if not paid or minimal print count
                if (billing.Status == "Paid")
                {
                    return Json(new { success = false, message = "Cannot delete paid billing." });
                }

                if ((billing.ReceiptPrintCount ?? 0) > 3)
                {
                    return Json(new { success = false, message = "Cannot delete billing that has been printed multiple times." });
                }

                // Hard delete
                _context.StudentBillings.Remove(billing);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Billing deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting billing: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int id, string status)
        {
            try
            {
                var billing = await _context.StudentBillings.FindAsync(id);
                if (billing == null)
                {
                    return Json(new { success = false, message = "Billing not found." });
                }

                billing.Status = status;
                billing.UpdatedBy = GetCurrentUserId();
                billing.UpdatedDate = DateTime.Now;

                // If marking as paid, set payment date
                if (status == "Paid" && !billing.PaymentDate.HasValue)
                {
                    billing.PaymentDate = DateOnly.FromDateTime(DateTime.Today);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Billing status changed to {status}." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error changing status: " + ex.Message });
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
    }
}
