using Application.Interfaces;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.IO;

namespace Persistence.Services;

/// <summary>
/// Generates PDF license agreement documents using PdfSharpCore
/// </summary>
public class LicensePdfService : ILicensePdfService
{
    public byte[] GenerateLicensePdf(string buyerName, string companyName, int datasetId, DateTime purchaseDate)
    {
        using var document = new PdfDocument();
        document.Info.Title = "Nadena Data License Agreement";

        var page = document.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;
        var gfx = XGraphics.FromPdfPage(page);
        var fontTitle = new XFont("Arial", 20, XFontStyle.Bold);
        var fontHeading = new XFont("Arial", 14, XFontStyle.Bold);
        var fontBody = new XFont("Arial", 11, XFontStyle.Regular);
        var fontSmall = new XFont("Arial", 9, XFontStyle.Italic);

        double y = 40;
        double marginX = 50;
        double lineHeight = 20;
        double sectionSpacing = 25;

        // Title
        gfx.DrawString("NADENA DATA LICENSE AGREEMENT", fontTitle, XBrushes.DarkBlue,
            new XRect(marginX, y, page.Width - 2 * marginX, 30), XStringFormats.TopCenter);
        y += 40;

        // Effective date
        gfx.DrawString($"Effective Date: {purchaseDate:MMMM dd, yyyy}", fontBody, XBrushes.Black,
            new XRect(marginX, y, page.Width - 2 * marginX, lineHeight), XStringFormats.TopLeft);
        y += lineHeight + sectionSpacing;

        // Parties section
        gfx.DrawString("1. PARTIES", fontHeading, XBrushes.DarkBlue, marginX, y);
        y += lineHeight;
        gfx.DrawString($"Licensee: {buyerName}", fontBody, XBrushes.Black, marginX, y);
        y += lineHeight;
        if (!string.IsNullOrWhiteSpace(companyName))
        {
            gfx.DrawString($"Company: {companyName}", fontBody, XBrushes.Black, marginX, y);
            y += lineHeight;
        }
        gfx.DrawString($"Dataset ID: {datasetId}", fontBody, XBrushes.Black, marginX, y);
        y += lineHeight;
        gfx.DrawString("Licensor: Nadena Platform (nadena.com)", fontBody, XBrushes.Black, marginX, y);
        y += lineHeight + sectionSpacing;

        // Grant of license
        gfx.DrawString("2. GRANT OF LICENSE", fontHeading, XBrushes.DarkBlue, marginX, y);
        y += lineHeight;
        gfx.DrawString("Subject to the terms of this Agreement, Nadena grants the Licensee a", fontBody, XBrushes.Black, marginX, y);
        y += lineHeight;
        gfx.DrawString("non-exclusive, non-transferable license to use the dataset for the", fontBody, XBrushes.Black, marginX, y);
        y += lineHeight;
        gfx.DrawString("permitted purposes described herein.", fontBody, XBrushes.Black, marginX, y);
        y += lineHeight + sectionSpacing;

        // Permitted uses
        gfx.DrawString("3. PERMITTED USES", fontHeading, XBrushes.DarkBlue, marginX, y);
        y += lineHeight;
        gfx.DrawString("- Academic and commercial research", fontBody, XBrushes.Black, marginX, y);
        y += lineHeight;
        gfx.DrawString("- AI/ML model training", fontBody, XBrushes.Black, marginX, y);
        y += lineHeight;
        gfx.DrawString("- Internal business analytics", fontBody, XBrushes.Black, marginX, y);
        y += lineHeight + sectionSpacing;

        // Prohibited uses
        gfx.DrawString("4. PROHIBITED USES", fontHeading, XBrushes.DarkBlue, marginX, y);
        y += lineHeight;
        gfx.DrawString("- Reselling or redistributing the dataset in any form", fontBody, XBrushes.Black, marginX, y);
        y += lineHeight;
        gfx.DrawString("- Attempting to re-identify anonymized individuals", fontBody, XBrushes.Black, marginX, y);
        y += lineHeight;
        gfx.DrawString("- Sublicensing or transferring rights to third parties", fontBody, XBrushes.Black, marginX, y);
        y += lineHeight + sectionSpacing;

        // Attribution
        gfx.DrawString("5. ATTRIBUTION", fontHeading, XBrushes.DarkBlue, marginX, y);
        y += lineHeight;
        gfx.DrawString("Licensee must cite Nadena as the source of the data in any public", fontBody, XBrushes.Black, marginX, y);
        y += lineHeight;
        gfx.DrawString("disclosure, publication, or product that utilizes this dataset.", fontBody, XBrushes.Black, marginX, y);
        y += lineHeight + sectionSpacing;

        // License term
        gfx.DrawString("6. LICENSE TERM", fontHeading, XBrushes.DarkBlue, marginX, y);
        y += lineHeight;
        var expiryDate = purchaseDate.AddYears(2);
        gfx.DrawString($"This license is valid for two (2) years from the effective date,", fontBody, XBrushes.Black, marginX, y);
        y += lineHeight;
        gfx.DrawString($"expiring on {expiryDate:MMMM dd, yyyy}.", fontBody, XBrushes.Black, marginX, y);
        y += lineHeight + sectionSpacing;

        // Consent verification
        gfx.DrawString("7. CONSENT VERIFICATION", fontHeading, XBrushes.DarkBlue, marginX, y);
        y += lineHeight;
        gfx.DrawString("All data in this dataset was contributed voluntarily by individuals", fontBody, XBrushes.Black, marginX, y);
        y += lineHeight;
        gfx.DrawString("who explicitly consented to their anonymized data being sold.", fontBody, XBrushes.Black, marginX, y);
        y += lineHeight;
        gfx.DrawString("Consent records are available upon request.", fontBody, XBrushes.Black, marginX, y);
        y += lineHeight + sectionSpacing;

        // Footer
        gfx.DrawString("Generated by Nadena Platform", fontSmall, XBrushes.Gray,
            new XRect(marginX, page.Height - 40, page.Width - 2 * marginX, 20), XStringFormats.TopCenter);

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }
}
