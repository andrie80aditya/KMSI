using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("StockMovements")]
    public class StockMovement
    {
        [Key]
        public int StockMovementId { get; set; }
        public int CompanyId { get; set; }
        public int SiteId { get; set; }
        public int BookId { get; set; }
        [StringLength(20)]
        public required string MovementType { get; set; }
        public int Quantity { get; set; }
        [StringLength(20)]
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public int? FromSiteId { get; set; }
        public int? ToSiteId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitCost { get; set; }
        [StringLength(500)]
        public string? Description { get; set; }
        public DateTime? MovementDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        // Navigation Properties
        public virtual Company? Company { get; set; }
        public virtual Site? Site { get; set; }
        public virtual Book? Book { get; set; }
        public virtual Site? FromSite { get; set; }
        public virtual Site? ToSite { get; set; }
    }
}
