using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("Grades")]
    public class Grade
    {
        [Key]
        public int GradeId { get; set; }
        public int CompanyId { get; set; }
        [StringLength(10)]
        public required string GradeCode { get; set; }
        [StringLength(50)]
        public required string GradeName { get; set; }
        [StringLength(255)]
        public string? Description { get; set; }
        public int? Duration { get; set; }
        public int? SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        // Navigation Properties
        public virtual Company? Company { get; set; }
        public virtual ICollection<GradeBook>? GradeBooks { get; set; }
        public virtual ICollection<Student>? Students { get; set; }
        public virtual ICollection<StudentGradeHistory>? StudentGradeHistories { get; set; }
        public virtual ICollection<StudentBilling>? StudentBillings { get; set; }
        public virtual ICollection<Registration>? Registrations { get; set; }
    }
}
