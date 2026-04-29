using Application.Wrappers;
using MediatR;
using System;

namespace Application.Features.Payments.Commands.ProcessDatasetSalePayments;

public class ProcessDatasetSalePaymentsCommand : IRequest<ServiceResponse<bool>>
{
    public Guid DatasetPurchaseId { get; set; }
    public Guid DatasetId { get; set; }
    public decimal SaleAmount { get; set; }
}
