namespace KMSI.Models.ViewModels
{
    public class GradeViewModel
    {
        public int GradeId { get; set; }
        public int CompanyId { get; set; }
        public string GradeCode { get; set; } = "";
        public string GradeName { get; set; } = "";
        public string? Description { get; set; }
        public int? Duration { get; set; }
        public int? SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
