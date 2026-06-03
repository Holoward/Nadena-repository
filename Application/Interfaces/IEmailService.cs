namespace Application.Interfaces;

/// <summary>
/// Service for sending email notifications
/// </summary>
public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string fullName);
    Task SendActivationEmailAsync(string toEmail, string fullName, string googleTakeoutInstructions);
    Task SendFileReceivedEmailAsync(string toEmail, string fullName);
    Task SendPaymentConfirmationEmailAsync(string toEmail, string fullName, decimal amount);
    Task SendAdminNotificationAsync(string subject, string body);
    Task SendDownloadConfirmationEmailAsync(string toEmail, string buyerName, int datasetId, byte[] pdfAttachment, string fileName);

    // New events
    Task SendDataPurchasedAsync(string toEmail, string fullName, decimal amount);
    Task SendPayoutProcessedAsync(string toEmail, string fullName, decimal amount);
    Task SendClientPurchaseConfirmedAsync(string toEmail, string datasetName, decimal amount);
    Task SendAdminPlatformFeeAsync(decimal amount);
    Task SendPasswordResetAsync(string toEmail, string link);
    Task SendEmailVerificationAsync(string toEmail, string link);
    Task SendRecurringDatasetRefreshedAsync(string toEmail, string datasetName);
    Task SendAccountDeletionConfirmationAsync(string toEmail, string fullName);
    Task SendCustomQuoteRequestAsync(string requesterEmail, string datasetName, int recordCount);
    Task LogEmailAsync(string toEmail, string subject, string body, string status = "Logged");
}
