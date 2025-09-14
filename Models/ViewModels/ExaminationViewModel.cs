namespace KMSI.Models.ViewModels
{
    public class ExaminationViewModel
    {
        public int ExaminationId { get; set; }
        public int CompanyId { get; set; }
        public int SiteId { get; set; }
        public string ExamCode { get; set; } = "";
        public string ExamName { get; set; } = "";
        public int GradeId { get; set; }
        public DateTime ExamDate { get; set; } = DateTime.Today;
        public TimeSpan StartTime { get; set; } = TimeSpan.FromHours(9);
        public TimeSpan EndTime { get; set; } = TimeSpan.FromHours(11);
        public string? Location { get; set; }
        public int ExaminerTeacherId { get; set; }
        public int MaxCapacity { get; set; } = 10;
        public string Status { get; set; } = "Scheduled";
        public string? Description { get; set; }
    }

    public class StudentExaminationViewModel
    {
        public int StudentExaminationId { get; set; }
        public int ExaminationId { get; set; }
        public int StudentId { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public string? AttendanceStatus { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public decimal? Score { get; set; }
        public decimal MaxScore { get; set; } = 100;
        public string? Grade { get; set; }
        public string? Result { get; set; }
        public string? TeacherNotes { get; set; }
    }
}
