namespace KMSI.Models.ViewModels
{
    public class CertificateViewModel
    {
        public int CertificateId { get; set; }
        public int StudentId { get; set; }
        public int StudentExaminationId { get; set; }
        public string CertificateNumber { get; set; } = "";
        public int GradeId { get; set; }
        public DateTime IssueDate { get; set; } = DateTime.Today;
        public string CertificateTitle { get; set; } = "";
        public string IssuedBy { get; set; } = "";
        public string? SignedBy { get; set; }
        public string? CertificatePath { get; set; }
        public string Status { get; set; } = "Issued";
        public string? Notes { get; set; }

        // Additional properties for display/creation
        public string? StudentName { get; set; }
        public string? StudentCode { get; set; }
        public string? GradeName { get; set; }
        public decimal? ExamScore { get; set; }
        public string? ExamResult { get; set; }
        public DateTime? ExamDate { get; set; }
    }

    public class GenerateCertificateViewModel
    {
        public int StudentExaminationId { get; set; }
        public string CertificateTitle { get; set; } = "";
        public string IssuedBy { get; set; } = "";
        public string? SignedBy { get; set; }
        public string? Notes { get; set; }
        public bool AutoGenerateNumber { get; set; } = true;
        public string? CustomCertificateNumber { get; set; }
    }
}
