using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("SystemNotifications")]
    public class SystemNotification
    {
        [Key]
        public int NotificationId { get; set; }
        public int CompanyId { get; set; }
        public int UserId { get; set; }
        [StringLength(255)]
        public required string Title { get; set; }
        [StringLength(1000)]
        public required string Message { get; set; }
        [StringLength(50)]
        public required string NotificationType { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        // Navigation Properties
        public virtual Company? Company { get; set; }
        public virtual User? User { get; set; }
    }
}
