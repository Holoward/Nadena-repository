using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace Application.Features.Admin.Commands.PayVolunteers;

public class PayVolunteersCommandHandler : IRequestHandler<PayVolunteersCommand, ServiceResponse<PayVolunteersResult>>
{
    private readonly IVolunteerRepository _volunteerRepository;
    private readonly IVolunteerPaymentRepository _volunteerPaymentRepository;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly IAuditLogService _auditLogService;
    private readonly IEmailService _emailService;

    public PayVolunteersCommandHandler(
        IVolunteerRepository volunteerRepository,
        IVolunteerPaymentRepository volunteerPaymentRepository,
        IConfiguration configuration,
        HttpClient httpClient,
        IAuditLogService auditLogService,
        IEmailService emailService)
    {
        _volunteerRepository = volunteerRepository;
        _volunteerPaymentRepository = volunteerPaymentRepository;
        _configuration = configuration;
        _httpClient = httpClient;
        _auditLogService = auditLogService;
        _emailService = emailService;
    }

    public async Task<ServiceResponse<PayVolunteersResult>> Handle(PayVolunteersCommand request, CancellationToken cancellationToken)
    {
        // Validate input
        if (request.VolunteerIds == null || request.VolunteerIds.Count == 0)
        {
            return new ServiceResponse<PayVolunteersResult>("No volunteers provided for payment");
        }

        if (request.TotalRevenue <= 0)
        {
            return new ServiceResponse<PayVolunteersResult>("Total revenue must be greater than zero");
        }

        // Step 1: Get PlatformFeePercent from NadenaSettings
        var platformFeePercent = _configuration.GetSection("NadenaSettings:PlatformFeePercent").Value;
        if (string.IsNullOrEmpty(platformFeePercent))
        {
            return new ServiceResponse<PayVolunteersResult>("PlatformFeePercent not configured");
        }

        if (!decimal.TryParse(platformFeePercent, out var platformFeePercentDecimal))
        {
            return new ServiceResponse<PayVolunteersResult>("Invalid PlatformFeePercent value in configuration");
        }

        // Step 2: Calculate platform fee
        var platformFee = request.TotalRevenue * (platformFeePercentDecimal / 100);

        // Step 3: Calculate distributable amount
        var distributableAmount = request.TotalRevenue - platformFee;

        // Step 4: Divide equally among all VolunteerIds
        var perVolunteerAmount = distributableAmount / request.VolunteerIds.Count;

        // Step 5: Fetch each volunteer's PayPal email
        var volunteers = await _volunteerRepository.GetVolunteersByIds(request.VolunteerIds);
        
        if (volunteers.Count != request.VolunteerIds.Count)
        {
            return new ServiceResponse<PayVolunteersResult>("One or more volunteers not found");
        }

        foreach (var volunteer in volunteers)
        {
            if (string.IsNullOrEmpty(volunteer.PayPalEmail))
            {
                return new ServiceResponse<PayVolunteersResult>($"Volunteer {volunteer.Id} does not have a PayPal email address");
            }
        }

        // Step 6: Call PayPal Payouts API
        var payPalBatchId = await CreatePayPalPayoutsAsync(volunteers, perVolunteerAmount, cancellationToken);

        if (string.IsNullOrEmpty(payPalBatchId))
        {
            return new ServiceResponse<PayVolunteersResult>("Failed to create PayPal payout batch");
        }

        // Step 7 & 8: Create VolunteerPayment records and update volunteer status
        var paymentSummaries = new List<VolunteerPaymentSummary>();

        foreach (var volunteer in volunteers)
        {
            // Create VolunteerPayment record
            var volunteerPayment = new VolunteerPayment
            {
                Id = Guid.NewGuid(),
                VolunteerId = volunteer.Id,
                DatasetId = request.DatasetId,
                GrossAmount = perVolunteerAmount,
                PlatformFee = platformFee / request.VolunteerIds.Count,
                NetAmount = perVolunteerAmount,
                PayPalBatchId = payPalBatchId,
                Status = "Pending",
                Created = DateTime.UtcNow,
                CreatedBy = "System"
            };

            await _volunteerPaymentRepository.AddPaymentRecord(volunteerPayment);

            // Step 9: Update volunteer's Status to Paid
            volunteer.Status = VolunteerStatus.Paid;
            volunteer.PaymentSent = true;
            await _volunteerRepository.UpdateAsync(volunteer);

            // Send payment confirmation email
            var userInfo = await _volunteerRepository.GetUserInfoByVolunteerIdAsync(volunteer.Id);
            if (userInfo.HasValue)
            {
                await _emailService.SendPaymentConfirmationEmailAsync(userInfo.Value.Email, userInfo.Value.FullName, perVolunteerAmount);
            }

            paymentSummaries.Add(new VolunteerPaymentSummary
            {
                VolunteerId = Guid.Parse(volunteer.UserId),
                PayPalEmail = volunteer.PayPalEmail ?? string.Empty,
                Amount = perVolunteerAmount,
                Status = "Pending"
            });
        }

        // Step 10: Return summary
        var result = new PayVolunteersResult
        {
            TotalPaidOut = distributableAmount,
            PlatformFee = platformFee,
            PerVolunteerAmount = perVolunteerAmount,
            PayPalBatchId = payPalBatchId,
            VolunteersPaid = volunteers.Count,
            Payments = paymentSummaries
        };

        // Audit logging for payments sent
        await _auditLogService.LogAsync(
            action: "PaymentsSent",
            entityType: "VolunteerPayment",
            entityId: payPalBatchId,
            success: true,
            newValues: "{\"TotalAmount\":" + distributableAmount + ",\"VolunteerCount\":" + volunteers.Count + ",\"PerVolunteerAmount\":" + perVolunteerAmount + "}");

        return new ServiceResponse<PayVolunteersResult>(result, "Volunteers paid successfully");
    }

