using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("BillingPeriods")]
    public class BillingPeriod
    {
        [Key]
        public int BillingPeriodId { get; set; }
        public int CompanyId { get; set; }
        [StringLength(50)]
        public required string PeriodName { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public DateOnly DueDate { get; set; }
        [StringLength(20)]
        public string Status { get; set; } = "Draft";
        public DateTime? GeneratedDate { get; set; }
        public DateTime? FinalizedDate { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        // Navigation Properties
        public virtual Company? Company { get; set; }
        public virtual ICollection<StudentBilling>? StudentBillings { get; set; }
        public virtual ICollection<TeacherPayroll>? TeacherPayrolls { get; set; }
    }
}
