using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("Registrations")]
    public class Registration
    {
        [Key]
        public int RegistrationId { get; set; }
        public int CompanyId { get; set; }
        public int SiteId { get; set; }
        public int StudentId { get; set; }
        [StringLength(20)]
        public required string RegistrationCode { get; set; }
        public DateOnly RegistrationDate { get; set; }
        public DateOnly? TrialDate { get; set; }
        public TimeOnly? TrialTime { get; set; }
        public int? AssignedTeacherId { get; set; }
        public int RequestedGradeId { get; set; }
        [StringLength(20)]
        public string Status { get; set; } = "Pending";
        [StringLength(20)]
        public string? TrialResult { get; set; }
        public DateOnly? ConfirmationDate { get; set; }
        [StringLength(20)]
        public string PaymentStatus { get; set; } = "Pending";
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PaymentAmount { get; set; }
        [StringLength(1000)]
        public string? Notes { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        // Navigation Properties
        public virtual Company? Company { get; set; }
        public virtual Site? Site { get; set; }
        public virtual Student? Student { get; set; }
        public virtual Teacher? AssignedTeacher { get; set; }
        public virtual Grade? RequestedGrade { get; set; }
    }
}
