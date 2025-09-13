using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("Examinations")]
    public class Examination
    {
        [Key]
        public int ExaminationId { get; set; }
        public int CompanyId { get; set; }
        public int SiteId { get; set; }
        [StringLength(20)]
        public required string ExamCode { get; set; }
        [StringLength(100)]
        public required string ExamName { get; set; }
        public int GradeId { get; set; }
        public DateOnly ExamDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        [StringLength(100)]
        public string? Location { get; set; }
        public int ExaminerTeacherId { get; set; }
        public int MaxCapacity { get; set; } = 10;
        [StringLength(20)]
        public string Status { get; set; } = "Scheduled";
        [StringLength(500)]
        public string? Description { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        // Navigation Properties
        public virtual Company? Company { get; set; }
        public virtual Site? Site { get; set; }
        public virtual Grade? Grade { get; set; }
        public virtual Teacher? ExaminerTeacher { get; set; }
        public virtual ICollection<StudentExamination>? StudentExaminations { get; set; }
    }
}
