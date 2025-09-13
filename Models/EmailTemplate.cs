using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("EmailTemplates")]
    public class EmailTemplate
    {
        [Key]
        public int EmailTemplateId { get; set; }
        public int CompanyId { get; set; }
        [StringLength(100)]
        public required string TemplateName { get; set; }
        [StringLength(50)]
        public required string TemplateType { get; set; }
        [StringLength(255)]
        public required string Subject { get; set; }
        public required string Body { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        // Navigation Properties
        public virtual Company? Company { get; set; }
        public virtual ICollection<EmailLog>? EmailLogs { get; set; }
    }
}
