using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("Attendances")]
    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }
        public int ClassScheduleId { get; set; }
        public int StudentId { get; set; }
        public int TeacherId { get; set; }
        public DateOnly AttendanceDate { get; set; }
        public TimeOnly? ActualStartTime { get; set; }
        public TimeOnly? ActualEndTime { get; set; }
        [StringLength(20)]
        public required string Status { get; set; }
        [StringLength(255)]
        public string? LessonTopic { get; set; }
        [StringLength(500)]
        public string? StudentProgress { get; set; }
        [StringLength(1000)]
        public string? TeacherNotes { get; set; }
        [StringLength(500)]
        public string? HomeworkAssigned { get; set; }
        [StringLength(500)]
        public string? NextLessonPrep { get; set; }
        public byte? StudentPerformanceScore { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        // Navigation Properties
        public virtual ClassSchedule? ClassSchedule { get; set; }
        public virtual Student? Student { get; set; }
        public virtual Teacher? Teacher { get; set; }
    }
}
