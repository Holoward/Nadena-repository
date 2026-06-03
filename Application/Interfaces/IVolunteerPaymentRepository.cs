using Domain.Entities;
using System.Threading.Tasks;

namespace Application.Interfaces;

public interface IVolunteerPaymentRepository
{
    Task<VolunteerPayment> AddPaymentRecord(VolunteerPayment payment);
}
