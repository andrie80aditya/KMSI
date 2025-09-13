using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("EmailLogs")]
    public class EmailLog
    {
        [Key]
        public int EmailLogId { get; set; }
        public int CompanyId { get; set; }
        public int? EmailTemplateId { get; set; }
        [StringLength(20)]
        public required string RecipientType { get; set; }
        public int RecipientId { get; set; }
        [StringLength(100)]
        [EmailAddress]
        public required string RecipientEmail { get; set; }
        [StringLength(255)]
        public required string Subject { get; set; }
        public string? Body { get; set; }
        [StringLength(20)]
        public required string Status { get; set; }
        public DateTime? SentDate { get; set; }
        [StringLength(500)]
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; } = 0;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        // Navigation Properties
        public virtual Company? Company { get; set; }
        public virtual EmailTemplate? EmailTemplate { get; set; }
    }
}