    private async Task<string> CreatePayPalPayoutsAsync(List<Volunteer> volunteers, decimal amount, CancellationToken cancellationToken)
    {
        var payPalMode = _configuration.GetSection("NadenaSettings:PayPalMode").Value ?? "sandbox";
        var payPalClientId = _configuration.GetSection("NadenaSettings:PayPalClientId").Value;
        var payPalClientSecret = _configuration.GetSection("NadenaSettings:PayPalClientSecret").Value;

        if (string.IsNullOrEmpty(payPalClientId) || string.IsNullOrEmpty(payPalClientSecret))
        {
            // Return empty string instead of throwing to handle gracefully
            return string.Empty;
        }

        // Get PayPal access token
        var accessToken = await GetPayPalAccessTokenAsync(payPalClientId, payPalClientSecret, payPalMode, cancellationToken);

        if (string.IsNullOrEmpty(accessToken))
        {
            return string.Empty;
        }

        // Build PayPal API URL
        var baseUrl = payPalMode.ToLower() == "live"
            ? "https://api-m.paypal.com"
            : "https://api-m.sandbox.paypal.com";

        // Create payout request
        var payoutItems = volunteers.Select(v => new
        {
            recipient_type = "EMAIL",
            amount = new
            {
                value = amount.ToString("F2"),
                currency = "USD"
            },
            receiver = v.PayPalEmail,
            sender_item_id = v.Id.ToString()
        }).ToList();

        var payoutRequest = new
        {
            sender_batch_header = new
            {
                sender_batch_id = Guid.NewGuid().ToString(),
                email_subject = "You have received a payment from Nadena",
                email_message = "Thank you for your contribution! You have received a payment for your work."
            },
            items = payoutItems
        };

        var jsonContent = JsonSerializer.Serialize(payoutRequest);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Set up HTTP request
        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/payments/payouts")
        {
            Content = httpContent
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        // Send request
        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"PayPal API error: {response.StatusCode} - {responseContent}");
        }

        // Parse response to get batch ID
        using var doc = JsonDocument.Parse(responseContent);
        if (doc.RootElement.TryGetProperty("batch_header", out var batchHeader))
        {
            if (batchHeader.TryGetProperty("payout_batch_id", out var batchId))
            {
                return batchId.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private async Task<string> GetPayPalAccessTokenAsync(string clientId, string clientSecret, string mode, CancellationToken cancellationToken)
    {
        var baseUrl = mode.ToLower() == "live"
            ? "https://api-m.paypal.com"
            : "https://api-m.sandbox.paypal.com";

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/oauth2/token");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to get PayPal access token: {response.StatusCode} - {responseContent}");
        }

        using var doc = JsonDocument.Parse(responseContent);
        if (doc.RootElement.TryGetProperty("access_token", out var accessToken))
        {
            return accessToken.GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}
