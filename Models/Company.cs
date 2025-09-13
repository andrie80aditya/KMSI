using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("Companies")]
    public class Company
    {
        [Key]
        public int CompanyId { get; set; }
        [StringLength(3)]
        public required string CompanyCode { get; set; }
        [StringLength(100)]
        public required string CompanyName { get; set; }
        public int? ParentCompanyId { get; set; }
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
        public bool IsHeadOffice { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        // Navigation Properties
        public virtual Company? ParentCompany { get; set; }
        public virtual ICollection<Company>? SubCompanies { get; set; }
        public virtual ICollection<Site>? Sites { get; set; }
        public virtual ICollection<User>? Users { get; set; }
        public virtual ICollection<Teacher>? Teachers { get; set; }
        public virtual ICollection<Student>? Students { get; set; }
        public virtual ICollection<Grade>? Grades { get; set; }
        public virtual ICollection<SystemSetting>? SystemSettings { get; set; }
        public virtual ICollection<SystemNotification>? SystemNotifications { get; set; }
        public virtual ICollection<EmailTemplate>? EmailTemplates { get; set; }
        public virtual ICollection<EmailLog>? EmailLogs { get; set; }
    }
}
