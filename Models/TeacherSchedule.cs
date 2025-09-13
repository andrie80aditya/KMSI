using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("TeacherSchedules")]
    public class TeacherSchedule
    {
        [Key]
        public int TeacherScheduleId { get; set; }
        public int TeacherId { get; set; }
        public byte DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        // Navigation Properties
        public virtual Teacher? Teacher { get; set; }
    }
}
