using Application.Interfaces;
using Application.Wrappers;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Payments.Commands.ProcessDatasetSalePayments;

public class ProcessDatasetSalePaymentsCommandHandler : IRequestHandler<ProcessDatasetSalePaymentsCommand, ServiceResponse<bool>>
{
    private readonly IConfiguration _configuration;
    private readonly IRepositoryAsync<Volunteer> _volunteerRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IRepositoryAsync<LedgerTransaction> _transactionRepository;

    public ProcessDatasetSalePaymentsCommandHandler(
        IConfiguration configuration,
        IRepositoryAsync<Volunteer> volunteerRepository,
        IWalletRepository walletRepository,
        IRepositoryAsync<LedgerTransaction> transactionRepository)
    {
        _configuration = configuration;
        _volunteerRepository = volunteerRepository;
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<ServiceResponse<bool>> Handle(ProcessDatasetSalePaymentsCommand request, CancellationToken cancellationToken)
    {
        var settings = _configuration.GetSection("NadenaSettings");
        var contributorSharePercent = settings.GetValue<decimal>("ContributorSharePercent", 0m);
        var modeSharePercent = settings.GetValue<decimal>("ModeSharePercent", 0m);
        var nadenaSharePercent = settings.GetValue<decimal>("NadenaSharePercent", 0m);

        var volunteers = await _volunteerRepository.ListAsync(cancellationToken);
        var donors = volunteers.Where(v => v.HasDonated).ToList();

        if (!donors.Any())
        {
            return new ServiceResponse<bool>(true, "No contributors to distribute funds to.");
        }

        var totalContributorPool = request.SaleAmount * (contributorSharePercent / 100m);
        var perContributorAmount = totalContributorPool / donors.Count;

        foreach (var volunteer in donors)
        {
            var wallet = await _walletRepository.GetByOwnerAsync(volunteer.UserId);
            
            if (wallet == null)
            {
                wallet = new Wallet
                {
                    OwnerType = "User",
                    OwnerId = volunteer.UserId,
                    Balance = 0,
                    PendingBalance = perContributorAmount
                };
                await _walletRepository.AddAsync(wallet);
            }
            else
            {
                wallet.PendingBalance += perContributorAmount;
                await _walletRepository.UpdateAsync(wallet);
            }

            var tx = new LedgerTransaction
            {
                FromWalletId = Guid.Empty,
                ToWalletId = wallet.Id,
                Amount = perContributorAmount,
                Type = "ContributorCredit",
                Status = "Pending",
                ReferenceId = request.DatasetPurchaseId.ToString(),
                ReferenceType = "DatasetPurchase"
            };
            await _transactionRepository.AddAsync(tx);
        }

        var modeFeeTx = new LedgerTransaction
        {
            FromWalletId = Guid.Empty,
            ToWalletId = Guid.Empty,
            Amount = request.SaleAmount * (modeSharePercent / 100m),
            Type = "ModeFee",
            Status = "Pending",
            ReferenceId = request.DatasetPurchaseId.ToString(),
            ReferenceType = "DatasetPurchase"
        };
        await _transactionRepository.AddAsync(modeFeeTx);

        var platformRevenueTx = new LedgerTransaction
        {
            FromWalletId = Guid.Empty,
            ToWalletId = Guid.Empty,
            Amount = request.SaleAmount * (nadenaSharePercent / 100m),
            Type = "PlatformRevenue",
            Status = "Completed",
            ReferenceId = request.DatasetPurchaseId.ToString(),
            ReferenceType = "DatasetPurchase"
        };
        await _transactionRepository.AddAsync(platformRevenueTx);

        return new ServiceResponse<bool>(true, $"Successfully distributed payments to {donors.Count} contributors.");
    }
}
