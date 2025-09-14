namespace KMSI.Models.ViewModels
{
    public class BookViewModel
    {
        public int BookId { get; set; }
        public int CompanyId { get; set; }
        public string BookCode { get; set; } = "";
        public string BookTitle { get; set; } = "";
        public string? Author { get; set; }
        public string? Publisher { get; set; }
        public string? ISBN { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
