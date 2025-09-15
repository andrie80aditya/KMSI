using iTextSharp.text.pdf;
using iTextSharp.text;
using KMSI.Models;

namespace KMSI.Services
{
    public class PdfService : IPdfService
    {
        public byte[] GenerateCertificatePdf(Certificate certificate)
        {
            using var memoryStream = new MemoryStream();
            var document = new Document(PageSize.A4.Rotate(), 50, 50, 50, 50);
            var writer = PdfWriter.GetInstance(document, memoryStream);

            document.Open();

            // Title
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 24, BaseColor.Blue);
            var title = new Paragraph(certificate.CertificateTitle, titleFont);
            title.Alignment = Element.ALIGN_CENTER;
            title.SpacingAfter = 20f;
            document.Add(title);

            // Certificate Number
            var certNoFont = FontFactory.GetFont(FontFactory.HELVETICA, 12, BaseColor.Gray);
            var certNo = new Paragraph($"Certificate No: {certificate.CertificateNumber}", certNoFont);
            certNo.Alignment = Element.ALIGN_CENTER;
            certNo.SpacingAfter = 30f;
            document.Add(certNo);

            // Main Content
            var contentFont = FontFactory.GetFont(FontFactory.HELVETICA, 16);
            var content = new Paragraph();
            content.Alignment = Element.ALIGN_CENTER;
            content.Leading = 24f;

            content.Add(new Chunk("This is to certify that\n\n", contentFont));

            var nameFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20, BaseColor.Black);
            content.Add(new Chunk($"{certificate.Student?.FirstName} {certificate.Student?.LastName}\n", nameFont));
            content.Add(new Chunk($"({certificate.Student?.StudentCode})\n\n", contentFont));

            content.Add(new Chunk($"has successfully completed the requirements for\n\n", contentFont));

