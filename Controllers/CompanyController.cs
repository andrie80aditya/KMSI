using KMSI.Data;
using KMSI.Models.ViewModels;
using KMSI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMSI.Controllers
{
    [Authorize(Policy = "HOAdminAndAbove")]
    public class CompanyController : BaseController
    {
        public CompanyController(KMSIDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            var companies = await _context.Companies
                .Include(c => c.ParentCompany)
                //.Where(c => c.IsActive)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            ViewBag.ParentCompanies = await _context.Companies
                .Where(c => c.IsActive && c.ParentCompanyId == null)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();

            ViewData["Breadcrumb"] = "Company Management";
            return View(companies);
        }

        [HttpGet]
        public async Task<IActionResult> GetCompany(int id)
        {
            var company = await _context.Companies
                .Include(c => c.ParentCompany)
                .FirstOrDefaultAsync(c => c.CompanyId == id);

            if (company == null)
                return NotFound();

            return Json(new
            {
                companyId = company.CompanyId,
                companyCode = company.CompanyCode,
                companyName = company.CompanyName,
                parentCompanyId = company.ParentCompanyId,
                address = company.Address,
                city = company.City,
                province = company.Province,
                phone = company.Phone,
                email = company.Email,
                isHeadOffice = company.IsHeadOffice,
                isActive = company.IsActive
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(CompanyViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Please fill all required fields." });
                }

                // Check if company code already exists
                var existingCompany = await _context.Companies
                    .FirstOrDefaultAsync(c => c.CompanyCode == model.CompanyCode);
                if (existingCompany != null)
                {
                    return Json(new { success = false, message = "Company code already exists." });
                }

                var company = new Company
                {
                    CompanyCode = model.CompanyCode,
                    CompanyName = model.CompanyName,
                    ParentCompanyId = model.ParentCompanyId,
                    Address = model.Address,
                    City = model.City,
                    Province = model.Province,
                    Phone = model.Phone,
                    Email = model.Email,
                    IsHeadOffice = model.IsHeadOffice,
                    IsActive = model.IsActive,
                    CreatedBy = GetCurrentUserId(),
                    CreatedDate = DateTime.Now
                };

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Company created successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating company: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(CompanyViewModel model)
        {
            try
            {
                var company = await _context.Companies.FindAsync(model.CompanyId);
                if (company == null)
                {
                    return Json(new { success = false, message = "Company not found." });
                }

                // Check if company code already exists (excluding current company)
                var existingCompany = await _context.Companies
                    .FirstOrDefaultAsync(c => c.CompanyCode == model.CompanyCode && c.CompanyId != model.CompanyId);
                if (existingCompany != null)
                {
                    return Json(new { success = false, message = "Company code already exists." });
                }

                // Prevent setting parent to itself or creating circular reference
                if (model.ParentCompanyId == model.CompanyId)
                {
                    return Json(new { success = false, message = "Company cannot be its own parent." });
                }

                company.CompanyCode = model.CompanyCode;
                company.CompanyName = model.CompanyName;
                company.ParentCompanyId = model.ParentCompanyId;
                company.Address = model.Address;
                company.City = model.City;
                company.Province = model.Province;
                company.Phone = model.Phone;
                company.Email = model.Email;
                company.IsHeadOffice = model.IsHeadOffice;
                company.IsActive = model.IsActive;
                company.UpdatedBy = GetCurrentUserId();
                company.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Company updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating company: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                {
                    return Json(new { success = false, message = "Company not found." });
                }

                // Check if company has sub-companies
                var hasSubCompanies = await _context.Companies.AnyAsync(c => c.ParentCompanyId == id && c.IsActive);
                if (hasSubCompanies)
                {
                    return Json(new { success = false, message = "Cannot delete company that has sub-companies." });
                }

                // Check if company has active users
                var hasUsers = await _context.Users.AnyAsync(u => u.CompanyId == id && u.IsActive);
                if (hasUsers)
                {
                    return Json(new { success = false, message = "Cannot delete company that has active users." });
                }

                // Check if company has active sites
                var hasSites = await _context.Sites.AnyAsync(s => s.CompanyId == id && s.IsActive);
                if (hasSites)
                {
                    return Json(new { success = false, message = "Cannot delete company that has active sites." });
                }

                // Soft delete - set IsActive to false
                company.IsActive = false;
                company.UpdatedBy = GetCurrentUserId();
                company.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Company deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting company: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetParentCompanies()
        {
            var companies = await _context.Companies
                .Where(c => c.IsActive)
                .Select(c => new { value = c.CompanyId, text = c.CompanyName })
                .OrderBy(c => c.text)
                .ToListAsync();

            return Json(companies);
        }
    }
}
