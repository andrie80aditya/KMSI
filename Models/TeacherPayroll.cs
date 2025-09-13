using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("TeacherPayrolls")]
    public class TeacherPayroll
    {
        [Key]
        public int PayrollId { get; set; }
        public int CompanyId { get; set; }
        public int SiteId { get; set; }
        public int BillingPeriodId { get; set; }
        public int TeacherId { get; set; }
        [StringLength(20)]
        public required string PayrollNumber { get; set; }
        public DateOnly PayrollDate { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal TotalTeachingHours { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasicSalary { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Allowances { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Deductions { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Tax { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal NetSalary { get; set; } = 0;
        [StringLength(20)]
        public string Status { get; set; } = "Draft";
        public DateOnly? PaymentDate { get; set; }
        [StringLength(20)]
        public string? PaymentMethod { get; set; }
        [StringLength(50)]
        public string? PaymentReference { get; set; }
        [StringLength(500)]
        public string? Notes { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        // Navigation Properties
        public virtual Company? Company { get; set; }
        public virtual Site? Site { get; set; }
        public virtual BillingPeriod? BillingPeriod { get; set; }
        public virtual Teacher? Teacher { get; set; }
    }
}
