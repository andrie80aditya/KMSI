using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace KMSI.Models
{
    [Table("StudentBillings")]
    public class StudentBilling
    {
        [Key]
        public int BillingId { get; set; }
        public int CompanyId { get; set; }
        public int SiteId { get; set; }
        public int BillingPeriodId { get; set; }
        public int StudentId { get; set; }
        [StringLength(20)]
        public required string BillingNumber { get; set; }
        public DateOnly BillingDate { get; set; }
        public DateOnly DueDate { get; set; }
        public int GradeId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TuitionFee { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? BookFees { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal? OtherFees { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Discount { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Tax { get; set; } = 0;
        [StringLength(20)]
        public string Status { get; set; } = "Outstanding";
        public DateOnly? PaymentDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PaymentAmount { get; set; }
        [StringLength(20)]
        public string? PaymentMethod { get; set; }
        [StringLength(50)]
        public string? PaymentReference { get; set; }
        public bool? ReceiptPrinted { get; set; } = false;
        public int? ReceiptPrintCount { get; set; } = 0;
        public DateTime? LastPrintDate { get; set; }
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
        public virtual Student? Student { get; set; }
        public virtual Grade? Grade { get; set; }
    }
}
