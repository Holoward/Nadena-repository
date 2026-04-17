using Application.Wrappers;
using MediatR;

namespace Application.Features.Admin.Commands.PayVolunteers;

public class PayVolunteersCommand : IRequest<ServiceResponse<PayVolunteersResult>>
{
    public List<int> VolunteerIds { get; set; } = new();
    public int DatasetId { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class PayVolunteersResult
{
    public decimal TotalPaidOut { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal PerVolunteerAmount { get; set; }
    public string PayPalBatchId { get; set; } = string.Empty;
    public int VolunteersPaid { get; set; }
    public List<VolunteerPaymentSummary> Payments { get; set; } = new();
}

public class VolunteerPaymentSummary
{
    public Guid VolunteerId { get; set; }
    public string PayPalEmail { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}
