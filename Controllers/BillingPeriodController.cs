using KMSI.Data;
using KMSI.Models.ViewModels;
using KMSI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMSI.Controllers
{
    [Authorize(Policy = "HOAdminAndAbove")]
    public class BillingPeriodController : BaseController
    {
        public BillingPeriodController(KMSIDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            var companyId = GetCurrentCompanyId();

            var billingPeriods = await _context.BillingPeriods
                .Include(bp => bp.Company)
                .Include(bp => bp.StudentBillings)
                .Include(bp => bp.TeacherPayrolls)
                .Where(bp => bp.CompanyId == companyId)
                .OrderByDescending(bp => bp.StartDate)
                .ToListAsync();

            ViewData["Breadcrumb"] = "Billing Periods";
            return View(billingPeriods);
        }

        [HttpGet]
        public async Task<IActionResult> GetBillingPeriod(int id)
        {
            var billingPeriod = await _context.BillingPeriods
                .Include(bp => bp.Company)
                .Include(bp => bp.StudentBillings)
                .Include(bp => bp.TeacherPayrolls)
                .FirstOrDefaultAsync(bp => bp.BillingPeriodId == id);

            if (billingPeriod == null)
                return NotFound();

            var totalBillingAmount = billingPeriod.StudentBillings?.Sum(sb =>
                sb.TuitionFee + (sb.BookFees ?? 0) + (sb.OtherFees ?? 0) - (sb.Discount ?? 0) + (sb.Tax ?? 0)) ?? 0;

            var totalPayrollAmount = billingPeriod.TeacherPayrolls?.Sum(tp => tp.NetSalary) ?? 0;

            return Json(new
            {
                billingPeriodId = billingPeriod.BillingPeriodId,
                companyId = billingPeriod.CompanyId,
                periodName = billingPeriod.PeriodName,
                startDate = billingPeriod.StartDate.ToString("yyyy-MM-dd"),
                endDate = billingPeriod.EndDate.ToString("yyyy-MM-dd"),
                dueDate = billingPeriod.DueDate.ToString("yyyy-MM-dd"),
                status = billingPeriod.Status,
                generatedDate = billingPeriod.GeneratedDate?.ToString("yyyy-MM-dd"),
                finalizedDate = billingPeriod.FinalizedDate?.ToString("yyyy-MM-dd"),
                companyName = billingPeriod.Company?.CompanyName,
                totalStudentBillings = billingPeriod.StudentBillings?.Count ?? 0,
                totalTeacherPayrolls = billingPeriod.TeacherPayrolls?.Count ?? 0,
                totalBillingAmount = totalBillingAmount,
                totalPayrollAmount = totalPayrollAmount,
                canEdit = billingPeriod.Status == "Draft",
                canFinalize = billingPeriod.Status == "Generated",
                canGenerate = billingPeriod.Status == "Draft"
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(BillingPeriodViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Please fill all required fields." });
                }

                var companyId = GetCurrentCompanyId();

                // Check for overlapping periods
                var existingPeriod = await _context.BillingPeriods
                    .FirstOrDefaultAsync(bp => bp.CompanyId == companyId
                        && bp.PeriodName == model.PeriodName);

                if (existingPeriod != null)
                {
                    return Json(new { success = false, message = "Billing period with this name already exists." });
                }

                // Check for date overlaps
                var overlappingPeriod = await _context.BillingPeriods
                    .FirstOrDefaultAsync(bp => bp.CompanyId == companyId
                        && ((DateOnly.FromDateTime(model.StartDate) <= bp.EndDate && DateOnly.FromDateTime(model.EndDate) >= bp.StartDate)));

                if (overlappingPeriod != null)
                {
                    return Json(new { success = false, message = "Date range overlaps with existing billing period." });
                }

                var billingPeriod = new BillingPeriod
                {
                    CompanyId = companyId,
                    PeriodName = model.PeriodName,
                    StartDate = DateOnly.FromDateTime(model.StartDate),
                    EndDate = DateOnly.FromDateTime(model.EndDate),
                    DueDate = DateOnly.FromDateTime(model.DueDate),
                    Status = "Draft",
                    CreatedBy = GetCurrentUserId(),
                    CreatedDate = DateTime.Now
                };

                _context.BillingPeriods.Add(billingPeriod);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Billing period created successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating billing period: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(BillingPeriodViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Please fill all required fields." });
                }

                var billingPeriod = await _context.BillingPeriods.FindAsync(model.BillingPeriodId);
                if (billingPeriod == null)
                {
                    return Json(new { success = false, message = "Billing period not found." });
                }

                // Only allow editing if status is Draft
                if (billingPeriod.Status != "Draft")
                {
                    return Json(new { success = false, message = "Cannot edit billing period after it has been generated." });
                }

                // Check for name conflicts (excluding current record)
                var existingPeriod = await _context.BillingPeriods
                    .FirstOrDefaultAsync(bp => bp.CompanyId == billingPeriod.CompanyId
                        && bp.PeriodName == model.PeriodName
                        && bp.BillingPeriodId != model.BillingPeriodId);

                if (existingPeriod != null)
                {
                    return Json(new { success = false, message = "Billing period with this name already exists." });
                }

                // Check for date overlaps (excluding current record)
                var overlappingPeriod = await _context.BillingPeriods
                    .FirstOrDefaultAsync(bp => bp.CompanyId == billingPeriod.CompanyId
                        && bp.BillingPeriodId != model.BillingPeriodId
                        && ((DateOnly.FromDateTime(model.StartDate) <= bp.EndDate && DateOnly.FromDateTime(model.EndDate) >= bp.StartDate)));

                if (overlappingPeriod != null)
                {
                    return Json(new { success = false, message = "Date range overlaps with existing billing period." });
                }

                billingPeriod.PeriodName = model.PeriodName;
                billingPeriod.StartDate = DateOnly.FromDateTime(model.StartDate);
                billingPeriod.EndDate = DateOnly.FromDateTime(model.EndDate);
                billingPeriod.DueDate = DateOnly.FromDateTime(model.DueDate);

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Billing period updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating billing period: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var billingPeriod = await _context.BillingPeriods
                    .Include(bp => bp.StudentBillings)
                    .Include(bp => bp.TeacherPayrolls)
                    .FirstOrDefaultAsync(bp => bp.BillingPeriodId == id);

                if (billingPeriod == null)
                {
                    return Json(new { success = false, message = "Billing period not found." });
                }

                // Only allow deletion if status is Draft and no related records exist
                if (billingPeriod.Status != "Draft")
                {
                    return Json(new { success = false, message = "Cannot delete billing period after it has been generated." });
                }

                if (billingPeriod.StudentBillings?.Any() == true || billingPeriod.TeacherPayrolls?.Any() == true)
                {
                    return Json(new { success = false, message = "Cannot delete billing period with existing billings or payrolls." });
                }

                _context.BillingPeriods.Remove(billingPeriod);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Billing period deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting billing period: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateBillings(int id)
        {
            try
            {
                var billingPeriod = await _context.BillingPeriods.FindAsync(id);
                if (billingPeriod == null)
                {
                    return Json(new { success = false, message = "Billing period not found." });
                }

                if (billingPeriod.Status != "Draft")
                {
                    return Json(new { success = false, message = "Billing period must be in Draft status to generate billings." });
                }

                // Check if billings already exist for this period
                var existingBillings = await _context.StudentBillings
                    .CountAsync(sb => sb.BillingPeriodId == id);

                if (existingBillings > 0)
                {
                    return Json(new { success = false, message = "Billings already exist for this period." });
                }

                // Update status and generated date
                billingPeriod.Status = "Generated";
                billingPeriod.GeneratedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Billing period marked as generated. You can now create billings for this period." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error generating billings: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> FinalizePeriod(int id)
        {
            try
            {
                var billingPeriod = await _context.BillingPeriods
                    .Include(bp => bp.StudentBillings)
                    .Include(bp => bp.TeacherPayrolls)
                    .FirstOrDefaultAsync(bp => bp.BillingPeriodId == id);

                if (billingPeriod == null)
                {
                    return Json(new { success = false, message = "Billing period not found." });
                }

                if (billingPeriod.Status != "Generated")
                {
                    return Json(new { success = false, message = "Billing period must be in Generated status to finalize." });
                }

                // Check if there are any outstanding billings
                var outstandingBillings = billingPeriod.StudentBillings?.Count(sb => sb.Status == "Outstanding") ?? 0;
                if (outstandingBillings > 0)
                {
                    return Json(new { success = false, message = $"Cannot finalize period with {outstandingBillings} outstanding billing(s)." });
                }

                // Update status and finalized date
                billingPeriod.Status = "Finalized";
                billingPeriod.FinalizedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Billing period finalized successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error finalizing billing period: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReopenPeriod(int id)
        {
            try
            {
                var billingPeriod = await _context.BillingPeriods.FindAsync(id);
                if (billingPeriod == null)
                {
                    return Json(new { success = false, message = "Billing period not found." });
                }

                if (billingPeriod.Status == "Draft")
                {
                    return Json(new { success = false, message = "Billing period is already in Draft status." });
                }

                // Only super admin can reopen finalized periods
                if (billingPeriod.Status == "Finalized" && !IsSuperAdmin())
                {
                    return Json(new { success = false, message = "Only Super Admin can reopen finalized periods." });
                }

                // Reopen to previous status
                if (billingPeriod.Status == "Finalized")
                {
                    billingPeriod.Status = "Generated";
                    billingPeriod.FinalizedDate = null;
                }
                else if (billingPeriod.Status == "Generated")
                {
                    billingPeriod.Status = "Draft";
                    billingPeriod.GeneratedDate = null;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Billing period reopened successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error reopening billing period: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBillingPeriodStats(int id)
        {
            try
            {
                var billingPeriod = await _context.BillingPeriods
                    .Include(bp => bp.StudentBillings)
                    .Include(bp => bp.TeacherPayrolls)
                    .FirstOrDefaultAsync(bp => bp.BillingPeriodId == id);

                if (billingPeriod == null)
                {
                    return Json(new { success = false, message = "Billing period not found." });
                }

                var studentBillings = billingPeriod.StudentBillings ?? new List<StudentBilling>();
                var teacherPayrolls = billingPeriod.TeacherPayrolls ?? new List<TeacherPayroll>();

                var stats = new
                {
                    studentBillings = new
                    {
                        total = studentBillings.Count,
                        outstanding = studentBillings.Count(sb => sb.Status == "Outstanding"),
                        paid = studentBillings.Count(sb => sb.Status == "Paid"),
                        cancelled = studentBillings.Count(sb => sb.Status == "Cancelled"),
                        totalAmount = studentBillings.Sum(sb => sb.TuitionFee + (sb.BookFees ?? 0) + (sb.OtherFees ?? 0) - (sb.Discount ?? 0) + (sb.Tax ?? 0)),
                        paidAmount = studentBillings.Where(sb => sb.Status == "Paid").Sum(sb => sb.PaymentAmount ?? 0),
                        outstandingAmount = studentBillings.Where(sb => sb.Status == "Outstanding").Sum(sb => sb.TuitionFee + (sb.BookFees ?? 0) + (sb.OtherFees ?? 0) - (sb.Discount ?? 0) + (sb.Tax ?? 0))
                    },
                    teacherPayrolls = new
                    {
                        total = teacherPayrolls.Count,
                        draft = teacherPayrolls.Count(tp => tp.Status == "Draft"),
                        processed = teacherPayrolls.Count(tp => tp.Status == "Processed"),
                        paid = teacherPayrolls.Count(tp => tp.Status == "Paid"),
                        totalAmount = teacherPayrolls.Sum(tp => tp.NetSalary),
                        totalHours = teacherPayrolls.Sum(tp => tp.TotalTeachingHours)
                    }
                };

                return Json(new { success = true, stats = stats });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading stats: " + ex.Message });
            }
        }
    }
}
