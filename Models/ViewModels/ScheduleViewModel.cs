namespace KMSI.Models.ViewModels
{
    public class ScheduleViewModel
    {
        public int ClassScheduleId { get; set; }
        public int CompanyId { get; set; }
        public int SiteId { get; set; }
        public int StudentId { get; set; }
        public int TeacherId { get; set; }
        public int GradeId { get; set; }
        public DateTime ScheduleDate { get; set; } = DateTime.Now.Date;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int Duration { get; set; }
        public string ScheduleType { get; set; } = "Regular";
        public string Status { get; set; } = "Scheduled";
        public string? Room { get; set; }
        public string? Notes { get; set; }
        public bool IsRecurring { get; set; } = false;
        public string? RecurrencePattern { get; set; }
    }
}
