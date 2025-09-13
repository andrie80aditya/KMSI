using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KMSI.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public int CompanyId { get; set; }

        public int? SiteId { get; set; }

        [Required]
        public int UserLevelId { get; set; }

        [StringLength(50)]
        public required string Username { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public required string Email { get; set; }

        [StringLength(255)]
        public required string PasswordHash { get; set; }

        [StringLength(50)]
        public required string FirstName { get; set; }

        [StringLength(50)]
        public required string LastName { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(1)]
        public string? Gender { get; set; }

        [StringLength(255)]
        public string? PhotoPath { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastLoginDate { get; set; }

        public DateTime? CreatedDate { get; set; } = DateTime.Now;

        public int? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public int? UpdatedBy { get; set; }

        // Navigation Properties
        public virtual Company? Company { get; set; }
        public virtual Site? Site { get; set; }
        public virtual UserLevel? UserLevel { get; set; }
    }
}
