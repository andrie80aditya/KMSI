using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("Sites")]
    public class Site
    {
        [Key]
        public int SiteId { get; set; }
        public int CompanyId { get; set; }
        [StringLength(10)]
        public required string SiteCode { get; set; }
        [StringLength(100)]
        public required string SiteName { get; set; }
        [StringLength(500)]
        public string? Address { get; set; }
        [StringLength(50)]
        public string? City { get; set; }
        [StringLength(50)]
        public string? Province { get; set; }
        [StringLength(20)]
        public string? Phone { get; set; }
        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }
        [StringLength(100)]
        public string? ManagerName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        // Navigation Properties
        public virtual Company? Company { get; set; }
        public virtual ICollection<User>? Users { get; set; }
        public virtual ICollection<Teacher>? Teachers { get; set; }
        public virtual ICollection<Student>? Students { get; set; }
        public virtual ICollection<Registration>? Registrations { get; set; }
        public virtual ICollection<Inventory>? Inventories { get; set; }
        public virtual ICollection<StockMovement>? StockMovements { get; set; }
        public virtual ICollection<TeacherPayroll>? TeacherPayrolls { get; set; }
        public virtual ICollection<StudentBilling>? StudentBillings { get; set; }
    }
}
