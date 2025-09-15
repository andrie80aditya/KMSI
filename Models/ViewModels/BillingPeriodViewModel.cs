namespace KMSI.Models.ViewModels
{
    public class BillingPeriodViewModel
    {
        public int BillingPeriodId { get; set; }
        public int CompanyId { get; set; }
        public string PeriodName { get; set; } = "";
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);
        public string Status { get; set; } = "Draft";
        public DateTime? GeneratedDate { get; set; }
        public DateTime? FinalizedDate { get; set; }

        // Display properties
        public string? CompanyName { get; set; }
        public int TotalStudentBillings { get; set; }
        public int TotalTeacherPayrolls { get; set; }
        public decimal TotalBillingAmount { get; set; }
        public decimal TotalPayrollAmount { get; set; }
        public bool CanEdit => Status == "Draft";
        public bool CanFinalize => Status == "Generated";
        public bool CanGenerate => Status == "Draft";
    }
}
