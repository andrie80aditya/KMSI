using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("Certificates")]
    public class Certificate
    {
        [Key]
        public int CertificateId { get; set; }
        public int StudentId { get; set; }
        public int StudentExaminationId { get; set; }
        [StringLength(50)]
        public required string CertificateNumber { get; set; }
        public int GradeId { get; set; }
        public DateOnly IssueDate { get; set; }
        [StringLength(200)]
        public required string CertificateTitle { get; set; }
        [StringLength(100)]
        public required string IssuedBy { get; set; }
        [StringLength(100)]
        public string? SignedBy { get; set; }
        [StringLength(255)]
        public string? CertificatePath { get; set; }
        [StringLength(20)]
        public string Status { get; set; } = "Issued";
        public int PrintCount { get; set; } = 0;
        public DateTime? LastPrintDate { get; set; }
        [StringLength(500)]
        public string? Notes { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        // Navigation Properties
        public virtual Student? Student { get; set; }
        public virtual StudentExamination? StudentExamination { get; set; }
        public virtual Grade? Grade { get; set; }
    }
}
