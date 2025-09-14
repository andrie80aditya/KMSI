namespace KMSI.Models.ViewModels
{
    public class SiteViewModel
    {
        public int SiteId { get; set; }
        public int CompanyId { get; set; }
        public string SiteCode { get; set; } = "";
        public string SiteName { get; set; } = "";
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? ManagerName { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
