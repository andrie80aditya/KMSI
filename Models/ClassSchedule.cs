using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("ClassSchedules")]
    public class ClassSchedule
    {
        [Key]
        public int ClassScheduleId { get; set; }
        public int CompanyId { get; set; }
        public int SiteId { get; set; }
        public int StudentId { get; set; }
        public int TeacherId { get; set; }
        public int GradeId { get; set; }
        public DateOnly ScheduleDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int Duration { get; set; }
        [StringLength(20)]
        public required string ScheduleType { get; set; }
        [StringLength(20)]
        public string Status { get; set; } = "Scheduled";
        [StringLength(50)]
        public string? Room { get; set; }
        [StringLength(500)]
        public string? Notes { get; set; }
        public bool IsRecurring { get; set; } = false;
        [StringLength(100)]
        public string? RecurrencePattern { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        // Navigation Properties
        public virtual Company? Company { get; set; }
        public virtual Site? Site { get; set; }
        public virtual Student? Student { get; set; }
        public virtual Teacher? Teacher { get; set; }
        public virtual Grade? Grade { get; set; }
    }
}
