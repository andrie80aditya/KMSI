namespace KMSI.Models.ViewModels
{
    public class StudentViewModel
    {
        public int StudentId { get; set; }
        public int CompanyId { get; set; }
        public int SiteId { get; set; }
        public string StudentCode { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? ParentName { get; set; }
        public string? ParentPhone { get; set; }
        public string? ParentEmail { get; set; }
        public string? PhotoPath { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.Now.Date;
        public string Status { get; set; } = "Pending";
        public int? CurrentGradeId { get; set; }
        public int? AssignedTeacherId { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
