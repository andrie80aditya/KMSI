using Microsoft.VisualBasic;

namespace KMSI.Models.ViewModels
{
    public class UserViewModel
    {
        public int UserId { get; set; }
        public int CompanyId { get; set; }
        public int? SiteId { get; set; }
        public int UserLevelId { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Password { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
