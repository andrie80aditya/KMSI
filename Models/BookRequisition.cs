using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("BookRequisitions")]
    public class BookRequisition
    {
        [Key]
        public int RequisitionId { get; set; }
        public int CompanyId { get; set; }
        public int SiteId { get; set; }
        [StringLength(20)]
        public required string RequisitionNumber { get; set; }
        public DateOnly RequestDate { get; set; }
        public DateOnly RequiredDate { get; set; }
        [StringLength(20)]
        public string Status { get; set; } = "Pending";
        public int TotalItems { get; set; } = 0;
        public int TotalQuantity { get; set; } = 0;
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? FulfilledDate { get; set; }
        [StringLength(500)]
        public string? Notes { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        // Navigation Properties
        public virtual Company? Company { get; set; }
        public virtual Site? Site { get; set; }
        public virtual User? ApprovedByUser { get; set; }
        public virtual ICollection<BookRequisitionDetail>? BookRequisitionDetails { get; set; }
    }
}
