using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace KMSI.Models
{
    [Table("Students")]
    public class Student
    {
        [Key]
        public int StudentId { get; set; }
        public int CompanyId { get; set; }
        public int SiteId { get; set; }
        [StringLength(20)]
        public required string StudentCode { get; set; }
        [StringLength(50)]
        public required string FirstName { get; set; }
        [StringLength(50)]
        public required string LastName { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        [StringLength(1)]
        public string? Gender { get; set; }
        [StringLength(20)]
        public string? Phone { get; set; }
        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }
        [StringLength(500)]
        public string? Address { get; set; }
        [StringLength(50)]
        public string? City { get; set; }
        [StringLength(100)]
        public string? ParentName { get; set; }
        [StringLength(20)]
        public string? ParentPhone { get; set; }
        [StringLength(100)]
        [EmailAddress]
        public string? ParentEmail { get; set; }
        [StringLength(255)]
        public string? PhotoPath { get; set; }
        public DateOnly RegistrationDate { get; set; }
        [StringLength(20)]
        public string Status { get; set; } = "Pending";
        public int? CurrentGradeId { get; set; }
        public int? AssignedTeacherId { get; set; }
        [StringLength(1000)]
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        // Navigation Properties
        public virtual Company? Company { get; set; }
        public virtual Site? Site { get; set; }
        public virtual Grade? CurrentGrade { get; set; }
        public virtual Teacher? AssignedTeacher { get; set; }
        public virtual ICollection<StudentBilling>? StudentBillings { get; set; }
        public virtual ICollection<StudentExamination>? StudentExaminations { get; set; }
        public virtual ICollection<StudentGradeHistory>? StudentGradeHistories { get; set; }
    }
}
