namespace KMSI.Models.ViewModels
{
    public class AttendanceViewModel
    {
        public int AttendanceId { get; set; }
        public int ClassScheduleId { get; set; }
        public int StudentId { get; set; }
        public int TeacherId { get; set; }
        public DateTime AttendanceDate { get; set; } = DateTime.Now.Date;
        public TimeSpan? ActualStartTime { get; set; }
        public TimeSpan? ActualEndTime { get; set; }
        public string Status { get; set; } = "Present";
        public string? LessonTopic { get; set; }
        public string? StudentProgress { get; set; }
        public string? TeacherNotes { get; set; }
        public string? HomeworkAssigned { get; set; }
        public string? NextLessonPrep { get; set; }
        public byte? StudentPerformanceScore { get; set; }
    }
}
