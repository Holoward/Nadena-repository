using Domain.Common;

namespace Domain.Entities;

public class VolunteerPayment : AuditableBaseEntityGuid
{
    public int VolunteerId { get; set; }
    public int DatasetId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal NetAmount { get; set; }
    public string? PayPalBatchId { get; set; }
    public string? PayPalPayoutItemId { get; set; }
    public string Status { get; set; }
    public DateTime? PaidAt { get; set; }
}
