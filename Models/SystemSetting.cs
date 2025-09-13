using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("SystemSettings")]
    public class SystemSetting
    {
        [Key]
        public int SettingId { get; set; }
        public int? CompanyId { get; set; }
        [StringLength(100)]
        public required string SettingKey { get; set; }
        [StringLength(1000)]
        public required string SettingValue { get; set; }
        [StringLength(20)]
        public string DataType { get; set; } = "String";
        [StringLength(255)]
        public string? Description { get; set; }
        [StringLength(50)]
        public string? Category { get; set; }
        public bool IsEditable { get; set; } = true;
        public DateTime? UpdatedDate { get; set; } = DateTime.Now;
        public int? UpdatedBy { get; set; }
        // Navigation Properties
        public virtual Company? Company { get; set; }
    }
}