            var gradeFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.Blue);
            content.Add(new Chunk($"{certificate.Grade?.GradeName}\n\n", gradeFont));

            if (certificate.StudentExamination?.Score.HasValue == true)
            {
                content.Add(new Chunk($"with a score of {certificate.StudentExamination.Score}/{certificate.StudentExamination.MaxScore}\n", contentFont));
                content.Add(new Chunk($"Grade: {certificate.StudentExamination.Grade}\n\n", contentFont));
            }

            content.Add(new Chunk($"on {certificate.IssueDate:dd MMMM yyyy}\n\n", contentFont));

            document.Add(content);

            // Footer
            var table = new PdfPTable(2);
            table.WidthPercentage = 100;
            table.SpacingBefore = 50f;

            var leftCell = new PdfPCell();
            leftCell.Border = Rectangle.NO_BORDER;
            leftCell.AddElement(new Paragraph($"Issued by:\n{certificate.IssuedBy}", contentFont));
            table.AddCell(leftCell);

            var rightCell = new PdfPCell();
            rightCell.Border = Rectangle.NO_BORDER;
            if (!string.IsNullOrEmpty(certificate.SignedBy))
            {
                rightCell.AddElement(new Paragraph($"Signed by:\n\n\n{certificate.SignedBy}", contentFont));
            }
            table.AddCell(rightCell);

            document.Add(table);

            document.Close();
            return memoryStream.ToArray();
        }

        public byte[] GenerateBillingPdf(StudentBilling billing)
        {
            using var memoryStream = new MemoryStream();
            var document = new Document(PageSize.A4, 40, 40, 40, 40);
            var writer = PdfWriter.GetInstance(document, memoryStream);

            document.Open();

            // Header
            var headerTable = new PdfPTable(2);
            headerTable.WidthPercentage = 100;
            headerTable.SetWidths(new float[] { 3f, 2f });

            // Left side - Company info
            var companyCell = new PdfPCell();
            companyCell.Border = Rectangle.NO_BORDER;
            var companyFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, BaseColor.Blue);
            var companyPara = new Paragraph();
            companyPara.Add(new Chunk("KAWAI MUSIC SCHOOL INDONESIA\n", companyFont));

            var addressFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            companyPara.Add(new Chunk($"{billing.Site?.Address}\n", addressFont));
            companyPara.Add(new Chunk($"{billing.Site?.City}, {billing.Site?.Province}\n", addressFont));
            companyPara.Add(new Chunk($"Phone: {billing.Site?.Phone}\n", addressFont));
            companyPara.Add(new Chunk($"Email: {billing.Site?.Email}", addressFont));

            companyCell.AddElement(companyPara);
            headerTable.AddCell(companyCell);

            // Right side - Invoice info
            var invoiceCell = new PdfPCell();
            invoiceCell.Border = Rectangle.NO_BORDER;
            invoiceCell.HorizontalAlignment = Element.ALIGN_RIGHT;

            var invoiceFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.Red);
            var invoicePara = new Paragraph();
            invoicePara.Add(new Chunk("INVOICE\n", invoiceFont));

            var detailFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            invoicePara.Add(new Chunk($"Invoice #: {billing.BillingNumber}\n", detailFont));
            invoicePara.Add(new Chunk($"Date: {billing.BillingDate:dd/MM/yyyy}\n", detailFont));
            invoicePara.Add(new Chunk($"Due Date: {billing.DueDate:dd/MM/yyyy}\n", detailFont));
            invoicePara.Add(new Chunk($"Status: {billing.Status}", detailFont));

            invoiceCell.AddElement(invoicePara);
            headerTable.AddCell(invoiceCell);

            document.Add(headerTable);
            document.Add(new Paragraph(" ")); // Space

            // Bill To section
            var billToFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
            var billToPara = new Paragraph("BILL TO:", billToFont);
            billToPara.SpacingAfter = 10f;
            document.Add(billToPara);

            var studentFont = FontFactory.GetFont(FontFactory.HELVETICA, 11);
            var studentInfo = new Paragraph();
            studentInfo.Add(new Chunk($"{billing.Student?.FirstName} {billing.Student?.LastName}\n", studentFont));
            studentInfo.Add(new Chunk($"Student Code: {billing.Student?.StudentCode}\n", studentFont));
            studentInfo.Add(new Chunk($"Grade: {billing.Grade?.GradeName}\n", studentFont));
            if (!string.IsNullOrEmpty(billing.Student?.Address))
            {
                studentInfo.Add(new Chunk($"Address: {billing.Student.Address}\n", studentFont));
            }
            if (!string.IsNullOrEmpty(billing.Student?.Phone))
            {
                studentInfo.Add(new Chunk($"Phone: {billing.Student.Phone}\n", studentFont));
            }
            studentInfo.SpacingAfter = 20f;
            document.Add(studentInfo);

            // Items table
            var itemsTable = new PdfPTable(4);
            itemsTable.WidthPercentage = 100;
            itemsTable.SetWidths(new float[] { 4f, 1f, 2f, 2f });

            // Table headers
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.White);
            var headerCells = new string[] { "Description", "Qty", "Unit Price", "Amount" };

            foreach (var headerText in headerCells)
            {
                var cell = new PdfPCell(new Phrase(headerText, headerFont));
                cell.BackgroundColor = BaseColor.DarkGray;
                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                cell.Padding = 8f;
                itemsTable.AddCell(cell);
            }

            var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

            // Tuition fee
            itemsTable.AddCell(new PdfPCell(new Phrase($"Tuition Fee - {billing.Grade?.GradeName}", cellFont)) { Padding = 6f });
            itemsTable.AddCell(new PdfPCell(new Phrase("1", cellFont)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6f });
            itemsTable.AddCell(new PdfPCell(new Phrase($"IDR {billing.TuitionFee:N0}", cellFont)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 6f });
            itemsTable.AddCell(new PdfPCell(new Phrase($"IDR {billing.TuitionFee:N0}", cellFont)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 6f });

            // Book fees if any
            if (billing.BookFees > 0)
            {
                itemsTable.AddCell(new PdfPCell(new Phrase("Book Fees", cellFont)) { Padding = 6f });
                itemsTable.AddCell(new PdfPCell(new Phrase("1", cellFont)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6f });
                itemsTable.AddCell(new PdfPCell(new Phrase($"IDR {billing.BookFees:N0}", cellFont)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 6f });
                itemsTable.AddCell(new PdfPCell(new Phrase($"IDR {billing.BookFees:N0}", cellFont)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 6f });
            }

            // Other fees if any
            if (billing.OtherFees > 0)
            {
                itemsTable.AddCell(new PdfPCell(new Phrase("Other Fees", cellFont)) { Padding = 6f });
                itemsTable.AddCell(new PdfPCell(new Phrase("1", cellFont)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 6f });
                itemsTable.AddCell(new PdfPCell(new Phrase($"IDR {billing.OtherFees:N0}", cellFont)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 6f });
                itemsTable.AddCell(new PdfPCell(new Phrase($"IDR {billing.OtherFees:N0}", cellFont)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 6f });
            }

            document.Add(itemsTable);

            // Totals section
            var totalsTable = new PdfPTable(2);
            totalsTable.WidthPercentage = 50;
            totalsTable.HorizontalAlignment = Element.ALIGN_RIGHT;
            totalsTable.SetWidths(new float[] { 1f, 1f });
            totalsTable.SpacingBefore = 20f;

            var totalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);

            var subtotal = billing.TuitionFee + (billing.BookFees ?? 0) + (billing.OtherFees ?? 0);

            // Subtotal
            totalsTable.AddCell(new PdfPCell(new Phrase("Subtotal:", totalFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 5f });
            totalsTable.AddCell(new PdfPCell(new Phrase($"IDR {subtotal:N0}", totalFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 5f });

            // Discount if any
            if (billing.Discount > 0)
            {
                totalsTable.AddCell(new PdfPCell(new Phrase("Discount:", totalFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 5f });
                totalsTable.AddCell(new PdfPCell(new Phrase($"-IDR {billing.Discount:N0}", totalFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 5f });
            }

            // Tax if any
            if (billing.Tax > 0)
            {
                totalsTable.AddCell(new PdfPCell(new Phrase("Tax:", totalFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 5f });
                totalsTable.AddCell(new PdfPCell(new Phrase($"IDR {billing.Tax:N0}", totalFont)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 5f });
            }

            // Total
            var totalAmount = subtotal - (billing.Discount ?? 0) + (billing.Tax ?? 0);
            totalsTable.AddCell(new PdfPCell(new Phrase("TOTAL:", boldFont)) { Border = Rectangle.TOP_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 8f });
            totalsTable.AddCell(new PdfPCell(new Phrase($"IDR {totalAmount:N0}", boldFont)) { Border = Rectangle.TOP_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 8f });

            document.Add(totalsTable);

            // Payment information if paid
            if (billing.Status == "Paid" && billing.PaymentDate.HasValue)
            {
                document.Add(new Paragraph(" ")); // Space
                var paymentFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.Green);
                var paymentInfo = new Paragraph("PAYMENT INFORMATION", paymentFont);
                paymentInfo.SpacingAfter = 10f;
                document.Add(paymentInfo);

                var paymentDetails = new Paragraph();
                paymentDetails.Add(new Chunk($"Payment Date: {billing.PaymentDate.Value:dd/MM/yyyy}\n", totalFont));
                paymentDetails.Add(new Chunk($"Payment Amount: IDR {billing.PaymentAmount:N0}\n", totalFont));
                paymentDetails.Add(new Chunk($"Payment Method: {billing.PaymentMethod}\n", totalFont));
                if (!string.IsNullOrEmpty(billing.PaymentReference))
                {
                    paymentDetails.Add(new Chunk($"Reference: {billing.PaymentReference}\n", totalFont));
                }
                document.Add(paymentDetails);
            }

            // Notes if any
            if (!string.IsNullOrEmpty(billing.Notes))
            {
                document.Add(new Paragraph(" ")); // Space
                var notesTitle = new Paragraph("Notes:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10));
                notesTitle.SpacingAfter = 5f;
                document.Add(notesTitle);

                var notes = new Paragraph(billing.Notes, FontFactory.GetFont(FontFactory.HELVETICA, 9));
                document.Add(notes);
            }

            // Footer
            document.Add(new Paragraph(" ")); // Space
            var footerFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.Gray);
            var footer = new Paragraph("Thank you for choosing Kawai Music School Indonesia!", footerFont);
            footer.Alignment = Element.ALIGN_CENTER;
            footer.SpacingBefore = 30f;
            document.Add(footer);

            document.Close();
            return memoryStream.ToArray();
        }
    }
}
