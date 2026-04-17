using System.IO;
using System.Net;
using System.Net.Mail;
using Application.Interfaces;
using Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Domain.Entities;
using Persistence.Context;

namespace Persistence.Services;

/// <summary>
/// Email service implementation using SMTP
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly ApplicationDbContext _dbContext;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger, ApplicationDbContext dbContext)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string fullName)
    {
        var subject = "Welcome to Nadena!";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4F46E5; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 10px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Nadena</h1>
        </div>
        <div class='content'>
            <h2>Welcome, {fullName}!</h2>
            <p>Thank you for joining Nadena. We're excited to have you as part of our data monetization platform.</p>
            <p>As a data contributor, you can:</p>
            <ul>
                <li>Contribute your data and earn rewards</li>
                <li>Control how your data is used</li>
                <li>Track your earnings and payments</li>
            </ul>
            <p>To get started, please upload your data files in your dashboard.</p>
        </div>
        <div class='footer'>
            <p>This is an automated message from Nadena. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendActivationEmailAsync(string toEmail, string fullName, string googleTakeoutInstructions)
    {
        var subject = "Your Nadena Account is Now Activated!";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4F46E5; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 10px; text-align: center; font-size: 12px; color: #666; }}
        .instructions {{ background-color: #fff; padding: 15px; border-left: 4px solid #4F46E5; margin: 15px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Nadena</h1>
        </div>
        <div class='content'>
            <h2>Account Activated, {fullName}!</h2>
            <p>Great news! Your Nadena account has been activated. You can now start uploading your data.</p>
            <h3>How to Export Your Google Data:</h3>
            <div class='instructions'>
                {googleTakeoutInstructions}
            </div>
            <p>Once you've exported your data, upload the ZIP file to your Nadena dashboard to start earning.</p>
        </div>
        <div class='footer'>
            <p>This is an automated message from Nadena. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendFileReceivedEmailAsync(string toEmail, string fullName)
    {
        var subject = "Your Data File Has Been Received";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4F46E5; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 10px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Nadena</h1>
        </div>
        <div class='content'>
            <h2>File Received, {fullName}!</h2>
            <p>We've received your data file. Thank you for contributing to Nadena!</p>
            <p>Our team will review your data and process it shortly. You can track the status in your dashboard.</p>
        </div>
        <div class='footer'>
            <p>This is an automated message from Nadena. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendPaymentConfirmationEmailAsync(string toEmail, string fullName, decimal amount)
    {
        var subject = "Payment Confirmation from Nadena";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4F46E5; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 10px; text-align: center; font-size: 12px; color: #666; }}
        .amount {{ font-size: 24px; font-weight: bold; color: #4F46E5; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Nadena</h1>
        </div>
        <div class='content'>
            <h2>Payment Processed, {fullName}!</h2>
            <p>We're pleased to confirm that your payment has been processed successfully.</p>
            <p class='amount'>Amount: ${amount:F2}</p>
            <p>This payment has been recorded in your Nadena earnings ledger. External disbursement methods may vary during early access.</p>
            <p>Thank you for being a valued Nadena data contributor!</p>
        </div>
        <div class='footer'>
            <p>This is an automated message from Nadena. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendAdminNotificationAsync(string subject, string body)
    {
        if (string.IsNullOrEmpty(_emailSettings.AdminEmail))
        {
            _logger.LogWarning("Admin email is not configured. Skipping admin notification.");
            return;
        }

        var fullBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #DC2626; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 10px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Nadena - Admin Alert</h1>
        </div>
        <div class='content'>
            {body}
        </div>
        <div class='footer'>
            <p>This is an automated message from Nadena. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(_emailSettings.AdminEmail, subject, fullBody);
    }

    public async Task SendDownloadConfirmationEmailAsync(string toEmail, string buyerName, int datasetId, byte[] pdfAttachment, string fileName)
    {
        var subject = $"Your Nadena Dataset #{datasetId} License Agreement";
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4F46E5; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .footer {{ padding: 10px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Nadena</h1>
        </div>
        <div class='content'>
            <h2>Purchase Confirmed, {buyerName}!</h2>
            <p>Your purchase of Dataset #{datasetId} has been confirmed.</p>
            <p>Please find your license agreement attached to this email as a PDF document.</p>
            <p>The license agreement outlines the permitted and prohibited uses of the data, as well as the 2-year license term.</p>
        </div>
        <div class='footer'>
            <p>This is an automated message from Nadena. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailWithAttachmentAsync(toEmail, subject, body, pdfAttachment, fileName);
    }

    public Task SendDataPurchasedAsync(string toEmail, string fullName, decimal amount)
        => LogOnly(toEmail, "Your data was purchased", $"Hi {fullName}, ${amount:F2} has been added to your earnings.");

    public Task SendPayoutProcessedAsync(string toEmail, string fullName, decimal amount)
        => LogOnly(toEmail, "Your payout was processed", $"Hi {fullName}, your payout of ${amount:F2} has been processed.");

    public Task SendClientPurchaseConfirmedAsync(string toEmail, string datasetName, decimal amount)
        => LogOnly(toEmail, "Purchase confirmed", $"Your purchase of {datasetName} is confirmed. Amount: ${amount:F2}.");

    public Task SendAdminPlatformFeeAsync(decimal amount)
        => LogOnly(_emailSettings.AdminEmail ?? "admin@nadena.com", "Platform fee credited", $"Platform fee credited: ${amount:F2}.");

    public Task SendPasswordResetAsync(string toEmail, string link)
        => LogOnly(toEmail, "Password reset", $"Use this link to reset your password: {link}");

    public Task SendEmailVerificationAsync(string toEmail, string link)
        => LogOnly(toEmail, "Verify your email", $"Click to verify your email: {link}");

    public Task SendRecurringDatasetRefreshedAsync(string toEmail, string datasetName)
        => LogOnly(toEmail, "Recurring dataset refreshed", $"Your recurring dataset {datasetName} has been refreshed and is ready.");

    public Task SendAccountDeletionConfirmationAsync(string toEmail, string fullName)
        => LogOnly(toEmail, "Account deletion scheduled", $"Hi {fullName}, your NADENA account is scheduled for deletion and your data will be retained for 30 days.");

    public Task SendCustomQuoteRequestAsync(string requesterEmail, string datasetName, int recordCount)
        => LogOnly(_emailSettings.AdminEmail ?? "admin@nadena.com", "Custom quote requested", $"Requester: {requesterEmail}\nDataset: {datasetName}\nRequested records: {recordCount}");

    public async Task LogEmailAsync(string toEmail, string subject, string body, string status = "Logged")
    {
        var log = new EmailLog
        {
            Id = Guid.NewGuid(),
            To = toEmail,
            Subject = subject,
            Body = body,
            Status = status,
            SentAt = DateTime.UtcNow
        };
        await _dbContext.EmailLogs.AddAsync(log);
        await _dbContext.SaveChangesAsync();
    }

    private async Task LogOnly(string toEmail, string subject, string body)
    {
        _logger.LogInformation("Console email to {ToEmail}\nSubject: {Subject}\nBody: {Body}", toEmail, subject, body);
        await LogEmailAsync(toEmail, subject, body, "ConsoleLogged");
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            await LogEmailAsync(toEmail, subject, body, "Logged");
            if (string.IsNullOrEmpty(_emailSettings.SmtpUser) || string.IsNullOrEmpty(_emailSettings.SmtpPassword))
            {
                _logger.LogWarning("SMTP credentials not configured. Email not sent to {ToEmail}", toEmail);
                return;
            }

            using var client = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_emailSettings.SmtpUser, _emailSettings.SmtpPassword),
                Timeout = 30000
            };

            var message = new MailMessage
            {
                From = new MailAddress(_emailSettings.SmtpFromEmail, _emailSettings.SmtpFromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {ToEmail} with subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}. Error: {Error}", toEmail, ex.Message);
            await LogEmailAsync(toEmail, subject, body, "Error");
        }
    }

    private async Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, byte[] attachmentBytes, string fileName)
    {
        try
        {
            await LogEmailAsync(toEmail, subject, body, "Logged");
            if (string.IsNullOrEmpty(_emailSettings.SmtpUser) || string.IsNullOrEmpty(_emailSettings.SmtpPassword))
            {
                _logger.LogWarning("SMTP credentials not configured. Email not sent to {ToEmail}", toEmail);
                return;
            }

            using var client = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_emailSettings.SmtpUser, _emailSettings.SmtpPassword),
                Timeout = 30000
            };

            var message = new MailMessage
            {
                From = new MailAddress(_emailSettings.SmtpFromEmail, _emailSettings.SmtpFromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var attachmentStream = new MemoryStream(attachmentBytes);
            var attachment = new Attachment(attachmentStream, fileName, "application/pdf");
            message.Attachments.Add(attachment);

            await client.SendMailAsync(message);
            _logger.LogInformation("Email with attachment sent successfully to {ToEmail} with subject: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with attachment to {ToEmail}. Error: {Error}", toEmail, ex.Message);
            await LogEmailAsync(toEmail, subject, body, "Error");
        }
    }
}
