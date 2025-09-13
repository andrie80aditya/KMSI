using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("UserLevels")]
    public class UserLevel
    {
        [Key]
        public int UserLevelId { get; set; }

        [StringLength(20)]
        public required string LevelCode { get; set; }

        [StringLength(50)]
        public required string LevelName { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        public int? SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<User>? Users { get; set; }
    }
}
