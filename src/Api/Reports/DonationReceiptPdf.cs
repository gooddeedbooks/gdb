using Dto.Donations;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Api.Reports
{
    public static class DonationReceiptPdf
    {
        private const string Background = "#dfe6cd";

        public static byte[] Generate(IEnumerable<DonationReceipt> receipts)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var flagPath = Path.Combine(AppContext.BaseDirectory, "Assets", "canada-flag.png");
            var flag = File.Exists(flagPath) ? File.ReadAllBytes(flagPath) : null;

            return Document.Create(document =>
            {
                foreach (var receipt in receipts)
                {
                    document.Page(page =>
                    {
                        page.Size(PageSizes.Letter);
                        page.Margin(36);
                        page.DefaultTextStyle(text => text.FontSize(11));
                        page.Content().Element(content => Compose(content, receipt, flag));
                    });
                }
            }).GeneratePdf();
        }

        private static void Compose(IContainer container, DonationReceipt r, byte[]? flag)
        {
            container.ShrinkVertical().Background(Background).CornerRadius(16).Padding(24).Column(column =>
            {
                column.Spacing(14);
                column.Item().Row(row =>
                {
                    if (flag != null)
                        row.ConstantItem(60).AlignMiddle().Image(flag);
                    row.RelativeItem().PaddingLeft(10).AlignMiddle().Text("Official donation receipt for income tax purposes").FontSize(17).Bold();
                    row.ConstantItem(110).Background(Colors.White).CornerRadius(8).Padding(6).AlignCenter().Text("Receipt # " + r.ReceiptNo).Bold();
                });
                column.Item().Row(row =>
                {
                    row.RelativeItem(3).Column(c =>
                    {
                        c.Item().Text(r.CharityName).FontSize(14).Bold();
                        c.Item().Text(Join(r.CharityStreet, r.CharityCity, r.CharityProvince, r.CharityPostalCode)).Bold();
                        c.Item().Text("Charitable registration #: " + r.RegistrationNumber).Bold();
                    });
                    row.RelativeItem(2).Column(c =>
                    {
                        c.Item().Text(t => { t.Span("Receipt issued  ").Bold(); t.Span(r.IssuedDate.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture)); });
                        c.Item().PaddingTop(4).Text(t => { t.Span("Location issued  ").Bold(); t.Span(r.LocationIssued); });
                    });
                });
                column.Item().Element(c => Field(c, "Donated by", r.DonorName, "(First and last name, and initial)"));
                column.Item().Element(c => Field(c, "Address", Join(r.DonorStreet, r.DonorCity, r.DonorProvince, r.DonorPostalCode), null));
                column.Item().Row(row =>
                {
                    row.RelativeItem().Element(c => Field(c, "Donation received", r.DonationDate.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture), "(Date or year)"));
                    row.ConstantItem(20);
                    row.RelativeItem().Column(c =>
                    {
                        c.Spacing(10);
                        c.Item().Element(x => Field(x, "Amount of gift", r.Amount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-CA")), null));
                        if (r.AdvantageAmount > 0)
                            c.Item().Element(x => Field(x, "Advantage", r.AdvantageAmount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-CA")), null));
                        c.Item().Column(e =>
                        {
                            e.Item().Text("Eligible amount of gift").Bold();
                            e.Item().Background(Colors.White).CornerRadius(8).Padding(8).Text(r.EligibleAmount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-CA"))).Bold();
                        });
                    });
                });
                column.Item().AlignRight().Width(330).Background(Colors.White).CornerRadius(8).Padding(14).PaddingTop(26).Row(row =>
                {
                    row.AutoItem().Text("Authorized signature:").Bold();
                    row.RelativeItem().PaddingLeft(6).BorderBottom(1).Text(string.Empty);
                });
                column.Item().AlignCenter().Text("Canada Revenue Agency  -  canada.ca/charities-giving").Bold();
            });
        }

        private static void Field(IContainer container, string label, string value, string? hint)
        {
            container.Column(c =>
            {
                c.Item().Text(label).Bold();
                c.Item().BorderBottom(1).PaddingVertical(3).PaddingLeft(6).Text(value);
                if (hint != null)
                    c.Item().Text(hint).FontSize(9);
            });
        }

        private static string Join(params string[] parts) => string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}
