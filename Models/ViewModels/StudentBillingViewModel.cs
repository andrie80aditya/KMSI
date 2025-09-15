namespace KMSI.Models.ViewModels
{
    public class StudentBillingViewModel
    {
        public int BillingId { get; set; }
        public int CompanyId { get; set; }
        public int SiteId { get; set; }
        public int BillingPeriodId { get; set; }
        public int StudentId { get; set; }
        public string BillingNumber { get; set; } = "";
        public DateTime BillingDate { get; set; } = DateTime.Today;
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);
        public int GradeId { get; set; }
        public decimal TuitionFee { get; set; }
        public decimal? BookFees { get; set; } = 0;
        public decimal? OtherFees { get; set; } = 0;
        public decimal? Discount { get; set; } = 0;
        public decimal? Tax { get; set; } = 0;
        public string Status { get; set; } = "Outstanding";
        public DateTime? PaymentDate { get; set; }
        public decimal? PaymentAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentReference { get; set; }
        public string? Notes { get; set; }

        // Calculated properties
        public decimal SubTotal => TuitionFee + (BookFees ?? 0) + (OtherFees ?? 0);
        public decimal TotalAmount => SubTotal - (Discount ?? 0) + (Tax ?? 0);

        // Display properties
        public string? StudentName { get; set; }
        public string? StudentCode { get; set; }
        public string? GradeName { get; set; }
        public string? SiteName { get; set; }
        public string? BillingPeriodName { get; set; }
    }

    public class GenerateBillingViewModel
    {
        public int BillingPeriodId { get; set; }
        public int? SiteId { get; set; }
        public int? GradeId { get; set; }
        public List<int>? SelectedStudentIds { get; set; }
        public bool GenerateForAllActiveStudents { get; set; } = true;
        public decimal? OverrideTuitionFee { get; set; }
        public decimal? BookFees { get; set; } = 0;
        public decimal? OtherFees { get; set; } = 0;
        public decimal? Discount { get; set; } = 0;
        public decimal? Tax { get; set; } = 0;
        public string? Notes { get; set; }
    }

    public class PaymentViewModel
    {
        public int BillingId { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Today;
        public decimal PaymentAmount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string? PaymentReference { get; set; }
        public string? Notes { get; set; }
    }
}
