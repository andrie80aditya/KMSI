using KMSI.Models;

namespace KMSI.Services
{
    public interface IPdfService
    {
        byte[] GenerateCertificatePdf(Certificate certificate);
        byte[] GenerateBillingPdf(StudentBilling billing);
    }
}
