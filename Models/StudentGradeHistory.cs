using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace KMSI.Models
{
    [Table("StudentGradeHistories")]
    public class StudentGradeHistory
    {
        [Key]
        public int StudentGradeHistoryId { get; set; }
        public int StudentId { get; set; }
        public int GradeId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        [StringLength(20)]
        public required string Status { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? CompletionPercentage { get; set; } = 0;
        public bool? IsCurrentGrade { get; set; } = false;
        [StringLength(500)]
        public string? Notes { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        // Navigation Properties
        public virtual Student? Student { get; set; }
        public virtual Grade? Grade { get; set; }
    }
}
