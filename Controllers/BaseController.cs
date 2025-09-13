using KMSI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KMSI.Controllers
{
    [Authorize]
    public class BaseController : Controller
    {
        protected readonly KMSIDbContext _context;

        public BaseController(KMSIDbContext context)
        {
            _context = context;
        }

        protected int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst("NameIdentifier")?.Value ?? "0");
        }

        protected int GetCurrentCompanyId()
        {
            return int.Parse(User.FindFirst("CompanyId")?.Value ?? "0");
        }

        protected int? GetCurrentSiteId()
        {
            var siteId = User.FindFirst("SiteId")?.Value;
            return string.IsNullOrEmpty(siteId) ? null : int.Parse(siteId);
        }

        protected string GetCurrentUserLevel()
        {
            return User.FindFirst("UserLevel")?.Value ?? "";
        }

        protected bool HasPermission(params string[] allowedLevels)
        {
            var userLevel = GetCurrentUserLevel();
            return allowedLevels.Contains(userLevel);
        }

        protected bool IsSuperAdmin()
        {
            return GetCurrentUserLevel() == "SUPER";
        }

        protected bool IsHOAdmin()
        {
            var level = GetCurrentUserLevel();
            return level == "SUPER" || level == "HO_ADMIN";
        }

        protected bool IsBranchManager()
        {
            var level = GetCurrentUserLevel();
            return level == "SUPER" || level == "HO_ADMIN" || level == "BRANCH_MGR";
        }
    }
}
