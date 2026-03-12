using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Azure.Identity;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

#pragma warning disable OPENAI001

//await SpeechTest.Run();
string speechText = await SpeechTest.Run();
Console.WriteLine(speechText);

// Read client addresses from JSON file
string clientsJson = File.ReadAllText("clients.json");
Console.WriteLine("Client data loaded:");
Console.WriteLine(clientsJson);

// Set QuestPDF license for evaluation/testing
QuestPDF.Settings.License = LicenseType.Community;

const string projectEndpoint = "https://rechnungen-resource.services.ai.azure.com/api/projects/rechnungen";
const string agentName = "rechnungen-agent";

AIProjectClient projectClient = new(
    endpoint: new Uri(projectEndpoint),
    tokenProvider: new DefaultAzureCredential());

var responseClient = projectClient.OpenAI.GetProjectResponsesClientForAgent(agentName);

// Combine speech text with client data for the LLM prompt
string prompt = $"""
Use the following client addresses data:
{clientsJson}

Based on this data and the user request: {speechText}
Create invoice data in JSON format.
""";

var response = responseClient.CreateResponse(prompt);

string outputText = response.Value.GetOutputText();
Console.WriteLine(outputText);

// Try to parse the JSON response
InvoiceData? invoiceData = null;
try
{
    invoiceData = JsonSerializer.Deserialize<InvoiceData>(outputText);
}
catch (JsonException)
{
    Console.WriteLine("The AI response is not in the expected JSON format. Unable to generate PDF.");
    return;
}

if (invoiceData == null)
{
    Console.WriteLine("Failed to parse invoice data. Unable to generate PDF.");
    return;
}

// Format the date to DD-MM-YYYY
var invoiceDateTime = DateTime.ParseExact(invoiceData.invoice_date, "yyyy-MM-dd", null);
var formattedDate = invoiceDateTime.ToString("dd-MM-yyyy");

// Generate PDF
var document = Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

        // Header with company info
        page.Header()
            .Column(column =>
            {
                column.Item().Text("Celia Fatia")
                    .FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text("Anneliese-Hoevel-Str. 8").FontSize(10);
                column.Item().Text("60435 Frankfurt").FontSize(10);
                column.Item().Text("Telefon: 0123 456789").FontSize(10);
                column.Item().Text("E-Mail: info@celiafattia.de").FontSize(10);
            });

        // Invoice title
        page.Content()
            .PaddingVertical(2, QuestPDF.Infrastructure.Unit.Centimetre)
            .Column(x =>
            {
                x.Spacing(20);

                // Invoice header
                x.Item().Row(row =>
                {
                    row.RelativeItem().Text("RECHNUNG").FontSize(24).Bold().FontColor(Colors.Red.Darken2);
                    row.ConstantItem(150).AlignRight().Text($"Rechnungsdatum: {formattedDate}").FontSize(12);
                });

                x.Item().Text($"Kunde: {invoiceData.customer_name}").FontSize(12).Bold();

                // Invoice table
                x.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(4); // Description
                        columns.RelativeColumn(1); // Quantity
                        columns.RelativeColumn(1); // Unit Price
                        columns.RelativeColumn(1); // Total
                    });

                    // Header row
                    table.Header(header =>
                    {
                        header.Cell().Element(Block).Text("Leistung").Bold();
                        header.Cell().Element(Block).Text("Menge").Bold();
                        header.Cell().Element(Block).Text("Einzelpreis").Bold();
                        header.Cell().Element(Block).Text("Gesamt").Bold();

                        static IContainer Block(IContainer container)
                        {
                            return container
                                .Border(1)
                                .Background(Colors.Grey.Lighten3)
                                .Padding(5)
                                .AlignCenter()
                                .AlignMiddle();
                        }
                    });

                    // Data row
                    table.Cell().Element(Block).Text(invoiceData.description);
                    table.Cell().Element(Block).Text("1").AlignCenter();
                    table.Cell().Element(Block).Text($"{invoiceData.amount_net:N2} {invoiceData.currency}").AlignRight();
                    table.Cell().Element(Block).Text($"{invoiceData.amount_net:N2} {invoiceData.currency}").AlignRight();

                    static IContainer Block(IContainer container)
                    {
                        return container
                            .Border(1)
                            .Padding(5)
                            .AlignMiddle();
                    }
                });

                // Totals section
                x.Item().Column(totals =>
                {
                    totals.Item().AlignRight().Text($"Nettobetrag: {invoiceData.amount_net:N2} {invoiceData.currency}").FontSize(12);
                    totals.Item().AlignRight().Text($"MwSt. ({invoiceData.tax_rate}%): {(invoiceData.amount_total - invoiceData.amount_net):N2} {invoiceData.currency}").FontSize(12);
                    totals.Item().AlignRight().Text($"Gesamtbetrag: {invoiceData.amount_total:N2} {invoiceData.currency}").FontSize(14).Bold();
                });

                // Payment terms
                x.Item().PaddingTop(20).Text("Zahlungsbedingungen: Zahlbar innerhalb von 14 Tagen ohne Abzug.").FontSize(10).Italic();
                x.Item().Text("Bitte überweisen Sie den Betrag auf unser Konto:").FontSize(10);
                x.Item().Text("IBAN: DE12 3456 7890 1234 5678 90").FontSize(10);
                x.Item().Text("BIC: ABCDEF12").FontSize(10);
            });

        page.Footer()
            .AlignCenter()
            .Text(x =>
            {
                x.Span("Seite ");
                x.CurrentPageNumber();
                x.Span(" von ");
                x.TotalPages();
            });
    });
});

document.GeneratePdf("Rechnung.pdf");
Console.WriteLine("PDF generated: Rechnung.pdf");

public class InvoiceData
{
    public required string customer_name { get; set; }
    public required string invoice_date { get; set; }
    public required string description { get; set; }
    public required decimal amount_net { get; set; }
    public required int tax_rate { get; set; }
    public required decimal amount_total { get; set; }
    public required string currency { get; set; }
}
