using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KMSI.Models
{
    [Table("GradeBooks")]
    public class GradeBook
    {
        [Key]
        public int GradeBookId { get; set; }
        public int GradeId { get; set; }
        public int BookId { get; set; }
        public bool IsRequired { get; set; } = true;
        public int Quantity { get; set; } = 1;
        public int? SortOrder { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }
        // Navigation Properties
        public virtual Grade? Grade { get; set; }
        public virtual Book? Book { get; set; }
    }
}
