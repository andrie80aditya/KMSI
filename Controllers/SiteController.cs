using KMSI.Data;
using KMSI.Models.ViewModels;
using KMSI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMSI.Controllers
{
    [Authorize(Policy = "HOAdminAndAbove")]
    public class SiteController : BaseController
    {
        public SiteController(KMSIDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            var companyId = GetCurrentCompanyId();
            var sites = await _context.Sites
                .Include(s => s.Company)
                .Where(s => s.CompanyId == companyId)
                .OrderBy(s => s.SiteName)
                .ToListAsync();

            ViewBag.Companies = await _context.Companies
                .Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            ViewData["Breadcrumb"] = "Site Management";
            return View(sites);
        }

        [HttpGet]
        public async Task<IActionResult> GetSite(int id)
        {
            var site = await _context.Sites
                .Include(s => s.Company)
                .FirstOrDefaultAsync(s => s.SiteId == id);

            if (site == null)
                return NotFound();

            return Json(new
            {
                siteId = site.SiteId,
                companyId = site.CompanyId,
                siteCode = site.SiteCode,
                siteName = site.SiteName,
                address = site.Address,
                city = site.City,
                province = site.Province,
                phone = site.Phone,
                email = site.Email,
                managerName = site.ManagerName,
                isActive = site.IsActive
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(SiteViewModel model)
        {
            try
            {
                // Check if site code already exists
                var existingSite = await _context.Sites
                    .FirstOrDefaultAsync(s => s.SiteCode == model.SiteCode);
                if (existingSite != null)
                {
                    return Json(new { success = false, message = "Site code already exists." });
                }

                var site = new Site
                {
                    CompanyId = model.CompanyId,
                    SiteCode = model.SiteCode,
                    SiteName = model.SiteName,
                    Address = model.Address,
                    City = model.City,
                    Province = model.Province,
                    Phone = model.Phone,
                    Email = model.Email,
                    ManagerName = model.ManagerName,
                    IsActive = model.IsActive,
                    CreatedBy = GetCurrentUserId(),
                    CreatedDate = DateTime.Now
                };

                _context.Sites.Add(site);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Site created successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating site: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(SiteViewModel model)
        {
            try
            {
                var site = await _context.Sites.FindAsync(model.SiteId);
                if (site == null)
                {
                    return Json(new { success = false, message = "Site not found." });
                }

                // Check if site code already exists (excluding current site)
                var existingSite = await _context.Sites
                    .FirstOrDefaultAsync(s => s.SiteCode == model.SiteCode && s.SiteId != model.SiteId);
                if (existingSite != null)
                {
                    return Json(new { success = false, message = "Site code already exists." });
                }

                site.CompanyId = model.CompanyId;
                site.SiteCode = model.SiteCode;
                site.SiteName = model.SiteName;
                site.Address = model.Address;
                site.City = model.City;
                site.Province = model.Province;
                site.Phone = model.Phone;
                site.Email = model.Email;
                site.ManagerName = model.ManagerName;
                site.IsActive = model.IsActive;
                site.UpdatedBy = GetCurrentUserId();
                site.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Site updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating site: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var site = await _context.Sites.FindAsync(id);
                if (site == null)
                {
                    return Json(new { success = false, message = "Site not found." });
                }

                // Check if site has active users
                var hasUsers = await _context.Users.AnyAsync(u => u.SiteId == id && u.IsActive);
                if (hasUsers)
                {
                    return Json(new { success = false, message = "Cannot delete site that has active users." });
                }

                // Check if site has active teachers
                var hasTeachers = await _context.Teachers.AnyAsync(t => t.SiteId == id && t.IsActive);
                if (hasTeachers)
                {
                    return Json(new { success = false, message = "Cannot delete site that has active teachers." });
                }

                // Check if site has active students
                var hasStudents = await _context.Students.AnyAsync(s => s.SiteId == id && s.IsActive);
                if (hasStudents)
                {
                    return Json(new { success = false, message = "Cannot delete site that has active students." });
                }

                // Soft delete - set IsActive to false
                site.IsActive = false;
                site.UpdatedBy = GetCurrentUserId();
                site.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Site deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting site: " + ex.Message });
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
    }
}
