namespace KMSI.Models.ViewModels
{
    public class RegistrationViewModel
    {
        public int RegistrationId { get; set; }
        public int CompanyId { get; set; }
        public int SiteId { get; set; }
        public int StudentId { get; set; }
        public string RegistrationCode { get; set; } = "";
        public DateTime RegistrationDate { get; set; } = DateTime.Now.Date;
        public DateTime? TrialDate { get; set; }
        public TimeSpan? TrialTime { get; set; }
        public int? AssignedTeacherId { get; set; }
        public int RequestedGradeId { get; set; }
        public string Status { get; set; } = "Pending";
        public string? TrialResult { get; set; }
        public DateTime? ConfirmationDate { get; set; }
        public string PaymentStatus { get; set; } = "Pending";
        public decimal? PaymentAmount { get; set; }
        public string? Notes { get; set; }

        // Student information for display/creation
        public string StudentFirstName { get; set; } = "";
        public string StudentLastName { get; set; } = "";
        public string? StudentPhone { get; set; }
        public string? StudentEmail { get; set; }
        public string? ParentName { get; set; }
        public string? ParentPhone { get; set; }
        public string? ParentEmail { get; set; }
    }
}
