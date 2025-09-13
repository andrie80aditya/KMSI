using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("Teachers")]
    public class Teacher
    {
        [Key]
        public int TeacherId { get; set; }
        public int UserId { get; set; }
        public int CompanyId { get; set; }
        public int SiteId { get; set; }
        [StringLength(20)]
        public required string TeacherCode { get; set; }
        [StringLength(100)]
        public string? Specialization { get; set; }
        public int? ExperienceYears { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? HourlyRate { get; set; }
        public int MaxStudentsPerDay { get; set; } = 8;
        public bool IsAvailableForTrial { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        // Navigation Properties
        public virtual User? User { get; set; }
        public virtual Company? Company { get; set; }
        public virtual Site? Site { get; set; }
    }
}
