using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("Inventory")]
    public class Inventory
    {
        [Key]
        public int InventoryId { get; set; }
        public int CompanyId { get; set; }
        public int SiteId { get; set; }
        public int BookId { get; set; }
        public int CurrentStock { get; set; } = 0;
        public int MinimumStock { get; set; } = 5;
        public int MaximumStock { get; set; } = 100;
        public int ReorderLevel { get; set; } = 10;
        public DateTime? LastUpdated { get; set; } = DateTime.Now;
        // Navigation Properties
        public virtual Company? Company { get; set; }
        public virtual Site? Site { get; set; }
        public virtual Book? Book { get; set; }
    }
}
