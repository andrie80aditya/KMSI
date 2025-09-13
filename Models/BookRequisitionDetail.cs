using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("BookRequisitionDetails")]
    public class BookRequisitionDetail
    {
        [Key]
        public int RequisitionDetailId { get; set; }
        public int RequisitionId { get; set; }
        public int BookId { get; set; }
        public int RequestedQuantity { get; set; }
        public int ApprovedQuantity { get; set; } = 0;
        public int FulfilledQuantity { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitCost { get; set; }
        [StringLength(255)]
        public string? Notes { get; set; }
        // Navigation Properties
        public virtual BookRequisition? BookRequisition { get; set; }
        public virtual Book? Book { get; set; }
    }
}
