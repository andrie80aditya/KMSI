namespace KMSI.Models.ViewModels
{
    public class CompanyViewModel
    {
        public int CompanyId { get; set; }
        public string CompanyCode { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public int? ParentCompanyId { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public bool IsHeadOffice { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }
}
