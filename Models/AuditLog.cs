using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }
        public int CompanyId { get; set; }
        public int UserId { get; set; }
        [StringLength(100)]
        public required string TableName { get; set; }
        public int RecordId { get; set; }
        [StringLength(20)]
        public required string Action { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        [StringLength(45)]
        public string? IPAddress { get; set; }
        [StringLength(500)]
        public string? UserAgent { get; set; }
        public DateTime? ActionDate { get; set; } = DateTime.Now;
        // Navigation Properties
        public virtual Company? Company { get; set; }
        public virtual User? User { get; set; }
    }
}
