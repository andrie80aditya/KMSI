namespace KMSI.Models.ViewModels
{
    public class TeacherViewModel
    {
        public int TeacherId { get; set; }
        public int UserId { get; set; }
        public int CompanyId { get; set; }
        public int SiteId { get; set; }
        public string TeacherCode { get; set; } = "";
        public string? Specialization { get; set; }
        public int? ExperienceYears { get; set; }
        public decimal? HourlyRate { get; set; }
        public int MaxStudentsPerDay { get; set; } = 8;
        public bool IsAvailableForTrial { get; set; } = true;
        public bool IsActive { get; set; } = true;

        // User information for display/creation
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
    }
}
