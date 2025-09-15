using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;

namespace KMSI.Services
{
    public class SmtpSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromEmail { get; set; } = "";
        public string FromName { get; set; } = "";
    }

    public interface IEmailService
    {
        Task<bool> SendCertificateEmailAsync(string studentEmail, string parentEmail,
            string studentName, byte[] certificatePdf, string certificateNumber);
        Task<bool> SendBillingEmailAsync(string studentEmail, string parentEmail,
            string studentName, byte[] billingPdf, string billingNumber, decimal totalAmount, DateTime dueDate);
    }
    
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;

        public EmailService(IOptions<SmtpSettings> smtpSettings)
        {
            _smtpSettings = smtpSettings.Value;
        }

        public async Task<bool> SendCertificateEmailAsync(string studentEmail, string parentEmail,
            string studentName, byte[] certificatePdf, string certificateNumber)
        {
            try
            {
                // Validate SMTP settings first
                if (string.IsNullOrEmpty(_smtpSettings.Host) ||
                    string.IsNullOrEmpty(_smtpSettings.Username) ||
                    string.IsNullOrEmpty(_smtpSettings.Password))
                {
                    Console.WriteLine("SMTP settings are incomplete");
                    return false;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));

                // Add student email if available
                if (!string.IsNullOrEmpty(studentEmail))
                {
                    message.To.Add(new MailboxAddress(studentName, studentEmail));
                }

                // Add parent email as CC if available
                if (!string.IsNullOrEmpty(parentEmail))
                {
                    message.Cc.Add(new MailboxAddress($"Parent of {studentName}", parentEmail));
                }

                // Skip if no recipients
                if (!message.To.Any() && !message.Cc.Any())
                {
                    return false;
                }

                message.Subject = $"Certificate - {certificateNumber}";

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = $@"
                    <h2>Certificate Issued</h2>
                    <p>Dear {studentName},</p>
                    <p>Congratulations! Your certificate has been issued.</p>
                    <p><strong>Certificate Number:</strong> {certificateNumber}</p>
                    <p>Please find your certificate attached to this email.</p>
                    <br>
                    <p>Best regards,<br>
                    Kawai Music School Indonesia</p>
                ";

                // Attach PDF
                bodyBuilder.Attachments.Add($"Certificate_{certificateNumber}.pdf", certificatePdf, ContentType.Parse("application/pdf"));

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                // Configure SSL/TLS properly
                client.ServerCertificateValidationCallback = (s, c, h, e) => true; // For development only

                // Connect with STARTTLS
                await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);

                // Authenticate
                await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                // Log the error with more detail
                Console.WriteLine($"Email sending failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> SendBillingEmailAsync(string studentEmail, string parentEmail,
            string studentName, byte[] billingPdf, string billingNumber, decimal totalAmount, DateTime dueDate)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));

                if (!string.IsNullOrEmpty(studentEmail))
                {
                    message.To.Add(new MailboxAddress(studentName, studentEmail));
                }

                if (!string.IsNullOrEmpty(parentEmail))
                {
                    message.Cc.Add(new MailboxAddress($"Parent of {studentName}", parentEmail));
                }

                if (!message.To.Any() && !message.Cc.Any())
                {
                    return false;
                }

                message.Subject = $"Invoice - {billingNumber}";

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 20px; text-align: center; color: white;'>
                            <h1 style='margin: 0;'>Invoice</h1>
                        </div>
                        <div style='padding: 20px; background-color: #f8f9fa;'>
                            <p>Dear <strong>{studentName}</strong>,</p>
                            <p>Please find your invoice attached for your music lessons.</p>
                            <div style='background: white; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <p><strong>Invoice Number:</strong> {billingNumber}</p>
                                <p><strong>Total Amount:</strong> IDR {totalAmount:N0}</p>
                                <p><strong>Due Date:</strong> {dueDate:dd MMMM yyyy}</p>
                            </div>
                            <div style='background: #fff3cd; padding: 15px; border-radius: 5px; border-left: 4px solid #ffc107; margin: 20px 0;'>
                                <p style='margin: 0; color: #856404;'><strong>Payment Instructions:</strong></p>
                                <p style='margin: 5px 0 0 0; color: #856404;'>Please make payment before the due date to avoid any service interruption.</p>
                            </div>
                            <p>If you have any questions about this invoice, please contact us.</p>
                            <p style='margin-top: 30px;'>Best regards,<br>
                            <strong>Kawai Music School Indonesia</strong></p>
                        </div>
                    </div>
                ";

                bodyBuilder.Attachments.Add($"Invoice_{billingNumber}.pdf", billingPdf, ContentType.Parse("application/pdf"));

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Billing email sending failed: {ex.Message}");
                return false;
            }
        }
    }
}
