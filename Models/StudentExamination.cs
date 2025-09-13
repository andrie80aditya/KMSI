using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("StudentExaminations")]
    public class StudentExamination
    {
        [Key]
        public int StudentExaminationId { get; set; }
        public int ExaminationId { get; set; }
        public int StudentId { get; set; }
        public DateTime? RegistrationDate { get; set; } = DateTime.Now;
        [StringLength(20)]
        public string? AttendanceStatus { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Score { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? MaxScore { get; set; } = 100;
        [StringLength(2)]
        public string? Grade { get; set; }
        [StringLength(20)]
        public string? Result { get; set; }
        [StringLength(1000)]
        public string? TeacherNotes { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        // Navigation Properties
        public virtual Examination? Examination { get; set; }
        public virtual Student? Student { get; set; }
    }
}
