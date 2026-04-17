namespace Application.Interfaces;

/// <summary>
/// Service for generating PDF license agreements for dataset purchases
/// </summary>
public interface ILicensePdfService
{
    /// <summary>
    /// Generates a PDF license agreement document as an in-memory byte array
    /// </summary>
    byte[] GenerateLicensePdf(string buyerName, string companyName, int datasetId, DateTime purchaseDate);
}
