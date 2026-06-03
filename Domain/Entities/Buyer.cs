using Domain.Common;

namespace Domain.Entities;

public class Buyer : AuditableBaseEntity
{
    public string UserId { get; set; }
    public string CompanyName { get; set; }
    public string UseCase { get; set; }
    public string Website { get; set; }
    public bool CompanyVerified { get; set; }
}
