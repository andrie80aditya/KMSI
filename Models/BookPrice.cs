using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("BookPrices")]
    public class BookPrice
    {
        [Key]
        public int BookPriceId { get; set; }
        public int BookId { get; set; }
        public int SiteId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        [StringLength(3)]
        public string Currency { get; set; } = "IDR";
        public DateOnly EffectiveDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        // Navigation Properties
        public virtual Book? Book { get; set; }
        public virtual Site? Site { get; set; }
    }
}
