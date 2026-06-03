using Application.Wrappers;
using MediatR;
using System;

namespace Application.Features.Payments.Commands.ProcessDatasetSalePayments;

/// <summary>
/// Command and handler for splitting a dataset sale's revenue. Reads split percentages from configuration and credits contributor wallets, logs Mode fee and Nadena revenue.
/// </summary>
public class ProcessDatasetSalePaymentsCommand : IRequest<ServiceResponse<bool>>
{
    public Guid DatasetPurchaseId { get; set; }
    public Guid DatasetId { get; set; }
    public decimal SaleAmount { get; set; }
}
