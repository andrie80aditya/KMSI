using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("Books")]
    public class Book
    {
        [Key]
        public int BookId { get; set; }
        public int CompanyId { get; set; }
        [StringLength(20)]
        public required string BookCode { get; set; }
        [StringLength(200)]
        public required string BookTitle { get; set; }
        [StringLength(100)]
        public string? Author { get; set; }
        [StringLength(100)]
        public string? Publisher { get; set; }
        [StringLength(20)]
        public string? ISBN { get; set; }
        [StringLength(50)]
        public string? Category { get; set; }
        [StringLength(500)]
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        // Navigation Properties
        public virtual Company? Company { get; set; }
        public virtual ICollection<GradeBook>? GradeBooks { get; set; }
        public virtual ICollection<BookPrice>? BookPrices { get; set; }
        public virtual ICollection<Inventory>? Inventories { get; set; }
        public virtual ICollection<StockMovement>? StockMovements { get; set; }
        public virtual ICollection<BookRequisitionDetail>? BookRequisitionDetails { get; set; }
    }
}
