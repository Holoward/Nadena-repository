using Domain.Common;

namespace Domain.Entities;

public class ConsentRecord : AuditableBaseEntity
{
    public string UserId { get; set; }
    public DateTime AgreedAt { get; set; }
    public string IpAddress { get; set; }
    public string ConsentText { get; set; }
    public string DocumentType { get; set; } = string.Empty; // TermsOfService | DataConsent
    public string FormVersion { get; set; } = "v1.0";
}
