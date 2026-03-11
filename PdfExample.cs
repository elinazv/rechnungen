using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FoundryTest
{
    public static class PdfExample
    {
        public static void GenerateSamplePdf()
        {
            // Set license for evaluation/testing
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(20));

                    page.Header()
                        .Text("Sample Invoice")
                        .SemiBold().FontSize(36).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(20);

                            x.Item().Text("Customer: Example Company");
                            x.Item().Text("Description: Sample Service");
                            x.Item().Text("Net Amount: 1000 EUR");
                            x.Item().Text("Tax Rate: 19%");
                            x.Item().Text("Total Amount: 1190 EUR");
                            x.Item().Text("Date: 2026-03-11");
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                        });
                });
            });

            document.GeneratePdf("sample_invoice.pdf");
            Console.WriteLine("Sample PDF generated: sample_invoice.pdf");
        }
    }
}