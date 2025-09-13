using KMSI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMSI.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly KMSIDbContext _context;

        public DashboardController(KMSIDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var companyId = int.Parse(User.FindFirst("CompanyId")?.Value ?? "0");
            var userLevel = User.FindFirst("UserLevel")?.Value ?? "";
            var siteId = User.FindFirst("SiteId")?.Value;

            // Get dashboard statistics
            var stats = new
            {
                TotalStudents = await _context.Students.CountAsync(s => s.CompanyId == companyId && s.IsActive),
                TotalTeachers = await _context.Teachers.CountAsync(t => t.CompanyId == companyId && t.IsActive),
                TotalSites = await _context.Sites.CountAsync(s => s.CompanyId == companyId && s.IsActive),
                ActiveRegistrations = await _context.Registrations.CountAsync(r => r.CompanyId == companyId && r.Status == "Pending"),
                MonthlyBilling = await _context.StudentBillings
                    .Where(b => b.CompanyId == companyId && b.BillingDate.Month == DateTime.Now.Month)
                    .SumAsync(b => b.TuitionFee + (b.BookFees ?? 0) + (b.OtherFees ?? 0)),
                PendingExams = await _context.Examinations.CountAsync(e => e.CompanyId == companyId && e.Status == "Scheduled")
            };

            // Get recent activities
            var recentStudents = await _context.Students
                .Where(s => s.CompanyId == companyId && s.IsActive)
                .OrderByDescending(s => s.CreatedDate)
                .Take(5)
                .Select(s => new { s.StudentCode, s.FirstName, s.LastName, s.CreatedDate })
                .ToListAsync();

            ViewBag.Stats = stats;
            ViewBag.RecentStudents = recentStudents;
            ViewBag.UserLevel = userLevel;

            return View();
        }
    }
}
