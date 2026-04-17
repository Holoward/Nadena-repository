using Application.Interfaces;
using Domain.Entities;
using Persistence.Context;
using System.Threading.Tasks;

namespace Persistence.Repository;

public class VolunteerPaymentRepository : IVolunteerPaymentRepository
{
    private readonly ApplicationDbContext _dbContext;

    public VolunteerPaymentRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<VolunteerPayment> AddPaymentRecord(VolunteerPayment payment)
    {
        await _dbContext.VolunteerPayments.AddAsync(payment);
        await _dbContext.SaveChangesAsync();
        return payment;
    }
}
